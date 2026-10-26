using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheatFinder_RECODE.Scanners
{
    internal class DirectoryScanner
    {
        private static readonly string[] SkipDirs =
        {
            "node_modules", "$recycle.bin", "windows", "program files", ".git"
        };

        public void scan()
        {
            var list = List.List.Instance;
            var matcher = new PatternMatcher();
            matcher.AddRange(list.CheatNames);

            int count = 0;
            foreach (var root in GetRoots())
                ScanDirectory(root, matcher, ref count);

            Program._logger.Log($"[INFO] Directories scanned: {count}");
        }

        private IEnumerable<string> GetRoots()
        {
            if (Program.mode == "full")
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                    .Select(d => d.RootDirectory.FullName);
            }

            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine(user, "Downloads"),
            }.Distinct();
        }

        private void ScanDirectory(string root, PatternMatcher matcher, ref int count)
        {
            if (!Directory.Exists(root)) return;
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string path = stack.Pop();
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        count++;
                        string dirName = Path.GetFileName(dir);
                        string lower = dirName.ToLowerInvariant();
                        if (SkipDirs.Contains(lower)) continue;

                        foreach (var id in matcher.Search(dirName).Keys)
                            Program._logger.Log($"[FIND] {dir} (matched: {matcher.GetName(id)})");

                        var attr = File.GetAttributes(dir);
                        if ((attr & FileAttributes.ReparsePoint) != 0) continue;
                        stack.Push(dir);
                    }
                }
                catch { }
            }
        }
    }
}
