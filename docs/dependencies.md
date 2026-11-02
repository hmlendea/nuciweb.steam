# Dependencies

## Overview

This document describes all external dependencies of NuciWeb.Steam, their purposes, versions, and integration points.

## Runtime Dependencies

### NuciWeb (4.1.0)

**Package:** `NuciWeb`
**Version:** 4.1.0
**Purpose:** Browser automation abstraction layer (`IWebProcessor`)
**Integration:** Core dependency — all processors depend on `IWebProcessor`
**Types Used:**
- `NuciWeb.IWebProcessor` — Main abstraction
- `NuciWeb.WebProcessor` — Default implementation
- `NuciWeb.Select` — Selector factory (`ById`, `ByXPath`, `ByClass`)

**Version Constraints:** Compatible with 4.x; breaking changes in 5.x would require updates.

---

### NuciWeb.Automation (1.0.1)

**Package:** `NuciWeb.Automation`
**Version:** 1.0.1
**Purpose:** Selector helpers and element interaction extensions
**Integration:** Used by all processors for selector construction
**Types Used:**
- `NuciWeb.Automation.Select` — Extended selector factory

---

### NuciExtensions (6.0.2)

**Package:** `NuciExtensions`
**Version:** 6.0.2
**Purpose:** General-purpose extension methods
**Integration:** Used by processors for `ClickAny`, `WaitForAnyElementToBeVisible`
**Key Extensions:**
- `IWebProcessor.ClickAny(params Selector[])`
- `IWebProcessor.WaitForAnyElementToBeVisible(params Selector[])`
- `IWebProcessor.AreAllElementsVisible(params Selector[])`

---

### SteamGuard.TOTP (1.1.1)

**Package:** `SteamGuard.TOTP`
**Version:** 1.1.1
**Purpose:** Steam Guard TOTP code generation
**Integration:** Used by `SteamAuthenticationProcessor` via `ISteamGuard` interface
**Types Used:**
- `SteamGuard.TOTP.ISteamGuard` — Interface
- `SteamGuard.TOTP.SteamGuard` — Default implementation

**Algorithm:** RFC 6238 TOTP (SHA-1, 30-second windows, 5 digits)

---

## Development Dependencies

### .NET SDK

**Version:** 10.0 (preview)
**Purpose:** Build, test, pack
**Target Framework:** `net10.0`

---

### MSBuild / NuGet

**Purpose:** Package restore, build, pack
**Version:** Bundled with .NET SDK

---

## Dependency Graph

```
NuciWeb.Steam (net10.0)
├── NuciWeb (4.1.0)
│   ├── WebDriver (transitive)
│   └── Selenium (transitive)
├── NuciWeb.Automation (1.0.1)
│   └── NuciWeb (transitive)
├── NuciExtensions (6.0.2)
└── SteamGuard.TOTP (1.1.1)
```

## Transitive Dependencies (Key)

| Package | Purpose | Risk |
|---------|---------|------|
| `Selenium.WebDriver` | Browser automation | Version pinned by NuciWeb |
| `Selenium.Support` | WebDriver support | Version pinned by NuciWeb |
| `System.Text.Json` | JSON (if used) | Framework provided |

## Version Management

### PackageReference (NuciWeb.Steam.csproj)

```xml
<PackageReference Include="NuciWeb" Version="4.1.0" />
<PackageReference Include="NuciWeb.Automation" Version="1.0.1" />
<PackageReference Include="NuciExtensions" Version="6.0.2" />
<PackageReference Include="SteamGuard.TOTP" Version="1.1.1" />
```

### Update Policy

| Dependency | Update Frequency | Testing Required |
|------------|------------------|------------------|
| NuciWeb | As needed for browser compatibility | Full integration test |
| NuciWeb.Automation | With NuciWeb | Selector compatibility |
| NuciExtensions | As needed | Extension method compatibility |
| SteamGuard.TOTP | Rare (algorithm stable) | TOTP generation test |

## Compatibility Matrix

| NuciWeb.Steam | .NET | NuciWeb | NuciWeb.Automation | NuciExtensions | SteamGuard.TOTP |
|---------------|------|---------|-------------------|----------------|-----------------|
| Current | 10.0 | 4.1.0 | 1.0.1 | 6.0.2 | 1.1.1 |

## License Compatibility

| Dependency | License | Compatible with GPL-3.0-or-later |
|------------|---------|----------------------------------|
| NuciWeb | MIT | Yes |
| NuciWeb.Automation | MIT | Yes |
| NuciExtensions | MIT | Yes |
| SteamGuard.TOTP | MIT | Yes |
| Selenium | Apache-2.0 | Yes (GPL-3.0 compatible) |

All dependencies use permissive licenses compatible with GPL-3.0-or-later.

## Security Considerations

| Dependency | Attack Surface | Mitigation |
|------------|----------------|------------|
| NuciWeb / Selenium | WebDriver binary execution | Use trusted WebDriver sources |
| SteamGuard.TOTP | Shared secret handling | Library doesn't persist secrets |
| NuciExtensions | Extension methods | Minimal, well-tested |

## Update Procedure

1. Update `PackageReference` versions in `NuciWeb.Steam.csproj`
2. Run `dotnet restore`
3. Build: `dotnet build`
4. Run integration tests (manual)
5. Verify selectors still work against live Steam
6. Update `CHANGELOG.md` if applicable

## Related Documentation

- [Build and Deployment](../build-and-deployment.md) — Build process
- [Architecture](../architecture.md) — Dependency injection pattern
- [Components: Application Services](../components/application-services.md) — Processor dependency usage
- [Security](../security.md) — Supply chain considerations