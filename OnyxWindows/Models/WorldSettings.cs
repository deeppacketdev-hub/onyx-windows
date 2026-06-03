using System.Text.Json.Serialization;

namespace OnyxWindows.Models;

/// <summary>
/// World settings derived from NBT level.dat parsing.
/// Mirrors the macOS WorldSettings struct.
/// </summary>
public class WorldSettings
{
    [JsonPropertyName("worldName")]
    public string WorldName { get; set; } = "";

    [JsonPropertyName("seed")]
    public long Seed { get; set; }

    [JsonPropertyName("gameMode")]
    public int GameMode { get; set; } = 0;

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; } = 2;

    [JsonPropertyName("hardcore")]
    public bool Hardcore { get; set; } = false;

    [JsonPropertyName("allowCommands")]
    public bool AllowCommands { get; set; } = false;

    [JsonPropertyName("spawnX")]
    public int SpawnX { get; set; }

    [JsonPropertyName("spawnY")]
    public int SpawnY { get; set; }

    [JsonPropertyName("spawnZ")]
    public int SpawnZ { get; set; }

    [JsonPropertyName("dayTime")]
    public long DayTime { get; set; }

    [JsonPropertyName("gameTime")]
    public long GameTime { get; set; }

    [JsonPropertyName("raining")]
    public bool Raining { get; set; }

    [JsonPropertyName("thundering")]
    public bool Thundering { get; set; }

    // World border
    [JsonPropertyName("borderCenterX")]
    public double BorderCenterX { get; set; }

    [JsonPropertyName("borderCenterZ")]
    public double BorderCenterZ { get; set; }

    [JsonPropertyName("borderSize")]
    public double BorderSize { get; set; } = 60000000;

    [JsonPropertyName("borderDamagePerBlock")]
    public double BorderDamagePerBlock { get; set; } = 0.2;

    [JsonPropertyName("borderSafeZone")]
    public double BorderSafeZone { get; set; } = 5;

    [JsonPropertyName("borderWarningBlocks")]
    public int BorderWarningBlocks { get; set; } = 5;

    [JsonPropertyName("borderWarningTime")]
    public int BorderWarningTime { get; set; } = 15;

    // Game rules (stored as key-value pairs)
    [JsonPropertyName("gameRules")]
    public Dictionary<string, string> GameRules { get; set; } = new();

    // Metadata
    [JsonPropertyName("dataVersion")]
    public int DataVersion { get; set; }

    [JsonPropertyName("mcVersion")]
    public string? McVersion { get; set; }
}
