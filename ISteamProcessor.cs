using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public interface ISteamProcessor
    {
        void LogIn(SteamAccount account);
    }
}
