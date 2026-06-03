using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

/// <summary>
/// Manages the collection of Minecraft instances.
/// Each instance lives in its own directory with an instance.json file.
/// </summary>
public class InstanceStore : ObservableBase
{
    private List<Instance> _instances = new();
    public List<Instance> AllInstances
    {
        get => _instances;
        set => SetProperty(ref _instances, value);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Loads all instances from the instances directory.
    /// Each subdirectory containing an instance.json is loaded.
    /// </summary>
    public void LoadInstances(string instancesDir)
    {
        var loaded = new List<Instance>();

        if (!Directory.Exists(instancesDir)) return;

        foreach (var dir in Directory.GetDirectories(instancesDir))
        {
            var jsonPath = Path.Combine(dir, "instance.json");
            if (!File.Exists(jsonPath)) continue;

            try
            {
                var json = File.ReadAllText(jsonPath);
                var instance = JsonSerializer.Deserialize<Instance>(json, _jsonOptions);
                if (instance != null)
                {
                    // Determine initial state based on whether version files exist
                    var versionDir = Path.Combine(
                        Path.GetDirectoryName(instancesDir)!, "versions",
                        instance.MinecraftVersion);
                    instance.State = Directory.Exists(versionDir)
                        ? InstanceState.Ready
                        : InstanceState.NotDownloaded;

                    loaded.Add(instance);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InstanceStore] Failed to load {dir}: {ex.Message}");
            }
        }

        AllInstances = loaded.OrderByDescending(i => i.LastPlayed ?? i.CreatedAt).ToList();
        OnPropertyChanged(nameof(AllInstances));
    }

    /// <summary>
    /// Saves a single instance to its directory.
    /// </summary>
    public void SaveInstance(Instance instance)
    {
        SaveInstance(instance, App.AppData.InstancesDirectory);
    }

    public void SaveInstance(Instance instance, string instancesDir)
    {
        var dir = Path.Combine(instancesDir, instance.DirectoryName);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var jsonPath = Path.Combine(dir, "instance.json");
        var json = JsonSerializer.Serialize(instance, _jsonOptions);
        File.WriteAllText(jsonPath, json);
    }

    /// <summary>
    /// Adds a new instance and saves it.
    /// </summary>
    public void AddInstance(Instance instance)
    {
        AddInstance(instance, App.AppData.InstancesDirectory);
    }

    public void AddInstance(Instance instance, string instancesDir)
    {
        AllInstances.Add(instance);
        SaveInstance(instance, instancesDir);
        OnPropertyChanged(nameof(AllInstances));
    }

    /// <summary>
    /// Removes an instance and deletes its directory.
    /// </summary>
    public void RemoveInstance(Instance instance)
    {
        RemoveInstance(instance, App.AppData.InstancesDirectory);
    }

    public void RemoveInstance(Instance instance, string instancesDir)
    {
        AllInstances.Remove(instance);

        var dir = Path.Combine(instancesDir, instance.DirectoryName);
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InstanceStore] Failed to delete {dir}: {ex.Message}");
            }
        }

        OnPropertyChanged(nameof(AllInstances));
    }

    /// <summary>
    /// Duplicates an instance with a new name.
    /// </summary>
    public Instance? DuplicateInstance(Instance source, string instancesDir)
    {
        var copy = new Instance
        {
            Name = $"{source.Name} (Copy)",
            MinecraftVersion = source.MinecraftVersion,
            ModLoader = source.ModLoader,
            ModLoaderVersion = source.ModLoaderVersion,
            RamMB = source.RamMB,
            JvmArguments = source.JvmArguments,
            WindowWidth = source.WindowWidth,
            WindowHeight = source.WindowHeight,
            Fullscreen = source.Fullscreen,
            GuiScale = source.GuiScale,
            State = InstanceState.NotDownloaded
        };

        var sourceDir = Path.Combine(instancesDir, source.DirectoryName);
        var destDir = Path.Combine(instancesDir, copy.DirectoryName);

        try
        {
            CopyDirectory(sourceDir, destDir);

            // Save the new instance.json
            SaveInstance(copy, instancesDir);
            AllInstances.Add(copy);
            OnPropertyChanged(nameof(AllInstances));
            return copy;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InstanceStore] Failed to duplicate: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an instance's state and notifies observers.
    /// </summary>
    public void UpdateInstanceState(Guid instanceId, InstanceState state)
    {
        var instance = AllInstances.FirstOrDefault(i => i.Id == instanceId);
        if (instance != null)
        {
            instance.State = state;
            OnPropertyChanged(nameof(AllInstances));
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }
}
