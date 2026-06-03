using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OnyxWindows.Helpers;
using OnyxWindows.Models;

namespace OnyxWindows.Services;

public enum AccountError { InvalidUsername, UsernameTaken, AuthFailed, NoMinecraftLicense, NoXboxAccount, TokenExpired, NetworkError, CredentialError }

/// <summary>
/// Account management — offline accounts, Microsoft OAuth, token storage via PasswordVault.
/// Full auth chain: MS Code → MS Token → Xbox Live → XSTS → MC Login → MC Profile.
/// </summary>
public class AccountManager : ObservableBase
{
    private List<Account> _accounts = new();
    public List<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }

    private bool _isAuthenticating;
    public bool IsAuthenticating { get => _isAuthenticating; set => SetProperty(ref _isAuthenticating, value); }

    private string? _authError;
    public string? AuthError { get => _authError; set => SetProperty(ref _authError, value); }

    public Account? ActiveAccount => _accounts.FirstOrDefault(a => a.IsActive);

    private readonly AppDataManager _appData;
    private const string VaultResource = "OnyxLauncher";
    private const string MsClientId = "00000000402b5328";

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AccountManager(AppDataManager appData)
    {
        _appData = appData;
    }

    // ── Persistence ──

    public void LoadAccounts()
    {
        var file = _appData.AccountsFile;
        if (File.Exists(file))
        {
            try
            {
                var json = File.ReadAllText(file);
                _accounts = JsonSerializer.Deserialize<List<Account>>(json, _jsonOpts) ?? new();

                // Load tokens from PasswordVault
                foreach (var acc in _accounts)
                {
                    var tokens = LoadTokenFromVault(acc.Id);
                    if (tokens != null)
                    {
                        acc.AccessToken = tokens.Value.accessToken;
                        acc.RefreshToken = tokens.Value.refreshToken;
                    }
                }
            }
            catch { _accounts = new(); }
        }

        // Migration from legacy nickname
        if (_accounts.Count == 0)
        {
            var nick = _appData.Config.Nickname;
            if (!string.IsNullOrEmpty(nick) && nick != "Player")
            {
                _accounts.Add(new Account
                {
                    Type = AccountType.Offline,
                    Username = nick,
                    Uuid = LaunchController.OfflineUUID(nick),
                    IsActive = true
                });
                SaveAccounts();
            }
        }

        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ActiveAccount));
    }

    public void SaveAccounts()
    {
        try
        {
            var json = JsonSerializer.Serialize(_accounts, _jsonOpts);
            File.WriteAllText(_appData.AccountsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountManager] Save failed: {ex.Message}");
        }
    }

    // ── Offline Accounts ──

    public bool AddOfflineAccount(string username, out string? error)
    {
        error = null;
        var trimmed = username.Trim();

        if (trimmed.Length < 3 || trimmed.Length > 16 || !trimmed.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            error = "Nickname must be 3-16 characters (a-z, A-Z, 0-9, _)";
            return false;
        }

        if (_accounts.Any(a => a.Username.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Account with this nickname already exists";
            return false;
        }

        var account = new Account
        {
            Type = AccountType.Offline,
            Username = trimmed,
            Uuid = LaunchController.OfflineUUID(trimmed),
            AccessToken = "0",
            IsActive = _accounts.Count == 0
        };

        _accounts.Add(account);
        SaveAccounts();
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ActiveAccount));
        return true;
    }

    // ── Account Management ──

    public void SetActiveAccount(Account account)
    {
        foreach (var a in _accounts)
            a.IsActive = a.Id == account.Id;
        SaveAccounts();
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ActiveAccount));
    }

    public void RemoveAccount(Account account)
    {
        DeleteTokenFromVault(account.Id);
        var skinDir = Path.Combine(_appData.SkinsDirectory, account.Id.ToString());
        if (Directory.Exists(skinDir))
            try { Directory.Delete(skinDir, true); } catch { }

        _accounts.RemoveAll(a => a.Id == account.Id);
        if (_accounts.Count > 0 && !_accounts.Any(a => a.IsActive))
            _accounts[0].IsActive = true;

        SaveAccounts();
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ActiveAccount));
    }

    public void UpdateAccountSkin(Account account, string? skinFilename)
    {
        var idx = _accounts.FindIndex(a => a.Id == account.Id);
        if (idx >= 0)
        {
            _accounts[idx].SkinFilename = skinFilename;
            SaveAccounts();
            OnPropertyChanged(nameof(Accounts));
        }
    }

    // ── Microsoft OAuth ──

    public string? GetMicrosoftAuthUrl()
    {
        IsAuthenticating = false;
        AuthError = null;
        var redirect = Uri.EscapeDataString("https://login.live.com/oauth20_desktop.srf");
        var scope = Uri.EscapeDataString("XboxLive.signin offline_access");
        return $"https://login.live.com/oauth20_authorize.srf?client_id={MsClientId}&response_type=code&redirect_uri={redirect}&scope={scope}";
    }

    public async Task CompleteMicrosoftWebAuth(string code)
    {
        IsAuthenticating = true;
        AuthError = null;

        try
        {
            // Exchange code for MS tokens
            var redirect = Uri.EscapeDataString("https://login.live.com/oauth20_desktop.srf");
            var body = $"client_id={MsClientId}&code={code}&grant_type=authorization_code&redirect_uri={redirect}";
            var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await HttpClientFactory.Shared.PostAsync("https://login.live.com/oauth20_token.srf", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var msAccessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            var msRefreshToken = doc.RootElement.GetProperty("refresh_token").GetString()!;

            await CompleteMicrosoftAuth(msAccessToken, msRefreshToken);
        }
        catch (Exception ex)
        {
            IsAuthenticating = false;
            AuthError = ex.Message;
        }
    }

    public void CancelMicrosoftAuth()
    {
        IsAuthenticating = false;
        AuthError = null;
    }

    private async Task CompleteMicrosoftAuth(string msAccessToken, string msRefreshToken)
    {
        // Xbox Live
        var (xblToken, userHash) = await AuthenticateXboxLive(msAccessToken);
        // XSTS
        var xstsToken = await AuthorizeXSTS(xblToken);
        // MC Login
        var (mcAccessToken, mcExpiresIn) = await LoginMinecraft(userHash, xstsToken);
        // MC Profile
        var (mcUsername, mcUUID) = await GetMinecraftProfile(mcAccessToken);

        var dashedUUID = FormatUUID(mcUUID);
        var existingIdx = _accounts.FindIndex(a => a.Uuid == dashedUUID);

        if (existingIdx >= 0)
        {
            _accounts[existingIdx].Username = mcUsername;
            _accounts[existingIdx].AccessToken = mcAccessToken;
            _accounts[existingIdx].RefreshToken = msRefreshToken;
            _accounts[existingIdx].TokenExpiresAt = DateTime.UtcNow.AddSeconds(mcExpiresIn);
            SaveTokenToVault(_accounts[existingIdx].Id, mcAccessToken, msRefreshToken);

            for (int i = 0; i < _accounts.Count; i++)
                _accounts[i].IsActive = i == existingIdx;
        }
        else
        {
            for (int i = 0; i < _accounts.Count; i++)
                _accounts[i].IsActive = false;

            var account = new Account
            {
                Type = AccountType.Microsoft,
                Username = mcUsername,
                Uuid = dashedUUID,
                AccessToken = mcAccessToken,
                RefreshToken = msRefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(mcExpiresIn),
                IsActive = true,
                Xuid = userHash
            };
            _accounts.Add(account);
            SaveTokenToVault(account.Id, mcAccessToken, msRefreshToken);
        }

        SaveAccounts();
        IsAuthenticating = false;
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(ActiveAccount));

        // Fetch head avatar
        var activeAcc = ActiveAccount;
        if (activeAcc != null)
            _ = FetchAndSaveHead(activeAcc);
    }

    // ── Xbox Live Auth ──

    private async Task<(string token, string userHash)> AuthenticateXboxLive(string msAccessToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = $"d={msAccessToken}" },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://user.auth.xboxlive.com/user/authenticate")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");

        var response = await HttpClientFactory.Shared.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("Xbox account not found");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("Token").GetString()!;
        var uhs = doc.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()!;
        return (token, uhs);
    }

    private async Task<string> AuthorizeXSTS(string xblToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { xblToken } },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://xsts.auth.xboxlive.com/xsts/authorize")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");

        var response = await HttpClientFactory.Shared.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("XSTS authorization denied");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Token").GetString()!;
    }

    private async Task<(string accessToken, int expiresIn)> LoginMinecraft(string userHash, string xstsToken)
    {
        var body = JsonSerializer.Serialize(new { identityToken = $"XBL3.0 x={userHash};{xstsToken}" });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await HttpClientFactory.Shared.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("No Minecraft license on this account");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return (doc.RootElement.GetProperty("access_token").GetString()!, doc.RootElement.GetProperty("expires_in").GetInt32());
    }

    private async Task<(string username, string uuid)> GetMinecraftProfile(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await HttpClientFactory.Shared.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new Exception("No Minecraft license found");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return (doc.RootElement.GetProperty("name").GetString()!, doc.RootElement.GetProperty("id").GetString()!);
    }

    // ── Token Refresh ──

    public async Task RefreshMicrosoftTokenIfNeeded(Account account)
    {
        if (account.Type != AccountType.Microsoft) return;
        if (account.TokenExpiresAt.HasValue && account.TokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5)) return;

        if (string.IsNullOrEmpty(account.RefreshToken))
            throw new Exception("Token expired — please re-authenticate");

        var scope = Uri.EscapeDataString("XboxLive.signin offline_access");
        var body = $"client_id={MsClientId}&grant_type=refresh_token&refresh_token={account.RefreshToken}&scope={scope}";
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await HttpClientFactory.Shared.PostAsync("https://login.live.com/oauth20_token.srf", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var newMsToken = doc.RootElement.GetProperty("access_token").GetString()!;
        var newRefresh = doc.RootElement.GetProperty("refresh_token").GetString()!;

        var (xbl, uh) = await AuthenticateXboxLive(newMsToken);
        var xsts = await AuthorizeXSTS(xbl);
        var (mcToken, expires) = await LoginMinecraft(uh, xsts);

        account.AccessToken = mcToken;
        account.RefreshToken = newRefresh;
        account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expires);

        var idx = _accounts.FindIndex(a => a.Id == account.Id);
        if (idx >= 0) _accounts[idx] = account;

        SaveTokenToVault(account.Id, mcToken, newRefresh);
        SaveAccounts();
    }

    // ── PasswordVault (replaces Keychain) ──

    private void SaveTokenToVault(Guid accountId, string accessToken, string? refreshToken)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var key = accountId.ToString();
            var tokenData = JsonSerializer.Serialize(new { accessToken, refreshToken });

            // Remove existing
            try { vault.Remove(vault.Retrieve(VaultResource, key)); } catch { }

            vault.Add(new Windows.Security.Credentials.PasswordCredential(VaultResource, key, tokenData));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountManager] Vault save failed: {ex.Message}");
        }
    }

    private (string accessToken, string? refreshToken)? LoadTokenFromVault(Guid accountId)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var credential = vault.Retrieve(VaultResource, accountId.ToString());
            credential.RetrievePassword();

            using var doc = JsonDocument.Parse(credential.Password);
            var at = doc.RootElement.GetProperty("accessToken").GetString()!;
            var rt = doc.RootElement.TryGetProperty("refreshToken", out var rtProp) ? rtProp.GetString() : null;
            return (at, rt);
        }
        catch { return null; }
    }

    private void DeleteTokenFromVault(Guid accountId)
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var credential = vault.Retrieve(VaultResource, accountId.ToString());
            vault.Remove(credential);
        }
        catch { }
    }

    // ── Head Avatar ──

    public async Task FetchAndSaveHead(Account account)
    {
        var cleanUUID = account.Uuid.Replace("-", "");
        if (string.IsNullOrEmpty(cleanUUID) || cleanUUID == "0") return;

        try
        {
            var url = $"https://mc-heads.net/avatar/{cleanUUID}/64";
            var data = await HttpClientFactory.Shared.GetByteArrayAsync(url);
            if (data.Length < 100) return;

            var dir = Path.Combine(_appData.SkinsDirectory, account.Id.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, "head.png"), data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountManager] Head fetch failed: {ex.Message}");
        }
    }

    public string? HeadImagePath(Account account)
    {
        if (account.SkinFilename != null)
        {
            var baseName = Path.GetFileNameWithoutExtension(account.SkinFilename);
            var headFile = Path.Combine(_appData.SkinsDirectory, $"{baseName}_head.png");
            if (File.Exists(headFile)) return headFile;
        }
        var cached = Path.Combine(_appData.SkinsDirectory, account.Id.ToString(), "head.png");
        return File.Exists(cached) ? cached : null;
    }

    // ── Helpers ──

    private static string FormatUUID(string raw)
    {
        var clean = raw.Replace("-", "");
        if (clean.Length != 32) return raw;
        return $"{clean[..8]}-{clean[8..12]}-{clean[12..16]}-{clean[16..20]}-{clean[20..]}";
    }
}
