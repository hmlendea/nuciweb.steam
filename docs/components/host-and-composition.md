# Host and Composition — SteamProcessor Construction and Lifetime

## Overview

This document details how `SteamProcessor` is constructed, how it composes internal processors, and the lifetime management of the object graph.

## Construction

### Constructor Overloads

```csharp
// Primary constructor with dependency injection
public SteamProcessor(IWebProcessor webProcessor)
{
    this.webProcessor = webProcessor;
    authenticationProcessor = new SteamAuthenticationProcessor(webProcessor);
    chatProcessor = new SteamChatProcessor(webProcessor);
    keyProcessor = new SteamKeyProcessor(webProcessor);
    profileProcessor = new SteamProfileProcessor(webProcessor);
    workshopProcessor = new SteamWorkshopProcessor(webProcessor);
}

// Parameterless constructor (creates default IWebProcessor)
public SteamProcessor() : this(CreateDefaultWebProcessor()) { }

static IWebProcessor CreateDefaultWebProcessor()
    => new NuciWeb.WebProcessor(); // Implementation detail
```

### Dependency Graph

```
SteamProcessor
├── IWebProcessor (injected or default)
├── SteamAuthenticationProcessor
│   ├── IWebProcessor (shared)
│   └── ISteamGuard (default: SteamGuard.TOTP.SteamGuard)
├── SteamChatProcessor
│   └── IWebProcessor (shared)
├── SteamKeyProcessor
│   └── IWebProcessor (shared)
├── SteamProfileProcessor
│   └── IWebProcessor (shared)
└── SteamWorkshopProcessor
    └── IWebProcessor (shared)
```

All internal processors share the same `IWebProcessor` instance, ensuring they operate on the same browser session.

## Composition Details

### Processor Instantiation

All five internal processors are instantiated eagerly in the `SteamProcessor` constructor:

```csharp
readonly SteamAuthenticationProcessor authenticationProcessor = new(webProcessor);
readonly SteamChatProcessor chatProcessor = new(webProcessor);
readonly SteamKeyProcessor keyProcessor = new(webProcessor);
readonly SteamProfileProcessor profileProcessor = new(webProcessor);
readonly SteamWorkshopProcessor workshopProcessor = new(webProcessor);
```

**Rationale:** Eager instantiation ensures:
- Fail-fast if `IWebProcessor` is misconfigured
- Consistent state across operations
- No lazy initialization overhead

### Delegation Pattern

Each public method delegates to the appropriate processor:

```csharp
public void LogIn(SteamAccount account)
    => authenticationProcessor.LogIn(account);

public void SetProfileName(string name)
    => profileProcessor.SetName(name);

// ... etc
```

**Benefits:**
- Single responsibility per processor
- Clear delegation chain
- Easy to trace execution flow
- Processors can be tested independently

## Lifetime Management

### SteamProcessor Lifetime

| Phase | Description |
|-------|-------------|
| **Construction** | Creates all internal processors, shares `IWebProcessor` |
| **Operation** | Delegates calls; maintains no operation state itself |
| **Reuse** | Can be reused for multiple operations sequentially |
| **Disposal** | No `IDisposable`; delegates to `IWebProcessor.Dispose()` |

### Internal Processor Lifetime

| Processor | Lifetime | State |
|-----------|----------|-------|
| `SteamAuthenticationProcessor` | Same as `SteamProcessor` | `UsedSteamGuardCodes` list (cleared per `LogIn` call) |
| `SteamChatProcessor` | Same as `SteamProcessor` | None |
| `SteamKeyProcessor` | Same as `SteamProcessor` | None |
| `SteamProfileProcessor` | Same as `SteamProcessor` | None |
| `SteamWorkshopProcessor` | Same as `SteamProcessor` | None |

**Key Point:** Only `SteamAuthenticationProcessor` maintains mutable state (`UsedSteamGuardCodes`), and it's scoped to a single `LogIn` call.

### IWebProcessor Lifetime

- **Owned by:** Consumer (if injected) or `SteamProcessor` (if default created)
- **Shared by:** All internal processors
- **Manages:** Browser session, cookies, WebDriver connection
- **Disposal:** Consumer must call `webProcessor.Dispose()` when done

```csharp
using var webProcessor = new NuciWeb.WebProcessor();
var processor = new SteamProcessor(webProcessor);
processor.LogIn(account);
// ... more operations
webProcessor.Dispose(); // Closes browser
```

## Thread Safety

**Not thread-safe.** The design assumes single-threaded use:

- `SteamAuthenticationProcessor.UsedSteamGuardCodes` is not synchronised
- Shared `IWebProcessor` is not thread-safe (browser automation)
- No locks or synchronisation primitives used

**For concurrent operations:** Create separate `SteamProcessor` instances with separate `IWebProcessor` instances (separate browser sessions).

## Testing Considerations

### Mocking IWebProcessor

```csharp
var mockWebProcessor = new Mock<IWebProcessor>();
var processor = new SteamProcessor(mockWebProcessor.Object);
// Test delegation, verify calls
```

### Mocking ISteamGuard

```csharp
var mockSteamGuard = new Mock<ISteamGuard>();
mockSteamGuard.Setup(g => g.GenerateAuthenticationCode("secret"))
    .Returns("ABCDE");
var authProcessor = new SteamAuthenticationProcessor(mockWebProcessor.Object, mockSteamGuard.Object);
```

### Integration Testing

Requires real `IWebProcessor` (NuciWeb + WebDriver) and Steam credentials. No automated integration tests in repository.

## Configuration

No configuration object — dependencies injected via constructor:

| Dependency | Source | Override |
|------------|--------|----------|
| `IWebProcessor` | Constructor parameter | Yes (inject custom) |
| `ISteamGuard` | `SteamAuthenticationProcessor` constructor | Yes (inject custom) |
| `SteamUrls` | Static class | No (compile-time constants) |

## Related Documentation

- [Presentation](./presentation.md) — Public API facade
- [Application Services](./application-services.md) — Internal processor implementations
- [Architecture](./architecture.md) — Overall architectural context
- [Concurrency and Scheduling](../concurrency-and-scheduling.md) — Threading model