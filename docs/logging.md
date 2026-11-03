# Logging

## Overview

NuciWeb.Steam **does not implement any internal logging**. The library emits no diagnostic events, metrics, traces, or log output of any kind. All observability is the responsibility of the consumer.

## Library Behaviour

| Aspect | Behaviour |
|--------|-----------|
| `ILogger` / `ILoggerFactory` | Not used |
| `System.Diagnostics.Trace` | Not used |
| `Console.WriteLine` / `Debug.WriteLine` | Not used |
| EventSource / ETW | Not used |
| Custom logging abstraction | Not used |
| Log levels | N/A |
| Structured logging | N/A |
| Correlation IDs | N/A |
| Sampling | N/A |

## Consumer Responsibility

Consumers **must** implement their own logging around library calls:

```csharp
using Microsoft.Extensions.Logging;

var logger = loggerFactory.CreateLogger<SteamAutomation>();

logger.LogInformation("Starting Steam automation for user {UserHash}", Hash(account.Username));

try
{
    steam.LogIn(account);
    logger.LogInformation("Authentication successful");

    string product = steam.ActivateKey(key);
    logger.LogInformation("Activated product: {Product}", product);
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
    logger.LogError(ex, "Unexpected error");
    throw;
}
finally
{
    webProcessor.Dispose();
}
```

## Required Logging Points

| Operation | Success Log | Failure Log |
|-----------|-------------|-------------|
| `LogIn` | "Authentication successful" | Exception details (no secrets) |
| `ActivateKey` | "Activated: {productName}" | `KeyActivationException.Code`, `Message` |
| `SetProfileName` | "Profile name updated" | Exception |
| `SetProfileIdentifier` | "Profile identifier updated" | Exception |
| `SetProfilePicture` | "Avatar updated" | Exception |
| `FavouriteWorkshopItem` | "Favourited: {id}" | Exception |
| `SubscribeToWorkshopItem` | "Subscribed: {id}" | Exception |
| `VisitChat` | "Navigated to chat" | Exception |
| `AcceptCookies` / `RejectCookies` | "Cookie preferences updated" | Exception |

## What to Log (Structured Fields)

| Field | Type | Example |
|-------|------|---------|
| `Operation` | string | "ActivateKey" |
| `UserHash` | string | SHA256(username)[:8] |
| `WorkshopItemId` | string | "123456789" |
| `KeyActivationCode` | enum | `TooManyAttempts` |
| `DurationMs` | long | 12500 |
| `Success` | bool | true/false |

## What NOT to Log

| Data | Reason |
|------|--------|
| `SteamAccount.Password` | Secret |
| `SteamAccount.TotpKey` | Secret |
| `SteamAccount.Username` (raw) | PII — use hash |
| TOTP codes | Secret |
| Session cookies | Secret |
| Full exception stack traces in production | Info leak (use Debug level) |

## Recommended Logging Framework

| Framework | Package | Notes |
|-----------|---------|-------|
| Microsoft.Extensions.Logging | `Microsoft.Extensions.Logging` | Standard, DI-friendly |
| Serilog | `Serilog` + `Serilog.Extensions.Logging` | Structured, sinks |
| NLog | `NLog` + `NLog.Extensions.Logging` | High performance |

## Log Levels Guidance

| Level | Use For |
|-------|---------|
| `Trace` | Selector interactions, element waits (very verbose) |
| `Debug` | Navigation URLs, form submissions, wait durations |
| `Information` | Operation start/end, key results (product name) |
| `Warning` | Typed exceptions (`KeyActivationException`, `AuthenticationException`) |
| `Error` | Infrastructure exceptions (`WebDriverException`, `TimeoutException`) |
| `Critical` | Browser crash, unrecoverable state |

## Correlation

For multi-operation workflows, pass a correlation ID:

```csharp
using var scope = logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = Guid.NewGuid(),
    ["UserHash"] = Hash(account.Username)
});

steam.LogIn(account);
steam.ActivateKey(key);
// All logs in scope include CorrelationId
```

## Integration with NuciWeb

If `NuciWeb.WebProcessor` supports logging (check version), configure separately:

```csharp
var webProcessor = new NuciWeb.WebProcessor(options);
// NuciWeb may have its own logging - configure per its docs
```

## Testing Logging

Verify logging output in tests:

```csharp
[Test]
public void ActivateKey_LogsSuccess()
{
    var logger = new FakeLogger();
    var automation = new SteamAutomation(logger);

    automation.ActivateKey("VALID-KEY");

    Assert.Contains(logger.Logs, l => l.Level == LogLevel.Information
        && l.Message.Contains("Activated"));
}
```

## Related Documentation

- [Error Handling](../error-handling.md) — Exception details to log
- [Security](../security.md) — Secret handling in logs
- [API Reference](../api-reference/steam-processor.md) — Operations to log
- [Quick Start](../quick-start.md) — Example with logging