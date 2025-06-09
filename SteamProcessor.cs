using OpenQA.Selenium;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public sealed class SteamProcessor(IWebProcessor webProcessor) : ISteamProcessor
    {
        readonly IWebProcessor webProcessor = webProcessor;
        readonly SteamAuthenticationProcessor authenticationProcessor = new(webProcessor);
        readonly SteamChatProcessor chatProcessor = new(webProcessor);
        readonly SteamKeyProcessor keyProcessor = new(webProcessor);
        readonly SteamProfileProcessor profileProcessor = new(webProcessor);
        readonly SteamWorkshopProcessor workshopProcessor = new(webProcessor);

        public void LogIn(SteamAccount account)
            => authenticationProcessor.LogIn(account);

        public void SetProfileName(string name)
            => profileProcessor.SetName(name);

        public void SetProfileIdentifier(string identifier)
            => profileProcessor.SetIdentifier(identifier);

        public void SetProfilePicture(string imagePath)
            => profileProcessor.SetProfilePicture(imagePath);

        public void AcceptCookies()
        {
            webProcessor.GoToUrl(SteamUrls.CookiePreferences);
            webProcessor.Click(By.XPath("//div[@class='account_settings_container']/div/div[2]"));
        }

        public void RejectCookies()
        {
            webProcessor.GoToUrl(SteamUrls.CookiePreferences);
            webProcessor.Click(By.XPath("//div[@class='account_settings_container']/div/div[1]"));
        }

        public void VisitChat()
            => chatProcessor.Visit();

        /// <summary>
        /// Activates the given key on the current account.
        /// </summary>
        /// <returns>The name of the activated product.</returns>
        /// <param name="key">The product key.</param>
        public string ActivateKey(string key)
            => keyProcessor.ActivateKey(key);

        public void FavouriteWorkshopItem(string workshopItemId)
            => workshopProcessor.AddToFavourite(workshopItemId);

        public void SubscribeToWorkshopItem(string workshopItemId)
            => workshopProcessor.Subscribe(workshopItemId);
    }
}
