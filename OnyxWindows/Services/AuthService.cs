using System;
using OnyxWindows.Services;

namespace OnyxWindows.Services;

public class OfflineAuthData
{
    public string Nickname { get; }
    public string Uuid { get; }
    public string AccessToken { get; }

    public OfflineAuthData(string nickname, string uuid, string accessToken = "0")
    {
        Nickname = nickname;
        Uuid = uuid;
        AccessToken = accessToken;
    }
}

public static class AuthService
{
    public static OfflineAuthData OfflineAuth(string nickname)
    {
        var uuid = LaunchController.OfflineUUID(nickname);
        return new OfflineAuthData(nickname, uuid, "0");
    }
}
