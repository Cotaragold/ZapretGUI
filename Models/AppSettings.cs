using System.Collections.Generic;

namespace ZapretGUI.Models;

public class AppSettings
{
    public string ZapretFolder { get; set; } = string.Empty;

    public string? LastGoodStrategy { get; set; }

    /// <summary>
    /// Список хостов для проверки стратегий в режиме Авто.
    /// null => используется встроенный набор популярных сайтов.
    /// </summary>
    public List<string>? TestHosts { get; set; }

    /// <summary>Сколько ждать (мс) запуска winws перед проверкой сайтов.</summary>
    public int TestWarmupMs { get; set; } = 4000;

    /// <summary>Таймаут (мс) на TCP+TLS подключение к одному сайту.</summary>
    public int TestConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// URL публичных списков заблокированных IP/подсетей (по одной записи на строку).
    /// null => используется встроенный набор (antifilter.download).
    /// </summary>
    public List<string>? IpListUrls { get; set; }

    /// <summary>Автоматически обновлять ipset-all.txt из этих списков при старте приложения.</summary>
    public bool AutoUpdateIpOnStartup { get; set; } = true;
}
