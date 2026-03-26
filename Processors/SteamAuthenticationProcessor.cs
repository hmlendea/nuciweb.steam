using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using SteamGuard.TOTP;

using NuciWeb.Steam.Models;
using NuciWeb.Automation;
using NuciWeb.Steam.Utils;

namespace NuciWeb.Steam.Processors
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

            string steamGuardCodeInputSelector = Select.ByXPath(SteamGuardCodeInputXPath);

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
                string steamGuardCodeCharacterInputSelector = Select.ByXPath($"{SteamGuardCodeInputXPath}/input[{steamGuardCharIndex + 1}]");
                webProcessor.SetText(steamGuardCodeCharacterInputSelector, steamGuardCode[steamGuardCharIndex].ToString());
            }
        }

        private void LogInOnPage(SteamAccount account, string url)
        {
            webProcessor.GoToUrl(url);
            webProcessor.Wait(TimeSpan.FromSeconds(2));

            string usernameSelector = Select.ByXPath(@"//form/div[1]/div/../input");
            string passwordSelector = Select.ByXPath(@"//form/div[2]/div/../input");
            string captchaInputSelector = Select.ById("input_captcha");
            string logInButtonSelector = Select.ByXPath(@"//form/div[4]/button[@type='submit']");
            string steamGuardCodeInputSelector = Select.ByXPath(SteamGuardCodeInputXPath);
            string avatarSelector = Select.ByXPath(@"//a[contains(@class,'playerAvatar')]");
            string accountPulldownSelector = Select.ById("account_pulldown");

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

            string rejectCookiesButtonSelector = Select.ById("rejectAllButton");

            if (webProcessor.DoesElementExist(rejectCookiesButtonSelector))
            {
                webProcessor.Click(rejectCookiesButtonSelector);
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
            string accountPulldownSelector = Select.ById("account_pulldown");
            string onlinePersonaSelector = Select.ByXPath(@"//div[@class='popup_body popup_menu']/a[2]/span[@class='account_name']");

            webProcessor.Click(accountPulldownSelector);
            webProcessor.WaitForAnyElementToBeVisible(onlinePersonaSelector);

            string currentUsername = webProcessor.GetText(onlinePersonaSelector).Trim();

            if (!currentUsername.Equals(expectedUsername, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new AuthenticationException($"Already logged in as a different user ('{currentUsername}').");
            }
        }

        void ValidateLogInResult()
        {
            string avatarSelector = Select.ByClass("user_avatar");
            string errorBoxSelector = Select.ById("error_display");
            string steamGuardIncorrectMessageSelector = Select.ById("login_twofactorauth_message_incorrectcode");

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
