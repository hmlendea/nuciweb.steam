using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamChatProcessor(IWebProcessor webProcessor) : ISteamChatProcessor
    {
        readonly IWebProcessor webProcessor = webProcessor;

        public void Visit()
        {
            webProcessor.GoToUrl(SteamUrls.Chat);
            webProcessor.WaitForElementToExist(By.ClassName("currentUserAvatar"));
        }
    }
}
