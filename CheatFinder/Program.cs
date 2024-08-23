using System;

namespace CheatFinder
{
    internal class Program
    {
        public static void Main()
        {
            Console.WriteLine("The Program was Activated on your PC, sorry");
            Console.WriteLine("Welcome to CheatFinder");
            Console.WriteLine("If you want to use the program for a company or other purposes, write @flonxi in Telegram...");
            Console.WriteLine("Please wait a few minutes for analysis...");

            var cheatFinder = new CheatFinder();
            var directoryScanner = new DirectoryScanner();

            string[] folders = cheatFinder.GetCheatFolders();
            string[] files = cheatFinder.GetCheatFiles();
            string[] patterns = cheatFinder.GetSearchPatterns();

            // Check all drives
            directoryScanner.CheckFullPC(patterns, folders, files);

            // Check Minecraft process titles
            cheatFinder.CheckMinecraftTitle();
        }
    }
}
