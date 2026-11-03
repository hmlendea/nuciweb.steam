# Quick Start

## Overview

Get started with NuciWeb.Steam in 5 minutes.

## Prerequisites

- .NET 10.0 SDK
- Google Chrome installed
- Steam account with Steam Guard (TOTP recommended)

## Installation

```bash
dotnet add package NuciWeb.Steam
```

## Basic Usage

### 1. Create SteamAccount

```csharp
using NuciWeb.Steam;

var account = new SteamAccount
{
    Username = "your_username",
    Password = "your_password",
    TotpKey = "YOUR_BASE32_SHARED_SECRET" // From Steam Guard setup
};
```

**Get TotpKey:** Steam Mobile App → Steam Guard → Menu → "Show QR Code" → Copy the Base32 secret (not the QR code).

### 2. Create Processor

```csharp
using NuciWeb; // IWebProcessor implementation

using var webProcessor = new WebProcessor(); // NuciWeb.WebProcessor
var steamProcessor = new SteamProcessor(webProcessor);
```

### 3. Log In

```csharp
steamProcessor.LogIn(account);
Console.WriteLine("Logged in!");
```

### 4. Activate a Key

```csharp
try
{
    string productName = steamProcessor.ActivateKey("AAAAA-BBBBB-CCCCC");
    Console.WriteLine($"Activated: {productName}");
}
catch (KeyActivationException ex)
{
    Console.WriteLine($"Failed: {ex.Code} - {ex.Message}");
}
```

### 5. Update Profile

```csharp
steamProcessor.SetProfileName("New Display Name");
steamProcessor.SetProfileIdentifier("customurl");
steamProcessor.SetProfilePicture(@"C:\path\to\avatar.jpg");
```

### 6. Workshop Interaction

```csharp
// Favourite (heart icon)
steamProcessor.FavouriteWorkshopItem(123456789);

// Subscribe (download for supported games)
steamProcessor.SubscribeToWorkshopItem(123456789);
```

### 7. Visit Chat

```csharp
steamProcessor.VisitChat();
// Chat UI loaded, ready for manual use
```

### 8. Clean Up

```csharp
// webProcessor.Dispose() called by using block
// Closes browser, clears session
```

---

## Complete Example

```csharp
using NuciWeb;
using NuciWeb.Steam;

var account = new SteamAccount
{
    Username = Environment.GetEnvironmentVariable("STEAM_USER"),
    Password = Environment.GetEnvironmentVariable("STEAM_PASS"),
    TotpKey = Environment.GetEnvironmentVariable("STEAM_TOTP")
};

using var web = new WebProcessor();
var steam = new SteamProcessor(web);

steam.LogIn(account);

var keys = new[]
{
    "AAAAA-BBBBB-CCCCC",
    "DDDDD-EEEEE-FFFFF"
};

foreach (var key in keys)
{
    try
    {
        var product = steam.ActivateKey(key);
        Console.WriteLine($"✓ {product}");
    }
    catch (KeyActivationException ex)
    {
        Console.WriteLine($"✗ {key}: {ex.Code}");
    }
    Thread.Sleep(10000); // Rate limit courtesy
}

steam.SetProfileName("Updated via NuciWeb.Steam");
```

---

## Persistent Session (Avoid Re-Login)

```csharp
var options = new WebProcessorOptions
{
    UserDataDir = @"C:\steam-automation-profile",
    Headless = true
};

using var web = new WebProcessor(options);
var steam = new SteamProcessor(web);

// First run: logs in, solves captcha if needed
// Subsequent runs: uses saved cookies, skips login
steam.LogIn(account); // Fast if session valid
```

---

## Error Handling

```csharp
try
{
    steam.LogIn(account);
}
catch (AuthenticationException ex)
{
    // Invalid credentials, 2FA failed, captcha
    Console.WriteLine($"Auth failed: {ex.Message}");
}

try
{
    steam.ActivateKey(key);
}
catch (KeyActivationException ex)
{
    switch (ex.Code)
    {
        case KeyActivationErrorCode.InvalidProductKey:
            Console.WriteLine("Key invalid");
            break;
        case KeyActivationErrorCode.AlreadyActivatedCurrentAccount:
            Console.WriteLine("Already owned");
            break;
        case KeyActivationErrorCode.TooManyAttempts:
            Console.WriteLine("Rate limited, wait");
            break;
        default:
            Console.WriteLine($"Error: {ex.Code}");
            break;
    }
}
```

---

## Next Steps

- [API Reference](../api-reference/steam-processor.md) — Full method docs
- [API Usage Examples](../api-usage-examples.md) — More patterns
- [FAQ](../faq.md) — Common questions
- [Troubleshooting](../troubleshooting.md) — Problem solving
- [Architecture](../architecture.md) — How it works

---

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Forgot `using` on `WebProcessor` | Browser stays open, cookies not cleared |
| No `TotpKey` with Steam Guard | Provide Base32 secret or handle 2FA manually |
| Rapid key activation | Add `Thread.Sleep(10000)` between calls |
| Headless first run fails | Run headed once to solve captcha, then headless |
| Selector errors | Steam UI changed — see Change Guide |

---

## Environment Variables (Recommended)

```bash
export STEAM_USER="your_username"
export STEAM_PASS="your_password"
export STEAM_TOTP="BASE32SECRET"
```

```csharp
var account = new SteamAccount
{
    Username = Environment.GetEnvironmentVariable("STEAM_USER"),
    Password = Environment.GetEnvironmentVariable("STEAM_PASS"),
    TotpKey = Environment.GetEnvironmentVariable("STEAM_TOTP")
};
```

Never hardcode credentials.