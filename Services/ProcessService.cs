using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ZapretGUI.Services;

public class ProcessService
{
    private readonly string _zapretFolder;

    public ProcessService(string zapretFolder)
    {
        _zapretFolder = zapretFolder;
    }

    public string WinwsExePath => Path.Combine(_zapretFolder, "bin", "winws.exe");

    public bool IsWinwsRunning()
    {
        var targetPath = WinwsExePath;
        if (!File.Exists(targetPath))
            return false;

        try
        {
            foreach (var process in Process.GetProcessesByName("winws"))
            {
                using (process)
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch (Win32Exception)
                    {
                        // нет доступа к MainModule — пропускаем
                    }
                    catch (InvalidOperationException)
                    {
                        // процесс уже завершился — пропускаем
                    }
                }
            }
        }
        catch
        {
            // что-то пошло не так при перечислении процессов
        }

        return false;
    }

    public void StopWinws()
    {
        var targetPath = WinwsExePath;
        try
        {
            foreach (var process in Process.GetProcessesByName("winws"))
            {
                using (process)
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            process.Kill(true);
                        }
                    }
                    catch (Win32Exception)
                    {
                        // нет доступа / недостаточно прав / системная ошибка — пропускаем
                    }
                    catch (InvalidOperationException)
                    {
                        // процесс уже завершился — пропускаем
                    }
                }
            }
        }
        catch
        {
            // игнорируем общее исключение
        }
    }

    public void StartAuto(string? lastGoodStrategy)
    {
        string? scriptToRun = null;

        if (!string.IsNullOrWhiteSpace(lastGoodStrategy) && File.Exists(lastGoodStrategy))
        {
            scriptToRun = lastGoodStrategy;
        }
        else
        {
            var generalPath = Path.Combine(_zapretFolder, "general.bat");
            if (File.Exists(generalPath))
                scriptToRun = generalPath;
        }

        if (scriptToRun == null)
            throw new FileNotFoundException("Не найден ни LastGoodStrategy, ни general.bat.");

        StartBatchDetached(scriptToRun, keepWindowOpen: false);
    }

    public void StartStrategy(string batchFilePath)
    {
        if (!File.Exists(batchFilePath))
            throw new FileNotFoundException("Файл стратегии не найден.", batchFilePath);

        StartBatchDetached(batchFilePath, keepWindowOpen: false);
    }

    /// <summary>
    /// Запуск стратегии без видимого окна — для автоматической проверки в режиме Авто.
    /// </summary>
    public void StartStrategyHidden(string batchFilePath)
    {
        if (!File.Exists(batchFilePath))
            throw new FileNotFoundException("Файл стратегии не найден.", batchFilePath);

        StartBatchDetached(batchFilePath, keepWindowOpen: false, hidden: true);
    }

    public void OpenService()
    {
        var servicePath = Path.Combine(_zapretFolder, "service.bat");
        if (!File.Exists(servicePath))
            throw new FileNotFoundException("Файл service.bat не найден.", servicePath);

        StartBatchDetached(servicePath, keepWindowOpen: true);
    }

    public IEnumerable<string> GetStrategies()
    {
        try
        {
            var files = Directory.GetFiles(_zapretFolder, "general*.bat", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private void StartBatchDetached(string batchFilePath, bool keepWindowOpen, bool hidden = false)
    {
        var fileName = "cmd.exe";
        var arguments = keepWindowOpen
            ? $"/k \"{Path.GetFileName(batchFilePath)}\""
            : $"/c \"{Path.GetFileName(batchFilePath)}\"";

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = _zapretFolder,
            // Скрытый запуск требует UseShellExecute=false + CreateNoWindow=true,
            // чтобы не мелькали окна cmd при автоподборе стратегии.
            UseShellExecute = !hidden,
            CreateNoWindow = hidden,
            WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
        };

        Process.Start(psi);
    }
}

