using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using BugzillaDumper.Models;

namespace BugzillaDumper.Converters;

/// <summary>MultiBinding converter: (BugSummary, keyword) → bool. Used for row highlight.</summary>
public class BugMatchesKeywordConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not BugSummary bug) return false;
        var kw = values[1] as string;
        if (string.IsNullOrWhiteSpace(kw)) return false;
        return ViewModels.MainViewModel.BugMatchesKeyword(bug, kw);
    }
}

/// <summary>Strips @moneydj.com from email addresses for display.</summary>
public class EmailToUsernameConverter : IValueConverter
{
    private const string Domain = "@moneydj.com";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string email) return value;
        var idx = email.IndexOf(Domain, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? email[..idx] : email;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value as string) switch
        {
            "NEW"      => Brush.Parse("#DBEAFE"),
            "ASSIGNED" => Brush.Parse("#FEF9C3"),
            "RESOLVED" => Brush.Parse("#DCFCE7"),
            "CLOSED"   => Brush.Parse("#F3F4F6"),
            "REOPENED" => Brush.Parse("#FFE4E6"),
            _          => Brush.Parse("#E5E7EB"),
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value as string) switch
        {
            "NEW"      => Brush.Parse("#1D4ED8"),
            "ASSIGNED" => Brush.Parse("#92400E"),
            "RESOLVED" => Brush.Parse("#166534"),
            "REOPENED" => Brush.Parse("#991B1B"),
            _          => Brush.Parse("#374151"),
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Bool → IBrush. Configure True/False brushes via XAML.</summary>
public class BoolToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush  { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueBrush : FalseBrush;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Bool → string. Configure True/False values via XAML.</summary>
public class BoolToStringConverter : IValueConverter
{
    public string TrueValue  { get; set; } = string.Empty;
    public string FalseValue { get; set; } = string.Empty;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueValue : FalseValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
