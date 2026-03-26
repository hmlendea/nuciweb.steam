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
