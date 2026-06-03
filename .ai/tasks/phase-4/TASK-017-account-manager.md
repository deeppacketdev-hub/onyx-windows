# TASK-017: AccountManager

## Metadata
- **Phase**: 4 | **Dependencies**: TASK-007 (CredentialStore), TASK-006 | **LOC**: ~550
- **Source**: [AccountManager.swift](../../onyx/Onyx/Services/AccountManager.swift) — 547 рядків

## Objective
Microsoft OAuth + offline account management. Найскладніший після LaunchController.

## MS OAuth Flow (ідентичний macOS)
```
1. Open WebView2 → https://login.live.com/oauth20_authorize.srf
   client_id=00000000402b5328, redirect_uri=https://login.live.com/oauth20_desktop.srf
   scope=XboxLive.signin offline_access, response_type=code
2. Intercept redirect → extract `code` param
3. POST /oauth20_token.srf → get MS access_token + refresh_token
4. POST user.auth.xboxlive.com/user/authenticate → Xbox Live token
5. POST xsts.auth.xboxlive.com/xsts/authorize → XSTS token
6. POST api.minecraftservices.com/authentication/login_with_xbox → MC access_token
7. GET api.minecraftservices.com/minecraft/profile → MC profile (UUID, username)
```

## Changes from macOS
- Keychain → CredentialStore (DPAPI) для token storage
- WKWebView → WebView2 для OAuth
- NSImage → BitmapImage для head avatars
- `mc-heads.net` avatar URLs: ідентичні

## Key Methods
- `LoginWithMicrosoftAsync()` — full OAuth flow
- `LoginOfflineAsync(username)` — create offline account
- `RefreshTokenAsync(account)` — refresh expired token
- `LogoutAsync(account)` — remove account + tokens
- `LoadAccountsAsync()` — load from accounts.json
- `UpdateAccountSkin(account, skinFilename)` — assign skin

## Acceptance Criteria
- [ ] MS OAuth flow завершується успішно
- [ ] Токени зберігаються через CredentialStore
- [ ] Token refresh працює
- [ ] Offline account створюється з MD5 UUID
- [ ] Аватар завантажується з mc-heads.net
