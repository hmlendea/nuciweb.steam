using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;

using OpenQA.Selenium;
using SteamGuard.TOTP;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    internal sealed class SteamAuthenticator : ISteamAuthenticator
    {
        readonly IWebProcessor webProcessor;
        readonly ISteamGuard steamGuard;
        readonly IList<string> UsedSteamGuardCodes;

        public SteamAuthenticator(IWebProcessor webProcessor)
            : this(webProcessor, new SteamGuard.TOTP.SteamGuard())
        {
        }

        public SteamAuthenticator(
            IWebProcessor webProcessor,
            ISteamGuard steamGuard)
        {
            this.webProcessor = webProcessor;
            this.steamGuard = steamGuard;
            this.UsedSteamGuardCodes = new List<string>();
        }

        public void LogIn(SteamAccount account)
        {
            LogInOnPage(account, SteamUrls.StoreLogin);
            LogInOnPage(account, SteamUrls.CommunityLogin);
        }

        void InputSteamGuardCode(string totpKey)
        {
            if (string.IsNullOrWhiteSpace(totpKey))
            {
                throw new ArgumentNullException(nameof(totpKey));
            }

            By steamGuardCodeInputSelector = By.XPath(@"//div[contains(@class,'segmentedinputs_SegmentedCharacterInput')]");

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
                By steamGuardCodeCharacterInputSelector = By.XPath($"//div[contains(@class,'segmentedinputs_SegmentedCharacterInput')]/input[{steamGuardCharIndex + 1}]");
                webProcessor.SetText(steamGuardCodeCharacterInputSelector, steamGuardCode[steamGuardCharIndex].ToString());
            }
        }

        private void LogInOnPage(SteamAccount account, string url)
        {
            webProcessor.GoToUrl(url);

            By usernameSelector = By.XPath(@"//form[contains(@class,'newlogindialog_LoginForm')]/div[1]/input");
            By passwordSelector = By.XPath(@"//form[contains(@class,'newlogindialog_LoginForm')]/div[2]/input");
            By captchaInputSelector = By.Id("input_captcha");
            By logInButtonSelector = By.XPath(@"//button[contains(@class,'newlogindialog_SubmitButton')]");
            By steamGuardCodeInputSelector = By.XPath(@"//div[contains(@class,'segmentedinputs_SegmentedCharacterInput')]");
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
    }
}
