using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace CheatFinder_RECODE.Utils
{
    internal class JavaProcessInfo
    {
        public int Pid;
        public int ParentPid;
        public string Name;
        public string ExecutablePath;
        public string CommandLine;
    }

    internal static class MinecraftPaths
    {
        private static readonly Lazy<List<JavaProcessInfo>> _javaProcesses = new Lazy<List<JavaProcessInfo>>(LoadJavaProcesses);
        private static readonly Lazy<List<string>> _gameDirs = new Lazy<List<string>>(FindGameDirs);

        // Все java-процессы с командной строкой (WMI запрашивается один раз)
        public static List<JavaProcessInfo> JavaProcesses => _javaProcesses.Value;

        // Все найденные папки игры (.minecraft и папки инстансов разных лаунчеров)
        public static List<string> GameDirs => _gameDirs.Value;

        private static List<JavaProcessInfo> LoadJavaProcesses()
        {
            var result = new List<JavaProcessInfo>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, ParentProcessId, Name, ExecutablePath, CommandLine FROM Win32_Process"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        string cmd = obj["CommandLine"]?.ToString() ?? "";
                        string exe = obj["ExecutablePath"]?.ToString() ?? "";

                        // Переименованный java.exe тоже ловим — по аргументам JVM
                        bool isJava = name.StartsWith("java", StringComparison.OrdinalIgnoreCase)
                                      || cmd.IndexOf("-Djava.library.path", StringComparison.OrdinalIgnoreCase) >= 0
                                      || cmd.IndexOf("net.minecraft", StringComparison.OrdinalIgnoreCase) >= 0
                                      || cmd.IndexOf("net.fabricmc", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!isJava) continue;

                        result.Add(new JavaProcessInfo
                        {
                            Pid = Convert.ToInt32(obj["ProcessId"]),
                            ParentPid = Convert.ToInt32(obj["ParentProcessId"]),
                            Name = name,
                            ExecutablePath = exe,
                            CommandLine = cmd
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] WMI process query failed: {ex.Message}");
            }
            return result;
        }

        private static List<string> FindGameDirs()
        {
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // 1. Папки из запущенной игры — самый надёжный источник
            foreach (var proc in JavaProcesses)
            {
                var match = Regex.Match(proc.CommandLine, "--gameDir\\s+(\"[^\"]+\"|\\S+)");
                if (match.Success) dirs.Add(match.Groups[1].Value.Trim('"'));
            }

            // 2. Стандартные папки
            dirs.Add(Path.Combine(appdata, ".minecraft"));
            dirs.Add(Path.Combine(appdata, ".tlauncher", "legacy", "Minecraft", "game"));
            dirs.Add(Path.Combine(appdata, ".feather"));
            dirs.Add(Path.Combine(user, ".lunarclient", "offline", "multiver"));

            // 3. Инстансы лаунчеров (каждый инстанс — отдельная игровая папка)
            string[] instanceRoots =
            {
                Path.Combine(appdata, "PrismLauncher", "instances"),
                Path.Combine(appdata, "PolyMC", "instances"),
                Path.Combine(appdata, "MultiMC", "instances"),
                Path.Combine(appdata, "ATLauncher", "instances"),
                Path.Combine(appdata, "gdlauncher_next", "instances"),
                Path.Combine(appdata, "gdlauncher_carbon", "data", "instances"),
                Path.Combine(appdata, "com.modrinth.theseus", "profiles"),
                Path.Combine(appdata, "ModrinthApp", "profiles"),
                Path.Combine(user, "curseforge", "minecraft", "Instances"),
            };

            foreach (var root in instanceRoots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var instance in Directory.GetDirectories(root))
                    {
                        // Prism/MultiMC держат игру в подпапке .minecraft или minecraft
                        string nested = new[] { ".minecraft", "minecraft" }
                            .Select(n => Path.Combine(instance, n))
                            .FirstOrDefault(Directory.Exists);
                        dirs.Add(nested ?? instance);
                    }
                }
                catch { }
            }

            return dirs.Where(Directory.Exists).ToList();
        }
    }
}
