using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CheatFinder_RECODE.List
{
    internal class List
    {

        public string[] CheatFiles = new WebClient()
            .DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/CheatFiles")
            .Split(',')
            .Select(x => x.Trim())
            .ToArray();

        public string[] CheatNames = new WebClient()
             .DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/CheatNames")
             .Split(',')
             .Select(x => x.Trim())
             .ToArray();

        public string[] CheatExtension = new WebClient()
             .DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/CheatExtension")
             .Split(',')
             .Select(x => x.Trim())
             .ToArray();

        public string[] Signatures = new WebClient()
             .DownloadString("https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/Signatures")
             .Split(',')
             .Select(x => x.Trim())
             .ToArray();
    }
}
