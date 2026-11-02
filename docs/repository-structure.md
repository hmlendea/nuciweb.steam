# Repository Structure

## Source Tree Layout

```
nuciweb.steam/
├── ARCHITECTURE.md              # Root architecture documentation
├── CODE_OF_CONDUCT.md           # Community standards
├── CONTRIBUTING.md              # Contribution guidelines
├── LICENSE                      # GNU GPL v3.0 or later
├── NuciWeb.Steam.slnx           # SDK-style solution file
├── PRIVACY.md                   # Privacy and data handling
├── README.md                    # Project overview and usage
├── SECURITY.md                  # Security policy
├── docs/                        # Generated documentation (this directory)
│   ├── INDEX.md                 # Master index
│   ├── api-reference/           # API reference documentation
│   ├── behaviour/               # User-facing behaviours
│   ├── components/              # Per-component deep dives
│   ├── flows/                   # End-to-end execution flows
│   └── ...                      # Other documentation files
├── NuciWeb.Steam/               # Main library project
│   ├── NuciWeb.Steam.csproj     # Project configuration
│   ├── ISteamProcessor.cs       # Public API interface
│   ├── SteamProcessor.cs        # Public API implementation (facade)
│   ├── KeyActivationErrorCode.cs # Key activation error codes enum
│   ├── KeyActivationException.cs # Key activation exception type
│   ├── Models/
│   │   └── SteamAccount.cs      # Credentials data model
│   ├── Processors/              # Internal domain processors
│   │   ├── ISteamAuthenticationProcessor.cs
│   │   ├── SteamAuthenticationProcessor.cs
│   │   ├── ISteamChatProcessor.cs
│   │   ├── SteamChatProcessor.cs
│   │   ├── ISteamKeyProcessor.cs
│   │   ├── SteamKeyProcessor.cs
│   │   ├── ISteamProfileProcessor.cs
│   │   ├── SteamProfileProcessor.cs
│   │   ├── ISteamWorkshopProcessor.cs
│   │   └── SteamWorkshopProcessor.cs
│   └── Utils/
│       └── SteamUrls.cs         # Centralised Steam URL constants
└── bin/                         # Build outputs (gitignored)
└── obj/                         # Build intermediates (gitignored)
```

## Module Organisation

### Public API Layer (`NuciWeb.Steam/` root)

| File | Responsibility |
|------|----------------|
| `ISteamProcessor.cs` | Public contract — 9 synchronous methods |
| `SteamProcessor.cs` | Facade implementation — composes and delegates to internal processors |
| `SteamAccount.cs` | Credentials DTO — Username, Password, TotpKey |
| `KeyActivationErrorCode.cs` | Typed error codes for key activation failures |
| `KeyActivationException.cs` | Exception with error code for typed catch handling |

### Internal Processors (`NuciWeb.Steam/Processors/`)

Each domain has an internal interface and implementation:

| Domain | Interface | Implementation | Responsibility |
|--------|-----------|----------------|----------------|
| Authentication | `ISteamAuthenticationProcessor` | `SteamAuthenticationProcessor` | Dual login (store + community), Steam Guard TOTP, session validation |
| Profile | `ISteamProfileProcessor` | `SteamProfileProcessor` | Display name, custom URL, avatar upload |
| Key Activation | `ISteamKeyProcessor` | `SteamKeyProcessor` | Product key activation with localised error mapping |
| Chat | `ISteamChatProcessor` | `SteamChatProcessor` | Navigate to chat, verify loaded |
| Workshop | `ISteamWorkshopProcessor` | `SteamWorkshopProcessor` | Favourite and subscribe with idempotency |

All processor interfaces are `internal` — not exposed to consumers.

### Utilities (`NuciWeb.Steam/Utils/`)

| File | Responsibility |
|------|----------------|
| `SteamUrls.cs` | Static class with Steam endpoint constants and format strings |

## Project Configuration

### NuciWeb.Steam.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NuciWeb.Steam</RootNamespace>
    <Version>1.5.4</Version>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <IsPackable>true</IsPackable>
    <PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NuciExtensions" Version="6.0.2" />
    <PackageReference Include="NuciWeb" Version="4.1.0" />
    <PackageReference Include="NuciWeb.Automation" Version="1.0.1" />
    <PackageReference Include="SteamGuard.TOTP" Version="1.1.1" />
  </ItemGroup>
</Project>
```

### Solution File (NuciWeb.Steam.slnx)

```xml
<Solution>
  <Project Path="NuciWeb.Steam/NuciWeb.Steam.csproj" />
</Solution>
```

SDK-style solution with single project. No test project currently exists.

## Namespace Organisation

- `NuciWeb.Steam` — Public API, models, exceptions
- `NuciWeb.Steam.Models` — Data models (`SteamAccount`)
- `NuciWeb.Steam.Processors` — Internal processor interfaces and implementations
- `NuciWeb.Steam.Utils` — Utility classes (`SteamUrls`)

## Build Outputs

- `bin/Debug/net10.0/` — Debug build artefacts
- `obj/Debug/net10.0/` — Build intermediates, generated files

## Git Ignore Patterns

Standard .NET gitignore applies:
- `bin/`
- `obj/`
- `*.user`
- `*.suo`
- `.vs/`
- `*.nupkg`