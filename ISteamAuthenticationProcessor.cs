using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    internal interface ISteamAuthenticationProcessor
    {
        void LogIn(SteamAccount account);
    }
}
