using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnyxWindows.Models;

// ── Mojang Version Manifest (https://piston-meta.mojang.com/mc/game/version_manifest_v2.json) ──

public class VersionManifest
{
    [JsonPropertyName("latest")]
    public LatestVersions Latest { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<VersionEntry> Versions { get; set; } = new();
}

public class LatestVersions
{
    [JsonPropertyName("release")]
    public string Release { get; set; } = "";

    [JsonPropertyName("snapshot")]
    public string Snapshot { get; set; } = "";
}

public class VersionEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("releaseTime")]
    public string ReleaseTime { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }

    [JsonPropertyName("complianceLevel")]
    public int ComplianceLevel { get; set; } = 0;
}

// ── Full Version Detail JSON ──

public class VersionDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = "";

    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    [JsonPropertyName("arguments")]
    public GameArguments? Arguments { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();

    [JsonPropertyName("downloads")]
    public VersionDownloads? Downloads { get; set; }

    [JsonPropertyName("assetIndex")]
    public AssetIndexInfo? AssetIndex { get; set; }

    [JsonPropertyName("assets")]
    public string? Assets { get; set; }

    [JsonPropertyName("javaVersion")]
    public JavaVersionInfo? JavaVersion { get; set; }

    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }

    [JsonPropertyName("logging")]
    public Dictionary<string, LoggingConfig>? Logging { get; set; }
}

public class JavaVersionInfo
{
    [JsonPropertyName("component")]
    public string Component { get; set; } = "";

    [JsonPropertyName("majorVersion")]
    public int MajorVersion { get; set; } = 8;
}

public class VersionDownloads
{
    [JsonPropertyName("client")]
    public DownloadInfo Client { get; set; } = new();

    [JsonPropertyName("server")]
    public DownloadInfo? Server { get; set; }
}

public class DownloadInfo
{
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class AssetIndexInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("totalSize")]
    public long TotalSize { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class LoggingConfig
{
    [JsonPropertyName("argument")]
    public string Argument { get; set; } = "";

    [JsonPropertyName("file")]
    public LoggingFile File { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class LoggingFile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

// ── Libraries ──

public class Library
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("downloads")]
    public LibraryDownloads? Downloads { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }

    [JsonPropertyName("extract")]
    public ExtractInfo? Extract { get; set; }
}

public class LibraryDownloads
{
    [JsonPropertyName("artifact")]
    public LibraryArtifact? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, LibraryArtifact>? Classifiers { get; set; }
}

public class LibraryArtifact
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class ExtractInfo
{
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }
}

// ── Rules ──

public class Rule
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "allow";

    [JsonPropertyName("os")]
    public OsRule? Os { get; set; }
}

public class OsRule
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

// ── Arguments ──

public class GameArguments
{
    [JsonPropertyName("game")]
    public List<JsonElement>? Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<JsonElement>? Jvm { get; set; }
}

/// <summary>
/// Conditional argument with rules (used in modern version JSONs).
/// </summary>
public class ConditionalArgument
{
    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    /// <summary>
    /// Extracts string values from the 'value' field (can be string or string[]).
    /// </summary>
    public List<string> GetValues()
    {
        if (Value.ValueKind == JsonValueKind.String)
            return new List<string> { Value.GetString()! };

        if (Value.ValueKind == JsonValueKind.Array)
        {
            var result = new List<string>();
            foreach (var element in Value.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                    result.Add(element.GetString()!);
            }
            return result;
        }

        return new List<string>();
    }
}

// ── Asset Index ──

public class AssetIndex
{
    [JsonPropertyName("objects")]
    public Dictionary<string, AssetObject> Objects { get; set; } = new();
}

public class AssetObject
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// First two characters of the hash — used as subdirectory prefix.
    /// </summary>
    [JsonIgnore]
    public string HashPrefix => Hash.Length >= 2 ? Hash[..2] : Hash;
}

// ── Download Item (internal, not from JSON) ──

public class DownloadItem
{
    public string Url { get; set; } = "";
    public string Destination { get; set; } = "";
    public string? ExpectedSha1 { get; set; }
    public long ExpectedSize { get; set; }
    public string Label { get; set; } = "";

    public DownloadItem() { }

    public DownloadItem(string url, string destination, string? expectedSha1, long expectedSize, string label)
    {
        Url = url;
        Destination = destination;
        ExpectedSha1 = expectedSha1;
        ExpectedSize = expectedSize;
        Label = label;
    }
}
