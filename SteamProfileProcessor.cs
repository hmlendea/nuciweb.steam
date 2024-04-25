using System;

using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamProfileProcessor : ISteamProfileProcessor
    {
        const string profileSaveButtonXpath = @"//form[contains(@action,'/edit/info')]/div[last()]/button[@type='submit']";

        readonly IWebProcessor webProcessor;

        public SteamProfileProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public void SetName(string profileName)
        {
            By profileNameSelector = By.Name("personaName");
            By saveButtonSelector = By.XPath(profileSaveButtonXpath);

            GoToEditProfilePage();

            webProcessor.SetText(profileNameSelector, profileName);
            webProcessor.Click(saveButtonSelector);
        }

        public void SetIdentifier(string identifier)
        {
            By identifierSelector = By.Name("customURL");
            By saveButtonSelector = By.XPath(profileSaveButtonXpath);

            GoToEditProfilePage();

            webProcessor.SetText(identifierSelector, identifier);
            webProcessor.Click(saveButtonSelector);
        }

        public void SetProfilePicture(string imagePath)
        {
            By profilePictureTabSelector = By.XPath(@"//div[contains(@class,'profileeditshell_Navigation')]/a[contains(@href,'/edit/avatar')]");
            By profilePictureInputSelector = By.XPath(@"//div[contains(@class,'avatar_AvatarDialogUploadArea')]/input");
            By profilePictureNewImageSelector = By.XPath(@"//div[contains(@class,'avatar_AvatarRow')]/div[1]/div[1]/img");
            By profilePictureCurrentImageSelector = By.XPath(@"//div[contains(@class,'profile_small_header_avatar')]/div[contains(@class,'avatar_Avatar')]/div/div/img");
            By saveButtonSelector = By.XPath(profileSaveButtonXpath);

            GoToEditProfilePage();

            webProcessor.Click(profilePictureTabSelector);

            string newProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureNewImageSelector);
            string currentProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureCurrentImageSelector);

            webProcessor.ExecuteScript("document.evaluate(\"" + profilePictureInputSelector.Criteria +  "\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.style = \"display: block;\";");
            webProcessor.AppendText(profilePictureInputSelector, imagePath);

            for (int i = 0; i < 5; i++)
            {
                string newProfilePictureUrl = webProcessor.GetSource(profilePictureNewImageSelector);

                if (newProfilePictureUrl.Equals(newProfilePictureOriginalUrl))
                {
                    webProcessor.Wait();
                }
                else
                {
                    break;
                }
            }

            webProcessor.Click(saveButtonSelector);

            for (int i = 0; i < 5; i++)
            {
                string currentProfilePictureUrl = webProcessor.GetSource(profilePictureCurrentImageSelector);

                if (currentProfilePictureUrl.Equals(currentProfilePictureOriginalUrl))
                {
                    webProcessor.Wait();
                }
                else
                {
                    break;
                }
            }
        }

        void GoToProfilePage()
        {
            webProcessor.GoToUrl(SteamUrls.Account);

            By addFundsLinkSelector = By.XPath(@"//a[@class='account_manage_link'][1]");
            By profilePictureSelector = By.XPath(@"//div[@id='global_actions']/a[contains(@class,'user_avatar')]");

            webProcessor.WaitForElementToExist(addFundsLinkSelector);
            webProcessor.Click(profilePictureSelector);
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
