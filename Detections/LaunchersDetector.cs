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
            if (!Directory.Exists(APPDATA + "/NTFLoader") || !Directory.Exists(APPDATA + "/NTFLoader/Nursultan") || 
                !Directory.Exists(APPDATA + "/NTFLoader/Expensive") || !Directory.Exists(APPDATA + "/NTFLoader/Celestial"))
            {
                Program._logger.Log("[Detect] NTFLoader.");
            }
        }
    }
}
