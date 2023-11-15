using System;

using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamProfileProcessor : ISteamProfileProcessor
    {
        const string profileSaveButtonXpath = @"//div[contains(@class,'profileedit_SaveCancelButtons')]/button[contains(@class,'Primary')]";

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
            By profilePictureImageSelector = By.XPath(@"//div[contains(@class,'avatar_AvatarRow')]/div[1]/div[1]/img");
            By saveButtonSelector = By.XPath(profileSaveButtonXpath);

            GoToEditProfilePage();

            webProcessor.Click(profilePictureTabSelector);

            string originalProfilePictureUrl = webProcessor.GetSource(profilePictureImageSelector);

            webProcessor.ExecuteScript("document.evaluate(\"" + profilePictureInputSelector.Criteria +  "\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.style = \"display: block;\";");
            webProcessor.AppendText(profilePictureInputSelector, imagePath);

            for (int i = 0; i < 5; i++)
            {
                string profilePictureUrl = webProcessor.GetSource(profilePictureImageSelector);

                if (profilePictureUrl.Equals(originalProfilePictureUrl))
                {
                    webProcessor.Wait();
                }
            }

            webProcessor.Click(saveButtonSelector);
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
