using NuciWeb.Steam.Models;

namespace NuciWeb.Steam.Processors
{
    internal interface ISteamAuthenticationProcessor
    {
        void LogIn(SteamAccount account);
    }
}
