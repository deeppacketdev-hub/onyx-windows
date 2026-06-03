using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace OnyxWindows.Converters;

/// <summary>
/// Converts bool to Visibility (true → Visible, false → Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Inverts a bool (true → false, false → true).
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return !b;
        return false;
    }
}

/// <summary>
/// Returns Visibility.Visible if string is not null/empty, Collapsed otherwise.
/// </summary>
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts an InstanceState enum to a localized status string.
/// </summary>
public class InstanceStateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Models.InstanceState state)
        {
            return state switch
            {
                Models.InstanceState.NotDownloaded => "Not Downloaded",
                Models.InstanceState.Ready => "Ready",
                Models.InstanceState.Preparing => "Preparing...",
                Models.InstanceState.Downloading => "Downloading...",
                Models.InstanceState.Running => "Running",
                Models.InstanceState.Stopping => "Stopping...",
                Models.InstanceState.Stopped => "Stopped",
                Models.InstanceState.Crashed => "Crashed",
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverse BoolToVisibility (true → Collapsed, false → Visible).
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Collapsed;
    }
}

/// <summary>
/// Converts LogType to color brush for terminal console.
/// </summary>
public class LogTypeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Services.LogType type)
        {
            var hex = type switch
            {
                Services.LogType.Info => "#E0E0E0",
                Services.LogType.Error => "#FF4C4C",
                Services.LogType.System => "#4CFF4C",
                _ => "#E0E0E0"
            };
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex(hex));
        }
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts an Instance to a themed emoji representing its content.
/// </summary>
public class InstanceToEmojiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Models.Instance inst)
        {
            var name = inst.Name ?? "";
            var ver = inst.MinecraftVersion ?? "";

            if (name.Contains("Survival", StringComparison.OrdinalIgnoreCase)) return "🌿";
            if (name.Contains("Tech", StringComparison.OrdinalIgnoreCase) || name.Contains("Industrial", StringComparison.OrdinalIgnoreCase)) return "⚙️";
            if (name.Contains("RPG", StringComparison.OrdinalIgnoreCase) || name.Contains("Adventure", StringComparison.OrdinalIgnoreCase)) return "🏰";
            if (name.Contains("Vanilla", StringComparison.OrdinalIgnoreCase)) return "🌲";
            if (name.Contains("PvP", StringComparison.OrdinalIgnoreCase) || name.Contains("Classic", StringComparison.OrdinalIgnoreCase)) return "🔵";

            if (ver.StartsWith("1.21")) return "🌿";
            if (ver.StartsWith("1.20")) return "⚙️";
            if (ver.StartsWith("1.19")) return "🏰";
            return "🌌";
        }
        return "🌌";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

/// <summary>
/// Converts an Instance to a themed LinearGradientBrush matching the HTML art headers.
/// </summary>
public class InstanceToCardArtBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var startHex = "#1A1A2A";
        var endHex = "#0D1A3B";

        if (value is Models.Instance inst)
        {
            var name = inst.Name ?? "";
            var ver = inst.MinecraftVersion ?? "";

            if (name.Contains("Survival", StringComparison.OrdinalIgnoreCase) || ver.StartsWith("1.21"))
            {
                startHex = "#1A2744"; // Art 1: Deep blue/teal
                endHex = "#0D3B2E";
            }
            else if (name.Contains("Tech", StringComparison.OrdinalIgnoreCase) || name.Contains("Industrial", StringComparison.OrdinalIgnoreCase) || ver.StartsWith("1.20"))
            {
                startHex = "#2A1A3E"; // Art 2: Deep Violet
                endHex = "#1A0D2E";
            }
            else if (name.Contains("RPG", StringComparison.OrdinalIgnoreCase) || name.Contains("Adventure", StringComparison.OrdinalIgnoreCase) || ver.StartsWith("1.19"))
            {
                startHex = "#2A1A1A"; // Art 3: Deep Rust
                endHex = "#3E1A0D";
            }
            else if (name.Contains("Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                startHex = "#1A2A1A"; // Art 4: Forest Green
                endHex = "#0D3B20";
            }
            else if (name.Contains("PvP", StringComparison.OrdinalIgnoreCase) || name.Contains("Classic", StringComparison.OrdinalIgnoreCase))
            {
                startHex = "#1A1A2A"; // Art 5: Night Blue
                endHex = "#0D1A3B";
            }
            else
            {
                startHex = "#2A2A1A"; // Art 6: Olive Gold
                endHex = "#3B3B0D";
            }
        }

        var startColor = OnyxWindows.Helpers.ColorExtensions.FromHex(startHex);
        var endColor = OnyxWindows.Helpers.ColorExtensions.FromHex(endHex);

        var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1)
        };
        brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = startColor, Offset = 0.0 });
        brush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop { Color = endColor, Offset = 1.0 });

        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

/// <summary>
/// Converts ModLoader type to appropriate badge colors/brushes.
/// Parameter determines whether we want "Background" or "Foreground".
/// </summary>
public class ModLoaderToBadgeBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var loaderStr = value?.ToString() ?? "";
        var isForeground = parameter?.ToString() == "Foreground";

        if (loaderStr.Contains("Fabric", StringComparison.OrdinalIgnoreCase) || loaderStr.Contains("Quilt", StringComparison.OrdinalIgnoreCase))
        {
            // Fabric styling: Background = rgba(124,109,250,0.3) (#4D7C6DFA or solid #38345C), Foreground = #a594f9
            return isForeground
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#A594F9"))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#38345C"));
        }
        else if (loaderStr.Contains("Forge", StringComparison.OrdinalIgnoreCase) || loaderStr.Contains("NeoForge", StringComparison.OrdinalIgnoreCase))
        {
            // Forge styling: Background = rgba(239,130,50,0.2) (#33EF8232 or solid #4D331C), Foreground = #f4a261
            return isForeground
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#F4A261"))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#4D331C"));
        }
        else
        {
            // Vanilla styling: Background = rgba(90,200,100,0.2) (#335AC864 or solid #1B3E25), Foreground = #7cc87d
            return isForeground
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#7CC87D"))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(OnyxWindows.Helpers.ColorExtensions.FromHex("#1B3E25"));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
