using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZapretGUI.Services;

/// <summary>
/// Скачивает публичные списки заблокированных IP/подсетей и дописывает их
/// в lists/ipset-all.txt. Источники настраиваются в appsettings.json (IpListUrls).
/// </summary>
public class IpBlockListService
{
    private static readonly HttpClient Http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(40),
    };

    private readonly ListService _listService;
    private readonly LogService _log;
    private readonly IReadOnlyList<string> _urls;

    // Публичный агрегатор российских блокировок. allyouneed.lst — комбинированный
    // список, агрегированный в CIDR-подсети (~15 тыс. записей), обновляется автоматически.
    private static readonly string[] DefaultUrls =
    {
        "https://antifilter.download/list/allyouneed.lst",
    };

    public IpBlockListService(ListService listService, LogService log, IReadOnlyList<string>? urls)
    {
        _listService = listService;
        _log = log;
        _urls = (urls != null && urls.Count > 0)
            ? urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList()
            : DefaultUrls;
    }

    public IReadOnlyList<string> Urls => _urls;

    /// <summary>
    /// Скачивает все источники и мёржит их в ipset-all.txt.
    /// Возвращает число реально добавленных записей. Ошибки отдельных URL не фатальны.
    /// </summary>
    public async Task<int> UpdateAsync(CancellationToken ct)
    {
        var collected = new List<string>();

        foreach (var url in _urls)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var text = await Http.GetStringAsync(url, ct);
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                var count = 0;
                foreach (var l in lines)
                {
                    var line = l.Trim();
                    if (line.Length > 0)
                    {
                        collected.Add(line);
                        count++;
                    }
                }

                _log.Info($"Скачан список IP: {url} ({count} строк).");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error($"Не удалось скачать {url}: {ex.Message}");
            }
        }

        if (collected.Count == 0)
            return 0;

        // Валидация IP/CIDR, дедуп и атомарная запись — внутри ListService.
        return _listService.MergeIpSet(collected);
    }
}
