using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public interface ISteamProcessor
    {
        void LogIn(SteamAccount account);

        string ActivateKey(string key);

        void FavouriteWorkshopItem(string workshopItemId);

        void SubscribeToWorkshopItem(string workshopItemId);
    }
}
