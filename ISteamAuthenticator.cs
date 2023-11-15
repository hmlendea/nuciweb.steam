using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    internal interface ISteamAuthenticator
    {
        void LogIn(SteamAccount account);
    }
}
