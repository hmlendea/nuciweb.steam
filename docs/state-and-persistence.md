# State and Persistence

## Overview

This document describes how NuciWeb.Steam manages state and what persists across operations.

## In-Memory State

### SteamProcessor Instance

| State | Lifetime | Scope |
|-------|----------|-------|
| `IWebProcessor` reference | Instance | All processors |
| Internal processor instances | Instance | `SteamProcessor` |
| `UsedSteamGuardCodes` list | Per-login | `SteamAuthenticationProcessor` |

### SteamAuthenticationProcessor

```csharp
private readonly List<string> _usedSteamGuardCodes = new();
```

- **Purpose:** Prevent TOTP code reuse during login retries
- **Lifetime:** Cleared on successful login, persists across failed attempts
- **Scope:** Single `LogIn` call (multiple internal attempts)

### No Persistent State

**NuciWeb.Steam does not persist:**
- Credentials (`SteamAccount` passed per call)
- Session cookies (held by `IWebProcessor`, consumer-owned)
- Configuration (injected via constructor)
- Operation history
- Cache

---

## Browser Session Persistence

### IWebProcessor Ownership

```
Consumer → creates IWebProcessor
         → injects into SteamProcessor
         → controls lifecycle (Dispose)
         → decides persistence strategy
```

### Persistence Options

| Strategy | Implementation | Use Case |
|----------|----------------|----------|
| Ephemeral | New `WebProcessor` per run | CI, testing, one-off scripts |
| Profile directory | `WebProcessor` with user data dir | Persistent login, avoid 2FA |
| Cookie export/import | Consumer serializes cookies | Distributed, containerized |

### Cookie Sharing

- Single `IWebProcessor` = single cookie jar
- Store (`store.steampowered.com`) and Community (`steamcommunity.com`) share session
- Login on one domain authenticates the other
- Consumer controls cookie persistence via `IWebProcessor` implementation

---

## SteamAccount Model

```csharp
public class SteamAccount
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TotpKey { get; set; } // Base32 shared secret
}
```

- **Transient:** Created per `LogIn` call
- **No persistence:** Library never stores `SteamAccount`
- **Consumer responsibility:** Secure storage, retrieval, disposal

---

## TOTP Shared Secret

### Format

- Base32 encoded (RFC 4648)
- Example: `ABCDEF23GHIJK456`
- Case-insensitive, no padding required

### Storage

| Location | Managed By |
|----------|------------|
| Consumer config | Consumer |
| Environment variable | Consumer |
| Secret manager | Consumer |
| **Not in library** | — |

### Usage

```csharp
var account = new SteamAccount
{
    Username = "user",
    Password = "pass",
    TotpKey = config["Steam:TotpKey"] // Consumer provides
};
```

---

## Operation State Transitions

### Login Flow

```
Unauthenticated
    │
    ▼ LogIn(account)
┌─────────┐
│ Store   │ → Navigate, enter credentials
│ Login   │
└─────────┘
    │
    ▼ 2FA required?
┌─────────┐
│ TOTP    │ → Generate code, submit
│ Entry   │
└─────────┘
    │
    ▼ Success?
┌─────────┐
│ Community│ → Navigate, validate session
│ Login   │
└─────────┘
    │
    ▼
Authenticated ──→ Ready for operations
```

### Authenticated Operations

```
Authenticated
    │
    ▼ ActivateKey / SetProfileName / etc.
┌─────────────┐
│ Navigate to │ → Target page
│ target URL  │
└─────────────┘
    │
    ▼
┌─────────────┐
│ Interact    │ → Click, input, wait
│ with page   │
└─────────────┘
    │
    ▼
┌─────────────┐
│ Validate    │ → Check success/error
│ result      │
└─────────────┘
    │
    ▼
Authenticated (unchanged)
```

---

## No Database, No Files

| Storage Type | Used? | Details |
|--------------|-------|---------|
| SQLite | No | — |
| JSON/XML files | No | — |
| Registry | No | — |
| Environment | No | Consumer only |
| Memory | Yes | Transient only |

---

## Consumer State Management Patterns

### Pattern 1: Per-Operation Processor

```csharp
foreach (var key in keys)
{
    using var web = new WebProcessor();
    var steam = new SteamProcessor(web);
    steam.LogIn(account);
    steam.ActivateKey(key);
} // web.Dispose() clears session
```

### Pattern 2: Reused Session

```csharp
using var web = new WebProcessor();
var steam = new SteamProcessor(web);
steam.LogIn(account);

foreach (var key in keys)
{
    steam.ActivateKey(key);
}
// Session persists, cookies saved if WebProcessor configured
```

### Pattern 3: Persistent Profile

```csharp
var options = new WebProcessorOptions
{
    UserDataDir = "/path/to/chrome/profile"
};
using var web = new WebProcessor(options);
var steam = new SteamProcessor(web);
// First run: LogIn, 2FA
// Subsequent runs: Already logged in
```

---

## Related Documentation

- [Architecture](../architecture.md) — Component ownership
- [Design Decisions](../design-decisions.md) — Consumer controls browser
- [Integrations](../integrations.md) — IWebProcessor integration
- [Configuration](../configuration.md) — Injection patterns