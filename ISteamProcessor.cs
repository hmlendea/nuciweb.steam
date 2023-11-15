using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public interface ISteamProcessor
    {
        void LogIn(SteamAccount account);

        void SetProfileName(string name);

        void SetProfileIdentifier(string identifier);

        void SetProfilePicture(string imagePath);

        void AcceptCookies();

        void RejectCookies();

        void VisitChat();

        string ActivateKey(string key);

        void FavouriteWorkshopItem(string workshopItemId);

        void SubscribeToWorkshopItem(string workshopItemId);
    }
}
