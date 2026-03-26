using NuciWeb.Automation;

namespace NuciWeb.Steam.Processors
{
    internal sealed class SteamChatProcessor(IWebProcessor webProcessor) : ISteamChatProcessor
    {
        readonly IWebProcessor webProcessor = webProcessor;

        public void Visit()
        {
            webProcessor.GoToUrl(SteamUrls.Chat);
            webProcessor.WaitForElementToExist(Select.ByClass("currentUserAvatar"));
        }
    }
}
