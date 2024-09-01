using System;

namespace CheatFinder
{
    internal class Program
    {
        private static readonly Logger _logger = new Logger("logs.txt");

        public static void Main()
        {
            // Set console title and initial messages
            Console.Title = "CheatFinder #1.1 / OpenSource Project / Created by Flonxi <3";
            Console.WriteLine("The Program was Activated on your PC, sorry");
            Console.WriteLine("Welcome to CheatFinder");
            Console.WriteLine("Created by Flonxi from Germany <3");
            Console.WriteLine("Please wait a few minutes for analysis...\n");


            // Create instances of CheatFinder and DirectoryScanner
            var cheatFinder = new CheatFinder();
            var directoryScanner = new DirectoryScanner();

            // Get cheat folders, files, and patterns
            string[] folders = cheatFinder.GetCheatFolders();
            string[] files = cheatFinder.GetCheatFiles();
            string[] patterns = cheatFinder.GetSearchPatterns();

            // Check if the system is running in a virtual machine
            bool isRunningInVM = VMDetection.IsRunningInVM();
            string vmStatus = isRunningInVM ? "[Detect] The system is running in a virtual machine." : "is not running on Virtual Machine";

            // Log VM status
            _logger.Log(vmStatus);


            // Log information about the cheat detection
            cheatFinder.StartDetection();

            // Check all drives and log results
            directoryScanner.CheckFullPC(patterns, folders, files);


        }
    }
}
