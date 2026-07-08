namespace ZapretGUI.Services;

public interface IFolderPicker
{
    bool TryPickFolder(out string? path);
}

