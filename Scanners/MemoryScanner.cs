using CheatFinder_RECODE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CheatFinder_RECODE.Scanners
{
    internal class MemoryScanner
    {
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_PRIVATE = 0x20000;
        private const uint PAGE_GUARD = 0x100;
        private const uint PAGE_NOACCESS = 0x01;

        private const int ChunkSize = 16 * 1024 * 1024;
        private const int Overlap = 512;
        private const int MaxContextsPerSignature = 3;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

        // RegionSize = SIZE_T, поэтому IntPtr: с uint на x64 все поля после него читались со сдвигом
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private struct Chunk
        {
            public long Address;
            public int Size;
        }

        public void scan()
        {
            var list = List.List.Instance;

            var matcher = new PatternMatcher();
            matcher.AddRange(list.Signatures, ignoreCase: true, utf16: true);
            matcher.AddRange(list.StrongSignatures, ignoreCase: false, utf16: true);

            var processes = MinecraftPaths.JavaProcesses;
            if (processes.Count == 0)
            {
                Program._logger.Log("[INFO] No java process found.");
                return;
            }

            foreach (var process in processes)
            {
                Program._logger.Log($"[SCAN] Scanning memory of {process.Name} (PID: {process.Pid})");
                ScanProcess(process, matcher);
            }
        }

        private static bool IsReadable(uint protect)
        {
            if ((protect & PAGE_GUARD) != 0 || (protect & PAGE_NOACCESS) != 0) return false;
            return (protect & 0xEE) != 0; // READONLY | READWRITE | WRITECOPY | EXECUTE_READ | EXECUTE_READWRITE | EXECUTE_WRITECOPY
        }

        private static bool IsExecutable(uint protect) => (protect & 0xF0) != 0;

        private void ScanProcess(JavaProcessInfo process, PatternMatcher matcher)
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, process.Pid);
            if (handle == IntPtr.Zero)
            {
                Program._logger.Log($"[ERROR] Failed to open process (PID: {process.Pid}). Run as Administrator.");
                return;
            }

            try
            {
                var chunks = new List<Chunk>();
                var peRegions = new List<long>();
                CollectChunks(handle, chunks, peRegions);

                foreach (var address in peRegions)
                {
                    Program._logger.Log($"[50/50 DETECT] PE image in private executable memory of {process.Name} (PID: {process.Pid}) at 0x{address:X} — possible injected DLL");
                }

                var hits = new Dictionary<int, List<string>>();
                var hitsLock = new object();

                Parallel.ForEach(
                    chunks,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    () => new byte[ChunkSize + Overlap],
                    (chunk, state, buffer) =>
                    {
                        IntPtr read;
                        if (!ReadProcessMemory(handle, new IntPtr(chunk.Address), buffer, new IntPtr(chunk.Size), out read))
                            return buffer;

                        int length = (int)read.ToInt64();
                        var found = matcher.Search(buffer, length);
                        if (found.Count == 0) return buffer;

                        lock (hitsLock)
                        {
                            foreach (var pair in found)
                            {
                                List<string> contexts;
                                if (!hits.TryGetValue(pair.Key, out contexts))
                                {
                                    contexts = new List<string>();
                                    hits[pair.Key] = contexts;
                                }
                                if (contexts.Count < MaxContextsPerSignature)
                                    contexts.Add(PatternMatcher.Context(buffer, length, pair.Value));
                            }
                        }
                        return buffer;
                    },
                    _ => { });

                foreach (var pair in hits.OrderBy(p => matcher.GetName(p.Key)))
                {
                    Program._logger.Log($"[FIND] Process: {process.Name} (PID: {process.Pid}) | Matched signature: {matcher.GetName(pair.Key)}");
                    foreach (var context in pair.Value)
                        Program._logger.Log($"[MEMORY] {context}");
                }

                long totalMb = chunks.Sum(c => (long)c.Size) / (1024 * 1024);
                Program._logger.Log($"[INFO] {process.Name} (PID: {process.Pid}) - scanned {totalMb} MB, signatures found: {hits.Count}");
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] {process.Name}: {ex.Message}");
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        // Делим все читаемые регионы на куски по 16 МБ с перекрытием, чтобы сигнатура на стыке не терялась
        private void CollectChunks(IntPtr handle, List<Chunk> chunks, List<long> peRegions)
        {
            long address = 0;
            var header = new byte[2];
            int mbiSize = Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));

            while (true)
            {
                MEMORY_BASIC_INFORMATION mbi;
                if (VirtualQueryEx(handle, new IntPtr(address), out mbi, new IntPtr(mbiSize)) == IntPtr.Zero)
                    break;

                long baseAddress = mbi.BaseAddress.ToInt64();
                long regionSize = mbi.RegionSize.ToInt64();
                if (regionSize <= 0) break;

                if (mbi.State == MEM_COMMIT && IsReadable(mbi.Protect))
                {
                    // Обычные DLL грузятся как MEM_IMAGE; PE-заголовок в приватной исполняемой памяти — признак ручного маппинга
                    if (mbi.Type == MEM_PRIVATE && IsExecutable(mbi.Protect) && baseAddress == mbi.AllocationBase.ToInt64())
                    {
                        IntPtr read;
                        if (ReadProcessMemory(handle, mbi.BaseAddress, header, new IntPtr(2), out read) && header[0] == 'M' && header[1] == 'Z')
                            peRegions.Add(baseAddress);
                    }

                    for (long offset = 0; offset < regionSize; offset += ChunkSize)
                    {
                        long size = Math.Min(ChunkSize + Overlap, regionSize - offset);
                        chunks.Add(new Chunk { Address = baseAddress + offset, Size = (int)size });
                    }
                }

                long next = baseAddress + regionSize;
                if (next <= address) break;
                address = next;
            }
        }
    }
}
