using CheatFinder_RECODE.Utils;
using System;
using System.IO;
using System.Text;

namespace CheatFinder_RECODE.Detections
{
    // Удаление чита перед проверкой — классика. Файл до очистки корзины лежит в $Recycle.Bin:
    // $R<id> — сами данные, $I<id> — исходный путь и время удаления.
    internal class RecycleBinScanner
    {
        public void check()
        {
            var list = List.List.Instance;
            var matcher = new PatternMatcher();
            matcher.AddRange(list.CheatNames);
            matcher.AddRange(list.CheatFiles);

            DateTime since = Program.mode == "full" ? DateTime.MinValue : DateTime.Now.AddDays(-3);

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                string recycle = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                if (!Directory.Exists(recycle)) continue;

                try
                {
                    foreach (var sid in Directory.GetDirectories(recycle))
                        ScanSid(sid, matcher, since);
                }
                catch { }
            }
        }

        private void ScanSid(string sidDir, PatternMatcher matcher, DateTime since)
        {
            try
            {
                foreach (var meta in Directory.GetFiles(sidDir, "$I*"))
                {
                    try
                    {
                        var info = new FileInfo(meta);
                        if (info.LastWriteTime < since) continue;

                        string originalPath = ReadOriginalPath(meta, out DateTime deletedTime);
                        if (originalPath == null) continue;

                        string fileName = Path.GetFileName(originalPath);
                        foreach (var id in matcher.Search(fileName).Keys)
                        {
                            Program._logger.Log($"[Detect] Recently deleted file matching cheat: {originalPath} (matched: {matcher.GetName(id)})");
                            Program._logger.Log($"[INFO] Deleted at: {deletedTime:dd-MM-yyyy HH:mm:ss}");
                        }

                        string ext = Path.GetExtension(fileName);
                        if (ext.Equals(".jar", StringComparison.OrdinalIgnoreCase) || ext.Equals(".dll", StringComparison.OrdinalIgnoreCase))
                            Program._logger.Log($"[50/50 DETECT] Recently deleted {ext}: {originalPath} (deleted {deletedTime:dd-MM-yyyy HH:mm:ss})");
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Формат $I: [0..7] версия, [8..15] размер, [16..23] FILETIME удаления, далее UTF-16 путь
        private string ReadOriginalPath(string metaFile, out DateTime deletedTime)
        {
            deletedTime = DateTime.MinValue;
            byte[] data = File.ReadAllBytes(metaFile);
            if (data.Length < 28) return null;

            long version = BitConverter.ToInt64(data, 0);
            long fileTime = BitConverter.ToInt64(data, 16);
            try { deletedTime = DateTime.FromFileTime(fileTime); } catch { }

            int pathOffset;
            int pathLengthChars;
            if (version == 2)
            {
                // Win10+: [24..27] длина пути в символах, затем сам путь
                if (data.Length < 28) return null;
                pathLengthChars = BitConverter.ToInt32(data, 24);
                pathOffset = 28;
            }
            else
            {
                pathOffset = 24;
                pathLengthChars = (data.Length - pathOffset) / 2;
            }

            int available = (data.Length - pathOffset) / 2;
            if (pathLengthChars <= 0 || pathLengthChars > available) pathLengthChars = available;

            string path = Encoding.Unicode.GetString(data, pathOffset, pathLengthChars * 2).TrimEnd('\0');
            return string.IsNullOrEmpty(path) ? null : path;
        }
    }
}
