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
        public static string StoreUrl => "https://store.steampowered.com";
        public static string CommunityUrl => "https://steamcommunity.com";

        public static string LoginUrl => $"{StoreUrl}/login/?redir=&redir_ssl=1";
        public static string KeyActivationUrl = $"{StoreUrl}/account/registerkey";
        public static string WorkshopItemUrlFormat => $"{CommunityUrl}/sharedfiles/filedetails/?id={{0}}";

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
        /// <summary>
        /// Activates the given key on the current account.
        /// </summary>
        /// <returns>The name of the activated product.</returns>
        /// <param name="key">The product key.</param>
        public string ActivateKey(string key)
        {
            By keyInputSelector = By.Id("product_key");
            By keyActivationButtonSelector = By.Id("register_btn");
            By agreementCheckboxSelector = By.Id("accept_ssa");

            By errorSelector = By.Id("error_display");
            By receiptSelector = By.Id("receipt_form");

            By productNameSelector = By.ClassName("registerkey_lineitem");

            if (!webProcessor.IsElementVisible(keyInputSelector))
            {
                webProcessor.GoToUrl(KeyActivationUrl);
            }

            webProcessor.SetText(keyInputSelector, key);
            webProcessor.UpdateCheckbox(agreementCheckboxSelector, true);

            webProcessor.Click(keyActivationButtonSelector);

            webProcessor.WaitForAnyElementToBeVisible(errorSelector, receiptSelector);

            ValidateKeyActivation();
            
            return webProcessor.GetText(productNameSelector);
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

        void ValidateKeyActivation()
        {
            By errorSelector = By.Id("error_display");

            if (!webProcessor.IsElementVisible(errorSelector))
            {
                return;
            }
            
            string errorMessage = webProcessor.GetText(errorSelector);

            if (errorMessage.Contains("is not valid") ||
                errorMessage.Contains("nu este valid"))
            {
                throw new KeyActivationException(
                    "Invalid product key.",
                    KeyActivationErrorCode.InvalidProductKey);
            }

            if (errorMessage.Contains("activated by a different Steam account") ||
                errorMessage.Contains("activat de un cont Steam diferit"))
            {
                throw new KeyActivationException(
                    "Key already activated by a different account.",
                    KeyActivationErrorCode.AlreadyActivatedDifferentAccount);
            }

            if (errorMessage.Contains("This Steam account already owns the product") ||
                errorMessage.Contains("Contul acesta Steam deține deja produsul"))
            {
                throw new KeyActivationException(
                    "Product already owned by this account.",
                    KeyActivationErrorCode.AlreadyActivatedCurrentAccount);
            }

            if (errorMessage.Contains("requires ownership of another product") ||
                errorMessage.Contains("necesită deținerea unui alt produs"))
            {
                throw new KeyActivationException(
                    "A base product is required in order to activate this key.",
                    KeyActivationErrorCode.BaseProductRequired);
            }

            if (errorMessage.Contains("this product is not available for purchase in this country") ||
                errorMessage.Contains("acest produs nu este disponibil pentru achiziție în această țară"))
            {
                throw new KeyActivationException(
                    "The key is locked to a specific region.",
                    KeyActivationErrorCode.RegionLocked);
            }

            if (errorMessage.Contains("too many recent activation attempts") ||
                errorMessage.Contains("prea multe încercări de activare recente"))
            {
                throw new KeyActivationException(
                    "Key activation limit reached.",
                    KeyActivationErrorCode.TooManyAttempts);
            }

            if (errorMessage.Contains("An unexpected error has occurred") ||
                errorMessage.Contains("A apărut o eroare neașteptată"))
            {
                throw new KeyActivationException(
                    "An unexpected error has occurred.",
                    KeyActivationErrorCode.Unexpected);
            }

            throw new KeyActivationException(errorMessage);
        }
    }
}
