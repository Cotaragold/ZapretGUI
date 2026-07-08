using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace ZapretGUI.Services;

/// <summary>Результат проверки одной стратегии.</summary>
public class StrategyTestResult
{
    public string StrategyPath { get; init; } = string.Empty;

    public string FileName => Path.GetFileName(StrategyPath);

    /// <summary>Запустился ли winws.exe для этой стратегии.</summary>
    public bool Started { get; set; }

    /// <summary>Сколько тестовых сайтов открылось.</summary>
    public int Reachable { get; set; }

    /// <summary>Всего проверяемых сайтов.</summary>
    public int Total { get; set; }

    /// <summary>Суммарная задержка по успешным сайтам (для тай-брейка).</summary>
    public long TotalLatencyMs { get; set; }

    public Dictionary<string, bool> PerHost { get; } = new(StringComparer.OrdinalIgnoreCase);

    public double AvgLatencyMs => Reachable > 0 ? TotalLatencyMs / (double)Reachable : double.MaxValue;

    public string Summary => Started
        ? $"{Reachable}/{Total}" + (Reachable > 0 ? $" ({AvgLatencyMs:F0} мс)" : string.Empty)
        : "не запустилась";
}

/// <summary>
/// Автоматический подбор рабочей стратегии: по очереди запускает каждый general*.bat,
/// проверяет доступность популярных заблокированных сайтов (TCP + TLS-handshake с SNI),
/// считает сколько открылось и выбирает лучшую.
/// </summary>
public class StrategyTestService
{
    private readonly ProcessService _process;
    private readonly LogService _log;
    private readonly IReadOnlyList<string> _hosts;
    private readonly int _warmupMs;
    private readonly int _connectTimeoutMs;

    // Популярные сайты, которые в РФ обычно режет DPI. TLS-handshake с этим SNI —
    // именно то, что инспектирует DPI, поэтому успешный handshake = обход работает.
    private static readonly string[] DefaultHosts =
    {
        "www.youtube.com",
        "youtube.com",
        "rutracker.org",
        "discord.com",
        "www.instagram.com",
        "instagram.com",
        "x.com",
        "www.facebook.com",
    };

    public StrategyTestService(
        ProcessService process,
        LogService log,
        IReadOnlyList<string>? hosts,
        int warmupMs,
        int connectTimeoutMs)
    {
        _process = process;
        _log = log;
        _hosts = (hosts != null && hosts.Count > 0)
            ? hosts.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).ToList()
            : DefaultHosts;
        _warmupMs = warmupMs > 0 ? warmupMs : 4000;
        _connectTimeoutMs = connectTimeoutMs > 0 ? connectTimeoutMs : 5000;
    }

    public int HostCount => _hosts.Count;

    /// <summary>
    /// Проверяет все стратегии по очереди и возвращает их отсортированными: лучшая первой.
    /// </summary>
    public async Task<IReadOnlyList<StrategyTestResult>> TestAllAsync(
        IReadOnlyList<string> strategies,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        var results = new List<StrategyTestResult>();

        for (int i = 0; i < strategies.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var strategy = strategies[i];
            progress?.Report($"Проверка {i + 1}/{strategies.Count}: {Path.GetFileName(strategy)}");

            var result = await TestStrategyAsync(strategy, ct);
            results.Add(result);

            _log.Info($"Стратегия {result.FileName}: {result.Summary}");
        }

        // Сначала по числу открытых сайтов, при равенстве — по меньшей средней задержке.
        return results
            .OrderByDescending(r => r.Reachable)
            .ThenBy(r => r.AvgLatencyMs)
            .ToList();
    }

    /// <summary>
    /// Запускает одну стратегию скрыто, ждёт запуска winws, проверяет сайты, останавливает winws.
    /// </summary>
    public async Task<StrategyTestResult> TestStrategyAsync(string strategyPath, CancellationToken ct)
    {
        var result = new StrategyTestResult
        {
            StrategyPath = strategyPath,
            Total = _hosts.Count,
        };

        // Чистим предыдущий winws, если остался.
        await Task.Run(() => _process.StopWinws(), ct);
        await Task.Delay(400, ct);

        await Task.Run(() => _process.StartStrategyHidden(strategyPath), ct);

        // Ждём, пока поднимется winws.exe (но не дольше warmup).
        var deadline = DateTime.UtcNow.AddMilliseconds(_warmupMs);
        bool up = false;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(300, ct);
            if (await Task.Run(() => _process.IsWinwsRunning(), ct))
            {
                up = true;
                break;
            }
        }

        if (!up)
        {
            result.Started = false;
            await Task.Run(() => _process.StopWinws(), ct);
            return result;
        }

        result.Started = true;

        // Небольшая пауза, чтобы фильтры winws точно активировались.
        await Task.Delay(800, ct);

        // Проверяем сайты параллельно.
        var probes = _hosts
            .Select(async host => (host, res: await ProbeHttpsAsync(host, ct)))
            .ToList();

        var probeResults = await Task.WhenAll(probes);

        foreach (var (host, res) in probeResults)
        {
            result.PerHost[host] = res.ok;
            if (res.ok)
            {
                result.Reachable++;
                result.TotalLatencyMs += res.ms;
            }
        }

        await Task.Run(() => _process.StopWinws(), ct);
        await Task.Delay(300, ct);

        return result;
    }

    /// <summary>
    /// TCP-подключение + TLS-handshake с SNI = host к порту 443.
    /// Успешный handshake означает, что DPI не сбросил ClientHello — обход работает.
    /// Сертификат не проверяем: важен сам факт установления TLS, а не валидность.
    /// </summary>
    private async Task<(bool ok, long ms)> ProbeHttpsAsync(string host, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(_connectTimeoutMs);

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, 443, linked.Token);

            using var ssl = new SslStream(
                tcp.GetStream(),
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (_, _, _, _) => true);

            var options = new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            };

            await ssl.AuthenticateAsClientAsync(options, linked.Token);

            sw.Stop();
            return (true, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            return (false, sw.ElapsedMilliseconds);
        }
    }
}
