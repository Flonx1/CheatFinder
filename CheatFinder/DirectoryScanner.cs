using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheatFinder
{
    public class DirectoryScanner
    {
        // Maintain a list of directories that are known to be problematic or restricted
        private readonly HashSet<string> _restrictedDirectories = new HashSet<string>();

        // Keeps track of reported errors to avoid repetitive messages
        private readonly HashSet<string> _reportedErrors = new HashSet<string>();

        // List of drives to be scanned (dynamically retrieved)
        private readonly string[] _drivesToScan;
        private readonly Logger _logger;

        public DirectoryScanner()
        {
            _drivesToScan = GetAllDrives();
            _logger = new Logger("logs.txt");
        }

        public void CheckFullPC(string[] searchPatterns, string[] folders, string[] files)
        {
            foreach (string driveLetter in _drivesToScan)
            {
                if (Directory.Exists(driveLetter))
                {
                    _logger.Log($"Scanning drive: {driveLetter}");

                    // Scan directories and files for each drive
                    try
                    {
                        ScanDirectories(driveLetter, searchPatterns);
                        CheckDirectories(driveLetter, folders);
                        CheckFiles(driveLetter, files);
                    }
                    catch (Exception ex)
                    {
                        // Uncomment for error logging
                        // _logger.Log($"[ERROR] An error occurred while scanning drive: {driveLetter}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log($"[ERROR] Drive {driveLetter} does not exist.");
                }
            }

            // Indicate that the scan is finished
            Console.WriteLine("Exit");

            // Wait for user input before closing the console window
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private string[] GetAllDrives()
        {
            // Get all available drives
            var drives = DriveInfo.GetDrives()
                                  .Where(drive => drive.IsReady) // Ensure drive is ready to use
                                  .Select(drive => drive.Name)
                                  .ToArray();

            return drives;
        }

        private void ScanDirectories(string baseDirectory, string[] searchPatterns)
        {
            if (_restrictedDirectories.Any(rd => baseDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                return;

            try
            {
                var directoriesToScan = new Queue<string>();
                directoriesToScan.Enqueue(baseDirectory);

                while (directoriesToScan.Count > 0)
                {
                    string currentDirectory = directoriesToScan.Dequeue();

                    if (_restrictedDirectories.Any(rd => currentDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    try
                    {
                        // Enqueue subdirectories
                        foreach (string directory in Directory.GetDirectories(currentDirectory))
                        {
                            directoriesToScan.Enqueue(directory);
                        }

                        // Check files in the current directory
                        foreach (string pattern in searchPatterns)
                        {
                            try
                            {
                                string[] foundFiles = Directory.GetFiles(currentDirectory, pattern, SearchOption.TopDirectoryOnly);
                                foreach (string foundFile in foundFiles)
                                {
                                    _logger.Log($"[DETECT] Found file with pattern '{pattern}' at path: {foundFile}");
                                }
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                LogError(currentDirectory, ex.Message);
                            }
                            catch (Exception ex)
                            {
                                // Uncomment for error logging
                                // _logger.Log($"[ERROR] An error occurred while scanning files in directory: {currentDirectory}: {ex.Message}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogError(currentDirectory, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Uncomment for error logging
                        // _logger.Log($"[ERROR] An error occurred while scanning directory: {currentDirectory}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError(baseDirectory, ex.Message);
            }
            catch (Exception ex)
            {
                // Uncomment for error logging
                // _logger.Log($"[ERROR] An error occurred while scanning directory: {baseDirectory}: {ex.Message}");
            }
        }

        private void CheckDirectories(string baseDirectory, string[] folders)
        {
            if (_restrictedDirectories.Any(rd => baseDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                return;

            try
            {
                var directoriesToCheck = new Queue<string>();
                directoriesToCheck.Enqueue(baseDirectory);

                while (directoriesToCheck.Count > 0)
                {
                    string currentDirectory = directoriesToCheck.Dequeue();

                    if (_restrictedDirectories.Any(rd => currentDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    try
                    {
                        // Enqueue subdirectories
                        foreach (string directory in Directory.GetDirectories(currentDirectory))
                        {
                            directoriesToCheck.Enqueue(directory);
                        }

                        // Check for specified folders
                        foreach (string folder in folders)
                        {
                            string path = Path.Combine(currentDirectory, folder);
                            if (Directory.Exists(path))
                            {
                                _logger.Log($"[DETECT] Found folder: {path}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogError(currentDirectory, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Uncomment for error logging
                        // _logger.Log($"[ERROR] An error occurred while checking directories in: {currentDirectory}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError(baseDirectory, ex.Message);
            }
            catch (Exception ex)
            {
                // Uncomment for error logging
                // _logger.Log($"[ERROR] An error occurred while checking directories in: {baseDirectory}: {ex.Message}");
            }
        }

        private void CheckFiles(string baseDirectory, string[] files)
        {
            if (_restrictedDirectories.Any(rd => baseDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                return;

            try
            {
                var filesToCheck = new Queue<string>();
                filesToCheck.Enqueue(baseDirectory);

                while (filesToCheck.Count > 0)
                {
                    string currentDirectory = filesToCheck.Dequeue();

                    if (_restrictedDirectories.Any(rd => currentDirectory.StartsWith(rd, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    try
                    {
                        // Enqueue subdirectories
                        foreach (string directory in Directory.GetDirectories(currentDirectory))
                        {
                            filesToCheck.Enqueue(directory);
                        }

                        // Check for specified files
                        foreach (string file in files)
                        {
                            string path = Path.Combine(currentDirectory, file);
                            if (File.Exists(path))
                            {
                                _logger.Log($"[DETECT] Found file: {path}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogError(currentDirectory, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Uncomment for error logging
                        // _logger.Log($"[ERROR] An error occurred while checking files in: {currentDirectory}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError(baseDirectory, ex.Message);
            }
            catch (Exception ex)
            {
                // Uncomment for error logging
                // _logger.Log($"[ERROR] An error occurred while checking files in: {baseDirectory}: {ex.Message}");
            }
        }

        private void LogError(string path, string message)
        {
            // Report the error only once per path
            if (_reportedErrors.Add(path))
            {
                // Uncomment for error logging
                // _logger.Log($"[ERROR] Unauthorized access to directory or file {path}: {message}");
            }
        }
    }
}
