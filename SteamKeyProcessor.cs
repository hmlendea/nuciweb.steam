using OpenQA.Selenium;

namespace NuciWeb.Steam
{
    internal sealed class SteamKeyProcessor : ISteamKeyProcessor
    {
        readonly IWebProcessor webProcessor;

        public SteamKeyProcessor(IWebProcessor webProcessor)
        {
            this.webProcessor = webProcessor;
        }

        public string ActivateKey(string key)
        {
            By keyInputSelector = By.Id("product_key");
            By keyActivationButtonSelector = By.Id("register_btn");
            By agreementCheckboxSelector = By.Id("accept_ssa");

            By errorSelector = By.Id("error_display");
            By receiptSelector = By.Id("receipt_form");

            By productNameSelector = By.ClassName("registerkey_lineitem");

            if (!webProcessor.IsElementVisible(keyInputSelector))
            {
                webProcessor.GoToUrl(SteamUrls.KeyActivation);
            }

            webProcessor.SetText(keyInputSelector, key);
            webProcessor.UpdateCheckbox(agreementCheckboxSelector, true);

            webProcessor.Click(keyActivationButtonSelector);

            webProcessor.WaitForAnyElementToBeVisible(errorSelector, receiptSelector);

            ValidateKeyActivation();

            return webProcessor.GetText(productNameSelector);
        }

        void ValidateKeyActivation()
        {
            By errorSelector = By.Id("error_display");

            if (!webProcessor.IsElementVisible(errorSelector))
            {
                return;
            }

            string errorMessage = webProcessor.GetText(errorSelector);

            if (errorMessage.Contains("is not valid") ||
                errorMessage.Contains("nu este valid"))
            {
                throw new KeyActivationException(
                    "Invalid product key.",
                    KeyActivationErrorCode.InvalidProductKey);
            }

            if (errorMessage.Contains("activated by a different Steam account") ||
                errorMessage.Contains("activat de un cont Steam diferit"))
            {
                throw new KeyActivationException(
                    "Key already activated by a different account.",
                    KeyActivationErrorCode.AlreadyActivatedDifferentAccount);
            }

            if (errorMessage.Contains("This Steam account already owns the product") ||
                errorMessage.Contains("Contul acesta Steam deține deja produsul"))
            {
                throw new KeyActivationException(
                    "Product already owned by this account.",
                    KeyActivationErrorCode.AlreadyActivatedCurrentAccount);
            }

            if (errorMessage.Contains("requires ownership of another product") ||
                errorMessage.Contains("necesită deținerea unui alt produs"))
            {
                throw new KeyActivationException(
                    "A base product is required in order to activate this key.",
                    KeyActivationErrorCode.BaseProductRequired);
            }

            if (errorMessage.Contains("this product is not available for purchase in this country") ||
                errorMessage.Contains("acest produs nu este disponibil pentru achiziție în această țară"))
            {
                throw new KeyActivationException(
                    "The key is locked to a specific region.",
                    KeyActivationErrorCode.RegionLocked);
            }

            if (errorMessage.Contains("too many recent activation attempts") ||
                errorMessage.Contains("prea multe încercări de activare recente"))
            {
                throw new KeyActivationException(
                    "Key activation limit reached.",
                    KeyActivationErrorCode.TooManyAttempts);
            }

            if (errorMessage.Contains("An unexpected error has occurred") ||
                errorMessage.Contains("A apărut o eroare neașteptată"))
            {
                throw new KeyActivationException(
                    "An unexpected error has occurred.",
                    KeyActivationErrorCode.Unexpected);
            }

            throw new KeyActivationException(errorMessage);
        }
    }
}
