using System.Text.Json.Serialization;

namespace OnyxWindows.Models;

/// <summary>
/// Theme type for the launcher UI.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeType
{
    Dark,
    Light,
    Custom,
    System
}

/// <summary>
/// App language — matches the macOS AppLanguage enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppLanguage
{
    De,
    En,
    Es,
    Fr,
    Pl,
    Uk
}

/// <summary>
/// Custom theme colors — hex strings for user-defined themes.
/// </summary>
public class CustomColors
{
    [JsonPropertyName("backgroundHex")]
    public string BackgroundHex { get; set; } = "#1A1A2E";

    [JsonPropertyName("surfaceHex")]
    public string SurfaceHex { get; set; } = "#16213E";

    [JsonPropertyName("accentHex")]
    public string AccentHex { get; set; } = "#0F3460";

    [JsonPropertyName("enableGradient")]
    public bool EnableGradient { get; set; } = false;

    [JsonPropertyName("gradientAngle")]
    public double GradientAngle { get; set; } = 180;
}

/// <summary>
/// Global configuration for the launcher — persisted to config.json.
/// Mirrors the macOS GlobalConfig struct.
/// </summary>
public class GlobalConfig
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = "Player";

    [JsonPropertyName("theme")]
    public ThemeType Theme { get; set; } = ThemeType.Dark;

    [JsonPropertyName("language")]
    public AppLanguage Language { get; set; } = AppLanguage.En;

    [JsonPropertyName("hasCompletedOnboarding")]
    public bool HasCompletedOnboarding { get; set; } = false;

    [JsonPropertyName("defaultRamMB")]
    public int DefaultRamMB { get; set; } = 2048;

    [JsonPropertyName("showConsoleOnLaunch")]
    public bool ShowConsoleOnLaunch { get; set; } = false;

    [JsonPropertyName("closeLauncherOnLaunch")]
    public bool CloseLauncherOnLaunch { get; set; } = false;

    [JsonPropertyName("enableTelemetry")]
    public bool EnableTelemetry { get; set; } = true;

    [JsonPropertyName("defaultWindowWidth")]
    public int? DefaultWindowWidth { get; set; }

    [JsonPropertyName("defaultWindowHeight")]
    public int? DefaultWindowHeight { get; set; }

    [JsonPropertyName("defaultFullscreen")]
    public bool DefaultFullscreen { get; set; } = false;

    [JsonPropertyName("defaultGuiScale")]
    public int? DefaultGuiScale { get; set; }

    [JsonPropertyName("customColors")]
    public CustomColors CustomColors { get; set; } = new();
}
