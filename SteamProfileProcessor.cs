using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamProfileProcessor(IWebProcessor webProcessor) : ISteamProfileProcessor
    {
        const string profileSaveButtonXpath = @"//button[contains(@class,'Primary')]";

        readonly IWebProcessor webProcessor = webProcessor;

        public void SetName(string profileName)
        {
            GoToEditProfilePage();

            webProcessor.SetText(By.Name("personaName"), profileName);
            webProcessor.Click(By.XPath(profileSaveButtonXpath));
        }

        public void SetIdentifier(string identifier)
        {
            GoToEditProfilePage();

            webProcessor.SetText(By.Name("customURL"), identifier);
            webProcessor.Click(By.XPath(profileSaveButtonXpath));
        }

        public void SetProfilePicture(string imagePath)
        {
            By profilePictureTabSelector = By.XPath(@"//a[contains(@href,'/edit/avatar')]");
            By profilePictureInputSelector = By.XPath(@"//input[@type='file']");
            By profilePictureNewImageSelector = By.XPath(@"//div[contains(@class,'DialogBody')]/div/div[3]/div[1]/div[1]/div/img[contains(@src,'avatar')]");
            By profilePictureCurrentImageSelector = By.XPath(@"//div[contains(@class,'profile_small_header_avatar')]/div/div/div/img[contains(@src,'akamai')]");
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
