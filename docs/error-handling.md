# Error Handling

## Overview

This document describes the error handling strategy, exception taxonomy, and handling patterns in NuciWeb.Steam.

## Exception Taxonomy

### Library-Defined Exceptions

| Exception | Base | Thrown By | Purpose |
|-----------|------|-----------|---------|
| `KeyActivationException` | `Exception` | `SteamKeyProcessor` | Typed key activation failures |
| `AuthenticationException` | `Exception` | `SteamAuthenticationProcessor` | Authentication failures |
| `ArgumentNullException` | `ArgumentException` | All public methods | Null argument validation |

### External Exceptions (Propagated)

| Exception | Source | Common Causes |
|-----------|--------|---------------|
| `NoSuchElementException` | `IWebProcessor` / Selenium | Selector changed, element not present |
| `ElementNotVisibleException` | `IWebProcessor` / Selenium | Element hidden, not interactive |
| `TimeoutException` | `IWebProcessor` / Selenium | Wait exceeded, page load slow |
| `WebDriverException` | Selenium | Browser crash, driver issue, network |
| `WebException` | .NET / Selenium | Network unreachable, DNS, timeout |
| `JavaScriptException` | `IWebProcessor` | Script execution failed |

---

## Exception Hierarchy

```
System.Exception
├── System.ArgumentException
│   └── System.ArgumentNullException
├── NuciWeb.Steam.KeyActivationException (Serializable)
│   └── Code: KeyActivationErrorCode
├── NuciWeb.Steam.AuthenticationException
└── External (Selenium, .NET)
    ├── OpenQA.Selenium.NoSuchElementException
    ├── OpenQA.Selenium.ElementNotVisibleException
    ├── OpenQA.Selenium.TimeoutException
    ├── OpenQA.Selenium.WebDriverException
    ├── System.Net.WebException
    └── OpenQA.Selenium.JavaScriptException
```

---

## Handling Patterns

### Pattern 1: Typed Exception Handling (Key Activation)

```csharp
try
{
    string product = steam.ActivateKey(key);
}
catch (KeyActivationException ex)
{
    switch (ex.Code)
    {
        case KeyActivationErrorCode.InvalidProductKey:
            // Key format wrong or doesn't exist
            break;
        case KeyActivationErrorCode.AlreadyActivatedCurrentAccount:
            // Already owned
            break;
        case KeyActivationErrorCode.AlreadyActivatedDifferentAccount:
            // Used on another account
            break;
        case KeyActivationErrorCode.BaseProductRequired:
            // DLC needs base game
            break;
        case KeyActivationErrorCode.RegionLocked:
            // Region restriction
            break;
        case KeyActivationErrorCode.TooManyAttempts:
            // Rate limited - retry after delay
            await Task.Delay(TimeSpan.FromMinutes(5));
            // Retry logic here
            break;
        case KeyActivationErrorCode.Unexpected:
            // Unmapped error - log for investigation
            logger.LogError("Unknown key activation error: {Message}", ex.Message);
            break;
    }
}
```

### Pattern 2: Authentication Exception Handling

```csharp
try
{
    steam.LogIn(account);
}
catch (AuthenticationException ex)
{
    if (ex.Message.Contains("Captcha"))
    {
        // Requires manual intervention - cannot automate
        logger.LogError("Captcha required for login");
    }
    else if (ex.Message.Contains("Steam Guard"))
    {
        // TOTP issue - check TotpKey, system time
        logger.LogError("Steam Guard code invalid - check TotpKey and system clock");
    }
    else if (ex.Message.Contains("different user"))
    {
        // Session conflict - clear cookies and retry
        logger.LogError("Logged in as different user");
    }
    else
    {
        // Invalid credentials, account locked, etc.
        logger.LogError("Authentication failed: {Message}", ex.Message);
    }
    throw; // Re-throw or handle
}
```

### Pattern 3: Browser/Infrastructure Exception Handling

```csharp
try
{
    steam.SetProfileName("New Name");
}
catch (NoSuchElementException ex)
{
    // Selector likely broken - Steam UI changed
    logger.LogError("Element not found - Steam UI may have changed: {Selector}", ex.Message);
    throw new InvalidOperationException("Steam UI changed, library update needed", ex);
}
catch (TimeoutException ex)
{
    // Page load slow, network issue, or element never appeared
    logger.LogError("Operation timed out: {Message}", ex.Message);
    // Could retry with longer timeout
}
catch (WebDriverException ex)
{
    // Browser crash, driver issue
    logger.LogError("WebDriver error: {Message}", ex.Message);
    // Requires browser/driver restart
}
catch (WebException ex)
{
    // Network level failure
    logger.LogError("Network error: {Message}", ex.Message);
    // Retry with backoff
}
```

### Pattern 4: Catch-All for Automation Scripts

```csharp
try
{
    // Automation sequence
    steam.LogIn(account);
    steam.ActivateKey(key);
    steam.FavouriteWorkshopItem(id);
}
catch (KeyActivationException ex)
{
    // Handle specifically
}
catch (AuthenticationException ex)
{
    // Handle specifically
}
catch (Exception ex) when (ex is NoSuchElementException || ex is TimeoutException || ex is WebDriverException)
{
    // Infrastructure failure - likely transient or UI change
    logger.LogError(ex, "Browser automation failed");
    // Screenshot, retry, alert
}
catch (Exception ex)
{
    // Unexpected - log and alert
    logger.LogError(ex, "Unexpected error in Steam automation");
    throw;
}
```

---

## Error Code Reference

### KeyActivationErrorCode

| Code | Value | Category | Retryable | Action |
|------|-------|----------|-----------|--------|
| `Unexpected` | 0 | Unknown | No | Log, investigate |
| `InvalidProductKey` | 1 | Client | No | Verify key |
| `AlreadyActivatedDifferentAccount` | 2 | Conflict | No | Key consumed |
| `AlreadyActivatedCurrentAccount` | 3 | Conflict | No | Already owned |
| `BaseProductRequired` | 4 | Precondition | No | Buy base game |
| `RegionLocked` | 5 | Policy | No | Region mismatch |
| `TooManyAttempts` | 6 | Rate limit | Yes (delay) | Wait, retry |

---

## Failure Classification

### Transient Failures (Retryable)

| Failure | Exception | Retry Strategy |
|---------|-----------|----------------|
| Network timeout | `TimeoutException`, `WebException` | Exponential backoff, max 3 retries |
| Rate limited | `KeyActivationException` (TooManyAttempts) | Wait 5+ minutes, retry |
| Element not ready | `TimeoutException` (wait) | Increase wait, retry once |
| TOTP code rejected | `AuthenticationException` (Steam Guard) | Auto-retried internally (10s wait) |

### Permanent Failures (Non-Retryable)

| Failure | Exception | Resolution |
|---------|-----------|------------|
| Invalid credentials | `AuthenticationException` | Fix credentials |
| Captcha required | `AuthenticationException` | Manual intervention |
| Invalid key format | `KeyActivationException` (InvalidProductKey) | Verify key |
| Key already used | `KeyActivationException` (AlreadyActivated*) | Key consumed |
| Region locked | `KeyActivationException` (RegionLocked) | Different key/region |
| Base product missing | `KeyActivationException` (BaseProductRequired) | Purchase base game |
| Selector broken | `NoSuchElementException` | Library update needed |

### Infrastructure Failures

| Failure | Exception | Resolution |
|---------|-----------|------------|
| Browser crash | `WebDriverException` | Restart browser/driver |
| Driver version mismatch | `WebDriverException` | Update ChromeDriver |
| No browser installed | `WebDriverException` | Install browser |
| System clock wrong | `AuthenticationException` (Steam Guard) | Sync NTP |

---

## Logging Guidelines

### What to Log

| Event | Level | Fields |
|-------|-------|--------|
| Operation start | Debug | Operation, parameters (no secrets) |
| Operation success | Info | Operation, result (product name) |
| Typed exception | Warning | Exception type, code, message |
| Infrastructure exception | Error | Exception, stack trace, context |
| Retry attempt | Info | Attempt number, delay |
| Manual intervention needed | Error | Operation, reason |

### What NOT to Log

| Data | Reason |
|------|--------|
| `SteamAccount.Password` | Secret |
| `SteamAccount.TotpKey` | Secret |
| `SteamAccount.Username` | PII (use hash/redacted) |
| Session cookies | Secret |
| TOTP codes | Secret |

### Example Structured Logging

```csharp
logger.LogInformation("Starting Steam automation for user {UserHash}", HashUsername(account.Username));

try
{
    steam.LogIn(account);
    logger.LogInformation("Authentication successful");

    string product = steam.ActivateKey(key);
    logger.LogInformation("Key activated: {Product}", product);
}
catch (KeyActivationException ex)
{
    logger.LogWarning(ex, "Key activation failed: {Code} - {Message}", ex.Code, ex.Message);
}
catch (AuthenticationException ex)
{
    logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error in Steam automation");
}
```

---

## Recovery Strategies

### Automatic Recovery (Library Internal)

| Scenario | Recovery |
|----------|----------|
| TOTP code already used | Wait 10s, generate new code (internal loop) |
| Cookie consent prompt | Auto-click reject (in LogIn) |
| Already on target page | Skip navigation (ActivateKey) |
| Already favourited/subscribed | Early return (Workshop) |

### Manual Recovery (Caller Responsibility)

| Scenario | Recovery |
|----------|----------|
| Rate limited (TooManyAttempts) | Wait 5+ minutes, retry |
| Network timeout | Retry with backoff |
| Selector broken | Update library / report issue |
| Captcha | Manual browser intervention |
| Browser crash | Dispose WebProcessor, create new |

---

## Testing Error Handling

### Unit Test Patterns

```csharp
[Test]
public void ActivateKey_InvalidKey_ThrowsKeyActivationException()
{
    // Arrange: mock IWebProcessor to return error display with "Invalid product code"
    // Act & Assert
    Assert.Throws<KeyActivationException>(() => processor.ActivateKey("INVALID"));
}

[Test]
public void ActivateKey_TooManyAttempts_ThrowsWithCorrectCode()
{
    // Arrange: mock error display with "too many recent activation attempts"
    // Act & Assert
    var ex = Assert.Throws<KeyActivationException>(() => processor.ActivateKey("KEY"));
    Assert.AreEqual(KeyActivationErrorCode.TooManyAttempts, ex.Code);
}
```

### Integration Test Scenarios

| Scenario | Expected Exception | Verification |
|----------|-------------------|--------------|
| Valid key | None | Product name returned |
| Invalid key | `KeyActivationException` (InvalidProductKey) | Code matches |
| Already owned | `KeyActivationException` (AlreadyActivatedCurrentAccount) | Code matches |
| Rate limited | `KeyActivationException` (TooManyAttempts) | Code matches |
| Wrong credentials | `AuthenticationException` | Message indicates auth failure |
| Captcha | `AuthenticationException` | Message contains "Captcha" |
| Network down | `WebException` / `TimeoutException` | Exception type |

---

## Related Documentation

- [API Reference](../api-reference/steam-processor.md) — Method exceptions
- [Data Model](../data-model.md) — Exception types
- [Authentication Flow](../flows/authentication.md) — Auth error details
- [Key Activation Flow](../flows/key-activation.md) — Key error details
- [Security](../security.md) — Secret handling in errors