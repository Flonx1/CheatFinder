using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CheatFinder
{
    public class CheatFinder
    {

        private readonly Logger _logger;
        public CheatFinder()
        {
            _logger = new Logger("logs.txt");
        }

        // Folders List
        public string[] GetCheatFolders()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string content = client.DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/CheatList");
                    return content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"[ERROR] Failed to load cheat folders: {ex.Message}");
                return new string[0];
            }
        }

        // Files list
        public string[] GetCheatFiles()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string content = client.DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/CheatFiles");
                    return content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"[ERROR] Failed to load cheat files: {ex.Message}");
                return new string[0];
            }
        }

        // Config pattern
        public string[] GetSearchPatterns() => new string[]
        {
            "*.celka", "*.wild", "*.deadcode", "*.nur"
        };

        public void CheckMinecraftTitle()
        {
            string[] cheatNames = GetCheatFolders();

            Process[] javaProcesses = Process.GetProcessesByName("java");
            Process[] javawProcesses = Process.GetProcessesByName("javaw");

            var detections = new List<string>();

            foreach (Process process in javaProcesses.Concat(javawProcesses))
            {
                string title = process.MainWindowTitle;
                string processName = process.ProcessName;

                detections.AddRange(CheckProcessTitle(title, cheatNames));

                detections.AddRange(CheckProcessSignature(processName, cheatNames));
            }

            foreach (var detection in detections)
            {
                _logger.Log(detection);
            }

        }
        private IEnumerable<string> CheckProcessTitle(string title, string[] cheatNames)
        {
            var results = new List<string>();

            if (string.IsNullOrEmpty(title))
                return results;

            foreach (string name in cheatNames)
            {
                if (title.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    bool isCheat = name.Contains("Ware") || name.Contains("Client") || name.Contains("DLC");
                    if (isCheat)
                    {
                        results.Add($"[DETECT] Potential cheat detected in title: {title}");
                    }
                    else
                    {
                        results.Add($"[FOUND] Minecraft title name: {title}");
                    }
                }
            }

            return results;
        }

        private IEnumerable<string> CheckProcessSignature(string processName, string[] cheatNames)
        {
            var results = new List<string>();

            foreach (string name in cheatNames)
            {
                if (processName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add($"[DETECT] Potential cheat detected in process name: {processName}");
                }
            }

            return results;
        }

        public void StartDetection()
        {
            MemoryScanner scanner = new MemoryScanner("logs.txt");

            string[] processNames = { "javaw", "java" };


            // Detect per memory the classes Patterns
            byte[][] patterns = new byte[][]
            {
                 Encoding.ASCII.GetBytes("bushroot"),
                 Encoding.ASCII.GetBytes("arkentoz"),
                 Encoding.ASCII.GetBytes("mixware"),
                 Encoding.ASCII.GetBytes("i.gishreloaded"),
                 Encoding.ASCII.GetBytes("wtf.expensive"),
                 Encoding.ASCII.GetBytes("wtf.mixware"),
                 Encoding.ASCII.GetBytes("ru.fals3r"),
                 Encoding.ASCII.GetBytes("ru.arkentoz"),
                 Encoding.ASCII.GetBytes("wtf.expensive"),

            };

            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                        _logger.Log($"Scanning process '{processName}.exe' with PID {process.Id}");

                        foreach (var pattern in patterns)
                        {
                            scanner.ScanProcessForPattern(process.Id, pattern, "logs.txt");
                        }
                    }
                }
                else
                {
                }
            }

            CheckMinecraftTitle();

            CheckJarFilesForPatterns();
        }




        public void CheckJarFilesForPatterns()
        {
            string username = Environment.UserName;
            string[] directoriesToCheck = new string[]
            {
                $@"C:\Users\{username}\AppData\Roaming\.minecraft\mods",
                $@"C:\Users\{username}\AppData\Roaming\.tlauncher\legacy\Minecraft\game\mods"
            };

            foreach (string directory in directoriesToCheck)
            {
                if (Directory.Exists(directory))
                {
                    var jarFiles = Directory.GetFiles(directory, "*.jar");
                    foreach (string jarFile in jarFiles)
                    {
                        CheckJarFile(jarFile);
                    }
                }
            }
        }

        public void CheckJarFile(string jarFilePath)
        {
            var directoryPatterns = new List<string>
            {
                "me/bushroot",
                "net/ccbluex",
                "hb",
                "hitbox",
                "liquidebounce",
                "quick",
                "wtf/mixware"
            };

            var methodNames = new List<string>
            {
                "setpanic",
                "panic",
                "aura",
                "unhook",
                "selfdestruct",
                "ispanic",
                "isdestruct",
                "isunhook",
                "isunhooked",
                "attack"
            };

            var patternPresence = new Dictionary<string, bool>();
            foreach (var pattern in directoryPatterns)
            {
                patternPresence[pattern] = false;
            }

            var methodPatterns = new List<Regex>();
            foreach (var methodName in methodNames)
            {
                methodPatterns.Add(new Regex(Regex.Escape(methodName), RegexOptions.IgnoreCase));
            }

            try
            {
                using (FileStream fileStream = new FileStream(jarFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string entryPath = entry.FullName;

                            foreach (var pattern in directoryPatterns)
                            {
                                if (entryPath.StartsWith(pattern) && !patternPresence[pattern])
                                {
                                    patternPresence[pattern] = true;
                                    _logger.Log($"[DETECT] Directory structure '{pattern}' found in JAR file: {jarFilePath}");
                                }
                            }

                            if (entryPath.EndsWith(".class"))
                            {
                                using (Stream entryStream = entry.Open())
                                {
                                    using (StreamReader reader = new StreamReader(entryStream))
                                    {
                                        string content = reader.ReadToEnd();

                                        foreach (var regex in methodPatterns)
                                        {
                                            if (regex.IsMatch(content))
                                            {
                                                ConsoleColor originalColor = Console.ForegroundColor;

                                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                                _logger.Log($"[DETECT] Method matching '{regex}' found in class file: {entryPath} in JAR file: {jarFilePath}");

                                                Console.ForegroundColor = originalColor;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"[ERROR] Could not open JAR file: {jarFilePath}. Exception: {ex.Message}");
            }

            foreach (var pattern in directoryPatterns)
            {
                if (!patternPresence[pattern])
                {
                }
            }
        }
        public void CheckExecutablesOnCDrive()
        {

            // Check the Apps sizes.
            var appsToCheck = new Dictionary<string, long>
            {
                { "Telegram.exe", 170L * 1024 * 1024 },
                { "Spotify.exe", 108L * 1024 * 1024 }
            };

            string rootPath = @"C:\";

            foreach (var app in appsToCheck)
            {
                var files = SafeEnumerateFiles(rootPath, app.Key);

                foreach (var filePath in files)
                {
                    CheckFileSize(filePath, app.Value);
                }
            }
        }

        private IEnumerable<string> SafeEnumerateFiles(string rootPath, string searchPattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(rootPath, searchPattern));

                foreach (var directory in Directory.GetDirectories(rootPath))
                {
                    files.AddRange(SafeEnumerateFiles(directory, searchPattern));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (Exception ex)
            {
                _logger.Log($"[ERROR] Exception while enumerating files: {ex.Message}");
            }

            return files;
        }

        private void CheckFileSize(string filePath, long minSizeBytes)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;
                double fileSizeMB = fileSize / (1024.0 * 1024.0);

                if (fileSize < minSizeBytes)
                {
                    _logger.Log($"[DETECT] File size suspiciously small: {fileSizeMB:F2} MB at {filePath}");
                }
                else
                {
                    _logger.Log($"[INFO] File size normal: {fileSizeMB:F2} MB at {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"[ERROR] Could not check file size for {filePath}. Exception: {ex.Message}");
            }
        }




    }
}