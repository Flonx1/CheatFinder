using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Script.Serialization;

namespace CheatFinder_RECODE.Scanners
{
    internal class VersionsChecker
    {
        // mainClass ванилы и нормальных загрузчиков (Forge/NeoForge/Fabric/Quilt/OptiFine)
        private static readonly string[] KnownMainClasses =
        {
            "net.minecraft.client.main.Main",
            "net.minecraft.launchwrapper.Launch",
            "net.fabricmc.loader.impl.launch.knot.KnotClient",
            "net.fabricmc.loader.launch.knot.KnotClient",
            "org.quiltmc.loader.impl.launch.knot.KnotClient",
            "cpw.mods.modlauncher.Launcher",
            "cpw.mods.bootstraplauncher.BootstrapLauncher",
            "net.minecraftforge.bootstrap.ForgeBootstrap",
            "net.neoforged.fml.startup.Client"
        };

        // Что нормально лежит в корне клиентского jar
        private static readonly string[] KnownJarRoots =
        {
            "net/minecraft/", "com/mojang/", "assets/", "data/", "META-INF/", "net/optifine/", "optifine/", "notch/", "srg/"
        };

        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private readonly HashSet<string> _checkedLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void check()
        {
            var list = List.List.Instance;
            var nameMatcher = new PatternMatcher();
            nameMatcher.AddRange(list.CheatNames);

            foreach (var gameDir in MinecraftPaths.GameDirs)
            {
                string versionsDir = Path.Combine(gameDir, "versions");
                if (!Directory.Exists(versionsDir)) continue;

                Program._logger.Log($"[SCAN] Checking versions in: {versionsDir}");

                string[] versionDirs;
                try { versionDirs = Directory.GetDirectories(versionsDir); }
                catch { continue; }

                foreach (var versionDir in versionDirs)
                {
                    try
                    {
                        CheckVersion(gameDir, versionDir, nameMatcher);
                    }
                    catch (Exception ex)
                    {
                        Program._logger.Log($"[ERROR] {versionDir}: {ex.Message}");
                    }
                }
            }
        }

        private void CheckVersion(string gameDir, string versionDir, PatternMatcher nameMatcher)
        {
            string name = Path.GetFileName(versionDir);
            string jsonPath = Path.Combine(versionDir, name + ".json");
            string jarPath = Path.Combine(versionDir, name + ".jar");

            foreach (var hit in nameMatcher.Search(name).Keys)
                Program._logger.Log($"[FIND] Suspicious version name: {versionDir} (matched: {nameMatcher.GetName(hit)})");

            if (!File.Exists(jsonPath))
            {
                if (File.Exists(jarPath))
                    Program._logger.Log($"[50/50 DETECT] Version jar without json: {jarPath}");
                return;
            }

            var root = _json.DeserializeObject(File.ReadAllText(jsonPath)) as Dictionary<string, object>;
            if (root == null) return;

            string mainClass = Get(root, "mainClass") as string;
            if (mainClass != null && !KnownMainClasses.Contains(mainClass))
                Program._logger.Log($"[50/50 DETECT] Unknown mainClass '{mainClass}' in {jsonPath}");

            string expectedSha1 = Get(root, "downloads", "client", "sha1") as string;
            bool hasJar = File.Exists(jarPath);

            if (hasJar && expectedSha1 != null)
            {
                string actual = ModsChecker.Sha1(jarPath);
                if (!string.Equals(actual, expectedSha1, StringComparison.OrdinalIgnoreCase))
                    Program._logger.Log($"[Detect] Modified client jar (sha1 {actual} != {expectedSha1}): {jarPath}");
            }

            if (hasJar && (expectedSha1 == null || Get(root, "inheritsFrom") == null))
                CheckJarLayout(jarPath, expectedSha1 == null);

            CheckLibraries(gameDir, root, jsonPath);
        }

        // Чужие пакеты в клиентском jar = в него что-то вшили
        private void CheckJarLayout(string jarPath, bool customVersion)
        {
            try
            {
                using (var fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    var unknownRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    bool deobfuscated = false;

                    foreach (var entry in archive.Entries)
                    {
                        string entryName = entry.FullName;
                        if (entryName == "net/minecraft/client/Minecraft.class" || entryName == "net/minecraft/client/MinecraftClient.class")
                            deobfuscated = true;

                        int slash = entryName.IndexOf('/');
                        if (slash < 0) continue; // файлы в корне (обфусцированные классы, pack.png, version.json)
                        if (KnownJarRoots.Any(r => entryName.StartsWith(r, StringComparison.OrdinalIgnoreCase))) continue;

                        int second = entryName.IndexOf('/', slash + 1);
                        unknownRoots.Add(second > 0 ? entryName.Substring(0, second) : entryName.Substring(0, slash));
                    }

                    foreach (var pkg in unknownRoots.Take(20))
                        Program._logger.Log($"[50/50 DETECT] Non-Minecraft package '{pkg}' inside client jar: {jarPath}");

                    // Ванильные 1.16.5 и 1.21.4 обфусцированы — деобфусцированный Minecraft.class значит самосборный клиент
                    if (deobfuscated && customVersion)
                        Program._logger.Log($"[50/50 DETECT] Deobfuscated (MCP/Yarn) client jar, custom build: {jarPath}");
                }
            }
            catch (InvalidDataException)
            {
                Program._logger.Log($"[50/50 DETECT] Broken/encrypted client jar: {jarPath}");
            }
        }

        // У каждой библиотеки в json есть sha1 — подменённая библиотека тоже способ загрузить чит
        private void CheckLibraries(string gameDir, Dictionary<string, object> root, string jsonPath)
        {
            var libraries = Get(root, "libraries") as object[];
            if (libraries == null) return;

            string librariesDir = Path.Combine(gameDir, "libraries");
            foreach (var lib in libraries.OfType<Dictionary<string, object>>())
            {
                string path = Get(lib, "downloads", "artifact", "path") as string;
                string sha1 = Get(lib, "downloads", "artifact", "sha1") as string;
                if (path == null || sha1 == null) continue;

                string file = Path.Combine(librariesDir, path.Replace('/', Path.DirectorySeparatorChar));
                // одна и та же библиотека встречается в десятках версий — хэшируем один раз
                if (!_checkedLibraries.Add(file) || !File.Exists(file)) continue;

                try
                {
                    string actual = ModsChecker.Sha1(file);
                    if (!string.Equals(actual, sha1, StringComparison.OrdinalIgnoreCase))
                        Program._logger.Log($"[50/50 DETECT] Library sha1 mismatch ({Path.GetFileName(jsonPath)}): {file}");
                }
                catch { }
            }
        }

        private static object Get(Dictionary<string, object> obj, params string[] keys)
        {
            object current = obj;
            foreach (var key in keys)
            {
                var dict = current as Dictionary<string, object>;
                if (dict == null || !dict.TryGetValue(key, out current)) return null;
            }
            return current;
        }
    }
}
