using System;
using System.Globalization;
using System.Windows.Data;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Converters;

/// <summary>
/// Converts file size in bytes to human-readable format
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatSize(bytes);
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
    }
}

/// <summary>
/// Converts boolean to visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts inverted boolean to visibility
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts CleanupRisk to appropriate color brush
/// </summary>
public class RiskLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CleanupRisk risk)
        {
            return risk switch
            {
                CleanupRisk.Safe => System.Windows.Application.Current.Resources["RiskSafeBrush"],
                CleanupRisk.Low => System.Windows.Application.Current.Resources["RiskLowBrush"],
                CleanupRisk.Medium => System.Windows.Application.Current.Resources["RiskMediumBrush"],
                CleanupRisk.High => System.Windows.Application.Current.Resources["RiskHighBrush"],
                _ => System.Windows.Application.Current.Resources["TextSecondaryBrush"]
            };
        }
        return System.Windows.Application.Current.Resources["TextSecondaryBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts percentage to width for size bars
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            var maxWidth = parameter != null ? double.Parse(parameter.ToString()!) : 200;
            return Math.Max(2, (percentage / 100) * maxWidth);
        }
        return 2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ScanState to display string
/// </summary>
public class ScanStateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ScanState state)
        {
            return state switch
            {
                ScanState.Idle => "Ready",
                ScanState.Scanning => "Scanning...",
                ScanState.Paused => "Paused",
                ScanState.Completed => "Completed",
                ScanState.Cancelled => "Cancelled",
                ScanState.Error => "Error",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts DateTime to relative time string
/// </summary>
public class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            
            if (span.TotalDays > 365)
                return $"{(int)(span.TotalDays / 365)} year(s) ago";
            if (span.TotalDays > 30)
                return $"{(int)(span.TotalDays / 30)} month(s) ago";
            if (span.TotalDays > 1)
                return $"{(int)span.TotalDays} day(s) ago";
            if (span.TotalHours > 1)
                return $"{(int)span.TotalHours} hour(s) ago";
            if (span.TotalMinutes > 1)
                return $"{(int)span.TotalMinutes} minute(s) ago";
            return "Just now";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to icon string (for dark mode toggle)
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDark && parameter is string icons)
        {
            var parts = icons.Split('|');
            if (parts.Length == 2)
            {
                return isDark ? parts[0] : parts[1]; // dark=sun, light=moon
            }
        }
        return "🌙";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts AppTheme enum to appropriate icon
/// </summary>
public class ThemeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Services.AppTheme theme)
        {
            return theme switch
            {
                Services.AppTheme.Default => "🌐",       // Globe - retrofuturistic default
                Services.AppTheme.Tech => "🔷",          // Blue diamond - cyberpunk/tech
                Services.AppTheme.Enterprise => "☀️",   // Sun - light/professional
                Services.AppTheme.TerminalGreen => "🟢", // Green circle - terminal
                Services.AppTheme.TerminalRed => "🔴",   // Red circle - terminal
                _ => "🎨"
            };
        }
        return "🎨";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts LiveCharts series Fill paint to a WPF brush for legend display
/// </summary>
public class SeriesFillToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint paint)
        {
            var skColor = paint.Color;
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue));
        }
        return System.Windows.Application.Current.Resources["PrimaryBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
