using System;
using System.IO;

namespace CheatFinder_RECODE.Detections
{
    internal class SizeAnalyzer
    {
        public void check()
        {
            long limit = 60 * 1024 * 1024; 

            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string[] paths = new string[]
            {
                Path.Combine(appdata, ".minecraft", "versions"),
                Path.Combine(appdata, ".tlauncher", "legacy", "Minecraft", "game", "versions")
            };

            foreach (var path in paths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    string[] files = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        try
                        {
                            FileInfo info = new FileInfo(file);
                            if (info.Length > limit)
                            {
                                double mb = info.Length / (1024.0 * 1024.0);
                                Program._logger.Log($"[50/50 DETECT] Large Jar File: {file} (Size: {mb:F2} MB)");
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}