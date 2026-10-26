using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace CheatFinder_RECODE.Scanners
{
    internal class LastActivity
    {
        private class ActivityEntry
        {
            public DateTime RunDate { get; set; }
            public string AppPath { get; set; }
            public string Source { get; set; }
        }

        public void getActivitysToday()
        {
            List<ActivityEntry> activities = new List<ActivityEntry>();

            ScanPrefetch(activities);
            ScanUserAssist(activities);
            ScanBAM(activities);
            ScanRecentFiles(activities);

            var todayActivities = activities
                .Where(a => a.RunDate.Date == DateTime.Today)
                .GroupBy(a => a.AppPath.ToLowerInvariant())
                .Select(g => g.OrderByDescending(a => a.RunDate).First())
                .OrderByDescending(a => a.RunDate)
                .ToList();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Program._logger.Log("  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            Program._logger.Log("  ║                          RUNNED APPS TODAY                                   ║");
            Program._logger.Log("  ╠═══════════════════════════════════════════════════════════════════════════════╣");
            Program._logger.Log("  ║  Date: " + DateTime.Today.ToString("dd-MM-yyyy") + new string(' ', 63 - DateTime.Today.ToString("dd-MM-yyyy").Length) + "║");
            Program._logger.Log("  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            if (todayActivities.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Program._logger.Log("  [!] No activity found for today.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Program._logger.Log("  ─────────────────────────────────────────────────────────────────────────────────");
            Console.ResetColor();

            int index = 1;
            foreach (var activity in todayActivities)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Program._logger.Log("  │ ");

                Console.ForegroundColor = ConsoleColor.Green;
                Program._logger.Log($"#{index:D3}");

                Console.ForegroundColor = ConsoleColor.White;
                Program._logger.Log(" │ ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Program._logger.Log("Runned date: ");

                Console.ForegroundColor = ConsoleColor.Cyan;
                Program._logger.Log(activity.RunDate.ToString("dd-MM-yyyy HH:mm:ss"));

                Console.ForegroundColor = ConsoleColor.White;
                Program._logger.Log(" │ ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Program._logger.Log("APP: ");

                Console.ForegroundColor = ConsoleColor.White;
                Program._logger.Log(activity.AppPath);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Program._logger.Log("  │     │ ");

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Program._logger.Log("Source: ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Program._logger.Log(activity.Source);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Program._logger.Log("  ─────────────────────────────────────────────────────────────────────────────────");
                Console.ResetColor();

                index++;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Program._logger.Log($"  ╔═══════════════════════════════════════════════════════════════════════════════╗");
            Program._logger.Log($"  ║  Total apps found today: {todayActivities.Count,-53}║");
            Program._logger.Log($"  ╚═══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

        }

        private void ScanPrefetch(List<ActivityEntry> activities)
        {
            try
            {
                string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

                if (Directory.Exists(prefetchPath))
                {
                    foreach (string file in Directory.GetFiles(prefetchPath, "*.pf"))
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.LastAccessTime.Date == DateTime.Today)
                            {
                                string fileName = Path.GetFileNameWithoutExtension(file);
                                int dashIndex = fileName.LastIndexOf('-');
                                string appName = dashIndex > 0 ? fileName.Substring(0, dashIndex) : fileName;

                                activities.Add(new ActivityEntry
                                {
                                    RunDate = fi.LastAccessTime,
                                    AppPath = appName + " (Prefetch: " + file + ")",
                                    Source = "Prefetch"
                                });
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void ScanUserAssist(List<ActivityEntry> activities)
        {
            try
            {
                string[] userAssistGuids = new string[]
                {
                    "{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}",
                    "{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}"
                };

                foreach (string guid in userAssistGuids)
                {
                    string keyPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{guid}\Count";

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        if (key == null) continue;

                        foreach (string valueName in key.GetValueNames())
                        {
                            try
                            {
                                string decodedName = DecodeRot13(valueName);

                                byte[] data = key.GetValue(valueName) as byte[];
                                if (data != null && data.Length >= 72)
                                {
                                    long fileTime = BitConverter.ToInt64(data, 60);
                                    if (fileTime > 0)
                                    {
                                        try
                                        {
                                            DateTime lastRun = DateTime.FromFileTime(fileTime);
                                            if (lastRun.Date == DateTime.Today)
                                            {
                                                activities.Add(new ActivityEntry
                                                {
                                                    RunDate = lastRun,
                                                    AppPath = decodedName,
                                                    Source = "UserAssist (Registry)"
                                                });
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanBAM(List<ActivityEntry> activities)
        {
            try
            {
                string bamPath = @"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings";

                using (RegistryKey bamKey = Registry.LocalMachine.OpenSubKey(bamPath))
                {
                    if (bamKey == null) return;

                    foreach (string sidSubKey in bamKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey userKey = bamKey.OpenSubKey(sidSubKey))
                            {
                                if (userKey == null) continue;

                                foreach (string valueName in userKey.GetValueNames())
                                {
                                    try
                                    {
                                        if (valueName.Contains("\\") && valueName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                        {
                                            byte[] data = userKey.GetValue(valueName) as byte[];
                                            if (data != null && data.Length >= 8)
                                            {
                                                long fileTime = BitConverter.ToInt64(data, 0);
                                                if (fileTime > 0)
                                                {
                                                    try
                                                    {
                                                        DateTime lastRun = DateTime.FromFileTime(fileTime);
                                                        if (lastRun.Date == DateTime.Today)
                                                        {
                                                            activities.Add(new ActivityEntry
                                                            {
                                                                RunDate = lastRun,
                                                                AppPath = valueName,
                                                                Source = "BAM (Background Activity Moderator)"
                                                            });
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void ScanRecentFiles(List<ActivityEntry> activities)
        {
            try
            {
                string recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);

                if (Directory.Exists(recentPath))
                {
                    foreach (string file in Directory.GetFiles(recentPath, "*.lnk"))
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);
                            if (fi.LastWriteTime.Date == DateTime.Today)
                            {
                                string targetPath = ResolveShortcut(file);
                                string displayPath = !string.IsNullOrEmpty(targetPath) ? targetPath : Path.GetFileNameWithoutExtension(file);

                                if (displayPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    activities.Add(new ActivityEntry
                                    {
                                        RunDate = fi.LastWriteTime,
                                        AppPath = displayPath,
                                        Source = "Recent Files"
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private string DecodeRot13(string input)
        {
            char[] array = input.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];

                if (number >= 'a' && number <= 'z')
                {
                    if (number > 'm') number -= 13;
                    else number += 13;
                }
                else if (number >= 'A' && number <= 'Z')
                {
                    if (number > 'M') number -= 13;
                    else number += 13;
                }
                array[i] = (char)number;
            }
            return new string(array);
        }

        private string ResolveShortcut(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                string targetPath = shortcut.TargetPath;

                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

                return targetPath;
            }
            catch
            {
                return null;
            }
        }
    }
}