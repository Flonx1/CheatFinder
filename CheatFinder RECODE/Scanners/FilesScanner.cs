using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheatFinder_RECODE.Scanners
{
    internal class FilesScanner
    {
        public void scan()
        {
            string mode = Program.mode;

            if (mode == "full")
            {
                var list = new List.List();
                var cheatFiles = list.CheatFiles;
                var cheatExtension = list.CheatExtension;

                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    if (!drive.IsReady) continue;
                    ScanFiles(drive.RootDirectory.FullName, cheatFiles, cheatExtension);
                }
            }
            else if (mode == "fast")
            {
                var list = new List.List();
                var cheatFiles = list.CheatFiles;
                var cheatExtension = list.CheatExtension;
                var signatures = list.Signatures;

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
                    ScanFiles(path, cheatFiles, cheatExtension);
                }
            }
        }

        private void ScanFiles(string path, string[] cheatFiles, string[] cheatExtension)
        {
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    string fileExt = Path.GetExtension(file);

                    foreach (var name in cheatFiles)
                    {
                        if (string.IsNullOrEmpty(name)) continue;
                        if (fileName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Program._logger.Log($"[FIND] {file} (matched file: {name})");
                            break;
                        }
                    }

                    foreach (var ext in cheatExtension)
                    {
                        if (string.IsNullOrEmpty(ext)) continue;
                        if (fileExt.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Program._logger.Log($"[FIND] {file} (matched extension: {ext})");
                            break;
                        }
                    }

 
                    
                }

                foreach (var dir in Directory.GetDirectories(path))
                {
                    ScanFiles(dir, cheatFiles, cheatExtension);
                }
            }
            catch
            {
            }
        }
    }
}