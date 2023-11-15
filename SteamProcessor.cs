using OpenQA.Selenium;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public sealed class SteamProcessor : ISteamProcessor
    {
        readonly IWebProcessor webProcessor;
        readonly ISteamAuthenticationProcessor authenticationProcessor;
        readonly ISteamKeyProcessor keyProcessor;
        readonly ISteamProfileProcessor profileProcessor;
        readonly ISteamWorkshopProcessor workshopProcessor;

        public SteamProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;

            authenticationProcessor = new SteamAuthenticationProcessor(webProcessor);
            keyProcessor = new SteamKeyProcessor(webProcessor);
            profileProcessor = new SteamProfileProcessor(webProcessor);
            workshopProcessor = new SteamWorkshopProcessor(webProcessor);
        }

        public void LogIn(SteamAccount account)
            => authenticationProcessor.LogIn(account);

        public void SetName(string profileName)
            => profileProcessor.SetName(profileName);

        public void AcceptCookies()
        {
            webProcessor.GoToUrl(SteamUrls.CookiePreferences);

            By acceptAllButtonSelector = By.XPath("//div[@class='account_settings_container']/div/div[2]");

            webProcessor.Click(acceptAllButtonSelector);
        }

        public void RejectCookies()
        {
            webProcessor.GoToUrl(SteamUrls.CookiePreferences);

            By rejectAllButtonSelector = By.XPath("//div[@class='account_settings_container']/div/div[1]");

            webProcessor.Click(rejectAllButtonSelector);
        }

        public void VisitChat()
        {
            webProcessor.GoToUrl(SteamUrls.Chat);

            By avatarSelector = By.ClassName("currentUserAvatar");

            webProcessor.WaitForElementToExist(avatarSelector);
        }

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
