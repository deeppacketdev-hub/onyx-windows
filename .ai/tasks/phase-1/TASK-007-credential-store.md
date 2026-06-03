# TASK-007: CredentialStore (DPAPI)

## Metadata
- **Phase**: 1 | **Dependencies**: TASK-001, TASK-002 (Account model) | **LOC**: ~100
- **Source**: НОВИЙ файл (замінює macOS Keychain)
- **Output**: `src/OnyxWindows/Services/CredentialStore.cs`

## Objective
Безпечне зберігання OAuth токенів через Windows DPAPI (Data Protection API).
Замінює macOS Keychain з AccountManager.swift.

## Implementation

```csharp
public class CredentialStore
{
    private readonly string _credentialsDir;

    public CredentialStore(AppDataManager appData)
    {
        _credentialsDir = Path.Combine(appData.BaseDirectory, "credentials");
        Directory.CreateDirectory(_credentialsDir);
    }

    public void SaveTokens(Guid accountId, string accessToken, string? refreshToken)
    {
        var data = JsonSerializer.Serialize(new TokenData(accessToken, refreshToken));
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(data),
            null,
            DataProtectionScope.CurrentUser);
        File.WriteAllBytes(TokenPath(accountId), encrypted);
    }

    public TokenData? LoadTokens(Guid accountId)
    {
        var path = TokenPath(accountId);
        if (!File.Exists(path)) return null;
        var encrypted = File.ReadAllBytes(path);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<TokenData>(Encoding.UTF8.GetString(decrypted));
    }

    public void DeleteTokens(Guid accountId) => File.Delete(TokenPath(accountId));

    private string TokenPath(Guid id) => Path.Combine(_credentialsDir, $"{id}.dat");
}

public record TokenData(string AccessToken, string? RefreshToken);
```

## ⚠️ Security Notes
- DPAPI шифрує тільки для поточного Windows користувача
- Файли `.dat` непрочитні без ключа користувача
- Потрібен NuGet: `System.Security.Cryptography.ProtectedData` (вже в .NET 8 Windows)

## Acceptance Criteria
- [ ] Токени зберігаються зашифровано
- [ ] Токени завантажуються та дешифруються
- [ ] Видалення токенів працює
- [ ] Round-trip test: Save → Load → порівняти
