using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CheatFinder_RECODE.Detections
{
    // Инструменты, которыми пользуются для инъекции/редактирования памяти и обхода проверок
    internal class ProcessDetector
    {
        private static readonly string[] HackTools =
        {
            "cheatengine", "cheat engine", "processhacker", "process hacker", "x64dbg", "x32dbg", "ollydbg",
            "ida", "ida64", "extreme injector", "xenos", "xenos64", "injector", "processexp",
            "artmoney", "scylla", "reclass", "dnspy", "ilspy", "jd-gui", "recaf", "bytecode-viewer"
        };

        // Процессы, которые маскируют/подменяют историю запусков (чистильщики артефактов)
        private static readonly string[] AntiForensics =
        {
            "privazer", "bleachbit", "ccleaner", "wise disk cleaner", "eraser", "wipe"
        };

        public void check()
        {
            Process[] processes;
            try { processes = Process.GetProcesses(); }
            catch (Exception ex) { Program._logger.Log($"[ERROR] Process enumeration failed: {ex.Message}"); return; }

            foreach (var process in processes)
            {
                string name;
                try { name = process.ProcessName.ToLowerInvariant(); }
                catch { continue; }

                foreach (var tool in HackTools)
                {
                    if (name.Contains(tool.Replace(" ", "")) || name.Contains(tool))
                    {
                        Program._logger.Log($"[Detect] Hack/injection tool running: {process.ProcessName} (PID: {process.Id})");
                        LogStart(process);
                        break;
                    }
                }

                foreach (var tool in AntiForensics)
                {
                    if (name.Contains(tool.Replace(" ", "")))
                    {
                        Program._logger.Log($"[50/50 DETECT] Anti-forensic/cleaner tool running: {process.ProcessName} (PID: {process.Id})");
                        break;
                    }
                }
            }

            CheckWindowTitles(processes);
        }

        // Названия читов в заголовках окон (ClickGUI, оверлеи)
        private void CheckWindowTitles(Process[] processes)
        {
            var list = List.List.Instance;
            var matcher = new PatternMatcher();
            matcher.AddRange(list.CheatNames);

            foreach (var process in processes)
            {
                string title;
                try { title = process.MainWindowTitle; }
                catch { continue; }
                if (string.IsNullOrWhiteSpace(title)) continue;

                foreach (var id in matcher.Search(title).Keys)
                    Program._logger.Log($"[FIND] Cheat name in window title: \"{title}\" (process: {SafeName(process)}, matched: {matcher.GetName(id)})");
            }
        }

        private void LogStart(Process process)
        {
            try { Program._logger.Log($"[INFO] Started at: {process.StartTime:dd-MM-yyyy HH:mm:ss}"); }
            catch { }
        }

        private static string SafeName(Process process)
        {
            try { return process.ProcessName; }
            catch { return "?"; }
        }
    }
}
