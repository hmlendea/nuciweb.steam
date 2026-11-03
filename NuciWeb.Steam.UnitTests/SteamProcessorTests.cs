using System;
using Moq;
using NuciWeb.Automation;
using NuciWeb.Steam;
using NuciWeb.Steam.Models;
using NuciWeb.Steam.Processors;
using NuciWeb.Steam.Utils;
using NUnit.Framework;

namespace NuciWeb.Steam.UnitTests
{
    [TestFixture]
    public sealed class SteamProcessorTests
    {
        Mock<IWebProcessor> webProcessorMock = null!;
        SteamProcessor subject = null!;

        const string CookiePreferencesUrl = "https://store.steampowered.com/account/cookiepreferences";

        [SetUp]
        public void SetUp()
        {
            webProcessorMock = new Mock<IWebProcessor>(MockBehavior.Loose);
            subject = new SteamProcessor(webProcessorMock.Object);
        }

        [Test]
        public void GivenValidAccount_WhenLogIn_ThenDelegatesToAuthenticationProcessor()
        {
            var account = new SteamAccount { Username = "test", Password = "pass", TotpKey = "ABCDEFGHIJKLMNOP" };

            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Wait(It.IsAny<TimeSpan>()));
            webProcessorMock.Setup(x => x.WaitForAnyElementToBeVisible(It.IsAny<string>(), It.IsAny<string>()));
            webProcessorMock.Setup(x => x.AreAllElementsVisible(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForAnyElementToBeVisible(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            webProcessorMock.Setup(x => x.GetText(It.IsAny<string>())).Returns("test");

            subject.LogIn(account);

            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.StoreLogin), Times.Once);
            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.CommunityLogin), Times.Once);
        }

        [Test]
        public void GivenName_WhenSetProfileName_ThenDelegatesToProfileProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForElementToExist(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.SetText(It.IsAny<string>(), It.IsAny<string>()));

            subject.SetProfileName("New Name");

            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.Account), Times.Once);
            webProcessorMock.Verify(x => x.SetText("//*[@name='personaName']", "New Name"), Times.Once);
        }

        [Test]
        public void GivenIdentifier_WhenSetProfileIdentifier_ThenDelegatesToProfileProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForElementToExist(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.SetText(It.IsAny<string>(), It.IsAny<string>()));

            subject.SetProfileIdentifier("newid");

            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.Account), Times.Once);
            webProcessorMock.Verify(x => x.SetText("//*[@name='customURL']", "newid"), Times.Once);
        }

        [Test]
        public void GivenImagePath_WhenSetProfilePicture_ThenDelegatesToProfileProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForElementToExist(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.GetSource(It.IsAny<string>())).Returns("url");
            webProcessorMock.Setup(x => x.ExecuteScript(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.AppendText(It.IsAny<string>(), It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Wait());

            subject.SetProfilePicture("/path/to/image.png");

            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.Account), Times.Once);
        }

        [Test]
        public void WhenAcceptCookies_ThenNavigatesToCookiePreferencesAndClicksAccept()
        {
            webProcessorMock.Setup(x => x.GoToUrl(CookiePreferencesUrl));
            webProcessorMock.Setup(x => x.Click("//div[@class='account_settings_container']/div/div[2]"));

            subject.AcceptCookies();

            webProcessorMock.Verify(x => x.GoToUrl(CookiePreferencesUrl), Times.Once);
            webProcessorMock.Verify(x => x.Click("//div[@class='account_settings_container']/div/div[2]"), Times.Once);
        }

        [Test]
        public void WhenRejectCookies_ThenNavigatesToCookiePreferencesAndClicksReject()
        {
            webProcessorMock.Setup(x => x.GoToUrl(CookiePreferencesUrl));
            webProcessorMock.Setup(x => x.Click("//div[@class='account_settings_container']/div/div[1]"));

            subject.RejectCookies();

            webProcessorMock.Verify(x => x.GoToUrl(CookiePreferencesUrl), Times.Once);
            webProcessorMock.Verify(x => x.Click("//div[@class='account_settings_container']/div/div[1]"), Times.Once);
        }

        [Test]
        public void WhenVisitChat_ThenDelegatesToChatProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));

            subject.VisitChat();

            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.Chat), Times.Once);
        }

        [Test]
        public void GivenKey_WhenActivateKey_ThenDelegatesToKeyProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.IsElementVisible("product_key")).Returns(true);
            webProcessorMock.Setup(x => x.SetText(It.IsAny<string>(), It.IsAny<string>()));
            webProcessorMock.Setup(x => x.UpdateCheckbox(It.IsAny<string>(), It.IsAny<bool>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForAnyElementToBeVisible(It.IsAny<string>(), It.IsAny<string>()));
            webProcessorMock.Setup(x => x.IsElementVisible("error_display")).Returns(false);
            webProcessorMock.Setup(x => x.GetText(It.IsAny<string>())).Returns("Product Name");

            var result = subject.ActivateKey("ABCDE-FGHIJ-KLMNO");

            Assert.That(result, Is.EqualTo("Product Name"));
            webProcessorMock.Verify(x => x.GoToUrl(SteamUrls.KeyActivation), Times.Once);
        }

        [Test]
        public void GivenWorkshopItemId_WhenFavouriteWorkshopItem_ThenDelegatesToWorkshopProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForElementToBeVisible(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));

            subject.FavouriteWorkshopItem("123456");

            webProcessorMock.Verify(x => x.GoToUrl("https://steamcommunity.com/sharedfiles/filedetails/?id=123456"), Times.Once);
        }

        [Test]
        public void GivenWorkshopItemId_WhenSubscribeToWorkshopItem_ThenDelegatesToWorkshopProcessor()
        {
            webProcessorMock.Setup(x => x.GoToUrl(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.WaitForElementToBeVisible(It.IsAny<string>()));
            webProcessorMock.Setup(x => x.Click(It.IsAny<string>()));

            subject.SubscribeToWorkshopItem("123456");

            webProcessorMock.Verify(x => x.GoToUrl("https://steamcommunity.com/sharedfiles/filedetails/?id=123456"), Times.Once);
        }
    }
}