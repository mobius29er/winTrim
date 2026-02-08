using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WinTrim.Core.Models;

namespace WinTrim.Avalonia.Converters;

/// <summary>
/// Converts file size in bytes to human-readable format
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatSize(bytes);
        }
        return "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
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
/// Converts boolean to visibility (IsVisible property in Avalonia)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        // Also treat non-null objects as visible
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts inverted boolean to visibility
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts CleanupRisk to appropriate color brush
/// </summary>
public class RiskLevelToBrushConverter : IValueConverter
{
    public static readonly RiskLevelToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CleanupRisk risk)
        {
            return risk switch
            {
                CleanupRisk.Safe => new SolidColorBrush(Color.Parse("#4CAF50")),   // Green
                CleanupRisk.Low => new SolidColorBrush(Color.Parse("#8BC34A")),    // Light Green
                CleanupRisk.Medium => new SolidColorBrush(Color.Parse("#FFC107")), // Amber
                CleanupRisk.High => new SolidColorBrush(Color.Parse("#F44336")),   // Red
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))                   // Grey
            };
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts percentage to width for size bars
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public static readonly PercentageToWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            var maxWidth = parameter != null ? double.Parse(parameter.ToString()!) : 200;
            return Math.Max(2, (percentage / 100) * maxWidth);
        }
        return 2.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts DateTime to relative time string
/// </summary>
public class DateTimeToRelativeConverter : IValueConverter
{
    public static readonly DateTimeToRelativeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts AppTheme enum to appropriate icon
/// </summary>
public class ThemeToIconConverter : IValueConverter
{
    public static readonly ThemeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string theme)
        {
            return theme switch
            {
                "Default" => "🌐",
                "Tech" => "🔷",
                "Enterprise" => "☀️",
                "TerminalGreen" => "🟢",
                "TerminalRed" => "🔴",
                _ => "🎨"
            };
        }
        return "🎨";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts nullable object to boolean for IsVisible binding
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to FontWeight (SemiBold for true, Normal for false)
/// Used to highlight "original" files in duplicate groups
/// </summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public static readonly BoolToFontWeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? FontWeight.SemiBold : FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts even/odd group number to alternating background color
/// Used to visually distinguish duplicate groups in the DataGrid
/// </summary>
public class EvenGroupToBackgroundConverter : IValueConverter
{
    public static readonly EvenGroupToBackgroundConverter Instance = new();

    // Subtle alternating colors for dark theme
    private static readonly SolidColorBrush EvenBrush = new(Color.FromArgb(20, 59, 130, 246)); // Subtle blue
    private static readonly SolidColorBrush OddBrush = new(Color.FromArgb(20, 139, 92, 246));  // Subtle purple
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? EvenBrush : OddBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsOriginal to background color (highlights the "keep" file)
/// </summary>
public class OriginalFileBackgroundConverter : IValueConverter
{
    public static readonly OriginalFileBackgroundConverter Instance = new();

    private static readonly SolidColorBrush OriginalBrush = new(Color.FromArgb(40, 16, 185, 129)); // Green tint
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? OriginalBrush : TransparentBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a percentage (0-100) to a width for progress bars.
/// Uses a fixed max width and calculates proportionally.
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public static readonly PercentToWidthConverter Instance = new();
    
    // This will be the maximum width in pixels (container minus padding)
    private const double MaxWidth = 1000;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            var width = Math.Max(0, Math.Min(100, percent)) / 100.0 * MaxWidth;
            return width;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the value is zero (for hiding elements during initial scan)
/// </summary>
public class IsZeroConverter : IValueConverter
{
    public static readonly IsZeroConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal) return intVal == 0;
        if (value is long longVal) return longVal == 0;
        if (value is double doubleVal) return doubleVal == 0;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the value is not zero (for showing elements once scan has real data)
/// </summary>
public class IsNotZeroConverter : IValueConverter
{
    public static readonly IsNotZeroConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal) return intVal != 0;
        if (value is long longVal) return longVal != 0;
        if (value is double doubleVal) return doubleVal != 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
