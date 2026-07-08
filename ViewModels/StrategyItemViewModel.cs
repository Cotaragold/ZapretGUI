using ZapretGUI.ViewModels;

namespace ZapretGUI.ViewModels;

public class StrategyItemViewModel : ViewModelBase
{
    public string FilePath { get; }

    public string FileName { get; }

    private bool _isLastGood;

    public bool IsLastGood
    {
        get => _isLastGood;
        set
        {
            if (SetField(ref _isLastGood, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _testResultText = string.Empty;

    /// <summary>Результат последней авто-проверки, напр. "5/8 (210 мс)".</summary>
    public string TestResultText
    {
        get => _testResultText;
        set => SetField(ref _testResultText, value);
    }

    public string DisplayName => IsLastGood ? $"{FileName} (LastGood)" : FileName;

    public StrategyItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = System.IO.Path.GetFileName(filePath);
    }
}

