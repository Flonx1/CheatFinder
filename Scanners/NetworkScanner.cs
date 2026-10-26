using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace CheatFinder_RECODE.Scanners
{
    internal class NetworkScanner
    {
        // Порты, на которых крутятся локальные прокси/бота читов и второй клиент (baritone web, автоматизация)
        private static readonly int[] SuspiciousLocalPorts = { 25565, 25566, 25567, 8080, 8888, 1337, 4444, 5000, 3000 };

        public void scan()
        {
            var javaPids = new HashSet<int>(MinecraftPaths.JavaProcesses.Select(p => p.Pid));

            // Куда подключена сама игра — ищем соединения на localhost (локальный чит-прокси) и слушающие порты java
            try
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();

                foreach (var conn in properties.GetActiveTcpConnections())
                {
                    bool local = IPAddress.IsLoopback(conn.RemoteEndPoint.Address);
                    if (local && conn.State == TcpState.Established)
                    {
                        Program._logger.Log($"[50/50 DETECT] Loopback TCP connection {conn.LocalEndPoint} -> {conn.RemoteEndPoint} — possible local cheat proxy/bot");
                    }
                }

                foreach (var listener in properties.GetActiveTcpListeners())
                {
                    if (SuspiciousLocalPorts.Contains(listener.Port))
                        Program._logger.Log($"[INFO] Local listener on port {listener.Port} ({listener.Address})");
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] Network enumeration failed: {ex.Message}");
            }

            // Внешние соединения java-процесса — через netstat -ano (сопоставление PID)
            ScanJavaConnections(javaPids);
        }

        private void ScanJavaConnections(HashSet<int> javaPids)
        {
            if (javaPids.Count == 0) return;

            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);

                    foreach (var raw in output.Split('\n'))
                    {
                        var parts = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5 || parts[0] != "TCP") continue;

                        int pid;
                        if (!int.TryParse(parts[parts.Length - 1], out pid) || !javaPids.Contains(pid)) continue;
                        if (parts[3] != "ESTABLISHED") continue;

                        string remote = parts[2];
                        if (remote.StartsWith("127.") || remote.StartsWith("[::1]"))
                            Program._logger.Log($"[50/50 DETECT] Java (PID {pid}) connected to localhost {remote} — possible injected proxy");
                        else
                            Program._logger.Log($"[INFO] Java (PID {pid}) connected to {remote}");
                    }
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] netstat failed: {ex.Message}");
            }
        }
    }
}
