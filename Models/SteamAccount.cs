namespace NuciWeb.Steam.Models
{
    public sealed class SteamAccount
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string TotpKey { get; set; }
    }
}
