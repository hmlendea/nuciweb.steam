# Security

## Overview

This document describes the security model, threat mitigations, and secure usage patterns for NuciWeb.Steam.

## Threat Model

### Assets

| Asset | Sensitivity | Exposure |
|-------|-------------|----------|
| Steam credentials (username/password) | Critical | Input only, never stored |
| Steam Guard TOTP shared secret | Critical | Input only, never stored |
| Session cookies (authentication) | High | Managed by `IWebProcessor` |
| Activated product keys | Medium | Transient in memory |
| Profile data (name, avatar) | Low | User-controlled public data |

### Threat Actors

| Actor | Capability | Mitigation |
|-------|------------|------------|
| Malicious caller | Controls `SteamAccount` input | No persistence, no logging |
| Compromised `IWebProcessor` | Access to browser session | Consumer controls implementation |
| Network attacker | MITM on Steam traffic | HTTPS enforced via `SteamUrls` |
| Malicious library update | Supply chain | Signed packages, verified sources |
| Local attacker | Memory scraping | Short-lived secrets, no persistence |

---

## Security Controls

### 1. Secret Handling

**Credentials (`SteamAccount`):**
- Passed as method parameter to `LogIn`
- Not stored in any field or property
- Not logged, serialized, or exposed
- Eligible for GC after `LogIn` returns

**TOTP Shared Secret (`TotpKey`):**
- Passed to `ISteamGuard.GenerateAuthenticationCode`
- Not retained after code generation
- Used only during Steam Guard challenge
- Not logged or exposed

**Session Cookies:**
- Managed entirely by `IWebProcessor` (Selenium/WebDriver)
- Library never accesses cookie values directly
- Cleared when `IWebProcessor.Dispose()` called

### 2. Transport Security

**All Steam URLs use HTTPS:**
```csharp
// SteamUrls.cs - all constants use https://
public static string Store => "https://store.steampowered.com";
public static string Community => "https://steamcommunity.com";
```

**No HTTP endpoints used** — eliminates downgrade attacks.

### 3. Input Validation

| Input | Validation | Failure |
|-------|------------|---------|
| `SteamAccount` | Not null | `ArgumentNullException` |
| `Username`/`Password` | Not null/empty (via `SetText`) | `ArgumentNullException` |
| `TotpKey` | Not null/empty when needed | `ArgumentNullException` |
| `key` (ActivateKey) | Not null | `ArgumentNullException` |
| `workshopItemId` | Not null | `ArgumentNullException` |
| `name`/`identifier`/`imagePath` | Not null | `ArgumentNullException` |

**No client-side validation of:**
- Key format (delegated to Steam)
- Profile name/identifier format (delegated to Steam)
- Image format/size (delegated to Steam)
- TOTP key format (delegated to `ISteamGuard`)

### 4. Output Sanitisation

**Exceptions:**
- `KeyActivationException.Message` contains Steam error text (may include product name)
- `AuthenticationException.Message` contains Steam error text
- No credentials, TOTP codes, or cookies in exception messages

**Return Values:**
- `ActivateKey` returns product name only (public information)
- No other methods return sensitive data

### 5. No Telemetry

- No usage analytics
- No crash reporting
- No performance metrics
- No feature usage tracking
- No phone-home behaviour

---

## Secure Usage Patterns

### Pattern 1: Short-Lived Credentials

```csharp
// Good: Credentials created, used, discarded
var account = new SteamAccount
{
    Username = GetUsernameFromSecureStore(),
    Password = GetPasswordFromSecureStore(),
    TotpKey = GetTotpKeyFromSecureStore()
};

steam.LogIn(account);
// account eligible for GC
```

### Pattern 2: Secure Store Integration

```csharp
// Use platform secure storage (DPAPI, Keychain, libsecret)
var account = new SteamAccount
{
    Username = await secureStore.GetAsync("steam.username"),
    Password = await secureStore.GetAsync("steam.password"),
    TotpKey = await secureStore.GetAsync("steam.totpkey")
};
```

### Pattern 3: Dispose Browser Session

```csharp
// Always dispose to clear cookies
using var webProcessor = new NuciWeb.WebProcessor();
var steam = new SteamProcessor(webProcessor);

try
{
    steam.LogIn(account);
    // ... operations
}
finally
{
    webProcessor.Dispose(); // Clears session cookies
}
```

### Pattern 4: Mock for Testing (No Real Credentials)

```csharp
// Unit tests never use real credentials
var mockWeb = new Mock<IWebProcessor>();
var mockGuard = new Mock<ISteamGuard>();
mockGuard.Setup(g => g.GenerateAuthenticationCode(It.IsAny<string>())).Returns("ABCDE");

var processor = new SteamProcessor(mockWeb.Object);
// Test logic without real Steam account
```

---

## Insecure Patterns to Avoid

### ❌ Don't: Log Credentials

```csharp
// BAD
logger.LogInformation("Logging in as {Username} with password {Password}", account.Username, account.Password);

// GOOD
logger.LogInformation("Logging in as {UserHash}", Hash(account.Username));
```

### ❌ Don't: Persist Credentials

```csharp
// BAD
File.WriteAllText("account.json", JsonSerializer.Serialize(account));

// GOOD: Never persist
```

### ❌ Don't: Reuse SteamAccount

```csharp
// BAD: Long-lived instance
private readonly SteamAccount _account = new SteamAccount { ... };

// GOOD: Create per operation
var account = new SteamAccount { ... };
steam.LogIn(account);
```

### ❌ Don't: Share IWebProcessor Across Security Boundaries

```csharp
// BAD: Same browser session for different users
var webProcessor = new WebProcessor();
var steam1 = new SteamProcessor(webProcessor);
var steam2 = new SteamProcessor(webProcessor); // Shares cookies!

// GOOD: Separate browser per user
using var web1 = new WebProcessor();
using var web2 = new WebProcessor();
```

---

## Supply Chain Security

### Package Verification

| Package | Source | Verification |
|---------|--------|--------------|
| NuciWeb | NuGet.org | Signed, verified |
| NuciWeb.Automation | NuGet.org | Signed, verified |
| NuciExtensions | NuGet.org | Signed, verified |
| SteamGuard.TOTP | NuGet.org | Signed, verified |

### Build Integrity

- Build on trusted CI/CD
- Verify package hashes
- Use `dotnet nuget verify` for packages
- Pin dependency versions in `csproj`

---

## Compliance

### GDPR

- No personal data collected by library
- Consumer controls all data processing
- `SteamAccount` contains PII — consumer responsibility
- No automated decision-making

### SOC 2 / ISO 27001

- Library itself has no controls
- Consumer must implement:
  - Access control (who can call automation)
  - Audit logging (what operations performed)
  - Encryption (credentials at rest/in transit)
  - Incident response (credential compromise)

---

## Vulnerability Reporting

Report security issues privately:

1. Email: security@nuciweb.example (example)
2. GitHub Security Advisory (private)
3. Do not file public issues for vulnerabilities

---

## Security Checklist for Consumers

- [ ] Credentials sourced from secure store (not config files)
- [ ] `SteamAccount` instances short-lived
- [ ] No logging of secrets or PII
- [ ] `IWebProcessor` disposed after use
- [ ] Separate browser session per user account
- [ ] System clock synced (NTP) for TOTP
- [ ] Dependencies updated regularly
- [ ] Package signatures verified
- [ ] CI/CD builds reproducible
- [ ] Incident response plan for credential compromise

---

## Related Documentation

- [Data Model](../data-model.md) — Secret classification
- [Configuration](../configuration.md) — Secure injection patterns
- [Logging](../logging.md) — What not to log
- [Error Handling](../error-handling.md) — Exception sanitisation
- [Dependencies](../dependencies.md) — Supply chain