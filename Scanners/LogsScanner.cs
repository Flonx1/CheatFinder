using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CheatFinder_RECODE.Scanners
{
    internal class LogsScanner
    {
        private const int MaxHitsPerFile = 10;

        public void scan()
        {
            var list = List.List.Instance;
            var matcher = new PatternMatcher();
            matcher.AddRange(list.CheatNames);
            matcher.AddRange(list.StrongSignatures, ignoreCase: false);

            DateTime since = Program.mode == "full" ? DateTime.MinValue : DateTime.Now.AddDays(-7);
            int scanned = 0;

            foreach (var gameDir in MinecraftPaths.GameDirs)
            {
                foreach (var folder in new[] { "logs", "crash-reports" })
                {
                    string dir = Path.Combine(gameDir, folder);
                    if (!Directory.Exists(dir)) continue;

                    IEnumerable<FileInfo> files;
                    try
                    {
                        files = new DirectoryInfo(dir).GetFiles()
                            .Where(f => f.LastWriteTime >= since)
                            .Where(f => f.Name.EndsWith(".log") || f.Name.EndsWith(".log.gz") || f.Name.EndsWith(".txt"))
                            .ToList();
                    }
                    catch { continue; }

                    foreach (var file in files)
                    {
                        ScanFile(file.FullName, matcher);
                        scanned++;
                    }
                }
            }

            Program._logger.Log($"[INFO] Log files scanned: {scanned}");
        }

        private void ScanFile(string path, PatternMatcher matcher)
        {
            int hits = 0;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (Stream stream = path.EndsWith(".gz") ? (Stream)new GZipStream(fs, CompressionMode.Decompress) : fs)
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    var reported = new HashSet<int>();
                    while ((line = reader.ReadLine()) != null && hits < MaxHitsPerFile)
                    {
                        // Чат пишут другие игроки — там названия читов ничего не доказывают
                        if (line.IndexOf("[CHAT]", StringComparison.Ordinal) >= 0) continue;

                        foreach (var id in matcher.Search(line).Keys)
                        {
                            if (!reported.Add(id)) continue;
                            Program._logger.Log($"[FIND] Cheat trace in log: {path} (matched: {matcher.GetName(id)})");
                            Program._logger.Log($"[LOG] {(line.Length > 300 ? line.Substring(0, 300) : line).Trim()}");
                            hits++;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
