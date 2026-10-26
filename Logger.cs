using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class Logger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();

    // Пока сканер работает в отдельном потоке, его строки копятся в буфере и выводятся одним блоком
    private readonly ThreadLocal<List<string>> _buffer = new ThreadLocal<List<string>>();

    public int Detects { get; private set; }
    public int Finds { get; private set; }
    public int Suspicious { get; private set; }
    public List<string> Highlights { get; } = new List<string>();

    public Logger(string logFilePath)
    {
        _logFilePath = logFilePath;
        File.WriteAllText(_logFilePath, $"--- Log Started {DateTime.Now} ---\n");
    }

    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string finalMessage = $"[{timestamp}] {message}";

        lock (_lock)
        {
            Count(message);
        }

        if (_buffer.Value != null)
        {
            _buffer.Value.Add(finalMessage);
            return;
        }

        Write(new[] { finalMessage });
    }

    public void BeginBuffer()
    {
        _buffer.Value = new List<string>();
    }

    public void FlushBuffer()
    {
        var lines = _buffer.Value;
        _buffer.Value = null;
        if (lines != null && lines.Count > 0) Write(lines);
    }

    private void Count(string message)
    {
        if (message.StartsWith("[Detect]", StringComparison.OrdinalIgnoreCase))
        {
            Detects++;
            Highlights.Add(message);
        }
        else if (message.StartsWith("[50/50", StringComparison.OrdinalIgnoreCase))
        {
            Suspicious++;
            Highlights.Add(message);
        }
        else if (message.StartsWith("[FIND]", StringComparison.OrdinalIgnoreCase))
        {
            Finds++;
            Highlights.Add(message);
        }
    }

    private void Write(IEnumerable<string> lines)
    {
        lock (_lock)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_logFilePath, true))
                {
                    foreach (var line in lines)
                    {
                        Console.ForegroundColor = ColorFor(line);
                        Console.WriteLine(line);
                        sw.WriteLine(line);
                    }
                    Console.ResetColor();
                }
            }
            catch
            {
            }
        }
    }

    private static ConsoleColor ColorFor(string line)
    {
        if (line.Contains("] [Detect]")) return ConsoleColor.Red;
        if (line.Contains("] [FIND]")) return ConsoleColor.Red;
        if (line.Contains("] [50/50")) return ConsoleColor.Yellow;
        if (line.Contains("] [ERROR]")) return ConsoleColor.DarkYellow;
        if (line.Contains("] [INFO]")) return ConsoleColor.DarkGray;
        return ConsoleColor.Gray;
    }
}
