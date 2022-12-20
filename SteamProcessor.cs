using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;

using OpenQA.Selenium;
using SteamGuard.TOTP;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    public sealed class SteamProcessor : ISteamProcessor
    {
        public static string StoreUrl => "https://store.steampowered.com";
        public static string CommunityUrl => "https://steamcommunity.com";

        public static string AccountUrl => $"{StoreUrl}/account";
        public static string ChatUrl => $"{CommunityUrl}/chat";
        public static string CookiePreferencesUrl => $"{AccountUrl}/cookiepreferences";
        public static string StoreLoginUrl => $"{StoreUrl}/login";
        public static string CommunityLoginUrl => $"{CommunityUrl}/login/home";
        public static string KeyActivationUrl = $"{StoreUrl}/account/registerkey";
        public static string WorkshopItemUrlFormat => $"{CommunityUrl}/sharedfiles/filedetails/?id={{0}}";

        readonly IWebProcessor webProcessor;
        readonly ISteamGuard steamGuard;
        readonly IList<string> UsedSteamGuardCodes;

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
            this.UsedSteamGuardCodes = new List<string>();
        }

        public void LogIn(SteamAccount account)
        {
            LogInOnPage(account, StoreLoginUrl);
            LogInOnPage(account, CommunityLoginUrl);
        }

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
            webProcessor.GoToUrl(CookiePreferencesUrl);

            By acceptAllButtonSelector = By.XPath("//div[@class='account_settings_container']/div/div[2]");

            webProcessor.Click(acceptAllButtonSelector);
        }

        public void RejectCookies()
        {
            webProcessor.GoToUrl(CookiePreferencesUrl);

            By rejectAllButtonSelector = By.XPath("//div[@class='account_settings_container']/div/div[1]");

            webProcessor.Click(rejectAllButtonSelector);
        }

        public void VisitChat()
        {
            webProcessor.GoToUrl(ChatUrl);

            By avatarSelector = By.ClassName("currentUserAvatar");

            webProcessor.WaitForElementToExist(avatarSelector);
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

            By favouriteButton1Selector = By.Id("FavoriteItemOptionAdd");
            By favouriteButton2Selector = By.Id("FavoriteItemBtn");
            By unfavouriteButtonSelector = By.Id("FavoriteItemOptionFavorited");
            By favouritedNoticeSelector = By.Id("JustFavorited");

            webProcessor.WaitForAnyElementToBeVisible(
                favouriteButton1Selector,
                favouriteButton2Selector,
                unfavouriteButtonSelector);

            if (webProcessor.IsElementVisible(unfavouriteButtonSelector))
            {
                return;
            }

            webProcessor.ClickAny(
                favouriteButton1Selector,
                favouriteButton2Selector);

            webProcessor.WaitForElementToBeVisible(favouritedNoticeSelector);
        }

        public void SubscribeToWorkshopItem(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            By subscribeButton1Selector = By.Id("SubscribeItemOptionAdd");
            By subscribeButton2Selector = By.Id("SubscribeItemBtn");
            By unsubscribeButtonSelector = By.Id("SubscribeItemOptionSubscribed");
            By subscribedNoticeSelector = By.Id("JustSubscribed");
            By modalDialogSelector = By.ClassName("newmodal");
            By requiredItemSelector = By.ClassName("requiredItem");

            webProcessor.WaitForAnyElementToBeVisible(
                subscribeButton1Selector,
                subscribeButton2Selector,
                unsubscribeButtonSelector);

            if (webProcessor.IsElementVisible(unsubscribeButtonSelector))
            {
                return;
            }

            webProcessor.ClickAny(
                subscribeButton1Selector,
                subscribeButton2Selector);

            webProcessor.WaitForAnyElementToBeVisible(
                subscribedNoticeSelector,
                modalDialogSelector);

            if (webProcessor.AreAllElementsVisible(modalDialogSelector, requiredItemSelector))
            {
                By continueButtonSelector = By.XPath("//div[@class='newmodal_buttons']/div[1]/span");
                webProcessor.Click(continueButtonSelector);
            }

            webProcessor.WaitForElementToBeVisible(subscribedNoticeSelector);
        }

        void GoToProfilePage()
        {
            webProcessor.GoToUrl(AccountUrl);

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

        void InputSteamGuardCode(string totpKey)
        {
            if (string.IsNullOrWhiteSpace(totpKey))
            {
                throw new ArgumentNullException(nameof(totpKey));
            }

            By steamGuardCodeInputSelector = By.XPath(@"//div[contains(@class,'newlogindialog_SegmentedCharacterInput')]");

            webProcessor.WaitForElementToBeVisible(steamGuardCodeInputSelector);
            string steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);

            // Wait for the next SG code if this one was already used as they are single-use
            while (
                string.IsNullOrWhiteSpace(steamGuardCode) ||
                UsedSteamGuardCodes.Contains(steamGuardCode))
            {
                steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);
                Thread.Sleep(10000); // TODO: Remove this!
            }

            UsedSteamGuardCodes.Add(steamGuardCode);

            for (int steamGuardCharIndex = 0; steamGuardCharIndex < 5; steamGuardCharIndex++)
            {
                By steamGuardCodeCharacterInputSelector = By.XPath($"//div[contains(@class,'newlogindialog_SegmentedCharacterInput')]/input[{steamGuardCharIndex + 1}]");
                webProcessor.SetText(steamGuardCodeCharacterInputSelector, steamGuardCode[steamGuardCharIndex].ToString());
            }
        }

        void GoToWorksopItemPage(string workshopItemId)
        {
            string workshopItemUrl = string.Format(WorkshopItemUrlFormat, workshopItemId);

            By mainContentSelector = By.Id("mainContents");

            webProcessor.GoToUrl(workshopItemUrl);
            webProcessor.WaitForElementToBeVisible(mainContentSelector);
        }

        private void LogInOnPage(SteamAccount account, string url)
        {
            webProcessor.GoToUrl(url);

            By usernameSelector = By.XPath(@"//form[contains(@class,'newlogindialog_LoginForm')]/div[1]/input");
            By passwordSelector = By.XPath(@"//form[contains(@class,'newlogindialog_LoginForm')]/div[2]/input");
            By captchaInputSelector = By.Id("input_captcha");
            By logInButtonSelector = By.XPath(@"//button[contains(@class,'newlogindialog_SubmitButton')]");
            By errorBoxSelector = By.XPath("//div[contains(@class,'newlogindialog_FormError')]");
            By steamGuardCodeInputSelector = By.XPath(@"//div[contains(@class,'newlogindialog_SegmentedCharacterInput')]");
            By editProfileButtonSelector = By.XPath(@"//div[contains(@class,'profile_header_actions')]");
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");
            By accountPulldownSelector = By.Id("account_pulldown");

            webProcessor.WaitForAnyElementToBeVisible(usernameSelector, editProfileButtonSelector);

            if (webProcessor.AreAllElementsVisible(avatarSelector, accountPulldownSelector))
            {
                Console.WriteLine("Validating..." + url);
                ValidateCurrentSession(account.Username);
                return;
            }

            if (webProcessor.IsElementVisible(captchaInputSelector))
            {
                throw new AuthenticationException("Captcha input required.");
            }

            webProcessor.SetText(usernameSelector, account.Username);
            webProcessor.SetText(passwordSelector, account.Password);

            webProcessor.Click(logInButtonSelector);

            webProcessor.WaitForElementToBeVisible(steamGuardCodeInputSelector);

            if (webProcessor.IsElementVisible(steamGuardCodeInputSelector))
            {
                InputSteamGuardCode(account.TotpKey);
            }

            ValidateLogInResult();
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
