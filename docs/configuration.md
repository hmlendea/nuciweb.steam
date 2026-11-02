# Configuration

## Overview

NuciWeb.Steam has minimal configuration — primarily through constructor injection and environment setup. No configuration files, environment variables, or settings classes are used.

## Configuration Points

### 1. IWebProcessor Injection (Primary)

**Location:** `SteamProcessor` constructor

```csharp
public SteamProcessor(IWebProcessor webProcessor)
```

**Options:**
| Implementation | Description | Use Case |
|----------------|-------------|----------|
| `new NuciWeb.WebProcessor()` | Default, uses ChromeDriver | Production, default |
| `new NuciWeb.WebProcessor(options)` | Configured WebProcessor | Custom browser, headless, proxy |
| Custom `IWebProcessor` | Test double, wrapper | Testing, mocking, logging |

**NuciWeb.WebProcessor Configuration:**
```csharp
var options = new NuciWeb.WebProcessorOptions
{
    Browser = BrowserType.Chrome, // or Firefox, Edge
    Headless = false,
    Arguments = new[] { "--disable-gpu", "--no-sandbox" },
    // Proxy, user data dir, etc.
};
var webProcessor = new NuciWeb.WebProcessor(options);
```

### 2. ISteamGuard Injection (Secondary)

**Location:** `SteamAuthenticationProcessor` constructor overload

```csharp
public SteamAuthenticationProcessor(IWebProcessor webProcessor, ISteamGuard steamGuard)
```

**Options:**
| Implementation | Description | Use Case |
|----------------|-------------|----------|
| `new SteamGuard.TOTP.SteamGuard()` | Default RFC 6238 TOTP | Production |
| Custom `ISteamGuard` | Mock, fixed codes, logging | Testing, deterministic TOTP |

**Default:** If not provided, `SteamAuthenticationProcessor` creates `new SteamGuard.TOTP.SteamGuard()` internally.

### 3. Browser / WebDriver (Environment)

**Not a library configuration** — but required for operation:

| Component | Requirement | Setup |
|-----------|-------------|-------|
| ChromeDriver | Matches Chrome version | `PATH` or NuciWeb config |
| GeckoDriver | Matches Firefox version | `PATH` or NuciWeb config |
| msedgedriver | Matches Edge version | `PATH` or NuciWeb config |
| Browser binary | Chrome/Firefox/Edge installed | System package manager |

### 4. System Time (Implicit)

**Critical for:** TOTP code generation (RFC 6238)

**Requirement:** System clock must be accurate (within ~30 seconds of UTC)

**Failure Mode:** Generated codes rejected by Steam → `AuthenticationException` ("Invalid Steam Guard code")

**Mitigation:** NTP sync, container time sync, VM time sync

---

## Configuration by Scenario

### Production (Default)

```csharp
var webProcessor = new NuciWeb.WebProcessor(); // Chrome, headed
var steam = new SteamProcessor(webProcessor);
```

### Headless CI/CD

```csharp
var options = new NuciWeb.WebProcessorOptions
{
    Browser = BrowserType.Chrome,
    Headless = true,
    Arguments = new[] { "--disable-gpu", "--no-sandbox", "--disable-dev-shm-usage" }
};
var webProcessor = new NuciWeb.WebProcessor(options);
var steam = new SteamProcessor(webProcessor);
```

### Custom Browser (Firefox)

```csharp
var options = new NuciWeb.WebProcessorOptions
{
    Browser = BrowserType.Firefox,
    Headless = false
};
var webProcessor = new NuciWeb.WebProcessor(options);
var steam = new SteamProcessor(webProcessor);
```

### Testing with Mocks

```csharp
var mockWeb = new Mock<IWebProcessor>();
var mockGuard = new Mock<ISteamGuard>();
mockGuard.Setup(g => g.GenerateAuthenticationCode(It.IsAny<string>())).Returns("ABCDE");

var authProcessor = new SteamAuthenticationProcessor(mockWeb.Object, mockGuard.Object);
// Or test SteamProcessor with mocked webProcessor
```

### Deterministic TOTP for Testing

```csharp
public class FixedSteamGuard : ISteamGuard
{
    private readonly string _fixedCode;
    public FixedSteamGuard(string code) => _fixedCode = code;
    public string GenerateAuthenticationCode(string sharedSecret) => _fixedCode;
}

// Usage
var steamGuard = new FixedSteamGuard("12345");
var authProcessor = new SteamAuthenticationProcessor(webProcessor, steamGuard);
```

---

## No Configuration Files

| File | Exists | Purpose |
|------|--------|---------|
| `appsettings.json` | No | Not used |
| `web.config` | No | Not used |
| `*.config` | No | Not used |
| Environment variables | No | Not read by library |
| Command line args | No | Not read by library |

---

## Runtime Requirements Summary

| Requirement | Version | Validation |
|-------------|---------|------------|
| .NET Runtime | 10.0 | `dotnet --version` |
| Chrome/Chromium | Latest stable | Browser auto-update |
| ChromeDriver | Matches Chrome | NuciWeb manages or PATH |
| System clock | NTP synced | `timedatectl status` |

---

## Related Documentation

- [Dependencies](../dependencies.md) — Package versions
- [Build and Deployment](../build-and-deployment.md) — CI/CD setup
- [Architecture](../architecture.md) — Constructor injection pattern
- [Components: Host and Composition](../components/host-and-composition.md) — Processor construction
- [Security](../security.md) — Secret handling in configuration