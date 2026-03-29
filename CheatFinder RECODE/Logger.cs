using System;
using System.IO;

public class Logger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();

    public Logger(string logFilePath)
    {
        _logFilePath = logFilePath;
        File.WriteAllText(_logFilePath, $"--- Log Started {DateTime.Now} ---\n");
    }

    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string finalMessage = $"[{timestamp}] {message}";


        Console.WriteLine(finalMessage);

        lock (_lock)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_logFilePath, true))
                {
                    sw.WriteLine(finalMessage);
                }
            }
            catch
            {
            }
        }
    }
}