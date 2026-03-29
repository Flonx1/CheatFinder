using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CheatFinder_RECODE.Scanners
{
    internal class MemoryScanner
    {
        private const int PROCESS_ALL_ACCESS = 0x001F0FFF;
        private const int PAGE_READWRITE = 0x04;
        private const int PAGE_READONLY = 0x02;
        private const int PAGE_EXECUTE_READWRITE = 0x40;
        private const int PAGE_EXECUTE_READ = 0x20;
        private const int PAGE_WRITECOPY = 0x08;
        private const int PAGE_EXECUTE_WRITECOPY = 0x80;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public void scan()
        {
            var list = new List.List();
            var signatures = list.Signatures;

            string[] targets = { "javaw", "java" };

            foreach (var target in targets)
            {
                var processes = Process.GetProcessesByName(target);

                if (processes.Length == 0)
                {
                    Console.WriteLine($"[INFO] No {target}.exe process found.");
                    continue;
                }

                foreach (var process in processes)
                {
                    Console.WriteLine($"[SCAN] Scanning memory of {process.ProcessName} (PID: {process.Id})");
                    ScanProcess(process, signatures);
                }
            }
        }

        private bool IsReadable(uint protect)
        {
            return protect == PAGE_READWRITE || protect == PAGE_READONLY ||
                   protect == PAGE_EXECUTE_READWRITE || protect == PAGE_EXECUTE_READ ||
                   protect == PAGE_WRITECOPY || protect == PAGE_EXECUTE_WRITECOPY;
        }

        private bool IsPrintable(byte b)
        {
            return b >= 0x20 && b <= 0x7E;
        }

        private void ScanProcess(Process process, string[] signatures)
        {
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
            if (processHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[ERROR] Failed to open process (PID: {process.Id}). Run as Administrator.");
                return;
            }

            try
            {
                IntPtr address = IntPtr.Zero;
                int findCount = 0;

                while (true)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    if (!VirtualQueryEx(processHandle, address, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))))
                        break;

                    if (mbi.RegionSize == 0)
                        break;

                    if (mbi.State == 0x1000 && IsReadable(mbi.Protect))
                    {
                        byte[] buffer;
                        try
                        {
                            buffer = new byte[mbi.RegionSize];
                        }
                        catch (OutOfMemoryException)
                        {
                            address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
                            continue;
                        }

                        int bytesRead;
                        if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, mbi.RegionSize, out bytesRead))
                        {
                            var strings = ExtractStrings(buffer, bytesRead, 4);

                            foreach (var str in strings)
                            {
                                foreach (var sig in signatures)
                                {
                                    if (string.IsNullOrEmpty(sig)) continue;
                                    if (str.IndexOf(sig, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        Program._logger.Log($"[FIND] Process: {process.ProcessName} (PID: {process.Id}) | Matched signature: {sig}");
                                        Program._logger.Log($"[MEMORY] {str}");
                                        Console.WriteLine();
                                        findCount++;
                                    }
                                }
                            }
                        }
                    }

                    address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
                }

                Program._logger.Log($"[INFO] {process.ProcessName} (PID: {process.Id}) - Finds: {findCount}");
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] {process.ProcessName}: {ex.Message}");
            }
            finally
            {
                CloseHandle(processHandle);
            }
        }

        private System.Collections.Generic.List<string> ExtractStrings(byte[] buffer, int length, int minLength)
        {
            var result = new System.Collections.Generic.List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                if (IsPrintable(buffer[i]))
                {
                    sb.Append((char)buffer[i]);
                }
                else
                {
                    if (sb.Length >= minLength)
                    {
                        result.Add(sb.ToString());
                    }
                    sb.Clear();
                }
            }

            if (sb.Length >= minLength)
            {
                result.Add(sb.ToString());
            }

            return result;
        }
    }
}