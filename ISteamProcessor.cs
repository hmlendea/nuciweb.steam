using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public interface ISteamProcessor
    {
        void LogIn(SteamAccount account);

        void SetProfileName(string profileName);

        void AcceptCookies();

        void RejectCookies();

        void VisitChat();

        string ActivateKey(string key);

        void FavouriteWorkshopItem(string workshopItemId);

        void SubscribeToWorkshopItem(string workshopItemId);
    }
}
