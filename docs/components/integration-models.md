# Integration Models — External Integration Boundaries

## Overview

This document describes the external integration boundaries of NuciWeb.Steam — the contracts, data flows, and failure semantics at each system boundary.

## Integration Map

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           NuciWeb.Steam Library                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │  IWebProcessor  │  │  ISteamGuard    │  │      SteamUrls (constants)  │  │
│  │   (NuciWeb)     │  │ (SteamGuard.    │  │                             │  │
│  │                 │  │  TOTP)          │  │                             │  │
│  └────────┬────────┘  └────────┬────────┘  └─────────────────────────────┘  │
│           │                    │                                           │
│           ▼                    ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Internal Processors                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐
│  Steam Store        │ │  Steam Community    │ │  Steam Web API      │
│  (store.steam-      │ │  (steamcommunity.  │ │  (api.steampowered. │
│   powered.com)      │ │   com)             │ │   com)              │
└─────────────────────┘ └─────────────────────┘ └─────────────────────┘
```

---

## IWebProcessor (NuciWeb)

**Direction:** Outbound
**Contract:** `NuciWeb.IWebProcessor`
**Implementation:** `NuciWeb.WebProcessor` (default), custom implementations possible
**Purpose:** Browser automation abstraction — navigation, element interaction, waiting, JavaScript execution

### Key Operations Used

| Operation | Used By | Purpose |
|-----------|---------|---------|
| `GoToUrl(string)` | All processors | Navigate to Steam URLs |
| `Click(Selector)` | All processors | Click buttons, links |
| `ClickAny(Selector[])` | Workshop | Handle multiple button variants |
| `SetText(Selector, string)` | Auth, Profile, Keys | Fill form inputs |
| `AppendText(Selector, string)` | Profile (avatar) | File input path |
| `UpdateCheckbox(Selector, bool)` | Keys | SSA agreement checkbox |
| `GetText(Selector)` | Auth, Keys | Read error messages, product names |
| `GetSource(Selector)` | Profile (avatar) | Read image URLs for comparison |
| `WaitForElementToBeVisible(Selector)` | All processors | Wait for interactive elements |
| `WaitForElementToExist(Selector)` | Profile, Workshop | Wait for page elements |
| `WaitForAnyElementToBeVisible(Selector[])` | Auth, Keys, Workshop | Branch on success/error |
| `IsElementVisible(Selector)` | All processors | Check state before acting |
| `AreAllElementsVisible(Selector[])` | Auth, Workshop | Check compound conditions |
| `DoesElementExist(Selector)` | Auth | Check for cookie prompt |
| `ExecuteScript(string)` | Profile (avatar) | Reveal hidden file input |
| `Wait()` / `Wait(TimeSpan)` | All processors | Generic waits |
| `Dispose()` | Consumer | Clean up browser session |

### Failure Semantics

| Failure | Exception | Handling |
|---------|-----------|----------|
| Navigation timeout | `WebException` / `TimeoutException` | Propagated to caller |
| Element not found | `NoSuchElementException` | Propagated to caller |
| Element not visible | `ElementNotVisibleException` | Propagated to caller |
| JavaScript error | `JavaScriptException` | Propagated to caller |
| Browser crash | `WebDriverException` | Propagated to caller |

### Configuration

- **Injection:** Via `SteamProcessor` constructor
- **Default:** `new NuciWeb.WebProcessor()` (uses WebDriver)
- **WebDriver:** Consumer must provide (ChromeDriver, GeckoDriver, etc.)
- **Browser:** Configurable via NuciWeb (Chrome, Firefox, Edge)

### Lifetime

- **Owned by:** Consumer (if injected) or `SteamProcessor` (if default)
- **Shared by:** All internal processors (same browser session)
- **Disposal:** Consumer responsibility — `webProcessor.Dispose()`

---

## ISteamGuard (SteamGuard.TOTP)

**Direction:** Outbound
**Contract:** `SteamGuard.TOTP.ISteamGuard` (from SteamGuard.TOTP library)
**Implementation:** `SteamGuard.TOTP.SteamGuard` (default), custom implementations possible
**Purpose:** Generate Steam Guard TOTP codes from shared secret

### Interface

```csharp
public interface ISteamGuard
{
    string GenerateAuthenticationCode(string sharedSecret);
}
```

### Usage

```csharp
// In SteamAuthenticationProcessor
string steamGuardCode = steamGuard.GenerateAuthenticationCode(totpKey);
// Returns 5-character code or null/empty on invalid secret
```

### Failure Semantics

| Failure | Behaviour |
|---------|-----------|
| Invalid shared secret (not Base32) | Returns `null` or empty string |
| Valid secret | Returns 5-character code |
| Time sync issues | Code may be rejected by Steam (handled by retry loop) |

### Configuration

- **Injection:** Via `SteamAuthenticationProcessor` constructor overload
- **Default:** `new SteamGuard.TOTP.SteamGuard()`
- **Algorithm:** RFC 6238 TOTP (SHA-1, 30-second windows, 5 digits)
- **Testability:** Can inject mock for deterministic codes

### Lifetime

- **Owned by:** `SteamAuthenticationProcessor` (per `SteamProcessor` instance)
- **Stateless:** No internal state between calls
- **Thread-safety:** Implementation-dependent (default is thread-safe)

---

## Steam Store (store.steampowered.com)

**Direction:** Outbound
**Protocol:** HTTPS
**Authentication:** Cookie-based session (managed by `IWebProcessor`)
**Purpose:** Account login, product key activation, profile management

### Endpoints Used

| Endpoint | Path | Used By | Method |
|----------|------|---------|--------|
| Login | `/login` | Auth | GET (form), POST (credentials) |
| Account | `/account` | Profile | GET (navigate to profile) |
| Key Activation | `/account/registerkey` | Keys | GET (form), POST (key + SSA) |
| Cookie Preferences | `/account/cookiepreferences` | Cookies | GET, POST |

### Data Exchanged

| Direction | Data | Sensitivity |
|-----------|------|-------------|
| Request | Username, password | High (credentials) |
| Request | Steam Guard TOTP code | High (2FA) |
| Request | Product key | Medium (consumable) |
| Request | SSA agreement checkbox | Low |
| Request | Profile name, custom URL | Low |
| Request | Avatar image file | Medium (personal) |
| Response | HTML pages, forms | Low |
| Response | Session cookies | High (authentication) |
| Response | Error messages | Low |
| Response | Product name (receipt) | Low |

### Failure Modes

| Failure | HTTP Status | Library Response |
|---------|-------------|------------------|
| Invalid credentials | 200 (form with error) | `AuthenticationException` |
| Captcha required | 200 (form with captcha) | `AuthenticationException` |
| Steam Guard mismatch | 200 (form with error) | `AuthenticationException` (retry loop) |
| Key invalid | 200 (error display) | `KeyActivationException` (typed) |
| Key already used | 200 (error display) | `KeyActivationException` (typed) |
| Region locked | 200 (error display) | `KeyActivationException` (typed) |
| Rate limited | 429 / 200 (error) | `KeyActivationException` (TooManyAttempts) |
| Network error | N/A | `IWebProcessor` exception |
| Server error | 5xx | `IWebProcessor` exception |

### Rate Limits

- Not explicitly handled — relies on Steam's responses
- Key activation: "too many recent activation attempts" → `TooManyAttempts`
- Login: Captcha after failed attempts

---

## Steam Community (steamcommunity.com)

**Direction:** Outbound
**Protocol:** HTTPS
**Authentication:** Shared cookie session with Steam Store (via `IWebProcessor`)
**Purpose:** Chat, Workshop, profile avatar, community login verification

### Endpoints Used

| Endpoint | Path | Used By | Method |
|----------|------|---------|--------|
| Community Login | `/login/home` | Auth | GET (verifies cookies) |
| Chat | `/chat` | Chat | GET |
| Workshop Item | `/sharedfiles/filedetails/?id={id}` | Workshop | GET |
| Profile Avatar Edit | `/profiles/{id}/edit/avatar` | Profile | GET, POST (file) |

### Data Exchanged

| Direction | Data | Sensitivity |
|-----------|------|-------------|
| Request | Session cookies | High |
| Request | Workshop item ID | Low |
| Request | Favourite/subscribe actions | Low |
| Request | Avatar image file | Medium |
| Response | HTML pages | Low |
| Response | Workshop item data | Low |
| Response | Chat interface | Low |
| Response | Modal dialogs (required items) | Low |

### Failure Modes

| Failure | Library Response |
|---------|------------------|
| Session expired | Redirect to login → `IWebProcessor` exception |
| Workshop item not found | 404 → `IWebProcessor` exception |
| Required base product | Modal dialog → Handled by Workshop processor |
| Network error | `IWebProcessor` exception |

---

## Steam Web API (api.steampowered.com)

**Direction:** Outbound (reserved)
**Protocol:** HTTPS
**Authentication:** API key (not currently used)
**Purpose:** Official Steam Web API endpoints — **not currently used by any flow**

### Status

- **Current:** No operations use Steam Web API
- **Future:** Reserved for read operations (profile, inventory, etc.) to reduce browser dependency
- **Integration:** Would require API key configuration and new processor domain

---

## SteamUrls (Constants)

**Direction:** Internal
**Contract:** `NuciWeb.Steam.Utils.SteamUrls` (static class)
**Purpose:** Centralised URL construction for all Steam endpoints

### Constants

```csharp
internal sealed class SteamUrls
{
    public static string Store => "https://store.steampowered.com";
    public static string Community => "https://steamcommunity.com";

    public static string Account => $"{Store}/account";
    public static string Chat => $"{Community}/chat";
    public static string CookiePreferences => $"{Account}/cookiepreferences";
    public static string StoreLogin => $"{Store}/login";
    public static string CommunityLogin => $"{Community}/login/home";
    public static string KeyActivation = $"{Store}/account/registerkey";
    public static string WorkshopItemFormat => $"{Community}/sharedfiles/filedetails/?id={{0}}";
}
```

### Properties

| Property | Value | Used By |
|----------|-------|---------|
| `Store` | `https://store.steampowered.com` | Base for store URLs |
| `Community` | `https://steamcommunity.com` | Base for community URLs |
| `Account` | `https://store.steampowered.com/account` | Profile navigation |
| `Chat` | `https://steamcommunity.com/chat` | Chat processor |
| `CookiePreferences` | `https://store.steampowered.com/account/cookiepreferences` | Cookie processors |
| `StoreLogin` | `https://store.steampowered.com/login` | Auth (store) |
| `CommunityLogin` | `https://steamcommunity.com/login/home` | Auth (community) |
| `KeyActivation` | `https://store.steampowered.com/account/registerkey` | Key processor |
| `WorkshopItemFormat` | `https://steamcommunity.com/sharedfiles/filedetails/?id={0}` | Workshop processor |

### Maintenance

- **Update trigger:** Steam site structure changes
- **Version bump:** Required when URLs change
- **Testing:** Manual verification after Steam UI updates
- **Localisation:** URLs are locale-agnostic (Steam handles via cookies/headers)

---

## Data Flow Summary

### Authentication Flow Data

```
Client → SteamAccount (username, password, totpKey)
    → SteamProcessor.LogIn()
    → SteamAuthenticationProcessor.LogIn()
    → IWebProcessor.GoToUrl(StoreLogin)
    → Steam Store: GET /login → HTML form
    → IWebProcessor.SetText(username, password)
    → IWebProcessor.Click(submit)
    → Steam Store: POST credentials → Steam Guard challenge
    → ISteamGuard.GenerateAuthenticationCode(totpKey) → 5-char code
    → IWebProcessor.SetText(5 char inputs)
    → Steam Store: POST TOTP → Session cookies
    → IWebProcessor.GoToUrl(CommunityLogin)
    → Steam Community: GET /login/home → Validates cookies
    → IWebProcessor.GetText(username) → Validate match
    → Return success
```

### Key Activation Flow Data

```
Client → string key
    → SteamProcessor.ActivateKey(key)
    → SteamKeyProcessor.ActivateKey(key)
    → IWebProcessor.GoToUrl(KeyActivation) if needed
    → IWebProcessor.SetText(key), UpdateCheckbox(SSA), Click(register)
    → Steam Store: POST /account/registerkey
    → IWebProcessor.WaitForAnyElement(receipt, error)
    → If receipt: IWebProcessor.GetText(productName) → Return
    → If error: IWebProcessor.GetText(error) → Map to KeyActivationErrorCode → Throw
```

### Profile Update Flow Data

```
Client → string name/identifier/imagePath
    → SteamProcessor.SetProfileName/Identifier/Picture()
    → SteamProfileProcessor
    → IWebProcessor.GoToUrl(Account) → Click(avatar) → Click(edit)
    → For name/identifier: SetText(input), Click(save)
    → For picture: Click(avatar tab), ExecuteScript(reveal input), AppendText(file), Wait for URL change, Click(save), Wait for URL change
    → Return
```

### Workshop Flow Data

```
Client → string workshopItemId
    → SteamProcessor.FavouriteWorkshopItem/Subscribe()
    → SteamWorkshopProcessor
    → IWebProcessor.GoToUrl(WorkshopItemFormat)
    → IWebProcessor.WaitForElement(mainContents)
    → Check current state (buttons visible)
    → If already done: Return
    → ClickAny(buttons)
    → WaitForElement(confirmation notice)
    → For subscribe: Check modal, Click(continue) if needed
    → Return
```

---

## Security Boundaries

| Boundary | Protection |
|----------|------------|
| Credentials in memory | `SteamAccount` held only during `LogIn`; not logged, persisted, or exposed |
| TOTP shared secret | Passed to `ISteamGuard`; not retained after code generation |
| Session cookies | Managed by `IWebProcessor`; library never accesses cookie values directly |
| HTTPS enforcement | All `SteamUrls` constants use `https://` |
| No telemetry | Library emits no diagnostics, metrics, or usage data |
| No third-party calls | All network traffic directed to Steam services only |

---

## Related Documentation

- [Architecture](./architecture.md) — Overall architectural context
- [Application Services](./application-services.md) — Processor implementations
- [Security](../security.md) — Security model and mitigations
- [Error Handling](../error-handling.md) — Error taxonomy at boundaries