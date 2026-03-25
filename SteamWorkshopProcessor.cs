using NuciWeb.Automation;

namespace NuciWeb.Steam
{
    internal sealed class SteamWorkshopProcessor(IWebProcessor webProcessor) : ISteamWorkshopProcessor
    {
        readonly IWebProcessor webProcessor = webProcessor;

        public void AddToFavourite(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            string favouriteButton1Selector = Select.ById("FavoriteItemOptionAdd");
            string favouriteButton2Selector = Select.ById("FavoriteItemBtn");
            string unfavouriteButtonSelector = Select.ById("FavoriteItemOptionFavorited");
            string favouritedNoticeSelector = Select.ById("JustFavorited");

            webProcessor.WaitForAnyElementToBeVisible(
                favouriteButton1Selector,
                favouriteButton2Selector,
                unfavouriteButtonSelector);

            if (webProcessor.IsElementVisible(unfavouriteButtonSelector))
            {
                return;
            }

            webProcessor.ClickAny(
                favouriteButton1Selector,
                favouriteButton2Selector);

            webProcessor.WaitForElementToBeVisible(favouritedNoticeSelector);
        }

        public void Subscribe(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            string subscribeButton1Selector = Select.ById("SubscribeItemOptionAdd");
            string subscribeButton2Selector = Select.ById("SubscribeItemBtn");
            string unsubscribeButtonSelector = Select.ById("SubscribeItemOptionSubscribed");
            string subscribedNoticeSelector = Select.ById("JustSubscribed");
            string modalDialogSelector = Select.ByClass("newmodal");
            string requiredItemSelector = Select.ByClass("requiredItem");

            webProcessor.WaitForAnyElementToBeVisible(
                subscribeButton1Selector,
                subscribeButton2Selector,
                unsubscribeButtonSelector);

            if (webProcessor.IsElementVisible(unsubscribeButtonSelector))
            {
                return;
            }

            webProcessor.ClickAny(
                subscribeButton1Selector,
                subscribeButton2Selector);

            webProcessor.WaitForAnyElementToBeVisible(
                subscribedNoticeSelector,
                modalDialogSelector);

            if (webProcessor.AreAllElementsVisible(modalDialogSelector, requiredItemSelector))
            {
                string continueButtonSelector = Select.ByXPath("//div[@class='newmodal_buttons']/div[1]/span");
                webProcessor.Click(continueButtonSelector);
            }

            webProcessor.WaitForElementToBeVisible(subscribedNoticeSelector);
        }

        void GoToWorksopItemPage(string workshopItemId)
        {
            string workshopItemUrl = string.Format(SteamUrls.WorkshopItemFormat, workshopItemId);

            string mainContentSelector = Select.ById("mainContents");

            webProcessor.GoToUrl(workshopItemUrl);
            webProcessor.WaitForElementToBeVisible(mainContentSelector);
        }
    }
}
