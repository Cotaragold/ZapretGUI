using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZapretGUI.Services;

public class LogService
{
    private readonly string _logFilePath;

    public LogService(string zapretFolder)
    {
        _logFilePath = Path.Combine(zapretFolder, "ZapretGUI.log");
    }

    public string LogFilePath => _logFilePath;

    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            File.AppendAllLines(_logFilePath, new[] { line }, new UTF8Encoding(false));
        }
        catch
        {
            // Логирование не должно «ронять» приложение
        }
    }

    public string ReadLastLines(int maxLines)
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return string.Empty;

            var lines = new List<string>();
            using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    lines.Add(sr.ReadLine() ?? string.Empty);
                    if (lines.Count > maxLines * 2)
                    {
                        // периодически чистим начало списка, чтобы не накапливать слишком много
                        lines.RemoveRange(0, maxLines);
                    }
                }
            }

            return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Count - maxLines)));
        }
        catch
        {
            return string.Empty;
        }
    }
}

