using System;
using System.Globalization;
using Windows.UI;

namespace OnyxWindows.Helpers;

/// <summary>
/// Color utility extensions for hex ↔ Color conversion.
/// Replaces the SwiftUI Color(hex:) initializer.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Parses a hex color string (#RRGGBB or #AARRGGBB) to a Windows.UI.Color.
    /// </summary>
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        return hex.Length switch
        {
            6 => Color.FromArgb(
                255,
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber)),

            8 => Color.FromArgb(
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber),
                byte.Parse(hex[6..8], NumberStyles.HexNumber)),

            _ => Color.FromArgb(255, 0, 0, 0)
        };
    }

    /// <summary>
    /// Converts a Color to a hex string (#RRGGBB).
    /// </summary>
    public static string ToHex(this Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Creates a SolidColorBrush from a hex string.
    /// </summary>
    public static Microsoft.UI.Xaml.Media.SolidColorBrush ToBrush(string hex)
    {
        var color = FromHex(hex);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.ColorHelper.FromArgb(color.A, color.R, color.G, color.B));
    }
}
