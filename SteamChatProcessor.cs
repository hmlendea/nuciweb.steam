using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamChatProcessor : ISteamChatProcessor
    {
        readonly IWebProcessor webProcessor;

        public SteamChatProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public void Visit()
        {
            webProcessor.GoToUrl(SteamUrls.Chat);

            By avatarSelector = By.ClassName("currentUserAvatar");

            webProcessor.WaitForElementToExist(avatarSelector);
        }
    }
}
