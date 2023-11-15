using System;

using OpenQA.Selenium;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public sealed class SteamProcessor : ISteamProcessor
    {
        readonly IWebProcessor webProcessor;
        readonly ISteamAuthenticationProcessor authenticationProcessor;
        readonly ISteamKeyProcessor keyProcessor;
        readonly ISteamWorkshopProcessor workshopProcessor;

        public SteamProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;

            authenticationProcessor = new SteamAuthenticationProcessor(webProcessor);
            keyProcessor = new SteamKeyProcessor(webProcessor);
            workshopProcessor = new SteamWorkshopProcessor(webProcessor);
        }

        public void LogIn(SteamAccount account)
            => authenticationProcessor.LogIn(account);

        public void SetProfileName(string profileName)
        {
            By profileNameSelector = By.Name("personaName");
            By saveButtonSelector = By.XPath(@"//div[contains(@class,'profileedit_SaveCancelButtons')]/button[@type='submit'][1]");

            GoToEditProfilePage();

            webProcessor.WaitForElementToExist(profileNameSelector);
            webProcessor.SetText(profileNameSelector, profileName);

            webProcessor.Click(saveButtonSelector);
            webProcessor.Wait(TimeSpan.FromSeconds(1));
        }

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

        void GoToProfilePage()
        {
            webProcessor.GoToUrl(SteamUrls.Account);

            By addFundsLinkSelector = By.XPath(@"//a[@class='account_manage_link'][1]");
            By avatarSelector = By.XPath(@"//div[@id='global_actions']/a[contains(@class,'user_avatar')]");

            webProcessor.WaitForElementToExist(addFundsLinkSelector);
            webProcessor.Click(avatarSelector);
        }

        void GoToEditProfilePage()
        {
            GoToProfilePage();

            By editProfileButton = By.XPath(@"//div[@class='profile_header_actions']/a[contains(@class,'btn_profile_action')][1]");

            webProcessor.WaitForElementToExist(editProfileButton);
            webProcessor.Click(editProfileButton);
        }
    }
}
