using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            "Aristois", "NeverHook", "ShitClient", "Bleach", "Zeus", "FreeCam", "ExLoader",
            "HitBox", "DoomsDayMod", "ExpensiveLib", "stx", "Kirka",
            "Paralyzed", "nexus", "Twinks", "bleachhack", "Lucid", "Visuals", "DrastiX",
            "Sotona", "Gorda", "Deathzatter", "SkillClient", "Hexeon", "Chainsaw", "Radoen",
            "Pandora", "Hazing", "Disco Party", "Volume", "salhack", "Skash", "Jigsaw",
            "rt", "Suicide", "Yay", "dark-light-client", "Auxentity", "cxclient", "Nebula",
            "kamiblue", "amera", "Qwinks", "Cryptic", "Kawaii", "Nova", "meteor-client",
            "ares-forge", "Atlas", "Onigger", "Parallaxa", "Tyrant", "Tempest", "Icarus",
            "Triton", "FadeAway", "Hexloit", "Intel", "Winter", "Supreme", "ares-fabric",
            "Cinnamon", "nivia", "Kryptonite", "Slowly", "Future", "TheBoys", "serenity",
            "Stick", "Quiet", "Wolfram", "BlueBerry", "Flare", "omegam0d", "PHack",
            "Saturn", "Vega", "Sigma", "Akrien", "Cheese", "BiT", "fatal", "apple",
            "Exist", "Aether", "Rainbow", "Skush", "Veteran", "Grey", "viaforgetill1171",
            "GarPloit", "Lite", "Exhibition", "Zamorozka", "DramaAlert", "Memestar",
            "JASTERINGO", "Hex", "Invictus", "Kraken", "FusionX", "Lyfe", "Swifting",
            "Nexus", "Vatic", "amd", "serenity", "Polaris", "Skid", "Atmosphere",
            "EximiusObf", "Fantasy", "1.8", "DEVIL", "Serpent", "GubiX", "Krypton",
            "Squid", "Fall", "DelugeUpdated", "Arion", "JaV", "LiquidBounce", "Plz",
            "Floon", "Komplexe", "Colgate", "Pulse", "Nitrogen", "Aware", "brainfreeze",
            "Celsum", "JamClient", "Wolfram Installer", "Sallos", "Apinity", "depression",
            "TeamHack", "Paris", "summit-client-master", "oneway.wtf", "oneway"
        };

        public string[] GetCheatFiles() => new string[]{
    // Elytra Swap Files
    "JJElytraSwap-1.3.jar",
    "elytra_swap-1.2.1-1.15.1.jar",
    "elytra_swap-1.6.4-1.16.1.jar",
    "elytra_swap-1.3.1-20w07a.jar",
    "elytra_swap-1.2.2-1.15.2.jar",
    "elytra_swap-1.2.2-1.15.1.jar",
    "quick_elytra-2.0.0-mc1.18.2.jar",
    "elytra-chestplate-swapper-1.3.0-MC1.20.jar",
    "elytra_swap-1.3.0-1.14.4.jar",
    "elytra-chestplate-swapper-1.2.1-MC1.19.jar",
    "elytra-chestplate-swapper-1.2.0-MC1.17.jar",
    "elytra-chestplate-swapper-1.2.1-MC1.18.jar",
    "elytra_swap-2.2.0-1.16.2.jar",
    "elytra_swap-1.3.0.1-1.15.2.jar",
    "elytra_swap-1.1.0-1.15.jar",
    "elytra_swap-1.4.0-1.15.jar",
    "elytra_swap-1.5.0-1.15.jar",
    "bedrock-miner-1.0.0-for-Minecraft-1.16.x.jar",
    
    // Mob Health Bar Files
    "mobhealthbar-forge-1.18.2-2.2.0.jar",
    "mobhealthbar-1.19-v2.1-forge.jar",
    "mobhealthbar-1-16-5.jar",
    "mobhealthbar-forge-1.19.3-2.2.0.jar",
    "mobhealthbar-forge-1.16.5-2.2.0.jar",
    "mobhealthbar-1-18-1.jar",
    "mobhealthbar-1-18-1-2-fabric.jar",
    "mobhealthbar-1-17-1-2-fabric.jar",
    "mobhealthbar-fabric-1.20.1-2.2.0.jar",
    "mobhealthbar-forge-1.20.1-2.2.0.jar",
    "mobhealthbar-1-16-5-2-fabric.jar",
    "mobhealthbar-1-17-1-fabric.jar",
    "mobhealthbar-1-16-5-2.jar",
    "bedrock-miner-1.0.0-for-minecraft-1.18.1.jar",
    "autototem-1.0.jar",
    "TickrateChanger-1.0.14.jar",
    "TickrateChanger-1.0.2.jar",
    "mobhealthbar-forge-1.19.4-2.2.0.jar",
    "bedrock-miner-1.0.1-for+Minecraft-1.19.4.jar",
    "mobhealthbar-forge-1.19-41.0.100-2.1.1.jar",
    
    // Miscellaneous Files
    "Hitbox.jar",
    "Cleaner.exe",
    "celestial.jar",
    "minecraft.jar",
    "nova-api-1.0-SNAPSHOT.jar",
    "Baritone.jar",
    "Baritone-1.16.5.jar",
    "ExcellentClient.jar",
    "Celestial.exe",
    "Celka.exe",
    "Nursultan.exe",
    "Loader.exe",
    "Nurik.exe",
    "Meriada.exe",
    "Launcher.exe",
    "Reach.jar",
    "wild-api-1.0-SNAPSHOT.jar",
    "lampware-api.jar",
    "Deadcode.dll",
    "modid-1.0.jar",
    "Takker.exe",
    "Expensive.exe"
};


        public string[] GetSearchPatterns() => new string[]
        {
            "*.celka", "*.wild", "*.deadcode", "*.nur"
        };

        public void CheckMinecraftTitle()
        {
            string[] cheatNames = GetCheatFolders();  // Reuse the list of cheat names for process title checking

            // Get all java and javaw processes
            Process[] javaProcesses = Process.GetProcessesByName("java");
            Process[] javawProcesses = Process.GetProcessesByName("javaw");

            // Store detection results
            var detections = new List<string>();

            foreach (Process process in javaProcesses.Concat(javawProcesses))
            {
                string title = process.MainWindowTitle;
                string processName = process.ProcessName;

                // Check for cheats in the process title
                detections.AddRange(CheckProcessTitle(title, cheatNames));

                // Check for cheats in the process name
                detections.AddRange(CheckProcessSignature(processName, cheatNames));
            }

            // Report all detections at once
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

            // List of process names to check
            string[] processNames = { "javaw", "java" };

            // Define a list of large patterns to search for
            byte[][] patterns = new byte[][]
            {
                 Encoding.ASCII.GetBytes("oneway.wtf"),
                 Encoding.ASCII.GetBytes("oneway"),
                 Encoding.ASCII.GetBytes("gishcode"),
                 Encoding.ASCII.GetBytes("delta"),
                 Encoding.ASCII.GetBytes("nursultan"),
                 Encoding.ASCII.GetBytes("bushroot"),
                 Encoding.ASCII.GetBytes("arkentoz"),
                 Encoding.ASCII.GetBytes("deadcode"),
                 Encoding.ASCII.GetBytes("mixware"),
                 Encoding.ASCII.GetBytes("i.gishreloaded"),
                 Encoding.ASCII.GetBytes("wtf.expensive"),
                 Encoding.ASCII.GetBytes("wtf.mixware"),
                 Encoding.ASCII.GetBytes("ru.fals3r"),
                 Encoding.ASCII.GetBytes("ru.arkentoz"),

            };

            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                       //Console.WriteLine($"Scanning process '{processName}.exe' with PID {process.Id}");
                        _logger.Log($"Scanning process '{processName}.exe' with PID {process.Id}");

                        // Scan the process memory for each pattern
                        foreach (var pattern in patterns)
                        {
                            scanner.ScanProcessForPattern(process.Id, pattern, "logs.txt");
                        }
                    }
                }
                else
                {
                   /// Console.WriteLine($"{processName}.exe is not running.");
                    //_logger.Log($"{processName}.exe is not running.");
                }
            }

            // Check Full Minecraft for Potential Titles of Cheats etc
            CheckMinecraftTitle();

            // Check for patterns in JAR files in the specified directories
            CheckJarFilesForPatterns();
        }




        public void CheckJarFilesForPatterns()
        {
            string username = Environment.UserName; // Dynamically get the current user's name
            string[] directoriesToCheck = new string[]
            {
                $@"C:\Users\{username}\AppData\Roaming\.minecraft\mods",
                $@"C:\Users\{username}\AppData\Roaming\.tlauncher\legacy\Minecraft\game\mods"
            };

            foreach (string directory in directoriesToCheck)
            {
                if (Directory.Exists(directory))
                {
                    var jarFiles = Directory.GetFiles(directory, "*.jar"); // Filter files by .jar extension
                    foreach (string jarFile in jarFiles)
                    {
                        CheckJarFile(jarFile);
                    }
                }
            }
        }

        public void CheckJarFile(string jarFilePath)
        {
            // Define the patterns you want to check for
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

            // Define the method patterns you want to search for
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
                "isunhooked"
            };

            // Dictionary to track the presence of each pattern
            var patternPresence = new Dictionary<string, bool>();
            foreach (var pattern in directoryPatterns)
            {
                patternPresence[pattern] = false;
            }

            // Compile regex patterns for method names
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
                        // Iterate over all entries in the JAR file
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Get the full path of the entry
                            string entryPath = entry.FullName;

                            // Check if the entry path matches any of the directory patterns
                            foreach (var pattern in directoryPatterns)
                            {
                                if (entryPath.StartsWith(pattern) && !patternPresence[pattern])
                                {
                                    patternPresence[pattern] = true;
                                    _logger.Log($"[DETECT] Directory structure '{pattern}' found in JAR file: {jarFilePath}");
                                }
                            }

                            // If the entry is a .class file, check its contents
                            if (entryPath.EndsWith(".class"))
                            {
                                using (Stream entryStream = entry.Open())
                                {
                                    using (StreamReader reader = new StreamReader(entryStream))
                                    {
                                        // Read the class file content (simple approach)
                                        string content = reader.ReadToEnd();

                                        // Check for method patterns
                                        foreach (var regex in methodPatterns)
                                        {
                                            if (regex.IsMatch(content))
                                            {
                                                ConsoleColor originalColor = Console.ForegroundColor;

                                                Console.ForegroundColor = ConsoleColor.DarkYellow; 
                                                _logger.Log($"[DETECT] Method matching '{regex}' found in class file: {entryPath} in JAR file: {jarFilePath}");

                                                // Reset the color to the original color
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

            // Output the results for directory patterns that were not found
            foreach (var pattern in directoryPatterns)
            {
                if (!patternPresence[pattern])
                {
                   /// _logger.Log($"[NOT FOUND] Directory structure '{pattern}' not found in JAR file: {jarFilePath}");
                }
            }
        }
   
        }
    }

    

