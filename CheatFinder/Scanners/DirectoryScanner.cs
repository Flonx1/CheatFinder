using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheatFinder
{
    public class DirectoryScanner
    {
        private readonly HashSet<string> _restrictedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reportedErrors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string[] _drivesToScan;
        private readonly Logger _logger;

        public DirectoryScanner()
        {
            _drivesToScan = GetAllDrives();
            _logger = new Logger("logs.txt");
        }

        public void CheckFullPC(string[] searchPatterns, string[] folders, string[] files)
        {
            Parallel.ForEach(_drivesToScan, driveLetter =>
            {
                if (Directory.Exists(driveLetter))
                {
                    _logger.Log($"Scanning drive: {driveLetter}");
                    try
                    {
                        FastScan(driveLetter, searchPatterns, folders, files);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"[ERROR] Scanning failed for drive {driveLetter}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log($"[ERROR] Drive {driveLetter} does not exist.");
                }
            });

            Console.WriteLine("Scanning Complete.");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private string[] GetAllDrives()
        {
            return DriveInfo.GetDrives()
                            .Where(drive => drive.IsReady)
                            .Select(drive => drive.Name)
                            .ToArray();
        }

        private void FastScan(string baseDirectory, string[] searchPatterns, string[] folders, string[] files)
        {
            var directories = new ConcurrentQueue<string>();
            directories.Enqueue(baseDirectory);

            while (directories.TryDequeue(out string currentDirectory))
            {
                if (_restrictedDirectories.Any(rd => currentDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    var subDirs = Directory.GetDirectories(currentDirectory);
                    foreach (var dir in subDirs)
                    {
                        directories.Enqueue(dir);
                    }

                    Parallel.Invoke(
                        () =>
                        {
                            foreach (var pattern in searchPatterns)
                            {
                                try
                                {
                                    var matchedFiles = Directory.GetFiles(currentDirectory, pattern, SearchOption.TopDirectoryOnly);
                                    foreach (var file in matchedFiles)
                                    {
                                        _logger.Log($"[DETECT] Found file with pattern '{pattern}' at: {file}");
                                    }
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    LogError(currentDirectory, ex.Message);
                                }
                            }
                        },
                        () =>
                        {
                            foreach (var folder in folders)
                            {
                                var path = Path.Combine(currentDirectory, folder);
                                if (Directory.Exists(path))
                                {
                                    _logger.Log($"[DETECT] Found folder: {path}");
                                }
                            }
                        },
                        () =>
                        {
                            foreach (var file in files)
                            {
                                var path = Path.Combine(currentDirectory, file);
                                if (File.Exists(path))
                                {
                                    _logger.Log($"[DETECT] Found file: {path}");
                                }
                            }
                        }
                    );
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogError(currentDirectory, ex.Message);
                }
            }
        }

        private void LogError(string path, string message)
        {
            if (_reportedErrors.Add(path))
            {

            }
        }
    }
}