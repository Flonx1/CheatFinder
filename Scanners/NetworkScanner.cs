using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CheatFinder_RECODE.Scanners
{
    internal class NetworkScanner
    {
        public void scan()
        {
            string mode = Program.mode;
            string localIp = GetLocalIPAddress();

            if (string.IsNullOrEmpty(localIp))
            {
                Console.WriteLine("[ERROR] Could not determine local IP.");
                return;
            }

            string subnet = localIp.Substring(0, localIp.LastIndexOf('.') + 1);
            Program._logger.Log($"[INFO] Local IP: {localIp}");
            Program._logger.Log($"[SCAN] Scanning network: {subnet}0/24");

            List<int> ports = new List<int>();

            
            for (int p = 25565; p <= 25800; p++)
            {
                ports.Add(p);
            }
                


            List<Task> tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                string targetIp = subnet + i;

                foreach (int port in ports)
                {
                    tasks.Add(ScanPort(targetIp, port));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        private async Task ScanPort(string ip, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(500);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && client.Connected)
                    {
                        Program._logger.Log($"[FIND] Local Server Found: {ip}:{port}");
                    }
                }
            }
            catch
            {
            }
        }

        private string GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}