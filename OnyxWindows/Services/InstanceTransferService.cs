using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public class ExportOptions
{
    public bool IncludeMods { get; set; } = true;
    public bool IncludeConfig { get; set; } = true;
    public bool IncludeResourcepacks { get; set; } = true;
    public bool IncludeShaderpacks { get; set; } = true;
    public bool IncludeSaves { get; set; } = false;
    public bool IncludeOptions { get; set; } = true;
}

public class OnyxProfile
{
    public string LauncherName { get; set; } = "Onyx";
    public string LauncherVersion { get; set; } = "1.0.0";
    public string InstanceName { get; set; } = "";
    public string MinecraftVersion { get; set; } = "";
    public string? ModLoader { get; set; }
    public string? ModLoaderVersion { get; set; }
    public int RamMB { get; set; } = 4096;
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}

public class InstanceTransferService : Helpers.ObservableBase
{
    private bool _isExporting;
    public bool IsExporting { get => _isExporting; set => SetProperty(ref _isExporting, value); }

    private bool _isImporting;
    public bool IsImporting { get => _isImporting; set => SetProperty(ref _isImporting, value); }

    private string _progress = "";
    public string Progress { get => _progress; set => SetProperty(ref _progress, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private readonly AppDataManager _appData;
    private readonly InstanceStore _instanceStore;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InstanceTransferService(AppDataManager appData, InstanceStore instanceStore)
    {
        _appData = appData;
        _instanceStore = instanceStore;
    }

    public async Task<bool> ExportInstance(Instance instance, string destinationZipPath, ExportOptions options)
    {
        IsExporting = true;
        Progress = "Preparing files for export...";
        ErrorMessage = null;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"OnyxExport_{instance.Id}");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            var srcMcDir = Path.Combine(_appData.InstancesDirectory.LocalPath, instance.DirectoryName, ".minecraft");

            // 1. Write Onyx Profile metadata
            var profile = new OnyxProfile
            {
                InstanceName = instance.Name,
                MinecraftVersion = instance.MinecraftVersion,
                ModLoader = instance.ModLoader.ToString().ToLower(),
                ModLoaderVersion = instance.ModLoaderVersion,
                RamMB = instance.RamMB
            };
            var profileJson = JsonSerializer.Serialize(profile, _jsonOpts);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "onyx_profile.json"), profileJson);

            // 2. Helper to copy directories
            void CopyCategory(string categoryName)
            {
                var srcPath = Path.Combine(srcMcDir, categoryName);
                if (Directory.Exists(srcPath))
                {
                    Progress = $"Copying {categoryName}...";
                    var destPath = Path.Combine(tempDir, categoryName);
                    Directory.CreateDirectory(destPath);
                    foreach (var file in Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories))
                    {
                        var rel = Path.GetRelativePath(srcPath, file);
                        var destFile = Path.Combine(destPath, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                        File.Copy(file, destFile);
                    }
                }
            }

            if (options.IncludeMods) CopyCategory("mods");
            if (options.IncludeConfig) CopyCategory("config");
            if (options.IncludeResourcepacks) CopyCategory("resourcepacks");
            if (options.IncludeShaderpacks) CopyCategory("shaderpacks");
            if (options.IncludeSaves) CopyCategory("saves");

            if (options.IncludeOptions)
            {
                var optTxt = Path.Combine(srcMcDir, "options.txt");
                if (File.Exists(optTxt))
                {
                    File.Copy(optTxt, Path.Combine(tempDir, "options.txt"));
                }
            }

            // 3. Compress to ZIP
            Progress = "Creating ZIP archive...";
            if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath);
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, destinationZipPath));

            // Clean up temp
            Directory.Delete(tempDir, true);
            Progress = "Export completed successfully!";
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    public async Task<Instance?> ImportInstance(string zipFilePath, string customName = "")
    {
        IsImporting = true;
        Progress = "Extracting ZIP package...";
        ErrorMessage = null;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"OnyxImport_{Guid.NewGuid()}");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempDir));

            // 1. Read Profile
            var profileFile = Path.Combine(tempDir, "onyx_profile.json");
            OnyxProfile? profile = null;

            if (File.Exists(profileFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(profileFile);
                    profile = JsonSerializer.Deserialize<OnyxProfile>(json, _jsonOpts);
                }
                catch { }
            }

            var instanceName = !string.IsNullOrEmpty(customName) ? customName :
                (profile != null ? profile.InstanceName : Path.GetFileNameWithoutExtension(zipFilePath));

            var mcVersion = profile != null ? profile.MinecraftVersion : "1.21";
            
            ModLoaderType loaderType = ModLoaderType.None;
            if (profile != null && !string.IsNullOrEmpty(profile.ModLoader))
            {
                if (Enum.TryParse<ModLoaderType>(profile.ModLoader, true, out var l))
                    loaderType = l;
            }

            // 2. Create Instance
            var instance = new Instance(instanceName, mcVersion)
            {
                ModLoader = loaderType,
                ModLoaderVersion = profile?.ModLoaderVersion,
                RamMB = profile?.RamMB ?? 4096
            };

            var instanceDir = Path.Combine(_appData.InstancesDirectory.LocalPath, instance.DirectoryName);
            var destMcDir = Path.Combine(instanceDir, ".minecraft");

            Directory.CreateDirectory(destMcDir);

            // 3. Move items
            Progress = "Restoring game files...";
            foreach (var dir in Directory.GetDirectories(tempDir))
            {
                var name = Path.GetFileName(dir);
                var dest = Path.Combine(destMcDir, name);
                Directory.Move(dir, dest);
            }

            foreach (var file in Directory.GetFiles(tempDir))
            {
                var name = Path.GetFileName(file);
                if (name == "onyx_profile.json") continue;
                var dest = Path.Combine(destMcDir, name);
                File.Move(file, dest);
            }

            // Clean up temp
            Directory.Delete(tempDir, true);

            // 4. Save Instance
            _instanceStore.AddInstance(instance);
            Progress = "Import completed successfully!";
            return instance;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
            return null;
        }
        finally
        {
            IsImporting = false;
        }
    }
}
