# Presentation Layer — Public API Facade and Models

## Overview

The presentation layer defines the public contract between NuciWeb.Steam and its consumers. It consists of the `ISteamProcessor` interface, its `SteamProcessor` implementation, and the data models/exceptions that cross the API boundary.

## Public Interface: ISteamProcessor

**Location:** `NuciWeb.Steam/ISteamProcessor.cs`

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

### Design Principles

- **Synchronous only** — All methods are synchronous; no `async`/`Task` in public API
- **No NuciWeb types** — Public signatures use only library-defined types (`SteamAccount`, `string`, primitives)
- **XML documentation** — All public members have `/// <summary>` documentation
- **Stable contract** — Changes require major version bump; additive only for new methods

### Method Contracts

| Method | Parameters | Returns | Throws | Idempotent |
|--------|------------|---------|--------|------------|
| `LogIn` | `SteamAccount` | `void` | `AuthenticationException`, `ArgumentNullException` | No |
| `SetProfileName` | `string name` | `void` | `ArgumentNullException`, automation exceptions | Yes (sets to value) |
| `SetProfileIdentifier` | `string identifier` | `void` | `ArgumentNullException`, automation exceptions | Yes (sets to value) |
| `SetProfilePicture` | `string imagePath` | `void` | `ArgumentNullException`, `FileNotFoundException`, automation exceptions | No (uploads file) |
| `AcceptCookies` | — | `void` | Automation exceptions | Yes |
| `RejectCookies` | — | `void` | Automation exceptions | Yes |
| `VisitChat` | — | `void` | Automation exceptions | Yes |
| `ActivateKey` | `string key` | `string` (product name) | `KeyActivationException`, `ArgumentNullException`, automation exceptions | No (consumes key) |
| `FavouriteWorkshopItem` | `string workshopItemId` | `void` | Automation exceptions | Yes (checks current state) |
| `SubscribeToWorkshopItem` | `string workshopItemId` | `void` | Automation exceptions | Yes (checks current state) |

## Implementation: SteamProcessor

**Location:** `NuciWeb.Steam/SteamProcessor.cs`

```csharp
public sealed class SteamProcessor(IWebProcessor webProcessor) : ISteamProcessor
{
    readonly IWebProcessor webProcessor = webProcessor;
    readonly SteamAuthenticationProcessor authenticationProcessor = new(webProcessor);
    readonly SteamChatProcessor chatProcessor = new(webProcessor);
    readonly SteamKeyProcessor keyProcessor = new(webProcessor);
    readonly SteamProfileProcessor profileProcessor = new(webProcessor);
    readonly SteamWorkshopProcessor workshopProcessor = new(webProcessor);

    public void LogIn(SteamAccount account)
        => authenticationProcessor.LogIn(account);

    public void SetProfileName(string name)
        => profileProcessor.SetName(name);

    public void SetProfileIdentifier(string identifier)
        => profileProcessor.SetIdentifier(identifier);

    public void SetProfilePicture(string imagePath)
        => profileProcessor.SetProfilePicture(imagePath);

    public void AcceptCookies()
    {
        webProcessor.GoToUrl(SteamUrls.CookiePreferences);
        webProcessor.Click(Select.ByXPath("//div[@class='account_settings_container']/div/div[2]"));
    }

    public void RejectCookies()
    {
        webProcessor.GoToUrl(SteamUrls.CookiePreferences);
        webProcessor.Click(Select.ByXPath("//div[@class='account_settings_container']/div/div[1]"));
    }

    public void VisitChat()
        => chatProcessor.Visit();

    public string ActivateKey(string key)
        => keyProcessor.ActivateKey(key);

    public void FavouriteWorkshopItem(string workshopItemId)
        => workshopProcessor.AddToFavourite(workshopItemId);

    public void SubscribeToWorkshopItem(string workshopItemId)
        => workshopProcessor.Subscribe(workshopItemId);
}
```

### Construction

Two constructor patterns:

```csharp
// With injected IWebProcessor (for testing or custom backend)
var processor = new SteamProcessor(customWebProcessor);

// With default NuciWeb implementation
var processor = new SteamProcessor();
```

The parameterless constructor creates a default `IWebProcessor` internally (implementation detail of NuciWeb).

### Lifetime and Ownership

- **Owned by client** — Consumer creates and disposes
- **Per-operation or long-lived** — Can be reused across multiple operations
- **Not thread-safe** — Internal processors maintain state (used TOTP codes)
- **Disposal** — Delegated to `IWebProcessor.Dispose()`; `SteamProcessor` holds no unmanaged resources

## Data Models

### SteamAccount

**Location:** `NuciWeb.Steam/Models/SteamAccount.cs`

```csharp
public sealed class SteamAccount
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TotpKey { get; set; }
}
```

**Properties:**
- `Username` — Steam account username/email
- `Password` — Steam account password
- `TotpKey` — Steam Guard shared secret (Base32 encoded) for TOTP generation

**Usage:**
```csharp
var account = new SteamAccount
{
    Username = "user@example.com",
    Password = "password123",
    TotpKey = "ABCDEF123456" // Base32 shared secret from Steam Guard setup
};
processor.LogIn(account);
```

**Constraints:**
- All properties required for `LogIn` (null/empty throws `ArgumentNullException` in authentication processor)
- `TotpKey` must be valid Base32 for TOTP generation
- Held only in-memory during `LogIn`; not retained by library

## Error Types

### KeyActivationErrorCode

**Location:** `NuciWeb.Steam/KeyActivationErrorCode.cs`

```csharp
public enum KeyActivationErrorCode
{
    Unexpected = 0,
    InvalidProductKey = 1,
    AlreadyActivatedDifferentAccount = 2,
    AlreadyActivatedCurrentAccount = 3,
    BaseProductRequired = 4,
    RegionLocked = 5,
    TooManyAttempts = 6
}
```

**Usage:** Enables typed `catch` handling:

```csharp
try
{
    string productName = processor.ActivateKey("AAAAA-BBBBB-CCCCC");
}
catch (KeyActivationException ex) when (ex.Code == KeyActivationErrorCode.InvalidProductKey)
{
    // Handle invalid key
}
catch (KeyActivationException ex) when (ex.Code == KeyActivationErrorCode.RegionLocked)
{
    // Handle region lock
}
```

**Stability:** Enum values are part of public contract; additive only, never reorder or remove.

### KeyActivationException

**Location:** `NuciWeb.Steam/KeyActivationException.cs`

```csharp
[Serializable]
public class KeyActivationException : Exception
{
    public KeyActivationException(string message)
        : this(message, KeyActivationErrorCode.Unexpected) { }

    public KeyActivationException(string message, KeyActivationErrorCode code)
        : base(message) => Code = code;

    public KeyActivationException(string message, Exception innerException)
        : this(message, KeyActivationErrorCode.Unexpected, innerException) { }

    public KeyActivationException(string message, KeyActivationErrorCode code, Exception innerException)
        : base(message, innerException) => Code = code;

    public KeyActivationErrorCode Code { get; set; }
}
```

**Constructor Overloads:** Four overloads covering all combinations of message, error code, and inner exception.

**Properties:**
- `Code` — `KeyActivationErrorCode` for typed handling
- `Message` — Human-readable error description (from Steam or mapped)
- `InnerException` — Underlying automation exception if applicable

### AuthenticationException

Thrown by `SteamAuthenticationProcessor` for authentication failures. Not defined in this library — comes from NuciWeb or base `Exception`. Common scenarios:
- Captcha required
- Invalid credentials
- Steam Guard code mismatch
- Session username mismatch (already logged in as different user)

## API Design Rationale

### Why Synchronous?

- Simplicity for automation scripts and console applications
- Avoids async complexity in consumer code
- `IWebProcessor` operations are blocking; async would be superficial
- Callers can wrap in `Task.Run` if needed

### Why Facade Pattern?

- Single entry point for all Steam operations
- Hides internal processor complexity
- Enables composition and delegation
- Stable public contract while internals evolve

### Why Internal Processor Interfaces?

- Encapsulation — consumers cannot couple to implementations
- Testability — processors can be mocked via interfaces (internal to test assembly)
- Separation of concerns — each domain isolated

## Related Documentation

- [Architecture](./architecture.md) — Overall architectural context
- [Host and Composition](./host-and-composition.md) — SteamProcessor construction and lifetime
- [Application Services](./application-services.md) — Internal processor implementations
- [API Reference](../api-reference/steam-processor.md) — Detailed API documentation