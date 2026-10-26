using Microsoft.Win32;
using System;
using System.IO;

namespace CheatFinder_RECODE.Detections
{
    internal class LaunchersDetector
    {
        private struct LoaderSig
        {
            public string Name;
            public string[] Paths;
            public string[] RegKeys;
        }

        public void check()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var loaders = new[]
            {
                new LoaderSig
                {
                    Name = "NTFLoader (Nursultan/Expensive/Celestial)",
                    Paths = new[]
                    {
                        Path.Combine(local, "NTFLoader"),
                        Path.Combine(local, "NTFLoader", "Nursultan"),
                        Path.Combine(local, "NTFLoader", "Expensive"),
                        Path.Combine(local, "NTFLoader", "Celestial"),
                    },
                    RegKeys = new[]
                    {
                        @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_nursultan",
                        @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_expensive",
                        @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_celestial",
                    }
                },
                new LoaderSig { Name = "Nursultan", Paths = new[] { Path.Combine(roaming, "Nursultan"), Path.Combine(local, "Nursultan") }, RegKeys = new string[0] },
                new LoaderSig { Name = "Expensive", Paths = new[] { Path.Combine(roaming, "Expensive"), Path.Combine(local, "Expensive") }, RegKeys = new string[0] },
                new LoaderSig { Name = "Celestial", Paths = new[] { Path.Combine(roaming, ".celestial"), Path.Combine(local, "Celestial") }, RegKeys = new string[0] },
                new LoaderSig { Name = "Rockstar", Paths = new[] { Path.Combine(roaming, "Rockstar"), Path.Combine(local, "Rockstar") }, RegKeys = new string[0] },
            };

            foreach (var loader in loaders)
            {
                foreach (var path in loader.Paths)
                {
                    // Исправлено: раньше условие срабатывало, когда папки НЕ существовало (ложный детект при каждом запуске)
                    if (Directory.Exists(path))
                        Program._logger.Log($"[Detect] {loader.Name} loader folder: {path}");
                }

                foreach (var reg in loader.RegKeys)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(reg))
                        {
                            if (key != null)
                                Program._logger.Log($"[Detect] {loader.Name} registry key: HKCU\\{reg}");
                        }
                    }
                    catch { }
                }
            }

            CheckWebViewProfiles(local);
        }

        // Лоадеры на CEF/WebView2 оставляют профиль с именем клиента в EBWebView\<profile>
        private void CheckWebViewProfiles(string local)
        {
            try
            {
                var list = List.List.Instance;
                foreach (var baseDir in new[] { local, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) })
                {
                    if (!Directory.Exists(baseDir)) continue;
                    foreach (var dir in Directory.GetDirectories(baseDir))
                    {
                        string webView = Path.Combine(dir, "EBWebView");
                        if (!Directory.Exists(webView)) continue;

                        foreach (var profile in Directory.GetDirectories(webView))
                        {
                            string profileName = Path.GetFileName(profile);
                            foreach (var cheat in list.CheatNames)
                            {
                                if (!string.IsNullOrEmpty(cheat) && profileName.IndexOf(cheat, StringComparison.OrdinalIgnoreCase) >= 0)
                                    Program._logger.Log($"[Detect] WebView2 profile of cheat loader: {profile}");
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
