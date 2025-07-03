using System;
using System.Management;

public class VMDetection
{
    public static bool IsRunningInVM()
    {
        try
        {
            string[] vmVendors = { "VMware", "VirtualBox", "KVM", "Hyper-V", "Parallels" };

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var manufacturer = obj["Manufacturer"]?.ToString();
                    var model = obj["Model"]?.ToString();

                    foreach (var vendor in vmVendors)
                    {
                        if ((manufacturer != null && manufacturer.IndexOf(vendor, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            (model != null && model.IndexOf(vendor, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            
            Console.WriteLine("Error checking VM status: " + ex.Message);
        }

        return false;
    }
}
