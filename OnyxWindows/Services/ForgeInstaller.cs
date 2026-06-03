using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Helpers;

namespace OnyxWindows.Services;

/// <summary>
/// Forge/NeoForge mod loader installer. Downloads and runs the installer JAR.
/// Uses System.IO.Compression for legacy Forge instead of /usr/bin/unzip.
/// </summary>
public class ForgeInstaller : ObservableBase
{
    private bool _isInstalling;
    public bool IsInstalling { get => _isInstalling; set => SetProperty(ref _isInstalling, value); }

    private string _status = "";
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    private readonly AppDataManager _appData;

    public ForgeInstaller(AppDataManager appData) => _appData = appData;

    public async Task InstallForge(string mcVersion, string forgeVersion, string javaPath, bool isNeoForge)
    {
        IsInstalling = true;
        try
        {
            Status = $"Downloading {(isNeoForge ? "NeoForge" : "Forge")} Installer...";

            var versionString = isNeoForge ? forgeVersion :
                (forgeVersion.StartsWith($"{mcVersion}-") ? forgeVersion : $"{mcVersion}-{forgeVersion}");

            var prefix = isNeoForge ? "neoforge" : "forge";
            var installerPath = Path.Combine(_appData.CacheDirectory, $"{prefix}-installer-{versionString}.jar");

            bool shouldDownload = true;
            if (File.Exists(installerPath) && IsValidJar(installerPath))
                shouldDownload = false;
            else if (File.Exists(installerPath))
                File.Delete(installerPath);

            if (shouldDownload)
            {
                string downloadUrl;
                if (isNeoForge)
                {
                    downloadUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{forgeVersion}/neoforge-{forgeVersion}-installer.jar";
                }
                else
                {
                    downloadUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{versionString}/forge-{versionString}-installer.jar";
                }

                var data = await HttpClientFactory.Shared.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(installerPath, data);
            }

            // Legacy Forge (MC <= 1.12.x)
            if (!isNeoForge && IsLegacyForge(mcVersion))
            {
                Status = "Extracting Legacy Forge...";
                await InstallLegacyForge(mcVersion, installerPath);
                Status = "Forge installed!";
                try { File.Delete(installerPath); } catch { }
                return;
            }

            // Modern Forge — create launcher_profiles.json stub
            var profilesPath = Path.Combine(_appData.CacheDirectory, "launcher_profiles.json");
            if (!File.Exists(profilesPath))
                await File.WriteAllTextAsync(profilesPath, "{ \"profiles\": {} }");

            Status = $"Running {(isNeoForge ? "NeoForge" : "Forge")} Installer...";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = $"-jar \"{installerPath}\" --installClient \"{_appData.CacheDirectory}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                if (e.Data.Contains("Downloading")) Status = "Downloading libraries...";
                else if (e.Data.Contains("Extracting")) Status = "Extracting files...";
                else if (e.Data.Contains("Processor")) Status = "Running processors...";
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"{(isNeoForge ? "NeoForge" : "Forge")} installation failed (exit code {process.ExitCode})");

            Status = $"{(isNeoForge ? "NeoForge" : "Forge")} installed!";
            try { File.Delete(installerPath); } catch { }
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private bool IsLegacyForge(string mcVersion)
    {
        var parts = mcVersion.Split('.').Select(p => int.TryParse(new string(p.TakeWhile(char.IsDigit).ToArray()), out var n) ? n : 0).ToArray();
        return parts.Length >= 2 && parts[0] == 1 && parts[1] <= 12;
    }

    private async Task InstallLegacyForge(string mcVersion, string installerPath)
    {
        using var archive = ZipFile.OpenRead(installerPath);

        // Extract install_profile.json
        var profileEntry = archive.GetEntry("install_profile.json");
        if (profileEntry == null) throw new Exception("install_profile.json not found in Forge installer");

        using var profileStream = profileEntry.Open();
        using var reader = new StreamReader(profileStream);
        var profileJson = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(profileJson);
        var root = doc.RootElement;

        var versionInfo = root.GetProperty("versionInfo");
        var installInfo = root.GetProperty("install");

        var id = versionInfo.GetProperty("id").GetString()!;
        var versionsDir = Path.Combine(_appData.CacheDirectory, "versions");
        var targetDir = Path.Combine(versionsDir, id);
        Directory.CreateDirectory(targetDir);

        // Save version JSON
        var versionData = versionInfo.GetRawText();
        await File.WriteAllTextAsync(Path.Combine(targetDir, $"{id}.json"), versionData);

        // Extract Forge JAR
        var filePath = installInfo.GetProperty("filePath").GetString()!;
        var mavenPath = installInfo.GetProperty("path").GetString()!;

        var parts = mavenPath.Split(':');
        if (parts.Length < 3) throw new Exception("Invalid Maven path");

        var group = parts[0].Replace('.', Path.DirectorySeparatorChar);
        var artifact = parts[1];
        var version = parts[2];
        var relJarPath = Path.Combine(group, artifact, version, $"{artifact}-{version}.jar");
        var targetJarPath = Path.Combine(_appData.LibrariesDirectory, relJarPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetJarPath)!);

        var jarEntry = archive.GetEntry(filePath);
        if (jarEntry != null)
        {
            jarEntry.ExtractToFile(targetJarPath, overwrite: true);
        }
    }

    private static bool IsValidJar(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var header = new byte[4];
            if (fs.Read(header, 0, 4) < 4) return false;
            return header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
        }
        catch { return false; }
    }
}
