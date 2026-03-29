using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Security.Principal;
using System.Management;
using System.IO;

namespace CheatFinder_RECODE.Detections
{
    internal class LastConnectedUsbDevices
    {

        public List<UsbDevice> getDevices()
        {
            List<UsbDevice> devices = new List<UsbDevice>();
            List<string> currentlyConnectedSerials = new List<string>();

            if (!IsAdministrator())
            {
                Console.WriteLine("Administrator privileges required for full USB device scan.");
                return devices;
            }

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string deviceId = obj["DeviceID"]?.ToString();
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        currentlyConnectedSerials.Add(deviceId);
                    }
                }
            }
            catch { }

            try
            {
                DateTime timeThreshold = DateTime.Now.AddMinutes(-15);

                string usbStorKey = @"SYSTEM\CurrentControlSet\Enum\USBSTOR";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(usbStorKey, false))
                {
                    if (key != null)
                    {
                        foreach (string deviceName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey deviceKey = key.OpenSubKey(deviceName, false))
                                {
                                    if (deviceKey != null)
                                    {
                                        foreach (string serialNumber in deviceKey.GetSubKeyNames())
                                        {
                                            try
                                            {
                                                using (RegistryKey serialKey = deviceKey.OpenSubKey(serialNumber, false))
                                                {
                                                    if (serialKey != null)
                                                    {
                                                        UsbDevice device = new UsbDevice();
                                                        device.DeviceName = deviceName;
                                                        device.SerialNumber = serialNumber;
                                                        device.FriendlyName = serialKey.GetValue("FriendlyName")?.ToString() ?? "Unknown";
                                                        
                                                        string[] deviceParams = deviceName.Split('&');
                                                        if (deviceParams.Length >= 2)
                                                        {
                                                            device.VendorID = deviceParams[0].Replace("Disk_Ven_", "").Replace("Ven_", "").Trim('_');
                                                            device.ProductID = deviceParams[1].Replace("Prod_", "").Replace("Rev_", "").Trim('_');
                                                        }

                                                        bool isConnected = currentlyConnectedSerials.Any(s => s.Contains(serialNumber));

                                                        try
                                                        {
                                                            DateTime lastWrite = GetRegistryKeyLastWriteTime(serialKey);
                                                            if (lastWrite != DateTime.MinValue)
                                                            {
                                                                device.LastConnected = lastWrite;
                                                            }
                                                        }
                                                        catch { }

                                                        if (!isConnected && device.LastConnected.HasValue)
                                                        {
                                                            if (device.LastConnected.Value >= timeThreshold)
                                                            {
                                                                TimeSpan timeSinceDisconnect = DateTime.Now - device.LastConnected.Value;
                                                                devices.Add(device);
                                                                Console.WriteLine($"Found Recently Disconnected USB: {device.FriendlyName}");
                                                                Console.WriteLine($"  Vendor: {device.VendorID}, Product: {device.ProductID}");
                                                                Console.WriteLine($"  Serial: {device.SerialNumber}");
                                                                Console.WriteLine($"  Last Activity: {device.LastConnected.Value.ToString("dd/MM/yyyy HH:mm:ss")}");
                                                                Console.WriteLine($"  Time Since Disconnect: {timeSinceDisconnect.Minutes} minutes, {timeSinceDisconnect.Seconds} seconds");
                                                                Console.WriteLine($"  Status: Recently Disconnected");
                                                                Console.WriteLine();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                string usbKey = @"SYSTEM\CurrentControlSet\Enum\USB";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(usbKey, false))
                {
                    if (key != null)
                    {
                        foreach (string vidPid in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey vidPidKey = key.OpenSubKey(vidPid, false))
                                {
                                    if (vidPidKey != null)
                                    {
                                        foreach (string instance in vidPidKey.GetSubKeyNames())
                                        {
                                            try
                                            {
                                                using (RegistryKey instanceKey = vidPidKey.OpenSubKey(instance, false))
                                                {
                                                    if (instanceKey != null)
                                                    {
                                                        string friendlyName = instanceKey.GetValue("FriendlyName")?.ToString();
                                                        string deviceDesc = instanceKey.GetValue("DeviceDesc")?.ToString();
                                                        
                                                        string displayName = friendlyName ?? deviceDesc ?? "Unknown Device";
                                                        
                                                        if (!string.IsNullOrEmpty(displayName) && displayName != "Unknown Device" && !displayName.Contains("Root Hub") && !displayName.Contains("@"))
                                                        {
                                                            UsbDevice device = new UsbDevice();
                                                            device.DeviceName = vidPid;
                                                            device.SerialNumber = instance;
                                                            device.FriendlyName = displayName;
                                                            
                                                            if (vidPid.StartsWith("VID_"))
                                                            {
                                                                string[] parts = vidPid.Split('&');
                                                                foreach (string part in parts)
                                                                {
                                                                    if (part.StartsWith("VID_"))
                                                                        device.VendorID = part.Replace("VID_", "");
                                                                    if (part.StartsWith("PID_"))
                                                                        device.ProductID = part.Replace("PID_", "");
                                                                }
                                                            }

                                                            bool isConnected = currentlyConnectedSerials.Any(s => s.Contains(instance));

                                                            try
                                                            {
                                                                DateTime lastWrite = GetRegistryKeyLastWriteTime(instanceKey);
                                                                if (lastWrite != DateTime.MinValue)
                                                                {
                                                                    device.LastConnected = lastWrite;
                                                                }
                                                            }
                                                            catch { }

                                                            if (!isConnected && device.LastConnected.HasValue)
                                                            {
                                                                if (!devices.Any(d => d.SerialNumber == device.SerialNumber && d.FriendlyName == device.FriendlyName))
                                                                {
                                                                    if (device.LastConnected.Value >= timeThreshold)
                                                                    {
                                                                        TimeSpan timeSinceDisconnect = DateTime.Now - device.LastConnected.Value;
                                                                        devices.Add(device);
                                                                        Console.WriteLine($"Found Recently Disconnected USB Device: {device.FriendlyName}");
                                                                        Console.WriteLine($"  VID: {device.VendorID}, PID: {device.ProductID}");
                                                                        Console.WriteLine($"  Serial: {device.SerialNumber}");
                                                                        Console.WriteLine($"  Last Activity: {device.LastConnected.Value.ToString("dd/MM/yyyy HH:mm:ss")}");
                                                                        Console.WriteLine($"  Time Since Disconnect: {timeSinceDisconnect.Minutes} minutes, {timeSinceDisconnect.Seconds} seconds");
                                                                        Console.WriteLine($"  Status: Recently Disconnected");
                                                                        Console.WriteLine();
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                Console.WriteLine($"Total recently disconnected USB devices (last 15 minutes): {devices.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return devices;
        }

        private DateTime GetRegistryKeyLastWriteTime(RegistryKey key)
        {
            try
            {
                Type registryKeyType = typeof(RegistryKey);
                System.Reflection.FieldInfo fieldInfo = registryKeyType.GetField("hkey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (fieldInfo != null)
                {
                    Microsoft.Win32.SafeHandles.SafeRegistryHandle handle = (Microsoft.Win32.SafeHandles.SafeRegistryHandle)fieldInfo.GetValue(key);
                    
                    long lastWriteTime;
                    int result = RegQueryInfoKey(handle.DangerousGetHandle(), null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWriteTime);
                    
                    if (result == 0)
                    {
                        return DateTime.FromFileTime(lastWriteTime);
                    }
                }
            }
            catch { }
            
            return DateTime.MinValue;
        }

        [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int RegQueryInfoKey(
            IntPtr hKey,
            StringBuilder lpClass,
            IntPtr lpcbClass,
            IntPtr lpReserved,
            IntPtr lpcSubKeys,
            IntPtr lpcbMaxSubKeyLen,
            IntPtr lpcbMaxClassLen,
            IntPtr lpcValues,
            IntPtr lpcbMaxValueNameLen,
            IntPtr lpcbMaxValueLen,
            IntPtr lpcbSecurityDescriptor,
            out long lpftLastWriteTime);

        private bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public class UsbDevice
    {
        public string DeviceName { get; set; }
        public string SerialNumber { get; set; }
        public string FriendlyName { get; set; }
        public string VendorID { get; set; }
        public string ProductID { get; set; }
        public DateTime? FirstInstalled { get; set; }
        public DateTime? LastConnected { get; set; }
        public DateTime? LastRemoved { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Device: {FriendlyName}");
            sb.AppendLine($"VID: {VendorID}, PID: {ProductID}");
            sb.AppendLine($"Serial: {SerialNumber}");
            if (FirstInstalled.HasValue)
                sb.AppendLine($"First Installed: {FirstInstalled.Value}");
            if (LastConnected.HasValue)
                sb.AppendLine($"Last Connected: {LastConnected.Value}");
            if (LastRemoved.HasValue)
                sb.AppendLine($"Last Removed: {LastRemoved.Value}");
            return sb.ToString();
        }
    }
}