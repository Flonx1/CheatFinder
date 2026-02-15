using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace CheatFinder_RECODE.Scanners
{
    internal class ModsChecker
    {
        public void check()
        {
            var list = new List.List();
            var signatures = list.Signatures;

            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string[] modPaths = new string[]
            {
                Path.Combine(appdata, ".minecraft", "mods"),
                Path.Combine(appdata, ".tlauncher", "legacy", "Minecraft", "game", "mods"),
                Path.Combine(appdata, ".lunarclient", "offline", "multiver", "mods"),
                Path.Combine(appdata, ".feather", "mods")
            };

            foreach (var modPath in modPaths)
            {
                if (!Directory.Exists(modPath))
                {
                    Program._logger.Log($"[INFO] Path not found: {modPath}");
                    continue;
                }

                Program._logger.Log($"[SCAN] Checking mods in: {modPath}");

                string[] jars;
                try
                {
                    jars = Directory.GetFiles(modPath, "*.jar", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var jar in jars)
                {
                    string jarName = Path.GetFileName(jar);

                    

                    foreach (var sig in signatures)
                    {
                        if (string.IsNullOrEmpty(sig)) continue;
                        if (jarName.IndexOf(sig, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[FIND] Suspicious mod name: {jar} (matched: {sig})");
                        }
                    }

                    ScanJarContents(jar, signatures);
                }
            }
        }

        private void ScanJarContents(string jarPath, string[] signatures)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(jarPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string entryName = entry.FullName;

                       

                        foreach (var sig in signatures)
                        {
                            if (string.IsNullOrEmpty(sig)) continue;
                            if (entryName.IndexOf(sig, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Program._logger.Log($"[FIND] Suspicious class path: {jarPath} -> {entryName} (matched: {sig})");
                            }
                        }

                        if (entry.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".properties", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.EndsWith(".mcmeta", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using (var stream = entry.Open())
                                {
                                    byte[] buffer;
                                    using (var ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        buffer = ms.ToArray();
                                    }

                                    var strings = ExtractStrings(buffer, 4);

                                    foreach (var str in strings)
                                    {
                                        

                                        foreach (var sig in signatures)
                                        {
                                            if (string.IsNullOrEmpty(sig)) continue;
                                            if (str.IndexOf(sig, StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                Program._logger.Log($"[FIND] Cheat string in mod: {jarPath} -> {entryName} (matched: {sig})");
                                                Program._logger.Log($"[CONTENT] {str}");
                                                Console.WriteLine();
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private System.Collections.Generic.List<string> ExtractStrings(byte[] buffer, int minLength)
        {
            var result = new System.Collections.Generic.List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] >= 0x20 && buffer[i] <= 0x7E)
                {
                    sb.Append((char)buffer[i]);
                }
                else
                {
                    if (sb.Length >= minLength)
                    {
                        result.Add(sb.ToString());
                    }
                    sb.Clear();
                }
            }

            if (sb.Length >= minLength)
            {
                result.Add(sb.ToString());
            }

            return result;
        }
    }
}