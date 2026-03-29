using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheatFinder_RECODE.Scanners
{
    internal class DirectoryScanner
    {
        public void scan()
        {
            string mode = Program.mode;

            if (mode == "full")
            {
                var list = new List.List();
                var cheatNames = list.CheatNames;

                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    if (!drive.IsReady) continue;
                    ScanDirectory(drive.RootDirectory.FullName, cheatNames);
                }
            }
            else if (mode == "fast")
            {
                var list = new List.List();
                var cheatNames = list.CheatNames;

                string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                string[] paths = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    Path.Combine(user, "Downloads")
                };

                foreach (var path in paths)
                {
                    if (!Directory.Exists(path)) continue;
                    ScanDirectory(path, cheatNames);
                }
            }
        }

        private void ScanDirectory(string path, string[] cheatNames)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    string dirName = Path.GetFileName(dir);

                    foreach (var name in cheatNames)
                    {
                        if (string.IsNullOrEmpty(name)) continue;
                        if (dirName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Program._logger.Log($"[FIND] {dir} (matched: {name})");
                            break;
                        }
                    }

                    ScanDirectory(dir, cheatNames);
                }
            }
            catch
            {
            }
        }
    }
}