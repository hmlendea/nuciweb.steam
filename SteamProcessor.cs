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
        readonly IWebProcessor webProcessor;
        readonly ISteamAuthenticator steamAuthenticator;

        public SteamProcessor(IWebProcessor webProcessor)
            : this(webProcessor, new SteamAuthenticator(webProcessor))
        {

        }

        internal SteamProcessor(
            IWebProcessor webProcessor,
            ISteamAuthenticator steamAuthenticator)
        {
            this.webProcessor = webProcessor;
            this.steamAuthenticator = steamAuthenticator;
        }

        public void LogIn(SteamAccount account)
            => steamAuthenticator.LogIn(account);

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
        {
            By keyInputSelector = By.Id("product_key");
            By keyActivationButtonSelector = By.Id("register_btn");
            By agreementCheckboxSelector = By.Id("accept_ssa");

            By errorSelector = By.Id("error_display");
            By receiptSelector = By.Id("receipt_form");

            By productNameSelector = By.ClassName("registerkey_lineitem");

            if (!webProcessor.IsElementVisible(keyInputSelector))
            {
                webProcessor.GoToUrl(SteamUrls.KeyActivation);
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

        void GoToWorksopItemPage(string workshopItemId)
        {
            string workshopItemUrl = string.Format(SteamUrls.WorkshopItemFormat, workshopItemId);

            By mainContentSelector = By.Id("mainContents");

            webProcessor.GoToUrl(workshopItemUrl);
            webProcessor.WaitForElementToBeVisible(mainContentSelector);
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
