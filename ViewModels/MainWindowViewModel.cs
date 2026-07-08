using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ZapretGUI.Commands;
using ZapretGUI.Services;

namespace ZapretGUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ZapretFolderService _zapretFolderService;
    private readonly ProcessService _processService;
    private readonly ListService _listService;
    private readonly LogService _logService;
    private readonly IDialogService _dialogService;
    private readonly StrategyTestService _testService;
    private readonly IpBlockListService _ipBlockListService;
    private readonly DispatcherTimer _statusTimer;
    private readonly DispatcherTimer _logTimer;
    private readonly SemaphoreSlim _statusRefreshSemaphore = new(1, 1);
    private readonly SemaphoreSlim _logsRefreshSemaphore = new(1, 1);

    private CancellationTokenSource? _autoCts;

    public string ZapretFolder { get; }

    private bool _isBusy;
    /// <summary>Идёт автоподбор стратегии — блокирует повторный запуск и меняет доступность кнопок.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsNotBusy => !_isBusy;

    private string _busyStatus = string.Empty;
    /// <summary>Текст прогресса автоподбора для отображения в UI.</summary>
    public string BusyStatus
    {
        get => _busyStatus;
        set => SetField(ref _busyStatus, value);
    }

    private bool _isUpdatingIp;
    /// <summary>Идёт скачивание/обновление списка заблокированных IP.</summary>
    public bool IsUpdatingIp
    {
        get => _isUpdatingIp;
        set
        {
            if (SetField(ref _isUpdatingIp, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    private string _ipUpdateStatus = string.Empty;
    /// <summary>Статус обновления списка IP для отображения в UI.</summary>
    public string IpUpdateStatus
    {
        get => _ipUpdateStatus;
        set => SetField(ref _ipUpdateStatus, value);
    }

    private bool _isWinwsRunning;
    public bool IsWinwsRunning
    {
        get => _isWinwsRunning;
        set
        {
            if (SetField(ref _isWinwsRunning, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => IsWinwsRunning ? "Running" : "Stopped";

    public ObservableCollection<StrategyItemViewModel> Strategies { get; } = new();

    // Lists tab
    private string _rawListInput = string.Empty;
    public string RawListInput
    {
        get => _rawListInput;
        set => SetField(ref _rawListInput, value);
    }

    private string _ipSetAllPreview = string.Empty;
    public string IpSetAllPreview
    {
        get => _ipSetAllPreview;
        set => SetField(ref _ipSetAllPreview, value);
    }

    private string _googleDomainsPreview = string.Empty;
    public string GoogleDomainsPreview
    {
        get => _googleDomainsPreview;
        set => SetField(ref _googleDomainsPreview, value);
    }

    private string _generalDomainsPreview = string.Empty;
    public string GeneralDomainsPreview
    {
        get => _generalDomainsPreview;
        set => SetField(ref _generalDomainsPreview, value);
    }

    private string _invalidLinesPreview = string.Empty;
    public string InvalidLinesPreview
    {
        get => _invalidLinesPreview;
        set => SetField(ref _invalidLinesPreview, value);
    }

    // Для применения
    private ParsedLists? _lastParsed;

    // Logs
    private string _logText = string.Empty;
    public string LogText
    {
        get => _logText;
        set => SetField(ref _logText, value);
    }

    // Commands
    public ICommand StartAutoCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand StartStrategyCommand { get; }
    public ICommand OpenServiceCommand { get; }
    public ICommand ParseListsCommand { get; }
    public ICommand ApplyListsCommand { get; }
    public ICommand UpdateIpCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand CloseCommand { get; }

    public MainWindowViewModel(
        ZapretFolderService zapretFolderService,
        ProcessService processService,
        ListService listService,
        LogService logService,
        IDialogService dialogService,
        StrategyTestService testService,
        IpBlockListService ipBlockListService)
    {
        _zapretFolderService = zapretFolderService;
        _processService = processService;
        _listService = listService;
        _logService = logService;
        _dialogService = dialogService;
        _testService = testService;
        _ipBlockListService = ipBlockListService;

        ZapretFolder = zapretFolderService.ZapretFolder;

        StartAutoCommand = new RelayCommand(async _ => await StartAutoBestAsync(), _ => !IsBusy);
        StopCommand = new RelayCommand(_ => StopWinws());
        UpdateIpCommand = new RelayCommand(async _ => await UpdateBlockedIpsAsync(), _ => !IsUpdatingIp);
        StartStrategyCommand = new RelayCommand(p => StartStrategy(p as StrategyItemViewModel));
        OpenServiceCommand = new RelayCommand(_ => OpenService());
        ParseListsCommand = new RelayCommand(_ => ParseLists());
        ApplyListsCommand = new RelayCommand(_ => ApplyLists(), _ => _lastParsed != null);
        MinimizeCommand = new RelayCommand(_ => System.Windows.Application.Current.MainWindow!.WindowState = WindowState.Minimized);
        CloseCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();

        _logTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _logTimer.Tick += async (_, _) => await RefreshLogsAsync();
        _logTimer.Start();

        LoadStrategies();
        _ = RefreshStatusAsync();
        _ = RefreshLogsAsync();

        // Обновляем список заблокированных IP в фоне при старте (если включено в настройках).
        if (_zapretFolderService.AutoUpdateIpOnStartup)
            _ = UpdateBlockedIpsAsync();
    }

    private void LoadStrategies()
    {
        Strategies.Clear();

        var lastGood = _zapretFolderService.LastGoodStrategy;

        foreach (var file in _processService.GetStrategies())
        {
            var vm = new StrategyItemViewModel(file)
            {
                IsLastGood = !string.IsNullOrWhiteSpace(lastGood) &&
                             string.Equals(Path.GetFullPath(lastGood), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase)
            };
            Strategies.Add(vm);
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (!await _statusRefreshSemaphore.WaitAsync(0))
            return;

        try
        {
            var isRunning = await Task.Run(() => _processService.IsWinwsRunning());
            IsWinwsRunning = isRunning;
        }
        finally
        {
            _statusRefreshSemaphore.Release();
        }
    }

    private async Task RefreshLogsAsync()
    {
        if (!await _logsRefreshSemaphore.WaitAsync(0))
            return;

        try
        {
            var text = await Task.Run(() => _logService.ReadLastLines(400));
            LogText = text;
        }
        finally
        {
            _logsRefreshSemaphore.Release();
        }
    }

    /// <summary>
    /// Режим Авто: по очереди проверяет все стратегии, выбирает ту, у которой открылось
    /// больше всего популярных сайтов, назначает её LastGood и запускает.
    /// </summary>
    private async Task StartAutoBestAsync()
    {
        if (IsBusy)
            return;

        var strategies = _processService.GetStrategies().ToList();
        if (strategies.Count == 0)
        {
            _logService.Error("Не найдено ни одной стратегии general*.bat для проверки.");
            _dialogService.ShowError("Не найдено ни одной стратегии general*.bat.");
            return;
        }

        var cts = new CancellationTokenSource();
        _autoCts = cts;
        IsBusy = true;
        BusyStatus = "Подбор рабочей стратегии...";

        // Сбрасываем прошлые результаты в списке.
        foreach (var item in Strategies)
            item.TestResultText = string.Empty;

        _logService.Info($"Старт автоподбора: {strategies.Count} стратегий, {_testService.HostCount} сайтов.");

        try
        {
            var progress = new Progress<string>(s => BusyStatus = s);
            var results = await _testService.TestAllAsync(strategies, progress, cts.Token);

            // Переносим результаты в элементы списка.
            var byPath = results.ToDictionary(r => r.StrategyPath, StringComparer.OrdinalIgnoreCase);
            foreach (var item in Strategies)
            {
                if (byPath.TryGetValue(item.FilePath, out var r))
                    item.TestResultText = r.Summary;
            }

            var best = results.FirstOrDefault(r => r.Started && r.Reachable > 0);

            if (best == null)
            {
                _logService.Error("Ни одна стратегия не открыла тестовые сайты. Запуск по умолчанию.");
                _processService.StartAuto(_zapretFolderService.LastGoodStrategy);
                BusyStatus = string.Empty;
                _dialogService.ShowError("Не удалось подобрать рабочую стратегию. Запущена стратегия по умолчанию.");
            }
            else
            {
                _zapretFolderService.LastGoodStrategy = best.StrategyPath;
                UpdateLastGoodFlags(best.StrategyPath);

                _processService.StartStrategy(best.StrategyPath);
                _logService.Info($"Выбрана лучшая стратегия {best.FileName}: {best.Summary}. Запущена.");
                BusyStatus = $"Работает: {best.FileName} ({best.Summary})";
            }

            await RefreshStatusAsync();
        }
        catch (OperationCanceledException)
        {
            _logService.Info("Автоподбор отменён.");
            BusyStatus = string.Empty;
            try { _processService.StopWinws(); } catch { /* best-effort */ }
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка автоподбора: " + ex.Message);
            _dialogService.ShowError("Ошибка автоподбора: " + ex.Message);
            BusyStatus = string.Empty;
        }
        finally
        {
            IsBusy = false;
            _autoCts = null;
            cts.Dispose();
        }
    }

    private void UpdateLastGoodFlags(string bestPath)
    {
        foreach (var item in Strategies)
        {
            item.IsLastGood = string.Equals(
                Path.GetFullPath(item.FilePath),
                Path.GetFullPath(bestPath),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Скачивает публичные списки заблокированных IP и дописывает их в ipset-all.txt.
    /// Вызывается автоматически при старте и вручную кнопкой.
    /// </summary>
    private async Task UpdateBlockedIpsAsync()
    {
        if (IsUpdatingIp)
            return;

        IsUpdatingIp = true;
        IpUpdateStatus = "Обновление списка заблокированных IP...";
        _logService.Info("Обновление списка заблокированных IP из публичных источников...");

        try
        {
            var added = await _ipBlockListService.UpdateAsync(CancellationToken.None);
            _logService.Info($"Список IP обновлён. Новых записей добавлено: {added}.");
            IpUpdateStatus = added > 0
                ? $"Добавлено новых IP: {added}"
                : "Список IP уже актуален";
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка обновления списка IP: " + ex.Message);
            IpUpdateStatus = "Ошибка обновления списка IP";
        }
        finally
        {
            IsUpdatingIp = false;
        }
    }

    private void StopWinws()
    {
        // Если идёт автоподбор — сначала отменяем его.
        if (IsBusy)
        {
            _autoCts?.Cancel();
            return;
        }

        try
        {
            _processService.StopWinws();
            _logService.Info("winws.exe остановлен.");
            _ = RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка остановки winws.exe: " + ex.Message);
            _dialogService.ShowError("Ошибка остановки winws.exe: " + ex.Message);
        }
    }

    private void StartStrategy(StrategyItemViewModel? strategy)
    {
        if (strategy == null)
            return;

        try
        {
            _processService.StartStrategy(strategy.FilePath);
            _logService.Info($"Запущена стратегия {strategy.FileName}.");
            _ = RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            _logService.Error($"Ошибка запуска стратегии {strategy.FileName}: {ex.Message}");
            _dialogService.ShowError($"Ошибка запуска стратегии: {ex.Message}");
        }
    }

    private void OpenService()
    {
        try
        {
            _processService.OpenService();
            _logService.Info("Открыт service.bat.");
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка открытия service.bat: " + ex.Message);
            _dialogService.ShowError("Ошибка открытия service.bat: " + ex.Message);
        }
    }

    private void ParseLists()
    {
        try
        {
            var parsed = _listService.Parse(RawListInput);
            _lastParsed = parsed;

            IpSetAllPreview = string.Join(Environment.NewLine, parsed.IpSetAll);
            GoogleDomainsPreview = string.Join(Environment.NewLine, parsed.GoogleDomains);
            GeneralDomainsPreview = string.Join(Environment.NewLine, parsed.GeneralDomains);
            InvalidLinesPreview = string.Join(Environment.NewLine, parsed.InvalidLines);

            _logService.Info("Выполнен разбор списков.");
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка разбора списков: " + ex.Message);
            _dialogService.ShowError("Ошибка разбора списков: " + ex.Message);
        }
    }

    private void ApplyLists()
    {
        if (_lastParsed == null)
            return;

        try
        {
            _listService.Apply(_lastParsed);
            _logService.Info("Списки успешно применены.");
            _dialogService.ShowInfo("Списки успешно применены.");
        }
        catch (Exception ex)
        {
            _logService.Error("Ошибка применения списков: " + ex.Message);
            _dialogService.ShowError("Ошибка применения списков: " + ex.Message);
        }
    }
}




