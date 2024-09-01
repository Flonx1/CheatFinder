using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class MemoryScanner
{


    private const int PROCESS_ALL_ACCESS = 0x001F0FFF;
    private const int PAGE_READWRITE = 0x04;
    private const int PAGE_READONLY = 0x02;
    private const int PAGE_EXECUTE_READWRITE = 0x40;

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
    private readonly Logger _logger;
    public MemoryScanner(string logFilePath)
    {
        _logger = new Logger(logFilePath);
    }


    public void ScanProcessForPattern(int processId, byte[] pattern, string outputPath)
    {
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            _logger.Log("Failed to open process.");
            return;
        }

        try
        {
            IntPtr address = (IntPtr)0x00000000; // Starting address (0 for this example)
            bool found = false;

            while (true)
            {
                MEMORY_BASIC_INFORMATION mbi;
                if (!VirtualQueryEx(processHandle, address, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))))
                {
                   /// _logger.Log("Failed to query memory.");
                    break;
                }

                if (mbi.RegionSize == 0)
                    break;

                if (mbi.State == 0x1000 && (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_READONLY || mbi.Protect == PAGE_EXECUTE_READWRITE))
                {
                    byte[] buffer = new byte[mbi.RegionSize];
                    int bytesRead;
                    if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, mbi.RegionSize, out bytesRead))
                    {
                        for (int i = 0; i < bytesRead - pattern.Length + 1; i++)
                        {
                            bool match = true;
                            for (int j = 0; j < pattern.Length; j++)
                            {
                                if (buffer[i + j] != pattern[j])
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match)
                            {
                                ///_logger.Log($"Pattern found at address {mbi.BaseAddress + i:X}");
                                found = true;

                                // Read and decode the memory content at the found address
                                byte[] memoryContent = new byte[pattern.Length * 2]; // Read a bit more than the pattern length
                                if (ReadProcessMemory(processHandle, mbi.BaseAddress + i, memoryContent, (uint)memoryContent.Length, out bytesRead))
                                {
                                    string decodedString = Encoding.ASCII.GetString(memoryContent, 0, bytesRead).Trim('\0');
                                    _logger.Log($"[Detect] Memory String: {decodedString}");
                                }
                                break;
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                // Move to the next region
                address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
            }

            if (!found)
            {
               /// _logger.Log("Pattern not found.");
            }
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }
}
