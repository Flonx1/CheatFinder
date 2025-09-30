using System;

namespace CheatFinder
{
    internal class Program
    {
        private static readonly Logger _logger = new Logger("logs.txt");

        public static void Main()
        {
            var version = 1.4;

            Console.Title = $"CheatFinder #{version} / OpenSource Project / Created by Flonxi <3";
            Console.WriteLine("The Program was Activated on your PC, sorry");
            Console.WriteLine("Welcome to CheatFinder");
            Console.WriteLine("Created by Flonxi from Germany <3");
            Console.WriteLine("Please wait a few minutes for analysis...\n");

            // Instances
            var cheatFinder = new CheatFinder();
            var directoryScanner = new DirectoryScanner();

            string[] folders = cheatFinder.GetCheatFolders();
            string[] files = cheatFinder.GetCheatFiles();
            string[] patterns = cheatFinder.GetSearchPatterns();

            bool isRunningInVM = VMDetection.IsRunningInVM();
            string vmStatus = isRunningInVM ? "[Detect] The system is running in a virtual machine." : "is not running on Virtual Machine";

            _logger.Log(vmStatus);

            cheatFinder.StartDetection();
            cheatFinder.CheckExecutablesOnCDrive();
            directoryScanner.CheckFullPC(patterns, folders, files);
            


        }
    }
}
