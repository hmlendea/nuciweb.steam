# Concurrency and Scheduling

## Overview

This document describes the concurrency model, threading guarantees, and scheduling behaviour of NuciWeb.Steam.

## Concurrency Model

### Single-Threaded by Design

**NuciWeb.Steam is not thread-safe.**

| Aspect | Guarantee |
|--------|-----------|
| `SteamProcessor` instance | Single-threaded use only |
| `IWebProcessor` instance | Single-threaded use only |
| Internal processors | Not thread-safe |
| Public API methods | Synchronous, blocking |

### No Internal Synchronization

- No `lock`, `Monitor`, `SemaphoreSlim`, or concurrent collections
- No `async`/`await` — no thread pool usage
- No background tasks or timers
- All operations execute on caller's thread

---

## Thread Safety Rules

### Consumer Responsibilities

| Rule | Enforcement |
|------|-------------|
| One thread per `SteamProcessor` | Consumer discipline |
| No concurrent `LogIn` calls | Consumer discipline |
| No concurrent operation calls | Consumer discipline |
| Dispose `IWebProcessor` from same thread | Consumer discipline |

### Violation Consequences

| Violation | Symptom |
|-----------|---------|
| Concurrent `NavigateTo` | `WebDriverException`, undefined navigation |
| Concurrent `FindElement` | Stale element, wrong element, exception |
| Concurrent `Click` | Click interception, wrong target |
| Shared `IWebProcessor` across threads | Cookie corruption, session mixing |

---

## Scheduling Behaviour

### Operation Execution

```
Caller Thread
    │
    ▼ steamProcessor.LogIn(account)
┌─────────────────────────────────────┐
│ SteamProcessor.LogIn                │
│   └─► SteamAuthenticationProcessor  │
│       └─► IWebProcessor.NavigateTo  │ (blocks)
│       └─► IWebProcessor.FindElement │ (blocks)
│       └─► IWebProcessor.SendKeys    │ (blocks)
│       └─► ...                       │
└─────────────────────────────────────┘
    │
    ▼ Returns (success or exception)
Caller Thread continues
```

### Blocking Characteristics

| Operation | Typical Duration | Blocks Caller |
|-----------|------------------|---------------|
| `LogIn` (with TOTP) | 10-30 seconds | Yes |
| `LogIn` (no 2FA) | 5-15 seconds | Yes |
| `ActivateKey` | 3-10 seconds | Yes |
| `SetProfileName` | 3-8 seconds | Yes |
| `SetProfilePicture` | 5-15 seconds | Yes |
| `FavouriteWorkshopItem` | 2-5 seconds | Yes |
| `SubscribeToWorkshopItem` | 2-5 seconds | Yes |
| `VisitChat` | 2-5 seconds | Yes |

### No Timeouts

- Library does not impose operation timeouts
- `IWebProcessor` may have internal timeouts (NuciWeb config)
- Consumer should wrap in `CancellationToken` if needed

---

## Parallelism Patterns (Consumer-Side)

### Pattern 1: Sequential (Default)

```csharp
using var web = new WebProcessor();
var steam = new SteamProcessor(web);
steam.LogIn(account);
steam.ActivateKey(key1);
steam.ActivateKey(key2);
steam.SetProfileName("Name");
```

### Pattern 2: Multiple Independent Processors

```csharp
// Each has own browser session
var tasks = accounts.Select(async account =>
{
    using var web = new WebProcessor();
    var steam = new SteamProcessor(web);
    steam.LogIn(account);
    steam.ActivateKey(key);
    return steam.ActivateKey(key);
});
await Task.WhenAll(tasks);
```

### Pattern 3: Producer-Consumer with Queue

```csharp
var queue = new ConcurrentQueue<string>(keys);
var processors = Enumerable.Range(0, 3).Select(_ =>
{
    var web = new WebProcessor();
    var steam = new SteamProcessor(web);
    steam.LogIn(account);
    return steam;
}).ToList();

var workers = processors.Select(p => Task.Run(() =>
{
    while (queue.TryDequeue(out var key))
    {
        try { p.ActivateKey(key); }
        catch { /* log, requeue */ }
    }
}));

Task.WaitAll(workers.ToArray());
```

---

## Scheduling Constraints

### Steam Rate Limits

| Operation | Limit | Consequence |
|-----------|-------|-------------|
| Login attempts | ~5/hour per IP | Captcha, temporary block |
| Key activation | ~10/minute | `TooManyAttempts` error |
| Profile changes | ~5/minute | Temporary block |
| Workshop actions | ~20/minute | Temporary block |

### Recommended Delays

```csharp
// Between key activations
await Task.Delay(TimeSpan.FromSeconds(10));

// Between profile updates
await Task.Delay(TimeSpan.FromSeconds(30));

// After login before operations
await Task.Delay(TimeSpan.FromSeconds(5));
```

---

## Cancellation

### No Built-In Cancellation

- Library has no `CancellationToken` parameters
- Consumer wraps calls:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var task = Task.Run(() => steamProcessor.ActivateKey(key), cts.Token);
await task;
```

### Cooperative Cancellation

- Only works if `IWebProcessor` respects cancellation
- NuciWeb may not support mid-operation cancellation
- Best effort: dispose `IWebProcessor` to abort

---

## Resource Management

### IWebProcessor Disposal

```csharp
using var web = new WebProcessor();
var steam = new SteamProcessor(web);
// ... operations
// web.Dispose() called automatically
// Closes browser, clears cookies, releases WebDriver
```

### No Finalizers

- `SteamProcessor` does not implement `IDisposable`
- No finalizer
- Consumer must dispose `IWebProcessor`

---

## Testing Concurrency

### Not Tested

- No unit tests for concurrent access
- No stress tests
- Manual verification only

### Known Safe Patterns

- Single `SteamProcessor` per thread ✓
- Multiple `SteamProcessor` each with own `IWebProcessor` ✓
- Sequential operations on single instance ✓

### Known Unsafe Patterns

- Shared `SteamProcessor` across threads ✗
- Shared `IWebProcessor` across threads ✗
- Concurrent operations on same instance ✗

---

## Related Documentation

- [Architecture](../architecture.md) — Single-threaded design
- [Design Decisions](../design-decisions.md) — Synchronous API rationale
- [State and Persistence](../state-and-persistence.md) — Session ownership
- [Integrations](../integrations.md) — IWebProcessor threading