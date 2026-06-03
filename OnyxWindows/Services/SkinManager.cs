using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public class SavedSkin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Filename { get; set; } = ""; // filename in skins directory
    public string Source { get; set; } = "file"; // "nickname", "file", "browse"
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public class BrowsableSkin
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string PreviewUrl { get; set; } = "";
    public string HeadUrl { get; set; } = "";
    public string SkinTextureUrl { get; set; } = "";
    public string Uuid { get; set; } = "";
}

public class SkinManager : Helpers.ObservableBase
{
    private List<SavedSkin> _savedSkins = new();
    public List<SavedSkin> SavedSkins { get => _savedSkins; set => SetProperty(ref _savedSkins, value); }

    private SavedSkin? _activeSkin;
    public SavedSkin? ActiveSkin { get => _activeSkin; set => SetProperty(ref _activeSkin, value); }

    private bool _isFetching;
    public bool IsFetching { get => _isFetching; set => SetProperty(ref _isFetching, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private List<BrowsableSkin> _browseResults = new();
    public List<BrowsableSkin> BrowseResults { get => _browseResults; set => SetProperty(ref _browseResults, value); }

    private bool _isSearchingSkins;
    public bool IsSearchingSkins { get => _isSearchingSkins; set => SetProperty(ref _isSearchingSkins, value); }

    private bool _isLoadingMore;
    public bool IsLoadingMore { get => _isLoadingMore; set => SetProperty(ref _isLoadingMore, value); }

    private HashSet<string> _downloadingSkinIds = new();
    public HashSet<string> DownloadingSkinIds { get => _downloadingSkinIds; set => SetProperty(ref _downloadingSkinIds, value); }

    private bool _hasMoreSkins = true;
    public bool HasMoreSkins { get => _hasMoreSkins; set => SetProperty(ref _hasMoreSkins, value); }

    private readonly AppDataManager _appData;
    private string LibraryFile => Path.Combine(_appData.SkinsDirectory.LocalPath, "library.json");

    private List<string> _shuffledPool = new();
    private int _loadedIndex = 0;
    private const int BatchSize = 10;

    private readonly List<string> _popularNames = new()
    {
        "Notch", "jeb_", "Dinnerbone", "Grumm", "Searge", "Marc_IRL", "_LadyAgnes", "MansOlson",
        "Dream", "GeorgeNotFound", "Sapnap", "BadBoyHalo", "TommyInnit", "Tubbo", "WilburSoot", "Philza",
        "Technoblade", "Ranboo", "KarlJacobs", "Quackity", "Skeppy", "eret", "Nihachu", "JackManifoldTV",
        "Punz", "Purpled", "Antfrost", "awesamdude", "Foolish_Gamers", "Hannahxxrose",
        "Grian", "Mumbo", "iskall85", "Stressmonster", "FalseSymmetry", "PearlescentMoon", "Renthedog",
        "TangoTek", "impulseSV", "BdoubleO100", "Keralis", "Welsknight", "xisumavoid", "cubfan135",
        "ClownPierce", "Rekrap2", "Branzy", "SpokeIsHere", "ParrotX2", "Zam", "Ashswag", "Roier",
        "CaptainSparklez", "DanTDM", "PopularMMOs", "Stampy", "iBallisticSquid", "LDShadowLady",
        "Cellbit", "Forever", "Felps", "BagheraJones", "AntoineDaniel", "Kameto", "Etoiles", "Aypierre",
        "Fit", "FitMC", "SalC1", "Baritone", "Toycat", "ibxtoycat", "Herobrine", "Hypixel", "Simon"
    };

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SkinManager(AppDataManager appData)
    {
        _appData = appData;
        LoadLibrary();
    }

    public void LoadLibrary()
    {
        if (File.Exists(LibraryFile))
        {
            try
            {
                var json = File.ReadAllText(LibraryFile);
                SavedSkins = JsonSerializer.Deserialize<List<SavedSkin>>(json, _jsonOpts) ?? new();
            }
            catch
            {
                SavedSkins = new();
            }
        }
        else
        {
            SavedSkins = new();
        }

        if (_appData.GlobalConfig.ActiveSkinName != null)
        {
            ActiveSkin = SavedSkins.FirstOrDefault(s => s.Name == _appData.GlobalConfig.ActiveSkinName);
        }
    }

    private void SaveLibrary()
    {
        try
        {
            var json = JsonSerializer.Serialize(SavedSkins, _jsonOpts);
            File.WriteAllText(LibraryFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Save library failed: {ex}");
        }
    }

    public async Task BrowsePopular()
    {
        IsSearchingSkins = true;
        BrowseResults = new();
        _loadedIndex = 0;
        HasMoreSkins = true;

        var unique = _popularNames.Distinct().ToList();
        var rng = new Random();
        _shuffledPool = unique.OrderBy(x => rng.Next()).ToList();

        await LoadNextBatch();
        IsSearchingSkins = false;
    }

    public async Task LoadMoreSkins()
    {
        if (IsLoadingMore || !HasMoreSkins) return;
        IsLoadingMore = true;
        await LoadNextBatch();
        IsLoadingMore = false;
    }

    private Task LoadNextBatch()
    {
        if (_shuffledPool.Count == 0) return Task.CompletedTask;

        if (_loadedIndex >= _shuffledPool.Count)
        {
            var rng = new Random();
            _shuffledPool = _shuffledPool.OrderBy(x => rng.Next()).ToList();
            _loadedIndex = 0;
        }

        var end = Math.Min(_loadedIndex + BatchSize, _shuffledPool.Count);
        var batch = _shuffledPool.GetRange(_loadedIndex, end - _loadedIndex);
        _loadedIndex = end;

        HasMoreSkins = true;

        var updatedResults = new List<BrowsableSkin>(BrowseResults);
        foreach (var name in batch)
        {
            var uniqueId = $"{name}_{Guid.NewGuid().ToString().Substring(0, 6)}";
            var skin = new BrowsableSkin
            {
                Id = uniqueId,
                Name = name,
                PreviewUrl = $"https://mc-heads.net/body/{name}/150",
                HeadUrl = $"https://mc-heads.net/avatar/{name}/128",
                SkinTextureUrl = $"https://mc-heads.net/skin/{name}",
                Uuid = name
            };
            updatedResults.Add(skin);
        }
        BrowseResults = updatedResults;
        return Task.CompletedTask;
    }

    public async Task SearchSkin(string query)
    {
        var q = query?.Trim();
        if (string.IsNullOrEmpty(q))
        {
            await BrowsePopular();
            return;
        }

        IsSearchingSkins = true;
        BrowseResults = new();

        var clean = q.Replace("-", "");
        var isUuid = clean.Length >= 28 && clean.All(c => "0123456789abcdefABCDEF".Contains(c));

        if (isUuid)
        {
            var skin = new BrowsableSkin
            {
                Id = clean,
                Name = "UUID: " + clean.Substring(0, Math.Min(8, clean.Length)),
                PreviewUrl = $"https://mc-heads.net/body/{clean}/150",
                HeadUrl = $"https://mc-heads.net/avatar/{clean}/128",
                SkinTextureUrl = $"https://mc-heads.net/skin/{clean}",
                Uuid = clean
            };
            BrowseResults = new List<BrowsableSkin> { skin };
        }
        else
        {
            var resolved = await ResolveSkinForUsername(q);
            if (resolved != null)
            {
                BrowseResults = new List<BrowsableSkin> { resolved };
            }
            else
            {
                var skin = new BrowsableSkin
                {
                    Id = q,
                    Name = q,
                    PreviewUrl = $"https://mc-heads.net/body/{q}/150",
                    HeadUrl = $"https://mc-heads.net/avatar/{q}/128",
                    SkinTextureUrl = $"https://mc-heads.net/skin/{q}",
                    Uuid = q
                };
                BrowseResults = new List<BrowsableSkin> { skin };
            }
        }

        IsSearchingSkins = false;
    }

    private async Task<BrowsableSkin?> ResolveSkinForUsername(string username)
    {
        try
        {
            var profileUrl = $"https://api.mojang.com/users/profiles/minecraft/{username}";
            var response = await HttpClientFactory.Shared.GetAsync(profileUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var uuid = root.GetProperty("id").GetString()!;
            var name = root.GetProperty("name").GetString()!;

            return new BrowsableSkin
            {
                Id = uuid,
                Name = name,
                PreviewUrl = $"https://mc-heads.net/body/{uuid}/150",
                HeadUrl = $"https://mc-heads.net/avatar/{uuid}/128",
                SkinTextureUrl = $"https://mc-heads.net/skin/{uuid}",
                Uuid = uuid
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadBrowsableSkin(BrowsableSkin skin, Account? account = null, AccountManager? accountManager = null)
    {
        lock (_downloadingSkinIds)
        {
            _downloadingSkinIds.Add(skin.Id);
        }

        try
        {
            var filename = $"{skin.Name.ToLower()}_{skin.Id.Substring(0, Math.Min(8, skin.Id.Length))}.png";
            var destPath = Path.Combine(_appData.SkinsDirectory.LocalPath, filename);

            var skinBytes = await HttpClientFactory.Shared.GetByteArrayAsync(skin.SkinTextureUrl);
            await File.WriteAllBytesAsync(destPath, skinBytes);

            var previewBytes = await HttpClientFactory.Shared.GetByteArrayAsync(skin.PreviewUrl);
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var renderPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_render.png");
            await File.WriteAllBytesAsync(renderPath, previewBytes);

            var headBytes = await HttpClientFactory.Shared.GetByteArrayAsync(skin.HeadUrl);
            var headPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_head.png");
            await File.WriteAllBytesAsync(headPath, headBytes);

            var entry = new SavedSkin { Name = skin.Name, Filename = filename, Source = "browse" };
            var list = new List<SavedSkin>(SavedSkins);
            list.RemoveAll(s => s.Name == skin.Name && s.Source == "browse");
            list.Add(entry);
            SavedSkins = list;
            SaveLibrary();

            if (account != null && accountManager != null)
            {
                accountManager.UpdateAccountSkin(account, filename);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Failed to download skin: {ex}");
        }
        finally
        {
            lock (_downloadingSkinIds)
            {
                _downloadingSkinIds.Remove(skin.Id);
            }
        }
    }

    public bool IsInLibrary(BrowsableSkin skin)
    {
        return SavedSkins.Any(s => s.Name == skin.Name);
    }

    public async Task FetchSkinFromNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname)) return;
        IsFetching = true;
        ErrorMessage = null;

        try
        {
            var profileUrl = $"https://api.mojang.com/users/profiles/minecraft/{nickname}";
            var response = await HttpClientFactory.Shared.GetAsync(profileUrl);

            string uuid;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                uuid = doc.RootElement.GetProperty("id").GetString()!;
            }
            else
            {
                uuid = "8667ba71b85a4004af54457a9734eed7";
            }

            var skinBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/skin/{uuid}");
            var filename = $"{nickname.ToLower()}_default.png";
            var destPath = Path.Combine(_appData.SkinsDirectory.LocalPath, filename);
            await File.WriteAllBytesAsync(destPath, skinBytes);

            var renderBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/body/{uuid}/150");
            var renderPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{nickname.ToLower()}_default_render.png");
            await File.WriteAllBytesAsync(renderPath, renderBytes);

            var headBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/avatar/{uuid}/128");
            var headPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{nickname.ToLower()}_default_head.png");
            await File.WriteAllBytesAsync(headPath, headBytes);

            var skinEntry = new SavedSkin { Name = $"{nickname} (Default)", Filename = filename, Source = "nickname" };
            var list = new List<SavedSkin>(SavedSkins);
            list.RemoveAll(s => s.Source == "nickname" && s.Name.Contains(nickname, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, skinEntry);
            SavedSkins = list;
            SaveLibrary();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to fetch skin: {ex.Message}";
        }
        finally
        {
            IsFetching = false;
        }
    }

    public async Task FetchSkinFromUUID(string uuid, Account? account = null, AccountManager? accountManager = null)
    {
        var cleanUuid = uuid?.Trim().Replace("-", "");
        if (string.IsNullOrEmpty(cleanUuid)) return;
        IsFetching = true;
        ErrorMessage = null;

        try
        {
            var hash = cleanUuid.Substring(0, Math.Min(8, cleanUuid.Length)).ToLower();
            var filename = $"uuid_{hash}.png";
            var destPath = Path.Combine(_appData.SkinsDirectory.LocalPath, filename);

            var skinBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/skin/{cleanUuid}");
            await File.WriteAllBytesAsync(destPath, skinBytes);

            var renderBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/body/{cleanUuid}/150");
            var renderPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"uuid_{hash}_render.png");
            await File.WriteAllBytesAsync(renderPath, renderBytes);

            var headBytes = await HttpClientFactory.Shared.GetByteArrayAsync($"https://mc-heads.net/avatar/{cleanUuid}/128");
            var headPath = Path.Combine(_appData.SkinsDirectory.LocalPath, $"uuid_{hash}_head.png");
            await File.WriteAllBytesAsync(headPath, headBytes);

            var list = new List<SavedSkin>(SavedSkins);
            list.RemoveAll(s => s.Filename == filename);

            var skinEntry = new SavedSkin { Name = cleanUuid.Substring(0, Math.Min(16, cleanUuid.Length)), Filename = filename, Source = "uuid" };
            list.Add(skinEntry);
            SavedSkins = list;
            SaveLibrary();

            if (account != null && accountManager != null)
            {
                accountManager.UpdateAccountSkin(account, filename);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to fetch skin: {ex.Message}";
        }
        finally
        {
            IsFetching = false;
        }
    }

    public void SetActiveSkin(SavedSkin? skin)
    {
        ActiveSkin = skin;
        _appData.GlobalConfig.ActiveSkinName = skin?.Name;
        _appData.SaveConfig();
    }

    public static async Task ApplySkinToInstances(string? skinFilename, string skinsDir, string instancesDir, List<Account> accounts)
    {
        if (!Directory.Exists(instancesDir)) return;

        foreach (var instanceDir in Directory.GetDirectories(instancesDir))
        {
            var mcDir = Path.Combine(instanceDir, ".minecraft");
            if (!Directory.Exists(mcDir)) continue;

            foreach (var account in accounts)
            {
                await ApplySkinToInstance(skinFilename, account.Username, skinsDir, mcDir);
            }
        }
    }

    public static Task ApplySkinToInstance(string? skinFilename, string accountUsername, string skinsDir, string instanceMinecraftDir)
    {
        var cslConfigDir = Path.Combine(instanceMinecraftDir, "CustomSkinLoader");
        var localSkinTexturesDir = Path.Combine(cslConfigDir, "LocalSkin", "skins");
        var cslCapeDir = Path.Combine(cslConfigDir, "LocalSkin", "capes");

        Directory.CreateDirectory(localSkinTexturesDir);
        Directory.CreateDirectory(cslCapeDir);

        var skinDest = Path.Combine(localSkinTexturesDir, $"{accountUsername}.png");
        if (File.Exists(skinDest))
        {
            try { File.Delete(skinDest); } catch { }
        }

        if (skinFilename == null) return Task.CompletedTask;

        var skinSource = Path.Combine(skinsDir, skinFilename);
        if (!File.Exists(skinSource))
        {
            Console.WriteLine($"[SkinManager] Skin file not found: {skinFilename}");
            return Task.CompletedTask;
        }

        try
        {
            File.Copy(skinSource, skinDest, true);
            Console.WriteLine($"[SkinManager] Skin texture copied for {accountUsername}: {skinFilename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Failed to copy skin for {accountUsername}: {ex}");
        }

        var cslConfigFile = Path.Combine(cslConfigDir, "CustomSkinLoader.json");
        var cslConfig = new Dictionary<string, object>
        {
            ["version"] = "14.16",
            ["enable"] = true,
            ["enableDynamicSkull"] = true,
            ["enableTransparentSkin"] = true,
            ["enableCape"] = true,
            ["threadSTInterval"] = 3,
            ["forceUpdateSkull"] = false,
            ["enableLocalProfileCache"] = true,
            ["enableCacheAutoClean"] = false,
            ["loadlist"] = new List<object>
            {
                new Dictionary<string, string>
                {
                    ["name"] = "LocalSkin",
                    ["type"] = "LocalSkin",
                    ["skin"] = "LocalSkin/skins/{USERNAME}.png",
                    ["cape"] = "LocalSkin/capes/{USERNAME}.png",
                    ["model"] = "auto"
                },
                new Dictionary<string, string>
                {
                    ["name"] = "Mojang",
                    ["type"] = "MojangAPI"
                },
                new Dictionary<string, string>
                {
                    ["name"] = "CustomSkinAPI (LittleSkin)",
                    ["type"] = "CustomSkinAPI",
                    ["root"] = "https://littleskin.cn/csl/"
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(cslConfig, _jsonOpts);
            File.WriteAllText(cslConfigFile, json);
            Console.WriteLine($"[SkinManager] CustomSkinLoader.json written at {cslConfigFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinManager] Failed to write CSL config: {ex}");
        }

        return Task.CompletedTask;
    }

    public static bool IsCslInstalled(string instanceDir)
    {
        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");
        if (!Directory.Exists(modsDir)) return false;

        foreach (var file in Directory.GetFiles(modsDir))
        {
            if (Path.GetFileName(file).ToLower().Contains("customskinloader"))
                return true;
        }
        return false;
    }

    public static async Task InstallCslToInstance(string instanceDir, string minecraftVersion, ModLoaderType? modLoader)
    {
        if (modLoader == null)
        {
            throw new Exception("Mod loader required for custom skins");
        }

        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");
        Directory.CreateDirectory(modsDir);

        var loaderName = modLoader.Value.ToString().ToLower();
        var url = $"https://api.modrinth.com/v2/project/customskinloader/version?game_versions=%5B%22{minecraftVersion}%22%5D&loaders=%5B%22{loaderName}%22%5D";

        var response = await HttpClientFactory.Shared.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var versions = doc.RootElement;

        if (versions.ValueKind == JsonValueKind.Array && versions.GetArrayLength() > 0)
        {
            var firstVersion = versions[0];
            var files = firstVersion.GetProperty("files");
            JsonElement? primaryFile = null;

            foreach (var file in files.EnumerateArray())
            {
                if (file.TryGetProperty("primary", out var prim) && prim.GetBoolean())
                {
                    primaryFile = file;
                    break;
                }
            }

            if (primaryFile == null && files.GetArrayLength() > 0)
            {
                primaryFile = files[0];
            }

            if (primaryFile != null)
            {
                var downloadUrl = primaryFile.Value.GetProperty("url").GetString()!;
                var filename = primaryFile.Value.GetProperty("filename").GetString()!;

                var fileBytes = await HttpClientFactory.Shared.GetByteArrayAsync(downloadUrl);
                var destFile = Path.Combine(modsDir, filename);
                await File.WriteAllBytesAsync(destFile, fileBytes);
                return;
            }
        }

        throw new Exception("Skin mod version not found");
    }

    public void DeleteSkin(SavedSkin skin)
    {
        var skinFile = Path.Combine(_appData.SkinsDirectory.LocalPath, skin.Filename);
        if (File.Exists(skinFile)) try { File.Delete(skinFile); } catch { }

        var baseName = Path.GetFileNameWithoutExtension(skin.Filename);
        var renderFile = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_render.png");
        if (File.Exists(renderFile)) try { File.Delete(renderFile); } catch { }

        var headFile = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_head.png");
        if (File.Exists(headFile)) try { File.Delete(headFile); } catch { }

        var list = new List<SavedSkin>(SavedSkins);
        list.RemoveAll(s => s.Id == skin.Id);
        SavedSkins = list;

        if (ActiveSkin?.Id == skin.Id) SetActiveSkin(null);
        SaveLibrary();
    }

    public string? GetSkinImage(SavedSkin skin)
    {
        var path = Path.Combine(_appData.SkinsDirectory.LocalPath, skin.Filename);
        return File.Exists(path) ? path : null;
    }

    public string? GetRenderImage(SavedSkin skin)
    {
        var baseName = Path.GetFileNameWithoutExtension(skin.Filename);
        var path = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_render.png");
        return File.Exists(path) ? path : null;
    }

    public string? GetHeadImage(SavedSkin skin)
    {
        var baseName = Path.GetFileNameWithoutExtension(skin.Filename);
        var path = Path.Combine(_appData.SkinsDirectory.LocalPath, $"{baseName}_head.png");
        return File.Exists(path) ? path : null;
    }
}
