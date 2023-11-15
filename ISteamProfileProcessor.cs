namespace NuciWeb.Steam
{
    internal interface ISteamProfileProcessor
    {
        void SetName(string name);

        void SetIdentifier(string identifier);

        void SetProfilePicture(string imagePath);
    }
}
