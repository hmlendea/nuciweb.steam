using System;
using System.Security.Authentication;

using OpenQA.Selenium;

using NuciWeb;
using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public sealed class SteamProcessor : ISteamProcessor
    {
        public string StoreUrl => "https://store.steampowered.com";
        public string CommunityUrl => "https://steamcommunity.com";

        public string LoginUrl => $"{StoreUrl}/login/?redir=&redir_ssl=1";
        public string WorkshopItemUrlFormat => $"{CommunityUrl}/sharedfiles/filedetails/?id={{0}}";

        readonly IWebProcessor webProcessor;

        public SteamProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public void LogIn(SteamAccount account)
        {
            webProcessor.GoToUrl(LoginUrl);

            By usernameSelector = By.Id("input_username");
            By passwordSelector = By.Id("input_password");
            By captchaInputSelector = By.Id("input_captcha");
            By logInButtonSelector = By.XPath(@"//*[@id='login_btn_signin']/button");
            By errorBoxSelector = By.Id("error_display");
            By steamGuardCodeInputSelector = By.Id("twofactorcode_entry");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");

            if (webProcessor.IsElementVisible(avatarSelector))
            {
                throw new AuthenticationException("Already logged in.");
            }
            
            if (webProcessor.IsElementVisible(captchaInputSelector))
            {
                throw new AuthenticationException("Captcha input required.");
            }

            webProcessor.SetText(usernameSelector, account.Username);
            webProcessor.SetText(passwordSelector, account.Password);
            
            webProcessor.Click(logInButtonSelector);
            webProcessor.WaitForAnyElementToBeVisible(
                steamGuardCodeInputSelector,
                errorBoxSelector,
                avatarSelector);

            if (webProcessor.IsElementVisible(steamGuardCodeInputSelector))
            {
                throw new AuthenticationException("Steam Guard input required.");
            }
            
            if (webProcessor.IsElementVisible(errorBoxSelector))
            {
                string errorMessage = webProcessor.GetText(errorBoxSelector);
                throw new AuthenticationException(errorMessage);
            }
        }

        public void FavouriteWorkshopItem(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            By favouriteButtonSelector = By.Id("FavoriteItemOptionAdd");
            By unfavouriteButtonSelector = By.Id("FavoriteItemOptionFavorited");
            By favouritedNoticeSelector = By.Id("JustFavorited");


            if (webProcessor.IsElementVisible(unfavouriteButtonSelector))
            {
                return;
            }

            webProcessor.Click(favouriteButtonSelector);
            webProcessor.WaitForElementToBeVisible(favouritedNoticeSelector);
        }

        public void SubscribeToWorkshopItem(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            By subscribeButtonSelector = By.Id("SubscribeItemOptionAdd");
            By unsubscribeButtonSelector = By.Id("SubscribeItemOptionSubscribed");
            By subscribedNoticeSelector = By.Id("JustSubscribed");

            if (webProcessor.IsElementVisible(unsubscribeButtonSelector))
            {
                return;
            }

            webProcessor.Click(subscribeButtonSelector);
            webProcessor.WaitForElementToBeVisible(subscribedNoticeSelector);
        }

        void GoToWorksopItemPage(string workshopItemId)
        {
            string workshopItemUrl = string.Format(WorkshopItemUrlFormat, workshopItemId);

            By mainContentSelector = By.Id("mainContents");

            webProcessor.GoToUrl(workshopItemUrl);
            webProcessor.WaitForElementToBeVisible(mainContentSelector);
        }
    }
}
