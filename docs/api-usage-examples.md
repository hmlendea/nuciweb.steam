# API Usage Examples

## Overview

Practical code examples for common NuciWeb.Steam scenarios.

## Setup Boilerplate

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
```

---

## Batch Key Activation

```csharp
var keys = File.ReadAllLines("keys.txt")
    .Where(k => !string.IsNullOrWhiteSpace(k))
    .ToList();

var results = new List<(string Key, string Result)>();

foreach (var key in keys)
{
    try
    {
        var product = steam.ActivateKey(key);
        results.Add((key, $"SUCCESS: {product}"));
        Console.WriteLine($"✓ {key} → {product}");
    }
    catch (KeyActivationException ex)
    {
        results.Add((key, $"FAILED: {ex.Code}"));
        Console.WriteLine($"✗ {key} → {ex.Code}");
    }

    // Rate limit courtesy
    Thread.Sleep(TimeSpan.FromSeconds(10));
}

// Summary
var success = results.Count(r => r.Result.StartsWith("SUCCESS"));
var failed = results.Count(r => r.Result.StartsWith("FAILED"));
Console.WriteLine($"\nSummary: {success} succeeded, {failed} failed");

// Save report
File.WriteAllLines("activation-report.txt",
    results.Select(r => $"{r.Key} | {r.Result}"));
```

---

## Profile Setup for New Account

```csharp
// Set display name
steam.SetProfileName("Pro Gamer");

// Set custom URL (must be unique)
steam.SetProfileIdentifier("progamer123");

// Set avatar
steam.SetProfilePicture(@"C:\avatars\gamer.jpg");

Console.WriteLine("Profile configured");
```

---

## Workshop Collection Builder

```csharp
var workshopIds = new[]
{
    123456789, // Map pack
    987654321, // Weapon skin
    555666777  // Sound mod
};

foreach (var id in workshopIds)
{
    try
    {
        // Favourite first (quick)
        steam.FavouriteWorkshopItem(id);
        Console.WriteLine($"❤ Favourited {id}");

        // Then subscribe (downloads content)
        steam.SubscribeToWorkshopItem(id);
        Console.WriteLine($"⬇ Subscribed {id}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ {id}: {ex.Message}");
    }

    Thread.Sleep(TimeSpan.FromSeconds(5));
}
```

---

## Multi-Account Key Activation

```csharp
var accounts = new[]
{
    new SteamAccount { Username = "acc1", Password = "p1", TotpKey = "k1" },
    new SteamAccount { Username = "acc2", Password = "p2", TotpKey = "k2" },
    new SteamAccount { Username = "acc3", Password = "p3", TotpKey = "k3" }
};

var keys = new[] { "KEY1-KEY1-KEY1", "KEY2-KEY2-KEY2" };

foreach (var account in accounts)
{
    Console.WriteLine($"\n=== {account.Username} ===");

    using var web = new WebProcessor();
    var steam = new SteamProcessor(web);

    try
    {
        steam.LogIn(account);

        foreach (var key in keys)
        {
            try
            {
                var product = steam.ActivateKey(key);
                Console.WriteLine($"  ✓ {product}");
            }
            catch (KeyActivationException ex)
            {
                Console.WriteLine($"  ✗ {ex.Code}");
            }
            Thread.Sleep(10000);
        }
    }
    catch (AuthenticationException ex)
    {
        Console.WriteLine($"  LOGIN FAILED: {ex.Message}");
    }
}
```

---

## Persistent Session Automation

```csharp
// Configure persistent profile
var options = new WebProcessorOptions
{
    UserDataDir = @"C:\steam-automation",
    Headless = true
};

using var web = new WebProcessor(options);
var steam = new SteamProcessor(web);

// Check if already logged in by trying a simple operation
bool needsLogin = true;
try
{
    steam.VisitChat(); // Quick check
    needsLogin = false;
    Console.WriteLine("Session valid, skipping login");
}
catch
{
    Console.WriteLine("Session expired, logging in");
}

if (needsLogin)
{
    steam.LogIn(account);
}

// Now run operations
steam.ActivateKey("KEY-KEY-KEY");
steam.SetProfileName("Auto-updated");
```

---

## Error Handling with Retry

```csharp
string ActivateWithRetry(string key, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return steam.ActivateKey(key);
        }
        catch (KeyActivationException ex) when (ex.Code == KeyActivationErrorCode.TooManyAttempts)
        {
            if (attempt == maxRetries) throw;

            var wait = TimeSpan.FromMinutes(Math.Pow(2, attempt)); // 2, 4, 8 min
            Console.WriteLine($"Rate limited, waiting {wait} (attempt {attempt}/{maxRetries})");
            Thread.Sleep(wait);
        }
        catch (KeyActivationException ex) when (ex.Code == KeyActivationErrorCode.Unexpected)
        {
            // Don't retry unknown errors
            throw;
        }
    }
    throw new InvalidOperationException("Unreachable");
}

// Usage
var product = ActivateWithRetry("KEY-KEY-KEY");
```

---

## Conditional Profile Update

```csharp
// Only update if different
var currentName = GetCurrentProfileName(steam); // Would need implementation
var desiredName = "New Name";

if (!string.Equals(currentName, desiredName, StringComparison.Ordinal))
{
    steam.SetProfileName(desiredName);
    Console.WriteLine("Profile name updated");
}
else
{
    Console.WriteLine("Profile name already current");
}
```

---

## Workshop Idempotency Check

```csharp
// The library handles this internally, but you can verify manually
bool IsFavourited(int workshopId)
{
    // Navigate and check heart icon state
    // Requires extending SteamWorkshopProcessor or direct IWebProcessor use
    return false; // Placeholder
}

bool IsSubscribed(int workshopId)
{
    // Navigate and check subscribe button state
    return false; // Placeholder
}

// Usage
if (!IsFavourited(123456))
    steam.FavouriteWorkshopItem(123456);

if (!IsSubscribed(123456))
    steam.SubscribeToWorkshopItem(123456);
```

---

## Headless CI Pipeline

```csharp
// GitHub Actions / CI environment
var options = new WebProcessorOptions
{
    Headless = true,
    // CI-specific options
    Arguments = new[]
    {
        "--no-sandbox",
        "--disable-dev-shm-usage",
        "--disable-gpu",
        "--window-size=1920,1080"
    }
};

using var web = new WebProcessor(options);
var steam = new SteamProcessor(web);

steam.LogIn(account);

// Run automation
foreach (var key in keys)
{
    steam.ActivateKey(key);
    Thread.Sleep(10000);
}

// Results captured in CI logs
```

---

## Custom IWebProcessor for Testing

```csharp
// Unit test with mocked browser
[Test]
public void ActivateKey_ValidKey_ReturnsProductName()
{
    // Arrange
    var mockWeb = new Mock<IWebProcessor>();
    mockWeb.SetupSequence(x => x.NavigateTo(It.IsAny<string>()))
           .Returns(Task.CompletedTask); // Multiple calls

    mockWeb.Setup(x => x.FindElement(It.IsAny<Selector>()))
           .Returns(new Mock<IWebElement>().Object);

    mockWeb.Setup(x => x.GetAttribute(It.IsAny<Selector>(), "value"))
           .Returns("Product Name");

    var processor = new SteamProcessor(mockWeb.Object);
    var account = new SteamAccount { Username = "u", Password = "p" };

    // Act
    processor.LogIn(account);
    var result = processor.ActivateKey("VALID-KEY-KEY");

    // Assert
    Assert.That(result, Is.EqualTo("Product Name"));
    mockWeb.Verify(x => x.NavigateTo(SteamUrls.KeyActivation), Times.Once);
}
```

---

## Related Documentation

- [Quick Start](../quick-start.md) — Basic setup
- [API Reference](../api-reference/steam-processor.md) — Full method signatures
- [FAQ](../faq.md) — Common questions
- [Testing](../testing.md) — Test strategies