using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheatFinder_RECODE.Scanners
{
    internal class FilesScanner
    {
        // Папки, которые бессмысленно рекурсивно обходить (сотни тысяч файлов, замедляют скан)
        private static readonly string[] SkipDirs =
        {
            "node_modules", "$recycle.bin", "windows", "program files", "programdata\\package cache",
            "appdata\\local\\temp\\mozilla", ".git", "obj", "bin\\debug", "bin\\release"
        };

        private static readonly string[] ArchiveExtensions = { ".zip", ".rar", ".7z", ".jar" };

        public void scan()
        {
            var list = List.List.Instance;
            var fileMatcher = new PatternMatcher();
            fileMatcher.AddRange(list.CheatFiles);
            fileMatcher.AddRange(list.CheatNames);

            var extensions = new HashSet<string>(list.CheatExtension.Select(e => e.StartsWith(".") ? e : "." + e), StringComparer.OrdinalIgnoreCase);

            int count = 0;
            foreach (var root in GetRoots())
            {
                ScanFiles(root, fileMatcher, extensions, ref count);
            }
            Program._logger.Log($"[INFO] Files scanned: {count}");
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
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine(user, "Downloads"),
            }.Distinct();
        }

        // Итеративный обход со стеком — рекурсия на весь диск переполняла стек на глубоких деревьях
        private void ScanFiles(string root, PatternMatcher fileMatcher, HashSet<string> extensions, ref int count)
        {
            if (!Directory.Exists(root)) return;
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string path = stack.Pop();
                string lower = path.ToLowerInvariant();
                if (SkipDirs.Any(s => lower.EndsWith("\\" + s) || lower.Contains("\\" + s + "\\"))) continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(path))
                    {
                        count++;
                        string fileName = Path.GetFileName(file);
                        string ext = Path.GetExtension(file);

                        foreach (var id in fileMatcher.Search(fileName).Keys)
                            Program._logger.Log($"[FIND] {file} (matched: {fileMatcher.GetName(id)})");

                        if (extensions.Contains(ext))
                            Program._logger.Log($"[FIND] {file} (matched extension: {ext})");

                        // dll/exe рядом с игровыми папками подозрительнее, чем где-то ещё
                        if (ArchiveExtensions.Contains(ext) && fileName.IndexOf("mod", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            foreach (var id in fileMatcher.Search(fileName).Keys)
                                Program._logger.Log($"[50/50 DETECT] Archive matching cheat name: {file} (matched: {fileMatcher.GetName(id)})");
                        }
                    }

                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        var attr = File.GetAttributes(dir);
                        if ((attr & FileAttributes.ReparsePoint) != 0) continue; // не ходим по симлинкам
                        stack.Push(dir);
                    }
                }
                catch { }
            }
        }
    }
}
