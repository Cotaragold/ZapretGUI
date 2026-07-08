using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ZapretGUI.Converters;

public abstract class StatusConverterBase : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isRunning = value is bool b && b;
        return ConvertFromBool(isRunning, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    protected abstract object ConvertFromBool(bool isRunning, Type targetType, object parameter, CultureInfo culture);
}

public class StatusBackgroundConverter : StatusConverterBase
{
    protected override object ConvertFromBool(bool isRunning, Type targetType, object parameter, CultureInfo culture)
    {
        return new SolidColorBrush(isRunning ? System.Windows.Media.Color.FromArgb(0x33, 0x2A, 0xFF, 0xC3) : System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0x3B, 0x6A));
    }
}

public class StatusBorderConverter : StatusConverterBase
{
    protected override object ConvertFromBool(bool isRunning, Type targetType, object parameter, CultureInfo culture)
    {
        return new SolidColorBrush(isRunning ? System.Windows.Media.Color.FromRgb(0x2A, 0xFF, 0xC3) : System.Windows.Media.Color.FromRgb(0xFF, 0x3B, 0x6A));
    }
}

public class StatusDotConverter : StatusConverterBase
{
    protected override object ConvertFromBool(bool isRunning, Type targetType, object parameter, CultureInfo culture)
    {
        return new SolidColorBrush(isRunning ? System.Windows.Media.Color.FromRgb(0x47, 0xFF, 0x9C) : System.Windows.Media.Color.FromRgb(0xFF, 0x3B, 0x6A));
    }
}


