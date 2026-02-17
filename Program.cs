using CheatFinder_RECODE.Detections;
using CheatFinder_RECODE.Scanners;
using System;

namespace CheatFinder_RECODE
{
    internal class Program
    {
        static float version = 1.3f;
        public static string mode = "";

        // Статическое поле, чтобы можно было вызывать из других классов через Program._logger.Log()
        public static Logger _logger;

        static void Main(string[] args)
        {
            // 1. Инициализация логгера САМАЯ ПЕРВАЯ
            _logger = new Logger("logs.txt");

            Console.Title = $"CheatFinder #{version} / Opensource Project / Created by Flonxi <3";

            // Используем логгер вместо Console.WriteLine, чтобы это сохранилось в файл
            _logger.Log($"Welcome, to cheatfinder #{version}");
            Console.WriteLine("[1] Full scan: it can takes a few hours.");
            Console.WriteLine("[2] Fast scan: it can takes a few minutes.");

            Console.WriteLine();
            Console.Write("Select: ");
            string selection = Console.ReadLine();

            if (selection == "1")
            {
                mode = "full";
            }
            else if (selection == "2")
            {
                mode = "fast";
            }
            else
            {
                mode = "fast"; 
            }

            _logger.Log($"Selected mode: {mode}");
            _logger.Log("Please wait...");

            string vmStatus = VMDetection.IsRunningInVM() ? "[Detect] Virtual Machine: YES" : "Virtual Machine: NO";
            _logger.Log(vmStatus);

            _logger.Log("Starting Loader scan.");
            new LaunchersDetector().check();

            _logger.Log("Starting Memory Scanner...");
            new MemoryScanner().scan();

            _logger.Log("Starting Mods Checker...");
            new ModsChecker().check();

            _logger.Log("Starting Network Scanner...");
            new NetworkScanner().scan();

            _logger.Log("Starting Size Analyzer...");
            new SizeAnalyzer().check();

            _logger.Log("Starting Files Scanner...");
            new FilesScanner().scan();

            _logger.Log("Starting Directory Scanner...");
            new DirectoryScanner().scan();

            _logger.Log("Done.");
            Console.ReadLine();
        }
    }
}