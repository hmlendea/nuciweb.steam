# Integrations

## Overview

This document describes external systems NuciWeb.Steam integrates with and how.

## Direct Dependencies

### NuciWeb (4.1.0)

**Purpose:** Browser automation abstraction (`IWebProcessor`)

**Integration Points:**
- `SteamProcessor` constructor: `IWebProcessor` injection
- All processors: delegate to `IWebProcessor` methods
- Selectors: `NuciWeb.Automation.Select` helpers

**Key APIs Used:**
| API | Purpose |
|-----|---------|
| `IWebProcessor.NavigateTo(url)` | Page navigation |
| `IWebProcessor.FindElement(selector)` | Element location |
| `IWebProcessor.Click(selector)` | Click element |
| `IWebProcessor.SendKeys(selector, text)` | Text input |
| `IWebProcessor.WaitForElement(selector)` | Explicit wait |
| `IWebProcessor.ExecuteScript(script)` | JavaScript execution |
| `IWebProcessor.GetAttribute(selector, name)` | Attribute reading |
| `IWebProcessor.GetPageSource()` | HTML source |

**Version Sensitivity:** Selector APIs stable. Breaking changes require processor updates.

---

### NuciWeb.Automation (1.0.1)

**Purpose:** Selector construction helpers

**Integration Points:**
- All processors use `Select.ById`, `ByXPath`, `ByCssSelector`, `ByClassName`

**Key APIs Used:**
| Helper | Example |
|--------|---------|
| `Select.ById("elementId")` | Stable ID selectors |
| `Select.ByXPath("//div[@class='x']")` | Complex queries |
| `Select.ByCssSelector(".class")` | CSS selectors |
| `Select.ByClassName("class")` | Class-based |

---

### NuciExtensions (6.0.2)

**Purpose:** Extension methods for `IWebProcessor`

**Integration Points:**
- Processors call extension methods on `IWebProcessor`

**Key APIs Used:**
| Extension | Purpose |
|-----------|---------|
| `ClickAny(selectors[])` | Try multiple selectors |
| `WaitForAnyElementToBeVisible(selectors[])` | Wait for any of multiple |
| `WaitForElementToBeVisible(selector)` | Single element wait |

---

### SteamGuard.TOTP (1.1.1)

**Purpose:** Steam Guard TOTP code generation (RFC 6238)

**Integration Points:**
- `SteamAuthenticationProcessor` uses `ISteamGuard` interface
- Default implementation: `SteamGuard.TOTP.SteamGuard`

**Key APIs Used:**
| API | Purpose |
|-----|---------|
| `ISteamGuard.GenerateCode(sharedSecret)` | Generate 5-char TOTP |
| `SteamGuardAccount` | Account model with shared secret |

**Version Sensitivity:** TOTP algorithm stable. Interface allows mocking.

---

## Steam Website (External System)

**Purpose:** Target of all automation

**Integration Points:**
| Domain | URLs | Selectors |
|--------|------|-----------|
| Store Login | `store.steampowered.com/login` | Username, password, 2FA fields |
| Community Login | `steamcommunity.com/login` | Same as store |
| Profile Edit | `steamcommunity.com/id/{id}/edit` | Name, identifier, avatar inputs |
| Key Activation | `store.steampowered.com/account/registerkey` | Key input, SSA checkbox |
| Chat | `steamcommunity.com/chat` | `currentUserAvatar` |
| Workshop | `steamcommunity.com/sharedfiles/filedetails/?id={id}` | Favourite/subscribe buttons |

**Contracts:**
- HTML structure (implicit, brittle)
- Error message text (localised, mapped in `SteamKeyProcessor`)
- Cookie-based session (shared Store ↔ Community)

**Failure Modes:**
- UI changes break selectors
- Captcha challenges
- Rate limiting
- Regional differences
- Localisation changes

---

## Consumer Integration Patterns

### Basic Usage

```csharp
using var webProcessor = new WebProcessor(); // NuciWeb implementation
var steamProcessor = new SteamProcessor(webProcessor);

var account = new SteamAccount
{
    Username = "user",
    Password = "pass",
    TotpKey = "BASE32SECRET" // optional
};

steamProcessor.LogIn(account);
steamProcessor.ActivateKey("AAAAA-BBBBB-CCCCC");
steamProcessor.SetProfileName("New Name");
```

### With Dependency Injection

```csharp
// Program.cs
services.AddSingleton<IWebProcessor>(sp => new WebProcessor(options));
services.AddTransient<ISteamProcessor, SteamProcessor>();

// Consumer
public class MyService
{
    private readonly ISteamProcessor _steam;
    public MyService(ISteamProcessor steam) => _steam = steam;
}
```

### Testing with Mock

```csharp
var mockWeb = new Mock<IWebProcessor>();
var processor = new SteamProcessor(mockWeb.Object);

// Setup mock expectations
mockWeb.Setup(x => x.NavigateTo(It.IsAny<string>()));
mockWeb.Setup(x => x.FindElement(It.IsAny<Selector>()))
       .Returns(new Mock<IWebElement>().Object);
```

---

## Integration Test Scenarios

### Required Manual Tests

| Scenario | Steps | Expected |
|----------|-------|----------|
| Full login flow | Valid account + TOTP | Session established |
| Login without TOTP | Valid account, no TOTP | Email/phone 2FA handled |
| Invalid credentials | Wrong password | Exception thrown |
| Key activation success | Valid unused key | Returns product name |
| Key already owned | Valid owned key | `AlreadyActivatedCurrentAccount` |
| Key invalid | Random key | `InvalidProductKey` |
| Profile update | Change name/avatar | Persists on Steam |
| Workshop favourite | Valid item ID | Heart icon filled |
| Workshop subscribe | Valid item ID | Subscribed state |
| Chat access | Navigate to chat | Chat UI loaded |

---

## Version Compatibility Matrix

| NuciWeb.Steam | NuciWeb | NuciWeb.Automation | NuciExtensions | SteamGuard.TOTP | .NET |
|---------------|---------|-------------------|----------------|-----------------|------|
| 1.x | 4.1.x | 1.0.x | 6.0.x | 1.1.x | 10.0 |

---

## Related Documentation

- [Dependencies](../dependencies.md) — Version details
- [Architecture](../architecture.md) — Integration architecture
- [Components: Integration Models](../components/integration-models.md) — Data models
- [Testing](../testing.md) — Integration test approach