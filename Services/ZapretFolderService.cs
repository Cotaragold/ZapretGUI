using System;
using System.IO;
using System.Text.Json;
using ZapretGUI.Models;

namespace ZapretGUI.Services;

public class ZapretFolderService
{
    private readonly string _appSettingsPath;
    private AppSettings _settings;

    public ZapretFolderService()
    {
        _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = LoadSettings();
    }

    public string ZapretFolder
    {
        get => _settings.ZapretFolder;
        private set => _settings.ZapretFolder = value;
    }

    public string? LastGoodStrategy
    {
        get => _settings.LastGoodStrategy;
        set
        {
            _settings.LastGoodStrategy = value;
            SaveSettings();
        }
    }

    // Настройки авто-проверки стратегий (только чтение, редактируются в appsettings.json)
    public System.Collections.Generic.IReadOnlyList<string>? TestHosts => _settings.TestHosts;

    public int TestWarmupMs => _settings.TestWarmupMs;

    public int TestConnectTimeoutMs => _settings.TestConnectTimeoutMs;

    public System.Collections.Generic.IReadOnlyList<string>? IpListUrls => _settings.IpListUrls;

    public bool AutoUpdateIpOnStartup => _settings.AutoUpdateIpOnStartup;

    public bool IsValidZapretFolder(string folder)
    {
        return File.Exists(Path.Combine(folder, "general.bat")) &&
               File.Exists(Path.Combine(folder, "service.bat")) &&
               Directory.Exists(Path.Combine(folder, "lists"));
    }

    public string? ResolveFolder(string? configuredPath, string baseDir)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) &&
            Directory.Exists(configuredPath) &&
            IsValidZapretFolder(configuredPath))
        {
            return configuredPath;
        }

        if (!string.IsNullOrWhiteSpace(baseDir) &&
            Directory.Exists(baseDir) &&
            IsValidZapretFolder(baseDir))
        {
            return baseDir;
        }

        return null;
    }

    public string ResolveFolder(string baseDir, IFolderPicker picker)
    {
        var baseDirNormalized = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolved = ResolveFolder(ZapretFolder, baseDirNormalized);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            SetZapretFolder(resolved);
            return resolved;
        }

        if (!picker.TryPickFolder(out var selected) || string.IsNullOrWhiteSpace(selected))
            throw new OperationCanceledException("Пользователь отменил выбор папки.");

        // Валидацию и сообщения об ошибках делает слой UI (App).
        return selected;
    }

    public void SetZapretFolder(string folder)
    {
        ZapretFolder = folder;
        SaveSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_appSettingsPath))
            {
                var json = File.ReadAllText(_appSettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // игнорируем, используем значения по умолчанию
        }

        return new AppSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_appSettingsPath, json);
        }
        catch
        {
            // если не удалось сохранить — не критично
        }
    }
}




