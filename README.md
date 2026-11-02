[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Build Status](https://github.com/hmlendea/nuciweb.steam/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nuciweb.steam/actions/workflows/dotnet.yml) [![Latest GitHub release](https://img.shields.io/github/v/release/hmlendea/nuciweb.steam)](https://github.com/hmlendea/nuciweb.steam/releases/latest)

# NuciWeb.Steam

## About

NuGet package that provides high-level Steam account automation flows on top of [NuciWeb](https://github.com/hmlendea/nuciweb) (`IWebProcessor`).

It automates common account tasks such as:
- signing in to Steam *(including Steam Guard TOTP)*
- accepting or rejecting cookie preferences
- opening Steam Chat
- activating product keys *(with typed activation error handling)*
- updating profile name, custom profile URL, and avatar
- favoriting and subscribing to Workshop items

## Installation

[![Get it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciWeb.Steam)

**.NET CLI**:
```bash
dotnet add package NuciWeb.Steam
```

**Package Manager**:
```powershell
Install-Package NuciWeb.Steam
```

## License

This project is licensed under the `GNU General Public License v3.0` or later. See [LICENSE](./LICENSE) for details.

## 🛡️ Privacy and Data

NuciWeb.Steam is a .NET library that automates Steam account operations. It processes Steam account credentials and profile data provided by the calling application and communicates exclusively with Steam services (steamcommunity.com, store.steampowered.com, api.steampowered.com) over HTTPS. The library does not persist, log, or transmit personal data to any third party or to the project maintainers.

For the detailed description of how the application handles privacy and personal data, see [PRIVACY.md](./PRIVACY.md).

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Steam account credentials (username, password, Steam Guard shared secret) | Authentication with Steam services | Transient in-memory only | Duration of operation | No |
| Profile data (display name, custom URL, avatar) | Profile updates | Transient in-memory only | Duration of operation | Yes |
| Product keys | Product key activation | Transient in-memory only | Duration of operation | Yes |
| Workshop item identifiers | Workshop interactions (favorite, subscribe) | Transient in-memory only | Duration of operation | Yes |
| HTTP request/response data with Steam services | Automation flow execution | Transient in-memory only | Duration of operation | No |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/nuciweb.steam/issues) for problems or suggestions

See the [contributing guidelines](./CONTRIBUTING.md) for details on how to report issues, suggest enhancements, and submit changes.

## 📜 Code of Conduct

See the [Code of Conduct](./CODE_OF_CONDUCT.md) for the standards of conduct expected in all project spaces.

## 🔒 Security

NuciWeb.Steam follows a coordinated disclosure policy for security vulnerabilities. Only the latest version distributed via NuGet and GitHub Releases receives security maintenance.

For information on reporting security vulnerabilities, see [SECURITY.md](./SECURITY.md).
