using System;
using System.IO;
using System.Text.Json;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

/// <summary>
/// Central hub for all file paths and configuration I/O.
/// Base directory: %APPDATA%\Onyx
/// Mirrors the macOS AppDataManager.
/// </summary>
public class AppDataManager
{
    // ── Root ──
    public string BaseDirectory { get; }

    // ── Subdirectories ──
    public string InstancesDirectory => Path.Combine(BaseDirectory, "instances");
    public string VersionsDirectory => Path.Combine(BaseDirectory, "versions");
    public string LibrariesDirectory => Path.Combine(BaseDirectory, "libraries");
    public string AssetsDirectory => Path.Combine(BaseDirectory, "assets");
    public string AssetsIndexesDirectory => Path.Combine(AssetsDirectory, "indexes");
    public string AssetsObjectsDirectory => Path.Combine(AssetsDirectory, "objects");
    public string MetaDirectory => Path.Combine(BaseDirectory, "meta");
    public string JavaDirectory => Path.Combine(BaseDirectory, "java");
    public string SkinsDirectory => Path.Combine(BaseDirectory, "skins");
    public string IconsDirectory => Path.Combine(BaseDirectory, "icons");
    public string CacheDirectory => Path.Combine(BaseDirectory, "cache");

    // ── Files ──
    public string ConfigFile => Path.Combine(BaseDirectory, "config.json");
    public string AccountsFile => Path.Combine(BaseDirectory, "accounts.json");

    // ── Config ──
    public GlobalConfig Config { get; set; }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppDataManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        BaseDirectory = Path.Combine(appData, "Onyx");
        Config = new GlobalConfig();
        LoadConfig();
    }

    /// <summary>
    /// Creates all required directories if they don't exist.
    /// </summary>
    public void InitializeDirectories()
    {
        var dirs = new[]
        {
            BaseDirectory, InstancesDirectory, VersionsDirectory, LibrariesDirectory,
            AssetsDirectory, AssetsIndexesDirectory, AssetsObjectsDirectory,
            MetaDirectory, JavaDirectory, SkinsDirectory, IconsDirectory, CacheDirectory
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Loads GlobalConfig from config.json, or creates a default.
    /// </summary>
    public void LoadConfig()
    {
        if (File.Exists(ConfigFile))
        {
            try
            {
                var json = File.ReadAllText(ConfigFile);
                Config = JsonSerializer.Deserialize<GlobalConfig>(json, _jsonOptions) ?? new GlobalConfig();
            }
            catch
            {
                Config = new GlobalConfig();
            }
        }
        else
        {
            Config = new GlobalConfig();
        }
    }

    /// <summary>
    /// Saves GlobalConfig to config.json.
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOptions);
            File.WriteAllText(ConfigFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDataManager] Failed to save config: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a Maven-style library name (group:artifact:version) to a file path.
    /// e.g. "net.fabricmc:intermediary:1.20.4" → "net/fabricmc/intermediary/1.20.4/intermediary-1.20.4.jar"
    /// </summary>
    public string LibraryPath(string mavenName)
    {
        var parts = mavenName.Split(':');
        if (parts.Length < 3) return mavenName;

        var group = parts[0].Replace('.', Path.DirectorySeparatorChar);
        var artifact = parts[1];
        var version = parts[2];

        var relativePath = Path.Combine(group, artifact, version, $"{artifact}-{version}.jar");
        return Path.Combine(LibrariesDirectory, relativePath);
    }

    /// <summary>
    /// Gets the instance directory for a given instance.
    /// </summary>
    public string GetInstanceDirectory(Instance instance)
    {
        return Path.Combine(InstancesDirectory, instance.DirectoryName);
    }
}
