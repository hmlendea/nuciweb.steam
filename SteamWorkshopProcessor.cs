using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamWorkshopProcessor : ISteamWorkshopProcessor
    {
        readonly IWebProcessor webProcessor;

        internal SteamWorkshopProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public void AddToFavourite(string workshopItemId)
        {
            GoToWorksopItemPage(workshopItemId);

            By favouriteButton1Selector = By.Id("FavoriteItemOptionAdd");
            By favouriteButton2Selector = By.Id("FavoriteItemBtn");
            By unfavouriteButtonSelector = By.Id("FavoriteItemOptionFavorited");
            By favouritedNoticeSelector = By.Id("JustFavorited");

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

            By subscribeButton1Selector = By.Id("SubscribeItemOptionAdd");
            By subscribeButton2Selector = By.Id("SubscribeItemBtn");
            By unsubscribeButtonSelector = By.Id("SubscribeItemOptionSubscribed");
            By subscribedNoticeSelector = By.Id("JustSubscribed");
            By modalDialogSelector = By.ClassName("newmodal");
            By requiredItemSelector = By.ClassName("requiredItem");

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
                By continueButtonSelector = By.XPath("//div[@class='newmodal_buttons']/div[1]/span");
                webProcessor.Click(continueButtonSelector);
            }

            webProcessor.WaitForElementToBeVisible(subscribedNoticeSelector);
        }

        void GoToWorksopItemPage(string workshopItemId)
        {
            string workshopItemUrl = string.Format(SteamUrls.WorkshopItemFormat, workshopItemId);

            By mainContentSelector = By.Id("mainContents");

            webProcessor.GoToUrl(workshopItemUrl);
            webProcessor.WaitForElementToBeVisible(mainContentSelector);
        }
    }
}
