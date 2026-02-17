using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheatFinder_RECODE.Detections
{
    internal class LaunchersDetector
    {
        string APPDATA = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public void check()
        {
            string[] paths = {
                APPDATA + "/NTFLoader",
                APPDATA + "/NTFLoader/Nursultan",
                APPDATA + "/NTFLoader/Expensive",
                APPDATA + "/NTFLoader/Celestial"
            };

            string[] regPaths = {
                @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_nursultan",
                @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_expensive",
                @"SOFTWARE\Microsoft\EdgeWebView\PreferenceMACs\WV2Profile_celestial"
            };

            if (paths.Any(p => !Directory.Exists(p)) ||
                regPaths.Any(r => Registry.CurrentUser.OpenSubKey(r) != null))
            {
                Program._logger.Log("[Detect] NTFLoader!!");
            }
        }
    }
}
