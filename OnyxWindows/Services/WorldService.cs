using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public class WorldItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FolderPath { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid InstanceId { get; set; }
    public string InstanceName { get; set; } = "";
    public DateTime LastPlayed { get; set; }
    public long? SizeBytes { get; set; }
}

public class WorldService : Helpers.ObservableBase
{
    private static WorldService? _shared;
    public static WorldService Shared => _shared ??= new WorldService();

    private List<WorldItem> _worlds = new();
    public List<WorldItem> Worlds { get => _worlds; set => SetProperty(ref _worlds, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public WorldService() { }

    public async Task LoadWorlds(List<Instance> instances, string instancesRoot)
    {
        IsLoading = true;

        var items = await Task.Run(() =>
        {
            var foundItems = new List<WorldItem>();

            foreach (var instance in instances)
            {
                var savesDir = Path.Combine(instancesRoot, instance.DirectoryName, ".minecraft", "saves");
                if (!Directory.Exists(savesDir)) continue;

                foreach (var dir in Directory.GetDirectories(savesDir))
                {
                    try
                    {
                        var levelDat = Path.Combine(dir, "level.dat");
                        if (!File.Exists(levelDat)) continue;

                        var levelName = Path.GetFileName(dir);
                        
                        // Parse level.dat to extract true level name
                        var parsed = NbtService.ReadLevelDat(levelDat);
                        if (parsed != null && parsed.Value.Tag.Value is Dictionary<string, NbtTag> compound)
                        {
                            if (compound.TryGetValue("Data", out var dataTag) && dataTag.Value is Dictionary<string, NbtTag> data)
                            {
                                if (data.TryGetValue("LevelName", out var nameTag) && nameTag.Value is string name)
                                {
                                    levelName = name;
                                }
                            }
                            else if (compound.TryGetValue("LevelName", out var nameTag) && nameTag.Value is string name)
                            {
                                levelName = name;
                            }
                        }

                        var lastWrite = File.GetLastWriteTime(levelDat);

                        foundItems.Add(new WorldItem
                        {
                            FolderPath = dir,
                            Name = levelName,
                            InstanceId = instance.Id,
                            InstanceName = instance.Name,
                            LastPlayed = lastWrite,
                            SizeBytes = CalculateFolderSize(dir)
                        });
                    }
                    catch
                    {
                        // Ignore individual failed directories
                    }
                }
            }

            return foundItems.OrderByDescending(w => w.LastPlayed).ToList();
        });

        Worlds = items;
        IsLoading = false;
    }

    public void DeleteWorld(WorldItem item)
    {
        try
        {
            if (Directory.Exists(item.FolderPath))
            {
                Directory.Delete(item.FolderPath, true);
            }
            var updated = new List<WorldItem>(Worlds);
            updated.RemoveAll(w => w.FolderPath == item.FolderPath);
            Worlds = updated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorldService] Failed to delete world save: {ex}");
        }
    }

    private static long CalculateFolderSize(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            return files.Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
}
