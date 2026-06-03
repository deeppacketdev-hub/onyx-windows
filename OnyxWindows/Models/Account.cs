using System;
using System.Text.Json.Serialization;

namespace OnyxWindows.Models;

/// <summary>
/// Account type — offline or Microsoft (licensed).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountType
{
    Offline,
    Microsoft
}

/// <summary>
/// User account for Minecraft. Tokens are NOT serialized to JSON — they are stored in PasswordVault.
/// </summary>
public class Account
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("type")]
    public AccountType Type { get; set; } = AccountType.Offline;

    [JsonPropertyName("username")]
    public string Username { get; set; } = "Player";

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = "0";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = false;

    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("skinFilename")]
    public string? SkinFilename { get; set; }

    [JsonPropertyName("xuid")]
    public string? Xuid { get; set; }

    // ── Tokens — NOT serialized, stored in PasswordVault ──
    [JsonIgnore]
    public string? AccessToken { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }

    [JsonIgnore]
    public DateTime? TokenExpiresAt { get; set; }
}
