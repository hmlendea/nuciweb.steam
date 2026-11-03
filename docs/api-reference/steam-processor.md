# SteamProcessor API Reference

## Overview

`SteamProcessor` is the sole public implementation of `ISteamProcessor`. It acts as a facade, delegating all operations to five internal processors that share a single `IWebProcessor` browser session.

## Namespace

```csharp
namespace NuciWeb.Steam;
```

## Interface: ISteamProcessor

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

## Class: SteamProcessor

```csharp
public sealed class SteamProcessor : ISteamProcessor
{
    public SteamProcessor(IWebProcessor webProcessor);

    // ISteamProcessor implementation
    public void LogIn(SteamAccount account);
    public void SetProfileName(string name);
    public void SetProfileIdentifier(string identifier);
    public void SetProfilePicture(string imagePath);
    public void AcceptCookies();
    public void RejectCookies();
    public void VisitChat();
    public string ActivateKey(string key);
    public void FavouriteWorkshopItem(string workshopItemId);
    public void SubscribeToWorkshopItem(string workshopItemId);
}
```

### Constructor

```csharp
public SteamProcessor(IWebProcessor webProcessor)
```

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `webProcessor` | `IWebProcessor` | Browser automation abstraction (shared by all internal processors) |

**Behaviour:**
1. Stores `webProcessor` reference
2. Instantiates `SteamAuthenticationProcessor(webProcessor)`
3. Instantiates `SteamChatProcessor(webProcessor)`
4. Instantiates `SteamKeyProcessor(webProcessor)`
5. Instantiates `SteamProfileProcessor(webProcessor)`
6. Instantiates `SteamWorkshopProcessor(webProcessor)`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `webProcessor` is null |

**Example:**
```csharp
using NuciWeb;
using NuciWeb.Steam;

var webProcessor = new WebProcessor(); // Uses ChromeDriver by default
var steam = new SteamProcessor(webProcessor);
```

---

## Method: LogIn

```csharp
public void LogIn(SteamAccount account)
```

**Description:** Authenticates on both Steam Store and Steam Community using the provided credentials. Handles Steam Guard TOTP if enabled on the account.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `account` | `SteamAccount` | Credentials container with username, password, and optional TOTP key |

**Returns:** `void` — Success indicated by no exception.

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `account` is null |
| `ArgumentNullException` | `account.TotpKey` is null/empty when Steam Guard required |
| `AuthenticationException` | Invalid credentials |
| `AuthenticationException` | Captcha required |
| `AuthenticationException` | Steam Guard code invalid |
| `AuthenticationException` | General authentication failure |
| `AuthenticationException` | Already logged in as different user (community) |
| `IWebProcessor` exceptions | Network, timeout, element not found |

**Flow:**
1. Navigate to `https://store.steampowered.com/login`
2. Check if already logged in (early return if same user)
3. Detect captcha (fail immediately if present)
4. Reject cookie consent if prompted
5. Submit username/password
6. If Steam Guard challenged: generate TOTP, input 5 chars
7. Validate login result (error display, Steam Guard error, or success)
8. Navigate to `https://steamcommunity.com/login/home`
8. Validate session username matches expected

**Example:**
```csharp
var account = new SteamAccount
{
    Username = "user@example.com",
    Password = "securePassword123",
    TotpKey = "BASE32SHAREDSECRET" // Optional, required if Steam Guard enabled
};

steam.LogIn(account);
// If no exception, authenticated on both store and community
```

**See Also:** [Authentication Flow](../flows/authentication.md), [SteamAccount](#steamaccount)

---

## Method: SetProfileName

```csharp
public void SetProfileName(string name)
```

**Description:** Updates the Steam profile display name.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `name` | `string` | New display name (max 32 characters per Steam) |

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `name` is null |
| `IWebProcessor` exceptions | Not logged in, network, timeout, element not found |

**Flow:**
1. Navigate to `https://store.steampowered.com/account`
2. Click avatar → Click "Edit Profile"
3. Wait for profile name input
4. Set text to `name`
5. Click save button
6. Wait for page reload

**Example:**
```csharp
steam.SetProfileName("New Display Name");
```

**See Also:** [Profile Management Flow](../flows/profile-management.md)

---

## Method: SetProfileIdentifier

```csharp
public void SetProfileIdentifier(string identifier)
```

**Description:** Updates the Steam profile custom URL identifier (the vanity URL part).

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `identifier` | `string` | Custom URL identifier (alphanumeric, underscores, 2-32 chars) |

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `identifier` is null |
| `IWebProcessor` exceptions | Not logged in, network, timeout, element not found |
| Steam validation error | Identifier taken, invalid format (not caught as exception) |

**Flow:**
1. Navigate to `https://store.steampowered.com/account`
2. Click avatar → Click "Edit Profile"
3. Wait for custom URL input
4. Set text to `identifier`
4. Click save button (same as profile name)
5. Wait for page reload

**Example:**
```csharp
steam.SetProfileIdentifier("mycustomurl");
// Profile becomes: https://steamcommunity.com/id/mycustomurl
```

**See Also:** [Profile Management Flow](../flows/profile-management.md)

---

## Method: SetProfilePicture

```csharp
public void SetProfilePicture(string imagePath)
```

**Description:** Uploads a new profile avatar image.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `imagePath` | `string` | Local file path to image (JPG/PNG, recommended < 1MB, 184x184px) |

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `imagePath` is null |
| `IWebProcessor` exceptions | Not logged in, network, timeout, element not found |
| `TimeoutException` | Avatar URL never changes (infinite loop) |
| Steam validation error | Invalid format, too large (not caught as exception) |

**Flow:**
1. Navigate to `https://store.steampowered.com/account`
2. Click avatar → Click "Edit Profile"
3. Click "Avatar" tab
4. Wait for avatar image, capture current `src` URL
5. Execute JavaScript to reveal hidden file input
6. Set file input to `imagePath`
7. Poll avatar image `src` until it changes (upload complete)
8. Click "Save Avatar" button
9. Wait for page reload

**Example:**
```csharp
steam.SetProfilePicture("/home/user/avatar.png");
```

**See Also:** [Profile Management Flow](../flows/profile-management.md)

---

## Method: AcceptCookies

```csharp
public void AcceptCookies()
```

**Description:** Accepts all cookies on Steam Store cookie preferences page.

**Parameters:** None

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `IWebProcessor` exceptions | Network, timeout, element not found |

**Flow:**
1. Navigate to `https://store.steampowered.com/account/cookiepreferences`
2. Click "Accept All" button
3. Wait for confirmation

**Example:**
```csharp
steam.AcceptCookies();
```

---

## Method: RejectCookies

```csharp
public void RejectCookies()
```

**Description:** Rejects all cookies on Steam Store cookie preferences page.

**Parameters:** None

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `IWebProcessor` exceptions | Network, timeout, element not found |

**Flow:**
1. Navigate to `https://store.steampowered.com/account/cookiepreferences`
2. Click "Reject All" button
3. Wait for confirmation

**Example:**
```csharp
steam.RejectCookies();
```

---

## Method: VisitChat

```csharp
public void VisitChat()
```

**Description:** Navigates to Steam Chat on Steam Community.

**Parameters:** None

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `IWebProcessor` exceptions | Not logged in, network, timeout, element not found |

**Flow:**
1. Navigate to `https://steamcommunity.com/chat`
2. Wait for `#currentUserAvatar` to be visible

**Example:**
```csharp
steam.VisitChat();
// Browser now at Steam Chat interface
```

**See Also:** [Chat Access Flow](../flows/chat-access.md)

---

## Method: ActivateKey

```csharp
public string ActivateKey(string key)
```

**Description:** Redeems a product key on Steam Store. Returns the product name on success.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `key` | `string` | Product key (format: `AAAAA-BBBBB-CCCCC` or similar) |

**Returns:** `string` — Human-readable product name from activation receipt.

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `key` is null |
| `KeyActivationException` | Invalid product key (`InvalidProductKey`) |
| `KeyActivationException` | Key used on different account (`AlreadyActivatedDifferentAccount`) |
| `KeyActivationException` | Key already owned (`AlreadyActivatedCurrentAccount`) |
| `KeyActivationException` | Base product required (`BaseProductRequired`) |
| `KeyActivationException` | Region locked (`RegionLocked`) |
| `KeyActivationException` | Rate limited (`TooManyAttempts`) |
| `KeyActivationException` | Unrecognised error (`Unexpected`) |
| `IWebProcessor` exceptions | Not logged in, network, timeout, element not found |

**Flow:**
1. Ensure on `https://store.steampowered.com/account/registerkey`
2. Input key, check SSA agreement, click register
3. Wait for receipt or error display
4. If receipt: extract and return product name
5. If error: map message to `KeyActivationErrorCode`, throw `KeyActivationException`

**Example:**
```csharp
try
{
    string productName = steam.ActivateKey("AAAAA-BBBBB-CCCCC");
    Console.WriteLine($"Activated: {productName}");
}
catch (KeyActivationException ex)
{
    Console.WriteLine($"Failed: {ex.Code} - {ex.Message}");
    switch (ex.Code)
    {
        case KeyActivationErrorCode.AlreadyActivatedCurrentAccount:
            Console.WriteLine("Already in library");
            break;
        case KeyActivationErrorCode.TooManyAttempts:
            Console.WriteLine("Rate limited, wait before retry");
            break;
    }
}
```

**See Also:** [Key Activation Flow](../flows/key-activation.md), [KeyActivationErrorCode](#keyactivationerrorcode), [KeyActivationException](#keyactivationexception)

---

## Method: FavouriteWorkshopItem

```csharp
public void FavouriteWorkshopItem(string workshopItemId)
```

**Description:** Favourites a Steam Workshop item. Idempotent — returns immediately if already favourited.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `workshopItemId` | `string` | Workshop item ID (numeric string) |

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `workshopItemId` is null |
| `IWebProcessor` exceptions | Not logged in, item not found, network, timeout, element not found |

**Flow:**
1. Navigate to `https://steamcommunity.com/sharedfiles/filedetails/?id={workshopItemId}`
2. Wait for `#mainContents`
3. If favourited button visible: return (idempotent)
4. Click favourite button (handles variants)
5. Wait for favourited notice

**Example:**
```csharp
steam.FavouriteWorkshopItem("123456789");
```

**See Also:** [Workshop Interaction Flow](../flows/workshop-interaction.md)

---

## Method: SubscribeToWorkshopItem

```csharp
public void SubscribeToWorkshopItem(string workshopItemId)
```

**Description:** Subscribes to a Steam Workshop item. Idempotent — returns immediately if already subscribed. Handles required base product modal.

**Parameters:**
| Name | Type | Description |
|------|------|-------------|
| `workshopItemId` | `string` | Workshop item ID (numeric string) |

**Returns:** `void`

**Exceptions:**
| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `workshopItemId` is null |
| `IWebProcessor` exceptions | Not logged in, item not found, network, timeout, element not found |

**Flow:**
1. Navigate to `https://steamcommunity.com/sharedfiles/filedetails/?id={workshopItemId}`
2. Wait for `#mainContents`
3. If subscribed button visible: return (idempotent)
4. Click subscribe button (handles variants)
5. Wait for subscribed notice
6. If required items modal visible: click "Continue"

**Example:**
```csharp
steam.SubscribeToWorkshopItem("123456789");
```

**See Also:** [Workshop Interaction Flow](../flows/workshop-interaction.md)

---

## Type: SteamAccount

```csharp
public sealed class SteamAccount
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TotpKey { get; set; }
}
```

**Properties:**
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Username` | `string` | Yes | Steam account email or username |
| `Password` | `string` | Yes | Account password |
| `TotpKey` | `string` | Conditional | Base32 shared secret for Steam Guard TOTP (required if 2FA enabled) |

**Usage:**
```csharp
var account = new SteamAccount
{
    Username = "user@example.com",
    Password = "password123",
    TotpKey = "JBSWY3DPEHPK3PXP" // Base32, from Steam Guard setup
};
```

**Security Notes:**
- Never log `TotpKey` or `Password`
- `SteamAccount` instances should be short-lived
- Clear references after `LogIn` completes

---

## Enum: KeyActivationErrorCode

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

**Values:**
| Value | Name | HTTP-Style Meaning |
|-------|------|-------------------|
| 0 | `Unexpected` | 500 Internal Server Error (unmapped) |
| 1 | `InvalidProductKey` | 400 Bad Request (invalid format) |
| 2 | `AlreadyActivatedDifferentAccount` | 409 Conflict (used elsewhere) |
| 3 | `AlreadyActivatedCurrentAccount` | 409 Conflict (already owned) |
| 4 | `BaseProductRequired` | 412 Precondition Failed (missing base) |
| 5 | `RegionLocked` | 403 Forbidden (region restriction) |
| 6 | `TooManyAttempts` | 429 Too Many Requests (rate limited) |

**Mapping:** Performed by `SteamKeyProcessor.MapErrorToCode` using English/Romanian string matching.

---

## Exception: KeyActivationException

```csharp
[Serializable]
public class KeyActivationException : Exception
{
    public KeyActivationErrorCode Code { get; }

    public KeyActivationException(KeyActivationErrorCode code, string message);
    public KeyActivationException(KeyActivationErrorCode code, string message, Exception inner);
    protected KeyActivationException(SerializationInfo info, StreamingContext context);
    public override void GetObjectData(SerializationInfo info, StreamingContext context);
}
```

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Code` | `KeyActivationErrorCode` | Typed error code for programmatic handling |
| `Message` | `string` | Original Steam error message (localised) |

**Constructors:**
| Constructor | Use Case |
|-------------|----------|
| `(code, message)` | Standard throw |
| `(code, message, inner)` | Wrapping another exception |
| Serialization | Binary serialization support |

**Example Handling:**
```csharp
try
{
    steam.ActivateKey(key);
}
catch (KeyActivationException ex) when (ex.Code == KeyActivationErrorCode.TooManyAttempts)
{
    await Task.Delay(TimeSpan.FromMinutes(5));
    steam.ActivateKey(key); // Retry
}
catch (KeyActivationException ex)
{
    logger.LogError("Key activation failed: {Code} - {Message}", ex.Code, ex.Message);
    throw;
}
```

---

## Exception: AuthenticationException

```csharp
public class AuthenticationException : Exception
{
    public AuthenticationException(string message);
    public AuthenticationException(string message, Exception inner);
}
```

**Description:** Thrown by `SteamAuthenticationProcessor` for any authentication failure.

**Common Messages:**
| Message | Cause |
|---------|-------|
| "Captcha input required." | Steam presented captcha challenge |
| "The provided Steam Guard code is not valid." | TOTP code rejected |
| "Authentication failure." | No avatar visible after login attempt |
| "Already logged in as a different user ('X')." | Community session mismatch |
| Steam error display text | Invalid credentials, account locked, etc. |

**Handling:**
```csharp
try
{
    steam.LogIn(account);
}
catch (AuthenticationException ex)
{
    if (ex.Message.Contains("Captcha"))
    {
        // Requires manual intervention
    }
    else if (ex.Message.Contains("Steam Guard"))
    {
        // TOTP issue - check TotpKey, system time
    }
    else
    {
        // Credentials or account issue
    }
    throw;
}
```

---

## Complete Usage Example

```csharp
using NuciWeb;
using NuciWeb.Steam;

var webProcessor = new WebProcessor();
var steam = new SteamProcessor(webProcessor);

try
{
    // Authenticate
    var account = new SteamAccount
    {
        Username = "user@example.com",
        Password = "password123",
        TotpKey = "BASE32SECRET"
    };
    steam.LogIn(account);

    // Profile updates
    steam.SetProfileName("New Name");
    steam.SetProfileIdentifier("customurl");
    steam.SetProfilePicture("/path/to/avatar.jpg");

    // Key activation
    string product = steam.ActivateKey("AAAAA-BBBBB-CCCCC");
    Console.WriteLine($"Activated: {product}");

    // Workshop
    steam.FavouriteWorkshopItem("123456789");
    steam.SubscribeToWorkshopItem("987654321");

    // Chat
    steam.VisitChat();
}
catch (KeyActivationException ex)
{
    Console.Error.WriteLine($"Key error: {ex.Code} - {ex.Message}");
}
catch (AuthenticationException ex)
{
    Console.Error.WriteLine($"Auth error: {ex.Message}");
}
finally
{
    webProcessor.Dispose(); // Clean up browser
}
```

---

## Related Documentation

- [API Reference Index](./INDEX.md) — Summary table
- [Architecture](../architecture.md) — Facade pattern
- [Components: Application Services](../components/application-services.md) — Internal processors
- [Flows](../flows/) — End-to-end sequences
- [Error Handling](../error-handling.md) — Exception taxonomy
- [Integration Models](../components/integration-models.md) — External boundaries