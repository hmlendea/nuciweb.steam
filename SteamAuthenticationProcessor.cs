using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;

using OpenQA.Selenium;
using SteamGuard.TOTP;

using NuciWeb.Steam.Models;

namespace NuciWeb.Steam
{
    internal sealed class SteamAuthenticationProcessor(
        IWebProcessor webProcessor,
        ISteamGuard steamGuard) : ISteamAuthenticationProcessor
    {
        readonly IWebProcessor webProcessor = webProcessor;
        readonly ISteamGuard steamGuard = steamGuard;
        readonly IList<string> UsedSteamGuardCodes = [];

        static string SteamGuardCodeInputXPath => @"//form/div/div/div/div/input/..";

        public SteamAuthenticationProcessor(IWebProcessor webProcessor)
            : this(webProcessor, new SteamGuard.TOTP.SteamGuard()) { }

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

            By steamGuardCodeInputSelector = By.XPath(SteamGuardCodeInputXPath);

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
                By steamGuardCodeCharacterInputSelector = By.XPath($"{SteamGuardCodeInputXPath}/input[{steamGuardCharIndex + 1}]");
                webProcessor.SetText(steamGuardCodeCharacterInputSelector, steamGuardCode[steamGuardCharIndex].ToString());
            }
        }

        private void LogInOnPage(SteamAccount account, string url)
        {
            webProcessor.GoToUrl(url);
            webProcessor.Wait(TimeSpan.FromSeconds(2));

            By usernameSelector = By.XPath(@"//form/div[1]/input");
            By passwordSelector = By.XPath(@"//form/div[2]/input");
            By captchaInputSelector = By.Id("input_captcha");
            By logInButtonSelector = By.XPath(@"//button[@type='submit']");
            By steamGuardCodeInputSelector = By.XPath(SteamGuardCodeInputXPath);
            By avatarSelector = By.XPath(@"//a[contains(@class,'playerAvatar')]");
            By accountPulldownSelector = By.Id("account_pulldown");

            webProcessor.WaitForAnyElementToBeVisible(usernameSelector, avatarSelector);

            if (webProcessor.AreAllElementsVisible(avatarSelector, accountPulldownSelector))
            {
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
            By onlinePersonaSelector = By.XPath("//span[contains(@class,'persona_name_text_content')]");

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
                throw new AuthenticationException(webProcessor.GetText(errorBoxSelector));
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
