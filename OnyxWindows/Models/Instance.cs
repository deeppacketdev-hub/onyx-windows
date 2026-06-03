using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OnyxWindows.Models;

/// <summary>
/// Instance state — matches the macOS InstanceState enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstanceState
{
    NotDownloaded,
    Ready,
    Preparing,
    Downloading,
    Running,
    Stopping,
    Stopped,
    Crashed
}

/// <summary>
/// Mod loader type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModLoaderType
{
    None,
    Fabric,
    Forge,
    NeoForge,
    Quilt
}

/// <summary>
/// A Minecraft instance — one configuration of version + mods + settings.
/// </summary>
public class Instance
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("minecraftVersion")]
    public string MinecraftVersion { get; set; } = "";

    [JsonPropertyName("modLoader")]
    public ModLoaderType ModLoader { get; set; } = ModLoaderType.None;

    [JsonPropertyName("modLoaderVersion")]
    public string? ModLoaderVersion { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastPlayed")]
    public DateTime? LastPlayed { get; set; }

    [JsonPropertyName("totalPlaytimeSeconds")]
    public double TotalPlaytimeSeconds { get; set; } = 0;

    [JsonPropertyName("lastSessionSeconds")]
    public double LastSessionSeconds { get; set; } = 0;

    [JsonPropertyName("ramMB")]
    public int RamMB { get; set; } = 2048;

    [JsonPropertyName("jvmArguments")]
    public string JvmArguments { get; set; } = "";

    [JsonPropertyName("windowWidth")]
    public int? WindowWidth { get; set; }

    [JsonPropertyName("windowHeight")]
    public int? WindowHeight { get; set; }

    [JsonPropertyName("fullscreen")]
    public bool Fullscreen { get; set; } = false;

    [JsonPropertyName("customJavaPath")]
    public string? CustomJavaPath { get; set; }

    [JsonPropertyName("iconFilename")]
    public string? IconFilename { get; set; }

    [JsonPropertyName("autoJoinServer")]
    public string? AutoJoinServer { get; set; }

    [JsonPropertyName("autoJoinPort")]
    public int? AutoJoinPort { get; set; }

    [JsonPropertyName("guiScale")]
    public int? GuiScale { get; set; }

    // ── Runtime state — NOT serialized ──
    [JsonIgnore]
    public InstanceState State { get; set; } = InstanceState.NotDownloaded;

    /// <summary>
    /// Sanitized directory name: lowercase, spaces → hyphens, special chars removed.
    /// Mirrors the Swift Instance.directoryName computed property.
    /// </summary>
    [JsonIgnore]
    public string DirectoryName
    {
        get
        {
            var sanitized = Name.ToLowerInvariant().Replace(' ', '-');
            sanitized = Regex.Replace(sanitized, @"[^a-z0-9\-_]", "");
            sanitized = Regex.Replace(sanitized, @"-{2,}", "-");
            sanitized = sanitized.Trim('-');
            return string.IsNullOrEmpty(sanitized) ? Id.ToString("N")[..8] : sanitized;
        }
    }

    /// <summary>
    /// Formatted total playtime string.
    /// </summary>
    [JsonIgnore]
    public string FormattedPlaytime
    {
        get
        {
            var ts = TimeSpan.FromSeconds(TotalPlaytimeSeconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m";
            return "< 1m";
        }
    }
}
