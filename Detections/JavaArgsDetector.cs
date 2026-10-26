using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CheatFinder_RECODE.Detections
{
    internal class JavaArgsDetector
    {
        // Аргументы, через которые в игру подгружают сторонний код
        private static readonly string[] SuspiciousArgs =
        {
            "-javaagent:", "-agentpath:", "-agentlib:", "-Xbootclasspath/a:", "-Xbootclasspath/p:",
            "-Dfabric.addMods=", "-Dfabric.loadMods=", "-Dfml.coreMods.load=",
            "-XX:+DisableAttachMechanism", "-XX:-BytecodeVerificationRemote", "-noverify", "-Xverify:none"
        };

        // DLL, которые нормально грузятся в javaw помимо системных и JRE
        private static readonly string[] KnownModulePrefixes =
        {
            "lwjgl", "openal", "glfw", "jemalloc", "jinput", "jna", "discord", "nvoglv", "atio", "ig", "amd", "nvd3d", "nvwgf", "steam", "obs-"
        };

        public void check()
        {
            var list = List.List.Instance;
            var nameMatcher = new PatternMatcher();
            nameMatcher.AddRange(list.CheatNames);
            nameMatcher.AddRange(list.CheatFiles);

            if (MinecraftPaths.JavaProcesses.Count == 0)
            {
                Program._logger.Log("[INFO] No java process found.");
                return;
            }

            foreach (var proc in MinecraftPaths.JavaProcesses)
            {
                Program._logger.Log($"[SCAN] {proc.Name} (PID: {proc.Pid}) parent: {ParentName(proc.ParentPid)} | {proc.ExecutablePath}");
                CheckCommandLine(proc, nameMatcher);
                CheckModules(proc);
            }
        }

        private void CheckCommandLine(JavaProcessInfo proc, PatternMatcher nameMatcher)
        {
            string cmd = proc.CommandLine;
            if (string.IsNullOrEmpty(cmd))
            {
                Program._logger.Log($"[ERROR] Can't read command line of PID {proc.Pid}. Run as Administrator.");
                return;
            }

            // Аргументы с токеном доступа не логируем
            Program._logger.Log($"[INFO] Command line: {Regex.Replace(cmd, "(--accessToken|--uuid|--xuid|--clientId)\\s+\\S+", "$1 ***")}");

            foreach (var arg in SuspiciousArgs)
            {
                int index = cmd.IndexOf(arg, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    string value = ReadValue(cmd, index + arg.Length);
                    string message = $"JVM argument {arg}{value} (PID: {proc.Pid})";

                    // Агент, файла которого уже нет — его удалили после запуска
                    string file = value.Split('=')[0];
                    bool deleted = file.Length > 3 && (file.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) && !File.Exists(file);

                    if (deleted || nameMatcher.Search(value).Count > 0)
                        Program._logger.Log($"[Detect] {message}{(deleted ? " — file deleted after launch" : "")}");
                    else
                        Program._logger.Log($"[50/50 DETECT] {message}");

                    index = cmd.IndexOf(arg, index + arg.Length, StringComparison.OrdinalIgnoreCase);
                }
            }

            foreach (var hit in nameMatcher.Search(cmd).Keys)
                Program._logger.Log($"[FIND] Cheat name in java command line (PID: {proc.Pid}): {nameMatcher.GetName(hit)}");

            CheckClasspath(proc, cmd);
        }

        // jar в classpath, лежащие вне libraries/versions — подсунутые в запуск
        private void CheckClasspath(JavaProcessInfo proc, string cmd)
        {
            var match = Regex.Match(cmd, "(?:-cp|-classpath|--class-path)\\s+(\"[^\"]+\"|\\S+)");
            if (!match.Success) return;

            foreach (var entry in match.Groups[1].Value.Trim('"').Split(';'))
            {
                string path = entry.Trim();
                if (path.Length == 0) continue;

                bool normal = path.IndexOf("\\libraries\\", StringComparison.OrdinalIgnoreCase) >= 0
                              || path.IndexOf("/libraries/", StringComparison.OrdinalIgnoreCase) >= 0
                              || path.IndexOf("\\versions\\", StringComparison.OrdinalIgnoreCase) >= 0
                              || path.IndexOf("/versions/", StringComparison.OrdinalIgnoreCase) >= 0;
                if (normal) continue;

                string state = File.Exists(path) || Directory.Exists(path) ? "" : " (missing)";
                Program._logger.Log($"[50/50 DETECT] Unusual classpath entry (PID: {proc.Pid}): {path}{state}");
            }
        }

        private void CheckModules(JavaProcessInfo proc)
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string javaHome = Path.GetDirectoryName(Path.GetDirectoryName(proc.ExecutablePath ?? "") ?? "") ?? "";

            try
            {
                using (var process = Process.GetProcessById(proc.Pid))
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        string path = module.FileName ?? "";
                        if (path.StartsWith(windows, StringComparison.OrdinalIgnoreCase)) continue;
                        if (javaHome.Length > 3 && path.StartsWith(javaHome, StringComparison.OrdinalIgnoreCase)) continue;

                        string file = Path.GetFileName(path).ToLowerInvariant();
                        if (KnownModulePrefixes.Any(p => file.StartsWith(p))) continue;
                        if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                        Program._logger.Log($"[50/50 DETECT] Unknown DLL loaded in {proc.Name} (PID: {proc.Pid}): {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] Can't list modules of PID {proc.Pid}: {ex.Message}");
            }
        }

        private static string ReadValue(string cmd, int start)
        {
            if (start >= cmd.Length) return "";
            if (cmd[start] == '"')
            {
                int close = cmd.IndexOf('"', start + 1);
                return close > start ? cmd.Substring(start + 1, close - start - 1) : cmd.Substring(start + 1);
            }
            int end = cmd.IndexOf(' ', start);
            return end > start ? cmd.Substring(start, end - start) : cmd.Substring(start);
        }

        private static string ParentName(int pid)
        {
            try
            {
                using (var parent = Process.GetProcessById(pid))
                {
                    return $"{parent.ProcessName} ({pid})";
                }
            }
            catch
            {
                return $"exited ({pid})";
            }
        }
    }
}
