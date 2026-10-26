using System;
using System.IO;

namespace CheatFinder_RECODE.Utils
{
    public static class LogUtils
    {
        public static void RedirectConsoleOutput(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Create);
            Console.SetOut(new StreamWriter(fileStream) { AutoFlush = true });
        }
    }
}
