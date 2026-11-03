# Application Services — Internal Domain Processors

## Overview

The five internal processors each own a distinct Steam domain. They are `internal` — not exposed to consumers — and implement corresponding internal interfaces for encapsulation and testability.

## Processor Summary

| Processor | Interface | Primary Responsibility | Key Dependencies |
|-----------|-----------|------------------------|------------------|
| `SteamAuthenticationProcessor` | `ISteamAuthenticationProcessor` | Dual login, Steam Guard TOTP, session validation | `IWebProcessor`, `ISteamGuard`, `SteamUrls` |
| `SteamProfileProcessor` | `ISteamProfileProcessor` | Profile name, custom URL, avatar | `IWebProcessor`, `SteamUrls` |
| `SteamKeyProcessor` | `ISteamKeyProcessor` | Product key activation with typed errors | `IWebProcessor`, `SteamUrls`, `KeyActivationException` |
| `SteamChatProcessor` | `ISteamChatProcessor` | Chat navigation and verification | `IWebProcessor`, `SteamUrls` |
| `SteamWorkshopProcessor` | `ISteamWorkshopProcessor` | Workshop favourite/subscribe | `IWebProcessor`, `SteamUrls` |

---

## SteamAuthenticationProcessor

**Location:** `NuciWeb.Steam/Processors/SteamAuthenticationProcessor.cs`

### Interface

```csharp
internal interface ISteamAuthenticationProcessor
{
    void LogIn(SteamAccount account);
}
```

### Constructor Overloads

```csharp
// Primary with ISteamGuard injection (for testing)
public SteamAuthenticationProcessor(IWebProcessor webProcessor, ISteamGuard steamGuard)
{
    this.webProcessor = webProcessor;
    this.steamGuard = steamGuard;
    UsedSteamGuardCodes = new List<string>();
}

// Default with SteamGuard.TOTP implementation
public SteamAuthenticationProcessor(IWebProcessor webProcessor)
    : this(webProcessor, new SteamGuard.TOTP.SteamGuard()) { }
```

### Key Methods

#### LogIn(SteamAccount account)

```csharp
public void LogIn(SteamAccount account)
{
    LogInOnPage(account, SteamUrls.StoreLogin);
    LogInOnPage(account, SteamUrls.CommunityLogin);
}
```

**Flow:**
1. Login to Steam Store (`store.steampowered.com/login`)
2. Login to Steam Community (`steamcommunity.com/login/home`) — cookies transfer
3. Both validate session username matches expected

#### LogInOnPage(SteamAccount account, string url)

```csharp
private void LogInOnPage(SteamAccount account, string url)
{
    webProcessor.GoToUrl(url);
    webProcessor.Wait(TimeSpan.FromSeconds(2));

    // Check if already logged in
    if (webProcessor.AreAllElementsVisible(avatarSelector, accountPulldownSelector))
    {
        ValidateCurrentSession(account.Username);
        return;
    }

    // Handle captcha
    if (webProcessor.IsElementVisible(captchaInputSelector))
    {
        throw new AuthenticationException("Captcha input required.");
    }

    // Reject cookies if prompt exists
    if (webProcessor.DoesElementExist(rejectCookiesButtonSelector))
    {
        webProcessor.Click(rejectCookiesButtonSelector);
    }

    // Fill credentials
    webProcessor.SetText(usernameSelector, account.Username);
    webProcessor.SetText(passwordSelector, account.Password);
    webProcessor.Click(logInButtonSelector);

    // Handle Steam Guard
    webProcessor.WaitForElementToBeVisible(steamGuardCodeInputSelector);
    if (webProcessor.IsElementVisible(steamGuardCodeInputSelector))
    {
        InputSteamGuardCode(account.TotpKey);
    }

    ValidateLogInResult();
}
```

#### InputSteamGuardCode(string totpKey)

```csharp
void InputSteamGuardCode(string totpKey)
{
    if (string.IsNullOrWhiteSpace(totpKey))
        throw new ArgumentNullException(nameof(totpKey));

    string steamGuardCodeInputSelector = Select.ByXPath(SteamGuardCodeInputXPath);
    webProcessor.WaitForElementToBeVisible(steamGuardCodeInputSelector);

    string steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);

    // Loop until unused code generated
    while (string.IsNullOrWhiteSpace(steamGuardCode) || UsedSteamGuardCodes.Contains(steamGuardCode))
    {
        steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);
        Thread.Sleep(10000); // TODO: Remove this!
    }

    UsedSteamGuardCodes.Add(steamGuardCode);

    // Input 5 characters individually
    for (int i = 0; i < 5; i++)
    {
        string charSelector = Select.ByXPath($"{SteamGuardCodeInputXPath}/input[{i + 1}]");
        webProcessor.SetText(charSelector, steamGuardCode[i].ToString());
    }
}
```

**Key Details:**
- TOTP codes are 5 characters, each entered into separate input field
- `UsedSteamGuardCodes` prevents reuse within single `LogIn` call
- 10-second sleep between generations if code already used (known issue)

#### ValidateCurrentSession(string expectedUsername)

```csharp
void ValidateCurrentSession(string expectedUsername)
{
    webProcessor.Click(accountPulldownSelector);
    webProcessor.WaitForAnyElementToBeVisible(onlinePersonaSelector);

    string currentUsername = webProcessor.GetText(onlinePersonaSelector).Trim();

    if (!currentUsername.Equals(expectedUsername, StringComparison.InvariantCultureIgnoreCase))
    {
        throw new AuthenticationException($"Already logged in as a different user ('{currentUsername}').");
    }
}
```

#### ValidateLogInResult()

```csharp
void ValidateLogInResult()
{
    webProcessor.WaitForAnyElementToBeVisible(
        avatarSelector,
        steamGuardIncorrectMessageSelector,
        errorBoxSelector);

    if (webProcessor.IsElementVisible(errorBoxSelector))
        throw new AuthenticationException(webProcessor.GetText(errorBoxSelector));
    else if (webProcessor.IsElementVisible(steamGuardIncorrectMessageSelector))
        throw new AuthenticationException("The provided Steam Guard code is not valid.");
    else if (!webProcessor.IsElementVisible(avatarSelector))
        throw new AuthenticationException("Authentication failure.");
}
```

### Selectors (XPath/ID)

| Element | Selector |
|---------|----------|
| Username input | `//form/div[1]/label/../input` |
| Password input | `//form/div[2]/label/../input` |
| Captcha input | `#input_captcha` |
| Login button | `//form/div[4]/button[@type='submit']` |
| Steam Guard input container | `//form/section/div[2]/section/div/input/..` |
| Steam Guard char input | `//form/section/div[2]/section/div/input/.. /input[{n}]` |
| Avatar (logged in) | `//a[contains(@class,'playerAvatar')]` |
| Account pulldown | `#account_pulldown` |
| Online persona name | `//div[@class='popup_body popup_menu']/a[2]/span[@class='account_name']` |
| User avatar (post-login) | `.user_avatar` |
| Error display | `#error_display` |
| Steam Guard incorrect message | `#login_twofactorauth_message_incorrectcode` |
| Reject cookies button | `#rejectAllButton` |

### State

- `UsedSteamGuardCodes: List<string>` — Cleared per `LogIn` call, prevents TOTP reuse

### Error Handling

| Scenario | Exception |
|----------|-----------|
| Null/empty TOTP key | `ArgumentNullException` |
| Captcha required | `AuthenticationException` |
| Steam Guard code invalid | `AuthenticationException` |
| General error display | `AuthenticationException` (with Steam message) |
| Session username mismatch | `AuthenticationException` |
| No avatar after login | `AuthenticationException` |

---

## SteamProfileProcessor

**Location:** `NuciWeb.Steam/Processors/SteamProfileProcessor.cs`

### Interface

```csharp
internal interface ISteamProfileProcessor
{
    void SetName(string name);
    void SetIdentifier(string identifier);
    void SetProfilePicture(string imagePath);
}
```

### Key Methods

#### SetName(string profileName)

```csharp
public void SetName(string profileName)
{
    GoToEditProfilePage();
    webProcessor.SetText(Select.ByName("personaName"), profileName);
    webProcessor.Click(Select.ByXPath(profileSaveButtonXpath));
}
```

#### SetIdentifier(string identifier)

```csharp
public void SetIdentifier(string identifier)
{
    GoToEditProfilePage();
    webProcessor.SetText(Select.ByName("customURL"), identifier);
    webProcessor.Click(Select.ByXPath(profileSaveButtonXpath));
}
```

#### SetProfilePicture(string imagePath)

```csharp
public void SetProfilePicture(string imagePath)
{
    string profilePictureTabSelector = Select.ByXPath(@"//a[contains(@href,'/edit/avatar')]");
    string profilePictureInputSelector = Select.ByXPath(@"//input[@type='file']");
    string profilePictureNewImageSelector = Select.ByXPath(@"//div[contains(@class,'DialogBody')]/div/div/div[3]/div[1]/div[1]/div/img[contains(@src,'avatar')]");
    string profilePictureCurrentImageSelector = Select.ByXPath(@"//div[contains(@class,'profile_small_header_avatar')]/div/div/img[contains(@src,'avatar')]");
    string saveButtonSelector = Select.ByXPath(profileSaveButtonXpath);

    GoToEditProfilePage();
    webProcessor.Click(profilePictureTabSelector);

    // Capture original URLs for comparison
    string newProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureNewImageSelector);
    string currentProfilePictureOriginalUrl = webProcessor.GetSource(profilePictureCurrentImageSelector);

    // Reveal hidden file input via JavaScript
    webProcessor.ExecuteScript("document.evaluate(\"" + profilePictureInputSelector + "\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.style = \"display: block;\";");
    webProcessor.AppendText(profilePictureInputSelector, imagePath);

    // Wait for new image URL to change
    for (int i = 0; i < 5; i++)
    {
        string newProfilePictureUrl = webProcessor.GetSource(profilePictureNewImageSelector);
        if (newProfilePictureUrl.Equals(newProfilePictureOriginalUrl))
            webProcessor.Wait();
        else
            break;
    }

    webProcessor.Click(saveButtonSelector);

    // Wait for current image URL to change
    for (int i = 0; i < 5; i++)
    {
        string currentProfilePictureUrl = webProcessor.GetSource(profilePictureCurrentImageSelector);
        if (currentProfilePictureUrl.Equals(currentProfilePictureOriginalUrl))
            webProcessor.Wait();
        else
            break;
    }
}
```

### Navigation Helpers

```csharp
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
```

### Selectors

| Element | Selector |
|---------|----------|
| Profile page avatar link | `//div[@id='global_actions']/a[contains(@class,'user_avatar')]` |
| Edit profile button | `//div[@class='profile_header_actions']/a[contains(@class,'btn_profile_action')][1]` |
| Avatar tab | `//a[contains(@href,'/edit/avatar')]` |
| File input | `//input[@type='file']` |
| New avatar preview | `//div[contains(@class,'DialogBody')]/div/div/div[3]/div[1]/div[1]/div/img[contains(@src,'avatar')]` |
| Current avatar | `//div[contains(@class,'profile_small_header_avatar')]/div/div/img[contains(@src,'avatar')]` |
| Save button | `//button[contains(@class,'Primary')]` |
| Display name input | `[name='personaName']` |
| Custom URL input | `[name='customURL']` |

### Error Handling

Relies on `IWebProcessor` exceptions for:
- Element not found
- Navigation timeout
- File not found (for avatar)
- JavaScript execution failure

---

## SteamKeyProcessor

**Location:** `NuciWeb.Steam/Processors/SteamKeyProcessor.cs`

### Interface

```csharp
internal interface ISteamKeyProcessor
{
    string ActivateKey(string key);
}
```

### Key Method

#### ActivateKey(string key)

```csharp
public string ActivateKey(string key)
{
    string keyInputSelector = Select.ById("product_key");
    string keyActivationButtonSelector = Select.ById("register_btn");
    string agreementCheckboxSelector = Select.ById("accept_ssa");

    string errorSelector = Select.ById("error_display");
    string receiptSelector = Select.ById("receipt_form");
    string productNameSelector = Select.ByClass("registerkey_lineitem");

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
```

#### ValidateKeyActivation()

```csharp
void ValidateKeyActivation()
{
    string errorSelector = Select.ById("error_display");

    if (!webProcessor.IsElementVisible(errorSelector))
        return;

    string errorMessage = webProcessor.GetText(errorSelector);

    // English and Romanian error message mapping
    if (errorMessage.Contains("is not valid") || errorMessage.Contains("nu este valid"))
        throw new KeyActivationException("Invalid product key.", KeyActivationErrorCode.InvalidProductKey);

    if (errorMessage.Contains("activated by a different Steam account") || errorMessage.Contains("activat de un cont Steam diferit"))
        throw new KeyActivationException("Key already activated by a different account.", KeyActivationErrorCode.AlreadyActivatedDifferentAccount);

    if (errorMessage.Contains("This Steam account already owns the product") || errorMessage.Contains("Contul acesta Steam deține deja produsul"))
        throw new KeyActivationException("Product already owned by this account.", KeyActivationErrorCode.AlreadyActivatedCurrentAccount);

    if (errorMessage.Contains("requires ownership of another product") || errorMessage.Contains("necesită deținerea unui alt produs"))
        throw new KeyActivationException("A base product is required in order to activate this key.", KeyActivationErrorCode.BaseProductRequired);

    if (errorMessage.Contains("this product is not available for purchase in this country") || errorMessage.Contains("acest produs nu este disponibil pentru achiziție în această țară"))
        throw new KeyActivationException("The key is locked to a specific region.", KeyActivationErrorCode.RegionLocked);

    if (errorMessage.Contains("too many recent activation attempts") || errorMessage.Contains("prea multe încercări de activare recente"))
        throw new KeyActivationException("Key activation limit reached.", KeyActivationErrorCode.TooManyAttempts);

    if (errorMessage.Contains("An unexpected error has occurred") || errorMessage.Contains("A apărut o eroare neașteptată"))
        throw new KeyActivationException("An unexpected error has occurred.", KeyActivationErrorCode.Unexpected);

    throw new KeyActivationException(errorMessage);
}
```

### Selectors

| Element | Selector |
|---------|----------|
| Product key input | `#product_key` |
| Register button | `#register_btn` |
| SSA agreement checkbox | `#accept_ssa` |
| Error display | `#error_display` |
| Receipt form | `#receipt_form` |
| Product name | `.registerkey_lineitem` |

### Error Codes

| Code | Condition | Message |
|------|-----------|---------|
| `InvalidProductKey` | "is not valid" / "nu este valid" | "Invalid product key." |
| `AlreadyActivatedDifferentAccount` | "activated by a different Steam account" / "activat de un cont Steam diferit" | "Key already activated by a different account." |
| `AlreadyActivatedCurrentAccount` | "already owns the product" / "deține deja produsul" | "Product already owned by this account." |
| `BaseProductRequired` | "requires ownership of another product" / "necesită deținerea unui alt produs" | "A base product is required in order to activate this key." |
| `RegionLocked` | "not available for purchase in this country" / "nu este disponibil pentru achiziție în această țară" | "The key is locked to a specific region." |
| `TooManyAttempts` | "too many recent activation attempts" / "prea multe încercări de activare recente" | "Key activation limit reached." |
| `Unexpected` | "unexpected error" / "eroare neașteptată" or unmapped | "An unexpected error has occurred." / raw message |

### Localisation Support

Currently supports English and Romanian error messages. Other locales may not map to typed errors and will throw `KeyActivationException` with `Unexpected` code and raw message.

---

## SteamChatProcessor

**Location:** `NuciWeb.Steam/Processors/SteamChatProcessor.cs`

### Interface

```csharp
internal interface ISteamChatProcessor
{
    void Visit();
}
```

### Implementation

```csharp
public void Visit()
{
    webProcessor.GoToUrl(SteamUrls.Chat);
    webProcessor.WaitForElementToExist(Select.ByClass("currentUserAvatar"));
}
```

### Selectors

| Element | Selector |
|---------|----------|
| Current user avatar | `.currentUserAvatar` |

### URL

`SteamUrls.Chat` → `https://steamcommunity.com/chat`

---

## SteamWorkshopProcessor

**Location:** `NuciWeb.Steam/Processors/SteamWorkshopProcessor.cs`

### Interface

```csharp
internal interface ISteamWorkshopProcessor
{
    void AddToFavourite(string workshopItemId);
    void Subscribe(string workshopItemId);
}
```

### Key Methods

#### AddToFavourite(string workshopItemId)

```csharp
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
        return; // Already favorited

    webProcessor.ClickAny(favouriteButton1Selector, favouriteButton2Selector);
    webProcessor.WaitForElementToBeVisible(favouritedNoticeSelector);
}
```

#### Subscribe(string workshopItemId)

```csharp
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
        return; // Already subscribed

    webProcessor.ClickAny(subscribeButton1Selector, subscribeButton2Selector);

    webProcessor.WaitForAnyElementToBeVisible(
        subscribedNoticeSelector,
        modalDialogSelector);

    // Handle required base product modal
    if (webProcessor.AreAllElementsVisible(modalDialogSelector, requiredItemSelector))
    {
        string continueButtonSelector = Select.ByXPath("//div[@class='newmodal_buttons']/div[1]/span");
        webProcessor.Click(continueButtonSelector);
    }

    webProcessor.WaitForElementToBeVisible(subscribedNoticeSelector);
}
```

#### GoToWorksopItemPage(string workshopItemId)

```csharp
void GoToWorksopItemPage(string workshopItemId)
{
    string workshopItemUrl = string.Format(SteamUrls.WorkshopItemFormat, workshopItemId);
    string mainContentSelector = Select.ById("mainContents");

    webProcessor.GoToUrl(workshopItemUrl);
    webProcessor.WaitForElementToBeVisible(mainContentSelector);
}
```

### Selectors

| Element | Selector |
|---------|----------|
| Favourite button (option) | `#FavoriteItemOptionAdd` |
| Favourite button (main) | `#FavoriteItemBtn` |
| Unfavourite button | `#FavoriteItemOptionFavorited` |
| Favourited notice | `#JustFavorited` |
| Subscribe button (option) | `#SubscribeItemOptionAdd` |
| Subscribe button (main) | `#SubscribeItemBtn` |
| Unsubscribe button | `#SubscribeItemOptionSubscribed` |
| Subscribed notice | `#JustSubscribed` |
| Modal dialog | `.newmodal` |
| Required item in modal | `.requiredItem` |
| Modal continue button | `//div[@class='newmodal_buttons']/div[1]/span` |
| Main content | `#mainContents` |

### URL Format

`SteamUrls.WorkshopItemFormat` → `https://steamcommunity.com/sharedfiles/filedetails/?id={0}`

### Idempotency

Both methods check current state before acting:
- `AddToFavourite` — returns early if `FavoriteItemOptionFavorited` visible
- `Subscribe` — returns early if `SubscribeItemOptionSubscribed` visible

---

## Common Patterns

### Selector Strategy

All processors use `NuciWeb.Automation.Select` helpers:
- `Select.ById("id")`
- `Select.ByClass("class")`
- `Select.ByName("name")`
- `Select.ByXPath("xpath")`

### Waiting Strategy

- `WaitForElementToBeVisible` — for interactive elements
- `WaitForElementToExist` — for presence checks
- `WaitForAnyElementToBeVisible` — for branching flows (success/error)
- `Wait` — generic short wait

### Error Propagation

Processors do not catch `IWebProcessor` exceptions — they propagate to caller for handling.

### No Logging

Processors emit no logs; consumers wrap calls for audit.

---

## Related Documentation

- [Host and Composition](./host-and-composition.md) — How processors are composed
- [Architecture](./architecture.md) — Overall architectural context
- [Flows](../flows/) — End-to-end execution flows
- [Error Handling](../error-handling.md) — Error taxonomy and patterns