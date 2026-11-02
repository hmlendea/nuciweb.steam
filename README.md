[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Build Status](https://github.com/hmlendea/nuciweb.steam/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nuciweb.steam/actions/workflows/dotnet.yml)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/nuciweb.steam)](https://github.com/hmlendea/nuciweb.steam/releases/latest)
[![License](https://img.shields.io/github/license/hmlendea/nuciweb.steam)](https://github.com/hmlendea/nuciweb.steam/blob/master/LICENSE)

# NuciWeb.Steam

NuGet package that provides high-level Steam account automation flows on top of [NuciWeb](https://github.com/hmlendea/nuciweb) (`IWebProcessor`).

## 📑 Table of Contents

- [Capabilities](#capabilities)
- [Usage](#usage)
- [Installation](#installation)
  - [Package Manager Installation](#package-manager-installation)
- [Privacy and Data](#privacy-and-data)
- [Development](#development)
  - [Requirements](#requirements)
  - [Build](#build)
  - [Test](#test)
  - [Continuous Integration](#continuous-integration)
- [GitHub Actions](#github-actions)
- [Project Structure](#project-structure)
  - [Projects and Packages](#projects-and-packages)
- [Architecture](#architecture)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [Security](#security)
- [Project Engagement](#project-engagement)
- [License](#license)

## ✨ Capabilities

- Signing in to Steam (including Steam Guard TOTP)
- Accepting or rejecting cookie preferences
- Opening Steam Chat
- Activating product keys (with typed activation error handling)
- Updating profile name, custom profile URL, and avatar
- Favoriting and subscribing to Workshop items

## 🚀 Usage

```csharp
using NuciWeb.Steam;

var processor = new SteamProcessor();
await processor.SignInAsync("username", "password", "sharedSecret");
await processor.ActivateKeyAsync("AAAAA-BBBBB-CCCCC");
```

## 📦 Installation

[![Obtain it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciWeb.Steam)

### Package Manager Installation

```bash
dotnet add package NuciWeb.Steam
```

Or, via the `Package Manager Console`:
```powershell
Install-Package NuciWeb.Steam
```

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

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build NuciWeb.Steam.slnx
```

### Test

```bash
dotnet test NuciWeb.Steam.slnx
```

### Continuous Integration

The `dotnet.yml` workflow runs on every push and pull request. It restores dependencies, builds the solution, and runs tests. Reproduce locally with the Build and Test commands above.

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [ dotnet.yml ](https://github.com/hmlendea/nuciweb.steam/blob/master/.github/workflows/dotnet.yml) | Build and test | Restores dependencies, builds the solution, and runs tests on every push and pull request |

## 🗂️ Project Structure

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `NuciWeb.Steam/` | Library | Main NuGet package providing Steam automation flows |

## 🏗️ Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the system architecture documentation.

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

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/nuciweb.steam/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for additional information.
