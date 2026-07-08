using System.Windows.Forms;

namespace ZapretGUI.Services;

public sealed class FolderPicker : IFolderPicker
{
    public bool TryPickFolder(out string? path)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку Zapret (с general.bat, service.bat и lists)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        var result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            path = dialog.SelectedPath;
            return true;
        }

        path = null;
        return false;
    }
}

