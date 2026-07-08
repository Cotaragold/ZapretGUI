using System.Windows;

namespace ZapretGUI.Services;

public interface IDialogService
{
    void ShowError(string message, string title = "Ошибка");
    void ShowInfo(string message, string title = "Готово");
}

public sealed class DialogService : IDialogService
{
    public void ShowError(string message, string title = "Ошибка")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string message, string title = "Готово")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

