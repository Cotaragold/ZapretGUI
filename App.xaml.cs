using System;
using System.IO;
using System.Windows;
using ZapretGUI.Services;
using ZapretGUI.ViewModels;

namespace ZapretGUI;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var dialogService = new DialogService();
            var folderPicker = new FolderPicker();
            var zapretFolderService = new ZapretFolderService();

            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string? zapretFolder;
            try
            {
                zapretFolder = zapretFolderService.ResolveFolder(baseDir, folderPicker);
            }
            catch (OperationCanceledException)
            {
                Shutdown();
                return;
            }

            while (true)
            {
                if (!string.IsNullOrWhiteSpace(zapretFolder) &&
                    Directory.Exists(zapretFolder) &&
                    zapretFolderService.IsValidZapretFolder(zapretFolder))
                {
                    zapretFolderService.SetZapretFolder(zapretFolder);
                    break;
                }

                dialogService.ShowError("Выбранная папка не содержит general.bat, service.bat и папку lists.", "Неверная папка Zapret");

                if (!folderPicker.TryPickFolder(out zapretFolder))
                {
                    Shutdown();
                    return;
                }
            }

            var processService = new ProcessService(zapretFolderService.ZapretFolder);
            var listService = new ListService(zapretFolderService.ZapretFolder);
            var logService = new LogService(zapretFolderService.ZapretFolder);
            var testService = new StrategyTestService(
                processService,
                logService,
                zapretFolderService.TestHosts,
                zapretFolderService.TestWarmupMs,
                zapretFolderService.TestConnectTimeoutMs);
            var ipBlockListService = new IpBlockListService(
                listService,
                logService,
                zapretFolderService.IpListUrls);

            var mainVm = new MainWindowViewModel(
                zapretFolderService,
                processService,
                listService,
                logService,
                dialogService,
                testService,
                ipBlockListService);

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка запуска приложения: {ex.Message}",
                "ZapretGUI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}


