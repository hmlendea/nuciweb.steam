using NuciWeb.Automation;

namespace NuciWeb.Steam
{
    internal sealed class SteamProfileProcessor(IWebProcessor webProcessor) : ISteamProfileProcessor
    {
        const string profileSaveButtonXpath = @"//button[contains(@class,'Primary')]";

        readonly IWebProcessor webProcessor = webProcessor;

        public void SetName(string profileName)
        {
            GoToEditProfilePage();

            webProcessor.SetText(Select.ByName("personaName"), profileName);
            webProcessor.Click(Select.ByXPath(profileSaveButtonXpath));
        }

        public void SetIdentifier(string identifier)
        {
            GoToEditProfilePage();

            webProcessor.SetText(Select.ByName("customURL"), identifier);
            webProcessor.Click(Select.ByXPath(profileSaveButtonXpath));
        }

        public void SetProfilePicture(string imagePath)
        {
            string profilePictureTabSelector = Select.ByXPath(@"//a[contains(@href,'/edit/avatar')]");
            string profilePictureInputSelector = Select.ByXPath(@"//input[@type='file']");
            string profilePictureNewImageSelector = Select.ByXPath(@"//div[contains(@class,'DialogBody')]/div/div/div[3]/div[1]/div[1]/div/img[contains(@src,'avatar')]");
            string profilePictureCurrentImageSelector = Select.ByXPath(@"//div[contains(@class,'profile_small_header_avatar')]/div/div/img[contains(@src,'avatar')]");
            string saveButtonSelector = Select.ByXPath(profileSaveButtonXpath);

            GoToEditProfilePage();

            webProcessor.Click(profilePictureTabSelector);

            string newProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureNewImageSelector);
            string currentProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureCurrentImageSelector);

            webProcessor.ExecuteScript("document.evaluate(\"" + profilePictureInputSelector +  "\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.style = \"display: block;\";");
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

            string addFundsLinkSelector = Select.ByXPath(@"//a[@class='account_manage_link'][1]");
            string profilePictureSelector = Select.ByXPath(@"//div[@id='global_actions']/a[contains(@class,'user_avatar')]");

            webProcessor.WaitForElementToExist(addFundsLinkSelector);
            webProcessor.Click(profilePictureSelector);
        }

        void GoToEditProfilePage()
        {
            GoToProfilePage();

            string editProfileButton = Select.ByXPath(@"//div[@class='profile_header_actions']/a[contains(@class,'btn_profile_action')][1]");

            webProcessor.WaitForElementToExist(editProfileButton);
            webProcessor.Click(editProfileButton);
        }
    }
}
