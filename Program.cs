using CheatFinder_RECODE.Detections;
using CheatFinder_RECODE.Scanners;
using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CheatFinder_RECODE
{
    internal class Program
    {
        static float version = 1.5f;
        public static string mode = "";

        public static Logger _logger;

        static void Main(string[] args)
        {
            _logger = new Logger("logs.txt");

            Console.Title = $"CheatFinder #{version} / Opensource Project / Created by Flonxi <3";

            _logger.Log($"Welcome, to cheatfinder #{version}");
            Console.WriteLine("[1] Full scan: it can takes a few hours (scans all drives).");
            Console.WriteLine("[2] Fast scan: it can takes a few minutes (user folders + game dirs).");
            Console.WriteLine();
            Console.Write("Select: ");

            string selection = Console.ReadLine();
            mode = selection == "1" ? "full" : "fast";

            _logger.Log($"Selected mode: {mode}");

            string vmStatus = VMDetection.IsRunningInVM() ? "[Detect] Virtual Machine: YES" : "Virtual Machine: NO";
            _logger.Log(vmStatus);

            if (MinecraftPaths.GameDirs.Count > 0)
                _logger.Log($"[INFO] Game folders found: {MinecraftPaths.GameDirs.Count}");
            else
                _logger.Log("[INFO] No Minecraft game folder found in standard locations.");

            var stopwatch = Stopwatch.StartNew();

            // Порядок: быстрые/живые проверки (память, процессы, запущенная java) первыми —
            // пока игра и инструменты ещё открыты, их важно поймать до того, как их закроют.
            var steps = new List<(string, Action)>
            {
                ("Java process & JVM args", () => new JavaArgsDetector().check()),
                ("Memory Scanner",          () => new MemoryScanner().scan()),
                ("Running processes",       () => new ProcessDetector().check()),
                ("Network",                 () => new NetworkScanner().scan()),
                ("Mods Checker",            () => new ModsChecker().check()),
                ("Versions Checker",        () => new VersionsChecker().check()),
                ("Size Analyzer",           () => new SizeAnalyzer().check()),
                ("Minecraft logs",          () => new LogsScanner().scan()),
                ("Recycle Bin",             () => new RecycleBinScanner().check()),
                ("Last Activity",           () => new LastActivity().getActivitysToday()),
                ("USB Devices",             () => new LastConnectedUsbDevices().getDevices()),
                ("Loaders",                 () => new LaunchersDetector().check()),
                ("Files Scanner",           () => new FilesScanner().scan()),
                ("Directory Scanner",       () => new DirectoryScanner().scan()),
            };

            foreach (var (name, action) in steps)
            {
                _logger.Log($"==== Starting: {name} ====");
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _logger.Log($"[ERROR] {name} crashed: {ex.Message}");
                }
            }

            stopwatch.Stop();
            PrintSummary(stopwatch.Elapsed);
            Console.ReadLine();
        }

        private static void PrintSummary(TimeSpan elapsed)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                          SUMMARY                              ║");
            Console.WriteLine("  ╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            _logger.Log($"Hard detects : {_logger.Detects}");
            _logger.Log($"Finds        : {_logger.Finds}");
            _logger.Log($"50/50 / susp.: {_logger.Suspicious}");
            _logger.Log($"Time elapsed : {elapsed:hh\\:mm\\:ss}");

            if (_logger.Highlights.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine("  Key findings (also in logs.txt):");
                Console.ResetColor();
                int shown = 0;
                foreach (var highlight in _logger.Highlights)
                {
                    Console.WriteLine("   - " + highlight);
                    if (++shown >= 40) { Console.WriteLine($"   ... and {_logger.Highlights.Count - shown} more"); break; }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  No suspicious artifacts found.");
                Console.ResetColor();
            }

            _logger.Log("Done. Full report saved to logs.txt");
        }
    }
}
