# Repository Overview

## Purpose

NuciWeb.Steam is a .NET library that provides high-level Steam account automation flows on top of NuciWeb (`IWebProcessor`). It enables developers to automate common Steam operations programmatically without directly handling browser automation complexities.

## Scope

The library covers the following Steam operations:

- **Authentication:** Sign in to Steam Store and Steam Community with Steam Guard TOTP support
- **Cookie Preferences:** Accept or reject cookie consent dialogs
- **Chat Access:** Navigate to and verify Steam Chat loading
- **Product Key Activation:** Activate Steam product keys with typed error handling for common failure scenarios
- **Profile Management:** Update display name, custom profile URL, and avatar image
- **Workshop Interactions:** Favorite and subscribe to Workshop items with idempotency checks

## Entry Points

### Public API

The sole public entry point is the `ISteamProcessor` interface, implemented by `SteamProcessor`:

```csharp
public interface ISteamProcessor
{
    void LogIn(SteamAccount account);
    void SetProfileName(string name);
    void SetProfileIdentifier(string identifier);
    void SetProfilePicture(string imagePath);
    void AcceptCookies();
    void RejectCookies();
    void VisitChat();
    string ActivateKey(string key);
    void FavouriteWorkshopItem(string workshopItemId);
    void SubscribeToWorkshopItem(string workshopItemId);
}
```

### Construction

```csharp
var processor = new SteamProcessor(webProcessor);
// or using default NuciWeb implementation
var processor = new SteamProcessor();
```

The `SteamProcessor` constructor accepts an `IWebProcessor` implementation (from NuciWeb). If none is provided, a default is created internally.

### Models

- `SteamAccount` — Credentials container with `Username`, `Password`, and `TotpKey` (Steam Guard shared secret)

### Error Types

- `KeyActivationException` — Thrown by `ActivateKey` with `KeyActivationErrorCode` for typed error handling
- `AuthenticationException` — Thrown by authentication flows (captcha, invalid credentials, TOTP mismatch, session mismatch)

## Target Audience

Developers building Steam automation tools, bots, or integration services who need reliable, high-level Steam operations without managing browser automation directly.

## Supported Platforms

- .NET 10.0+
- Windows, Linux, macOS (where NuciWeb/WebDriver is supported)
- Requires a WebDriver-compatible browser (Chrome, Firefox, Edge)

## Non-Goals

- Steam Web API wrapper (purely browser automation)
- Multi-account concurrent management (single-threaded design)
- Persistent session storage (delegated to `IWebProcessor`)
- Headless/API-only mode (requires browser)