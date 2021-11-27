using System;
using System.Security.Authentication;

using OpenQA.Selenium;
using SteamGuard.TOTP;

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
        readonly ISteamGuard steamGuard;

        public SteamProcessor(IWebProcessor webProcessor)
            : this(webProcessor, new SteamGuard.TOTP.SteamGuard())
        {
        }

        public SteamProcessor(
            IWebProcessor webProcessor,
            ISteamGuard steamGuard)
        {
            this.webProcessor = webProcessor;
            this.steamGuard = steamGuard;
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
                ValidateCurrentSession(account.Username);
            }

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
                InputSteamGuardCode(account.TotpKey);
            }
            
            ValidateLogInResult();
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

        void ValidateCurrentSession(string expectedUsername)
        {
            By accountPulldownSelector = By.Id("account_pulldown");
            By onlinePersonaSelector = By.XPath("//span[contains(@class,'online')]");

            webProcessor.Click(accountPulldownSelector);
            webProcessor.WaitForAnyElementToBeVisible(onlinePersonaSelector);

            string currentUsername = webProcessor.GetText(onlinePersonaSelector).Trim();

            if (!currentUsername.Equals(expectedUsername, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new AuthenticationException("Already logged in as a different user.");
            }
        }

        void ValidateLogInResult()
        {
            By avatarSelector = By.XPath("//a[contains(@class,'user_avatar')]");
            By errorBoxSelector = By.Id("error_display");
            By steamGuardIncorrectMessageSelector = By.Id("login_twofactorauth_message_incorrectcode");

            webProcessor.WaitForAnyElementToBeVisible(
                avatarSelector,
                steamGuardIncorrectMessageSelector,
                errorBoxSelector);
            
            if (webProcessor.IsElementVisible(errorBoxSelector))
            {
                string errorMessage = webProcessor.GetText(errorBoxSelector);
                throw new AuthenticationException(errorMessage);
            }
            else if (webProcessor.IsElementVisible(steamGuardIncorrectMessageSelector))
            {
                throw new AuthenticationException("The provided Steam Guard code is not valid.");
            }
            else if (!webProcessor.IsElementVisible(avatarSelector))
            {
                throw new AuthenticationException("Authentication failure.");
            }
        }

        void InputSteamGuardCode(string totpKey)
        {
            if (string.IsNullOrWhiteSpace(totpKey))
            {
                throw new ArgumentNullException(nameof(totpKey));
            }

            By steamGuardCodeInputSelector = By.Id("twofactorcode_entry");
            By steamGuardSubmitButtonSelector = By.XPath("//*[@id='login_twofactorauth_buttonset_entercode']/div[1]");

            webProcessor.WaitForElementToBeVisible(steamGuardCodeInputSelector);

            string steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);
            webProcessor.SetText(steamGuardCodeInputSelector, steamGuardCode);
            webProcessor.Click(steamGuardSubmitButtonSelector);
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
