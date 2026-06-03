using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public enum LogType { Info, Error, System }

public class LogLine
{
    public string Text { get; set; } = "";
    public LogType Type { get; set; } = LogType.Info;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Core launch orchestration — handles the full Minecraft launch sequence:
/// 1. Version JSON download/merge
/// 2. Libraries/assets download
/// 3. Mod loader preparation (Fabric/Forge)
/// 4. Natives extraction (using ZipFile for Windows)
/// 5. Classpath generation
/// 6. Process spawn
/// 7. Log capture and playtime tracking
/// </summary>
public class LaunchController : ObservableBase
{
    private readonly AppDataManager _appData;
    private readonly InstanceStore _instanceStore;
    private Process? _gameProcess;

    public DiscordRpcService? DiscordRpc { get; set; }

    private string _launchStatus = "";
    public string LaunchStatus { get => _launchStatus; set => SetProperty(ref _launchStatus, value); }

    private bool _isLaunching;
    public bool IsLaunching { get => _isLaunching; set => SetProperty(ref _isLaunching, value); }

    private List<LogLine> _logLines = new();
    public List<LogLine> LogLines { get => _logLines; set => SetProperty(ref _logLines, value); }

    public LaunchController(AppDataManager appData, InstanceStore instanceStore)
    {
        _appData = appData;
        _instanceStore = instanceStore;
    }

    /// <summary>
    /// Generate a deterministic offline UUID from a nickname (MD5-based, version 3).
    /// </summary>
    public static string OfflineUUID(string nickname)
    {
        var input = Encoding.UTF8.GetBytes($"OfflinePlayer:{nickname}");
        var hash = MD5.HashData(input);

        hash[6] = (byte)((hash[6] & 0x0f) | 0x30); // version 3
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80); // variant 1

        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}";
    }

    public async Task Launch(Instance instance, Account account)
    {
        await LaunchInstance(instance, account, App.Downloads);
    }

    /// <summary>
    /// Main launch entry point.
    /// </summary>
    public async Task LaunchInstance(Instance instance, Account account, DownloadManager downloadManager)
    {
        if (IsLaunching || instance.State == InstanceState.Running) return;

        IsLaunching = true;
        _logLines.Clear();
        LaunchStatus = instance.Name;

        try
        {
            _instanceStore.UpdateInstanceState(instance.Id, InstanceState.Preparing);
            AppendLog($"Launching {instance.Name} (MC {instance.MinecraftVersion})", LogType.System);

            // 1. Download/load version detail JSON
            var versionDetail = await LoadVersionDetail(instance.MinecraftVersion);
            if (versionDetail == null)
                throw new Exception($"Failed to load version detail for {instance.MinecraftVersion}");

            // 2. Merge parent version if inheritsFrom is set
            if (!string.IsNullOrEmpty(versionDetail.InheritsFrom))
            {
                var parent = await LoadVersionDetail(versionDetail.InheritsFrom);
                if (parent != null)
                    versionDetail = MergeVersionDetails(parent, versionDetail);
            }

            // 3. Build download list and download
            var builder = new InstanceBuilder(_appData);
            var downloadList = builder.BuildDownloadList(versionDetail);

            // Download version files
            _instanceStore.UpdateInstanceState(instance.Id, InstanceState.Downloading);
            var success = await downloadManager.DownloadAll(downloadList);

            // Download assets
            if (versionDetail.AssetIndex != null)
            {
                var assetItems = builder.BuildAssetDownloadList(versionDetail.AssetIndex.Id);
                success &= await downloadManager.DownloadAll(assetItems);
            }

            if (!success)
                AppendLog("Some downloads failed, attempting to continue...", LogType.Error);

            // 4. Handle mod loader
            string mainClass = versionDetail.MainClass;
            var extraJvmArgs = new List<string>();
            var extraClasspath = new List<string>();

            if (instance.ModLoader != ModLoaderType.None && !string.IsNullOrEmpty(instance.ModLoaderVersion))
            {
                AppendLog($"Preparing {instance.ModLoader} {instance.ModLoaderVersion}...", LogType.System);

                if (instance.ModLoader == ModLoaderType.Fabric || instance.ModLoader == ModLoaderType.Quilt)
                {
                    var fabricInstaller = new FabricInstaller(_appData);
                    var result = await fabricInstaller.PrepareLoader(
                        instance.MinecraftVersion,
                        instance.ModLoaderVersion,
                        instance.ModLoader == ModLoaderType.Quilt);

                    mainClass = result.MainClass;
                    extraJvmArgs.AddRange(result.JvmArgs);
                    extraClasspath.AddRange(result.Classpath);
                }
                else if (instance.ModLoader == ModLoaderType.Forge || instance.ModLoader == ModLoaderType.NeoForge)
                {
                    var javaManager = new JavaManager(_appData.JavaDirectory);
                    var javaMajor = builder.RequiredJavaVersion(versionDetail);
                    var javaPath = await javaManager.GetJavaExecutable(javaMajor);

                    var forgeInstaller = new ForgeInstaller(_appData);
                    await forgeInstaller.InstallForge(
                        instance.MinecraftVersion,
                        instance.ModLoaderVersion,
                        javaPath,
                        instance.ModLoader == ModLoaderType.NeoForge);

                    // Load Forge version JSON
                    var forgeDetail = await LoadForgeVersionDetail(instance.MinecraftVersion, instance.ModLoaderVersion, instance.ModLoader == ModLoaderType.NeoForge);
                    if (forgeDetail != null)
                    {
                        mainClass = forgeDetail.MainClass;
                        // Merge libraries
                        versionDetail.Libraries.AddRange(forgeDetail.Libraries);
                    }
                }
            }

            // 5. Prepare Java
            var javaManager2 = new JavaManager(_appData.JavaDirectory);
            var requiredJava = builder.RequiredJavaVersion(versionDetail);
            var javaExe = !string.IsNullOrEmpty(instance.CustomJavaPath)
                ? instance.CustomJavaPath
                : await javaManager2.GetJavaExecutable(requiredJava);

            AppendLog($"Using Java: {javaExe}", LogType.System);

            // 6. Extract natives
            var gameDir = Path.Combine(_appData.InstancesDirectory, instance.DirectoryName);
            var nativesDir = Path.Combine(gameDir, "natives");
            await ExtractNatives(versionDetail, nativesDir);

            // 7. Build classpath
            var classpath = builder.BuildClasspath(versionDetail);
            if (extraClasspath.Count > 0)
                classpath = string.Join(';', extraClasspath) + ";" + classpath;

            // Deduplicate
            classpath = DeduplicateClasspath(classpath.Split(';').ToList());

            // 8. Build arguments
            var nickname = account.Username;
            var uuid = account.Uuid;
            var accessToken = account.AccessToken ?? "0";
            var userType = account.Type == AccountType.Microsoft ? "msa" : "legacy";

            var args = BuildArguments(instance, versionDetail, gameDir, nativesDir,
                classpath, nickname, uuid, accessToken, userType,
                mainClass, extraJvmArgs);

            // 9. Create game directory
            if (!Directory.Exists(gameDir))
                Directory.CreateDirectory(gameDir);

            // Write options.txt if needed
            WriteOptionsFile(instance, gameDir);

            // 10. Launch process
            _instanceStore.UpdateInstanceState(instance.Id, InstanceState.Running);
            AppendLog("Starting Minecraft process...", LogType.System);

            DiscordRpc?.SetPlaying(instance.Name, instance.MinecraftVersion);

            var startTime = DateTime.UtcNow;

            _gameProcess = new Process();
            _gameProcess.StartInfo = new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = string.Join(' ', args.Select(EscapeArg)),
                WorkingDirectory = gameDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _gameProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    AppendLog(e.Data, LogType.Info);
            };

            _gameProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    AppendLog(e.Data, LogType.Error);
            };

            _gameProcess.Start();
            _gameProcess.BeginOutputReadLine();
            _gameProcess.BeginErrorReadLine();

            IsLaunching = false;

            await _gameProcess.WaitForExitAsync();

            var sessionSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            instance.TotalPlaytimeSeconds += sessionSeconds;
            instance.LastSessionSeconds = sessionSeconds;
            instance.LastPlayed = DateTime.UtcNow;
            _instanceStore.SaveInstance(instance, _appData.InstancesDirectory);

            var exitCode = _gameProcess.ExitCode;
            _instanceStore.UpdateInstanceState(instance.Id,
                exitCode == 0 ? InstanceState.Ready : InstanceState.Crashed);

            AppendLog($"Minecraft exited with code {exitCode}", exitCode == 0 ? LogType.System : LogType.Error);

            DiscordRpc?.ClearPresence();
            _gameProcess = null;
        }
        catch (Exception ex)
        {
            AppendLog($"Launch failed: {ex.Message}", LogType.Error);
            _instanceStore.UpdateInstanceState(instance.Id, InstanceState.Crashed);
            DiscordRpc?.ClearPresence();
        }
        finally
        {
            IsLaunching = false;
        }
    }

    /// <summary>
    /// Force-kill the running game process.
    /// </summary>
    public void StopGame(Instance instance)
    {
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            _instanceStore.UpdateInstanceState(instance.Id, InstanceState.Stopping);
            try { _gameProcess.Kill(entireProcessTree: true); }
            catch { }
        }
    }

    public void StopGame()
    {
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            try { _gameProcess.Kill(entireProcessTree: true); }
            catch { }
        }
    }

    // ── Version Detail Loading ──

    private async Task<VersionDetail?> LoadVersionDetail(string versionId)
    {
        var versionDir = Path.Combine(_appData.VersionsDirectory, versionId);
        var jsonPath = Path.Combine(versionDir, $"{versionId}.json");

        // Download if not cached
        if (!File.Exists(jsonPath))
        {
            var manifestJson = Path.Combine(_appData.MetaDirectory, "version_manifest_v2.json");
            if (!File.Exists(manifestJson)) return null;

            var manifestData = await File.ReadAllTextAsync(manifestJson);
            var manifest = JsonSerializer.Deserialize<VersionManifest>(manifestData);
            var entry = manifest?.Versions.FirstOrDefault(v => v.Id == versionId);
            if (entry == null) return null;

            Directory.CreateDirectory(versionDir);
            using var response = await HttpClientFactory.Shared.GetAsync(entry.Url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(jsonPath, content);
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        return JsonSerializer.Deserialize<VersionDetail>(json);
    }

    private async Task<VersionDetail?> LoadForgeVersionDetail(string mcVersion, string forgeVersion, bool isNeoForge)
    {
        var versionsDir = Path.Combine(_appData.CacheDirectory, "versions");
        if (!Directory.Exists(versionsDir)) return null;

        // Find the Forge version directory
        foreach (var dir in Directory.GetDirectories(versionsDir))
        {
            var dirName = Path.GetFileName(dir);
            var jsonFile = Path.Combine(dir, $"{dirName}.json");
            if (File.Exists(jsonFile))
            {
                var json = await File.ReadAllTextAsync(jsonFile);
                return JsonSerializer.Deserialize<VersionDetail>(json);
            }
        }
        return null;
    }

    private VersionDetail MergeVersionDetails(VersionDetail parent, VersionDetail child)
    {
        // Child overrides, parent fills gaps
        child.Downloads ??= parent.Downloads;
        child.AssetIndex ??= parent.AssetIndex;
        child.Assets ??= parent.Assets;
        child.JavaVersion ??= parent.JavaVersion;
        child.Arguments ??= parent.Arguments;
        child.MinecraftArguments ??= parent.MinecraftArguments;
        child.Logging ??= parent.Logging;

        if (string.IsNullOrEmpty(child.MainClass))
            child.MainClass = parent.MainClass;

        // Merge libraries (child first, then parent)
        var merged = new List<Library>(child.Libraries);
        merged.AddRange(parent.Libraries);
        child.Libraries = merged;

        return child;
    }

    // ── Arguments Building ──

    private List<string> BuildArguments(
        Instance instance, VersionDetail versionDetail,
        string gameDir, string nativesDir, string classpath,
        string nickname, string uuid, string accessToken, string userType,
        string mainClass, List<string> extraJvmArgs)
    {
        var args = new List<string>();

        // JVM arguments
        args.Add($"-Xmx{instance.RamMB}m");
        args.Add($"-Xms{Math.Min(instance.RamMB, 512)}m");
        args.Add($"-Djava.library.path={nativesDir}");
        args.Add($"-Dminecraft.launcher.brand=Onyx");
        args.Add($"-Dminecraft.launcher.version=1.0");

        // Custom JVM arguments
        if (!string.IsNullOrEmpty(instance.JvmArguments))
        {
            args.AddRange(instance.JvmArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Extra JVM args from mod loader
        args.AddRange(extraJvmArgs);

        // Modern version JVM arguments
        if (versionDetail.Arguments?.Jvm != null)
        {
            foreach (var element in versionDetail.Arguments.Jvm)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var arg = SubstituteTemplates(element.GetString()!, instance, versionDetail, gameDir, nativesDir, classpath, nickname, uuid, accessToken, userType);
                    args.Add(arg);
                }
                else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var cond = element.Deserialize<ConditionalArgument>();
                    if (cond != null && ShouldIncludeArgument(cond.Rules))
                    {
                        foreach (var val in cond.GetValues())
                        {
                            args.Add(SubstituteTemplates(val, instance, versionDetail, gameDir, nativesDir, classpath, nickname, uuid, accessToken, userType));
                        }
                    }
                }
            }
        }

        // Classpath
        if (!args.Any(a => a.Contains("-cp") || a.Contains("-classpath")))
        {
            args.Add("-cp");
            args.Add(classpath);
        }

        // Main class
        args.Add(mainClass);

        // Game arguments
        if (versionDetail.Arguments?.Game != null)
        {
            foreach (var element in versionDetail.Arguments.Game)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    args.Add(SubstituteTemplates(element.GetString()!, instance, versionDetail, gameDir, nativesDir, classpath, nickname, uuid, accessToken, userType));
                }
                else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var cond = element.Deserialize<ConditionalArgument>();
                    if (cond != null && ShouldIncludeArgument(cond.Rules))
                    {
                        foreach (var val in cond.GetValues())
                        {
                            args.Add(SubstituteTemplates(val, instance, versionDetail, gameDir, nativesDir, classpath, nickname, uuid, accessToken, userType));
                        }
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(versionDetail.MinecraftArguments))
        {
            // Legacy format
            var legacyArgs = versionDetail.MinecraftArguments.Split(' ');
            foreach (var arg in legacyArgs)
            {
                args.Add(SubstituteTemplates(arg, instance, versionDetail, gameDir, nativesDir, classpath, nickname, uuid, accessToken, userType));
            }
        }

        // Window size
        if (instance.WindowWidth.HasValue && instance.WindowHeight.HasValue)
        {
            args.AddRange(new[] { "--width", instance.WindowWidth.Value.ToString(), "--height", instance.WindowHeight.Value.ToString() });
        }

        // Fullscreen
        if (instance.Fullscreen)
            args.Add("--fullscreen");

        // Auto-join server
        if (!string.IsNullOrEmpty(instance.AutoJoinServer))
        {
            args.AddRange(new[] { "--server", instance.AutoJoinServer });
            if (instance.AutoJoinPort.HasValue && instance.AutoJoinPort.Value > 0)
                args.AddRange(new[] { "--port", instance.AutoJoinPort.Value.ToString() });
        }

        return args;
    }

    private bool ShouldIncludeArgument(List<Rule>? rules)
    {
        if (rules == null || rules.Count == 0) return true;

        bool allowed = false;
        foreach (var rule in rules)
        {
            bool ruleMatches;
            if (rule.Os != null)
            {
                var nameMatches = rule.Os.Name == null || rule.Os.Name == "windows";
                var archMatches = rule.Os.Arch == null || rule.Os.Arch == "x86_64" || rule.Os.Arch == "amd64" || rule.Os.Arch == "x64";
                ruleMatches = nameMatches && archMatches;
            }
            else
            {
                ruleMatches = true;
            }
            if (ruleMatches)
                allowed = rule.Action == "allow";
        }
        return allowed;
    }

    private string SubstituteTemplates(string arg, Instance instance, VersionDetail versionDetail,
        string gameDir, string nativesDir, string classpath,
        string nickname, string uuid, string accessToken, string userType)
    {
        return arg
            .Replace("${version_name}", versionDetail.Id)
            .Replace("${game_directory}", gameDir)
            .Replace("${assets_root}", _appData.AssetsDirectory)
            .Replace("${assets_index_name}", versionDetail.AssetIndex?.Id ?? "legacy")
            .Replace("${auth_uuid}", uuid)
            .Replace("${auth_access_token}", accessToken)
            .Replace("${user_type}", userType)
            .Replace("${version_type}", versionDetail.Type)
            .Replace("${natives_directory}", nativesDir)
            .Replace("${classpath}", classpath)
            .Replace("${auth_player_name}", nickname)
            .Replace("${user_properties}", "{}")
            .Replace("${library_directory}", _appData.LibrariesDirectory)
            .Replace("${classpath_separator}", ";");
    }

    // ── Natives Extraction (Windows — ZipFile instead of /usr/bin/unzip) ──

    private async Task ExtractNatives(VersionDetail detail, string nativesDir)
    {
        // Clean and recreate
        if (Directory.Exists(nativesDir))
            Directory.Delete(nativesDir, true);
        Directory.CreateDirectory(nativesDir);

        var builder = new InstanceBuilder(_appData);
        var nativeLibs = builder.NativeLibraries(detail);

        foreach (var artifact in nativeLibs)
        {
            var jarPath = Path.Combine(_appData.LibrariesDirectory, artifact.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(jarPath))
            {
                Debug.WriteLine($"[LaunchController] Native JAR not found: {artifact.Path}");
                continue;
            }

            try
            {
                using var archive = ZipFile.OpenRead(jarPath);
                foreach (var entry in archive.Entries)
                {
                    // Extract .dll files (Windows native libraries)
                    var ext = Path.GetExtension(entry.FullName).ToLowerInvariant();
                    if (ext == ".dll" || ext == ".so")
                    {
                        // Skip META-INF
                        if (entry.FullName.StartsWith("META-INF", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var destPath = Path.Combine(nativesDir, entry.Name);
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LaunchController] Failed to extract natives from {artifact.Path}: {ex.Message}");
            }
        }
        // No xattr/codesign needed on Windows!
    }

    // ── Classpath Deduplication ──

    private string DeduplicateClasspath(List<string> paths)
    {
        var artifactMap = new Dictionary<string, (string version, string path)>();
        var resultPaths = new List<string>();
        var libsDir = _appData.LibrariesDirectory;

        foreach (var path in paths)
        {
            if (path.StartsWith(libsDir, StringComparison.OrdinalIgnoreCase))
            {
                var relative = path[(libsDir.Length + 1)..];
                var parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Length >= 3)
                {
                    var version = parts[^2];
                    var baseKey = string.Join("/", parts.Take(parts.Length - 2));

                    // Handle classifier
                    var filename = parts[^1];
                    var classifier = "";
                    if (filename.Contains(version))
                    {
                        var suffix = filename[(filename.IndexOf(version) + version.Length)..];
                        suffix = suffix.Replace(".jar", "").Replace(".zip", "");
                        if (suffix.StartsWith('-'))
                            classifier = suffix[1..];
                    }
                    var artifactKey = string.IsNullOrEmpty(classifier) ? baseKey : $"{baseKey}:{classifier}";

                    if (artifactMap.TryGetValue(artifactKey, out var existing))
                    {
                        if (string.Compare(version, existing.version, StringComparison.Ordinal) > 0)
                            artifactMap[artifactKey] = (version, path);
                    }
                    else
                    {
                        artifactMap[artifactKey] = (version, path);
                    }
                }
            }
        }

        var addedArtifacts = new HashSet<string>();
        foreach (var path in paths)
        {
            if (path.StartsWith(libsDir, StringComparison.OrdinalIgnoreCase))
            {
                var relative = path[(libsDir.Length + 1)..];
                var parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Length >= 3)
                {
                    var version = parts[^2];
                    var baseKey = string.Join("/", parts.Take(parts.Length - 2));
                    var filename = parts[^1];
                    var classifier = "";
                    if (filename.Contains(version))
                    {
                        var suffix = filename[(filename.IndexOf(version) + version.Length)..];
                        suffix = suffix.Replace(".jar", "").Replace(".zip", "");
                        if (suffix.StartsWith('-'))
                            classifier = suffix[1..];
                    }
                    var artifactKey = string.IsNullOrEmpty(classifier) ? baseKey : $"{baseKey}:{classifier}";

                    if (!addedArtifacts.Contains(artifactKey) && artifactMap.TryGetValue(artifactKey, out var best))
                    {
                        resultPaths.Add(best.path);
                        addedArtifacts.Add(artifactKey);
                    }
                }
                else
                {
                    resultPaths.Add(path);
                }
            }
            else
            {
                resultPaths.Add(path);
            }
        }

        return string.Join(';', resultPaths);
    }

    // ── Helpers ──

    private void WriteOptionsFile(Instance instance, string gameDir)
    {
        var optionsPath = Path.Combine(gameDir, "options.txt");
        if (File.Exists(optionsPath)) return; // Don't overwrite existing

        var lines = new List<string>();
        if (instance.GuiScale.HasValue)
            lines.Add($"guiScale:{instance.GuiScale.Value}");

        if (lines.Count > 0)
            File.WriteAllLines(optionsPath, lines);
    }

    private void AppendLog(string text, LogType type)
    {
        var line = new LogLine { Text = text, Type = type };
        _logLines.Add(line);
        Debug.WriteLine($"[Onyx Game Log] [{type}] {text}");
        OnPropertyChanged(nameof(LogLines));
    }

    private static string EscapeArg(string arg)
    {
        if (arg.Contains(' ') && !arg.StartsWith('"'))
            return $"\"{arg}\"";
        return arg;
    }
}
