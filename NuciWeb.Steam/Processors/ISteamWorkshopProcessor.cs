namespace NuciWeb.Steam.Processors
{
    internal interface ISteamWorkshopProcessor
    {
        void AddToFavourite(string workshopItemId);

        void Subscribe(string workshopItemId);
    }
}
