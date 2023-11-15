namespace NuciWeb.Steam
{
    internal sealed class SteamUrls
    {
        public static string Store => "https://store.steampowered.com";
        public static string Community => "https://steamcommunity.com";

        public static string Account => $"{Store}/account";
        public static string Chat => $"{Community}/chat";
        public static string CookiePreferences => $"{Account}/cookiepreferences";
        public static string StoreLogin => $"{Store}/login";
        public static string CommunityLogin => $"{Community}/login/home";
        public static string KeyActivation = $"{Store}/account/registerkey";
        public static string WorkshopItemFormat => $"{Community}/sharedfiles/filedetails/?id={{0}}";
    }
}
