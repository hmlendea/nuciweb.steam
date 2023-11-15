namespace NuciWeb.Steam
{
    internal interface ISteamProfileProcessor
    {
        void SetName(string name);

        void SetProfilePicture(string imagePath);
    }
}
