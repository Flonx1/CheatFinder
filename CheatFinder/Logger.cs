using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

public class Logger
{
    private readonly string _logFilePath;
    private readonly Timer _consoleLogTimer;
    private readonly List<string> _logMessages;
    private readonly object _lock;

    public Logger(string logFilePath, double logIntervalMilliseconds = 2000)
    {
        _logFilePath = logFilePath;
        _logMessages = new List<string>();
        _lock = new object();

        // Initialize and start the timer
        _consoleLogTimer = new Timer(logIntervalMilliseconds);
        _consoleLogTimer.Elapsed += OnTimedEvent;
        _consoleLogTimer.AutoReset = true;
        _consoleLogTimer.Enabled = true;
    }

    public void Log(string message)
    {
        lock (_lock)
        {
            // Append log messages to the file
            using (StreamWriter sw = new StreamWriter(_logFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now}: {message}");
            }
            // Add log message to the in-memory list
            _logMessages.Add($"{DateTime.Now}: {message}");
        }
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            // Output all collected log messages to the console
            foreach (var message in _logMessages)
            {
                Console.WriteLine(message);
            }
            // Clear the list after outputting
            _logMessages.Clear();
        }
    }
}
