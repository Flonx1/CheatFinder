using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CheatFinder_RECODE.Scanners
{
    internal class ModsChecker
    {
        private const long MaxEntrySize = 8 * 1024 * 1024;
        private const int MaxNestingDepth = 3;

        private static readonly string[] ContentExtensions = { ".class", ".json", ".cfg", ".txt", ".properties", ".xml", ".mcmeta", ".toml" };
        private static readonly string[] NativeExtensions = { ".dll", ".exe", ".so", ".dylib", ".sys" };

        private class JarResult
        {
            public string Path;
            public string Sha1;
            public string ModId;
            public List<string> Lines = new List<string>();
        }

        public void check()
        {
            var list = List.List.Instance;

            var nameMatcher = new PatternMatcher();
            nameMatcher.AddRange(list.Signatures);
            nameMatcher.AddRange(list.CheatNames);

            var contentMatcher = new PatternMatcher();
            contentMatcher.AddRange(list.Signatures);
            contentMatcher.AddRange(list.StrongSignatures, ignoreCase: false);

            var jars = new List<string>();
            foreach (var gameDir in MinecraftPaths.GameDirs)
            {
                string modPath = Path.Combine(gameDir, "mods");
                if (!Directory.Exists(modPath)) continue;

                Program._logger.Log($"[SCAN] Checking mods in: {modPath}");
                try
                {
                    jars.AddRange(Directory.GetFiles(modPath, "*.jar", SearchOption.AllDirectories));
                    // .jar.disabled и переименованные архивы тоже открываем — zip остаётся zip'ом
                    jars.AddRange(Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) && IsZip(f)));
                }
                catch { }
            }

            if (jars.Count == 0)
            {
                Program._logger.Log("[INFO] No mods found.");
                return;
            }

            var results = new JarResult[jars.Count];
            Parallel.For(0, jars.Count, i => results[i] = CheckJar(jars[i], nameMatcher, contentMatcher));

            foreach (var result in results)
                foreach (var line in result.Lines)
                    Program._logger.Log(line);

            CheckModrinth(results);
            Program._logger.Log($"[INFO] Mods checked: {jars.Count}");
        }

        private JarResult CheckJar(string jarPath, PatternMatcher nameMatcher, PatternMatcher contentMatcher)
        {
            var result = new JarResult { Path = jarPath };
            try
            {
                result.Sha1 = Sha1(jarPath);

                string jarName = Path.GetFileName(jarPath);
                foreach (var hit in nameMatcher.Search(jarName).Keys)
                    result.Lines.Add($"[FIND] Suspicious mod name: {jarPath} (matched: {nameMatcher.GetName(hit)})");

                if (!jarPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                    result.Lines.Add($"[50/50 DETECT] Archive with non-jar extension in mods folder: {jarPath}");

                // FileShare.ReadWrite — запущенная игра держит jar открытым
                using (var fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    result.ModId = ReadModId(archive);
                    if (result.ModId != null)
                    {
                        foreach (var hit in nameMatcher.Search(result.ModId).Keys)
                            result.Lines.Add($"[FIND] Suspicious mod id '{result.ModId}': {jarPath} (matched: {nameMatcher.GetName(hit)})");
                    }

                    ScanArchive(archive, jarPath, contentMatcher, nameMatcher, result.Lines, 0);
                }
            }
            catch (InvalidDataException)
            {
                result.Lines.Add($"[50/50 DETECT] Broken/encrypted jar: {jarPath}");
            }
            catch { }
            return result;
        }

        private void ScanArchive(ZipArchive archive, string displayPath, PatternMatcher contentMatcher, PatternMatcher nameMatcher, List<string> lines, int depth)
        {
            var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in archive.Entries)
            {
                string entryName = entry.FullName;

                foreach (var hit in nameMatcher.Search(entryName).Keys)
                {
                    string sig = nameMatcher.GetName(hit);
                    if (reported.Add("path:" + sig))
                        lines.Add($"[FIND] Suspicious class path: {displayPath} -> {entryName} (matched: {sig})");
                }

                if (NativeExtensions.Any(e => entryName.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    lines.Add($"[50/50 DETECT] Native binary inside mod: {displayPath} -> {entryName}");

                if (entry.Length > MaxEntrySize) continue;

                // Fabric/Quilt jar-in-jar: META-INF/jars/*.jar
                if (entryName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) && depth < MaxNestingDepth)
                {
                    try
                    {
                        using (var ms = new MemoryStream(ReadEntry(entry)))
                        using (var nested = new ZipArchive(ms, ZipArchiveMode.Read))
                        {
                            ScanArchive(nested, displayPath + " -> " + entryName, contentMatcher, nameMatcher, lines, depth + 1);
                        }
                    }
                    catch { }
                    continue;
                }

                if (!ContentExtensions.Any(e => entryName.EndsWith(e, StringComparison.OrdinalIgnoreCase))) continue;

                try
                {
                    byte[] buffer = ReadEntry(entry);
                    foreach (var pair in contentMatcher.Search(buffer))
                    {
                        string sig = contentMatcher.GetName(pair.Key);
                        if (!reported.Add("content:" + sig)) continue;
                        lines.Add($"[FIND] Cheat string in mod: {displayPath} -> {entryName} (matched: {sig})");
                        lines.Add($"[CONTENT] {PatternMatcher.Context(buffer, buffer.Length, pair.Value)}");
                    }
                }
                catch { }
            }
        }

        private static byte[] ReadEntry(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var ms = new MemoryStream((int)Math.Max(0, Math.Min(entry.Length, MaxEntrySize))))
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        // id мода из fabric.mod.json / quilt.mod.json / mods.toml / mcmod.info
        private static string ReadModId(ZipArchive archive)
        {
            try
            {
                foreach (var file in new[] { "fabric.mod.json", "quilt.mod.json", "META-INF/mods.toml", "META-INF/neoforge.mods.toml", "mcmod.info" })
                {
                    var entry = archive.GetEntry(file);
                    if (entry == null) continue;

                    string text = Encoding.UTF8.GetString(ReadEntry(entry));
                    var match = file.EndsWith(".toml")
                        ? Regex.Match(text, "modId\\s*=\\s*\"([^\"]+)\"")
                        : Regex.Match(text, "\"(?:id|modid)\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
            }
            catch { }
            return null;
        }

        // Моды, которых нет на Modrinth, не обязательно читы (могут быть с CurseForge), но их стоит открыть руками
        private void CheckModrinth(JarResult[] results)
        {
            var hashes = results.Where(r => r.Sha1 != null).Select(r => r.Sha1).Distinct().ToList();
            if (hashes.Count == 0) return;

            try
            {
                var serializer = new JavaScriptSerializer();
                string body = serializer.Serialize(new Dictionary<string, object> { { "hashes", hashes }, { "algorithm", "sha1" } });

                string response;
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.Headers[HttpRequestHeader.UserAgent] = "Flonx1/CheatFinder";
                    response = client.UploadString("https://api.modrinth.com/v2/version_files", body);
                }

                var known = serializer.Deserialize<Dictionary<string, object>>(response);
                foreach (var result in results.Where(r => r.Sha1 != null && !known.ContainsKey(r.Sha1)))
                {
                    string id = result.ModId != null ? $" (mod id: {result.ModId})" : "";
                    Program._logger.Log($"[50/50 DETECT] Mod not found on Modrinth{id}: {result.Path}");
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] Modrinth check failed: {ex.Message}");
            }
        }

        private static bool IsZip(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return fs.Length > 4 && fs.ReadByte() == 'P' && fs.ReadByte() == 'K';
                }
            }
            catch
            {
                return false;
            }
        }

        public static string Sha1(string path)
        {
            using (var sha = SHA1.Create())
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
