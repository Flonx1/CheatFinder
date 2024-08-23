using System;
using System.Diagnostics;
using System.Linq;

namespace CheatFinder
{
    public class CheatFinder
    {
        public string[] GetCheatFolders() => new string[]
        {
            "Celestial", "NeoWare", "PONOSCLIENT", "Meteor", "Minced", "WexSide", "LampWare",
            "Baritone", "Matix", "Impact", "Wurst", "ZexisClient", "FlugerClient", "Enormity",
            "LeonTap", "expensive", "KeazClient", "WinnerClient", "RynWare", "DeadCode",
            "waveclient", "WhiteCode", "bush1root", "ExcellentClient", "MixWare", "Emortality",
            "BebraWare", "ExpensiveClient", "Celka", "Calestial", "ExpensiveClientCrack", "Expensive",
            "Takker", "NiobiumClient", "Niobium", "DoomsDay", "Dooms", "Day", "CortexClient",
            "Cortex", "Troxill", "Troxillclient", "NoRender", "DreamPool", "cabaletta", "LiquideBounce",
            "GishCode", "Inertia", "Future", "RusherHack", "Zamorozka", "WintWare", "Nursultan",
            "Norules", "Akrien", "xray", "Eternity", "WEXSIDE", "Rich", "RichPremium", "ArchWare",
            "BoberWare", "Hitbox", "FLauncher", "ATMOSPHERE", "Wolfram", "Huzuni", "Legit", "FLUX",
            "Aristois", "NeverHook", "ShitClient", "Bleach", "Zeus", "FreeCam", "ExLoader"
        };

        public string[] GetCheatFiles() => new string[]
        {
            "Hitbox.jar", "Cleaner.exe", "celestial.jar", "minecraft.jar", "nova-api-1.0-SNAPSHOT.jar",
            "Baritone.jar", "Baritone-1.16.5.jar", "ExcellentClient.jar", "Celestial.exe", "Celka.exe",
            "Nursultan.exe", "Loader.exe", "Nurik.exe", "Meriada.exe", "Launcher.exe", "Reach.jar",
            "wild-api-1.0-SNAPSHOT.jar", "lampware-api.jar", "Deadcode.dll", "modid-1.0.jar", "Takker.exe","Expensive.exe"
        };

        public string[] GetSearchPatterns() => new string[]
        {
            "*.celka", "*.wild", "*.deadcode", "*.nur"
        };

        public void CheckMinecraftTitle()
        {
            string[] cheatNames = GetCheatFolders();  // Reuse the list of cheat names for process title checking

            Process[] javaProcesses = Process.GetProcessesByName("java");
            Process[] javawProcesses = Process.GetProcessesByName("javaw");

            foreach (Process process in javaProcesses.Concat(javawProcesses))
            {
                CheckProcessTitle(process.MainWindowTitle, cheatNames);
            }
        }

        private void CheckProcessTitle(string title, string[] cheatNames)
        {
            if (string.IsNullOrEmpty(title))
                return;

            foreach (string name in cheatNames)
            {
                if (title.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    bool isCheat = name.Contains("Ware") || name.Contains("Client") || name.Contains("DLC");
                    if (isCheat)
                    {
                        Console.WriteLine($"[DETECT] Potential cheat detected in title: {title}");
                    }
                    else
                    {
                        Console.WriteLine($"[FOUND] Minecraft title name: {title}");
                    }
                    }
                }
            }
        }
    }

