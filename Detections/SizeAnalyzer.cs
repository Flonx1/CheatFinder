using CheatFinder_RECODE.Utils;
using System;
using System.IO;

namespace CheatFinder_RECODE.Detections
{
    internal class SizeAnalyzer
    {
        // Ванильный клиент 1.16.5 ~13 МБ, 1.21.4 ~27 МБ. Больше ~60 МБ = в jar вшит клиент-чит
        private const long JarLimit = 60L * 1024 * 1024;

        public void check()
        {
            foreach (var gameDir in MinecraftPaths.GameDirs)
            {
                string versions = Path.Combine(gameDir, "versions");
                if (!Directory.Exists(versions)) continue;

                try
                {
                    foreach (var versionDir in Directory.GetDirectories(versions))
                    {
                        string name = Path.GetFileName(versionDir);
                        string jar = Path.Combine(versionDir, name + ".jar");
                        if (!File.Exists(jar)) continue;

                        try
                        {
                            var info = new FileInfo(jar);
                            if (info.Length > JarLimit)
                            {
                                double mb = info.Length / (1024.0 * 1024.0);
                                Program._logger.Log($"[50/50 DETECT] Large client jar: {jar} (Size: {mb:F2} MB)");
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
    }
}
