namespace NuciWeb.Steam
{
    internal interface ISteamWorkshopProcessor
    {
        void AddToFavourite(string workshopItemId);

        void Subscribe(string workshopItemId);
    }
}
