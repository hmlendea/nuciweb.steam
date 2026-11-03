# Build and Deployment

## Overview

This document describes the build process, packaging, and deployment for NuciWeb.Steam.

## Build Process

### Prerequisites

| Tool | Version | Installation |
|------|---------|--------------|
| .NET SDK | 10.0 | `dotnet --version` |
| NuGet | Bundled | `dotnet restore` |

### Commands

```bash
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release

# Pack NuGet package
dotnet pack -c Release -o ./artifacts

# Run tests (when test project exists)
dotnet test
```

### Build Output

| Configuration | Output Directory | Artifacts |
|---------------|------------------|-----------|
| Debug | `bin/Debug/net10.0/` | `NuciWeb.Steam.dll`, `.pdb` |
| Release | `bin/Release/net10.0/` | `NuciWeb.Steam.dll`, `.pdb`, `.xml` |
| Pack | `artifacts/` | `NuciWeb.Steam.{version}.nupkg` |

---

## Project Configuration

### NuciWeb.Steam.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>NuciWeb.Steam</PackageId>
    <Version>1.0.0</Version>
    <Authors>NuciWeb Contributors</Authors>
    <Description>Steam account automation via browser automation</Description>
    <PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/nuciweb/nuciweb.steam</PackageProjectUrl>
    <RepositoryUrl>https://github.com/nuciweb/nuciweb.steam</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>steam;automation;browser;webdriver</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NuciWeb" Version="4.1.0" />
    <PackageReference Include="NuciWeb.Automation" Version="1.0.1" />
    <PackageReference Include="NuciExtensions" Version="6.0.2" />
    <PackageReference Include="SteamGuard.TOTP" Version="1.1.1" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

### Key Properties

| Property | Value | Purpose |
|----------|-------|---------|
| `TargetFramework` | `net10.0` | .NET 10 target |
| `ImplicitUsings` | `disable` | Explicit usings required |
| `Nullable` | `enable` | Nullable reference types |
| `PackageLicenseExpression` | `GPL-3.0-or-later` | SPDX license |
| `GeneratePackageOnBuild` | `false` | Manual pack control |

---

## CI/CD Pipeline

### GitHub Actions Workflows

**Build & Test:** `.github/workflows/dotnet.yml` (canonical template)

```yaml
name: .NET

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v7

    - name: Setup .NET
      uses: actions/setup-dotnet@v5
      with:
        dotnet-version: 10.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal
```

**NuGet Release:** `.github/workflows/nuget-release.yml` (canonical template)

Triggers on GitHub Release publication. Uses NuGet trusted publishing (OIDC) with `NUGET_USER` secret.

### Workflow Triggers

| Event | Branches | Jobs |
|-------|----------|------|
| `push` | `master` | Build, Test |
| `pull_request` | `master` | Build, Test |
| `release` (published) | Any | NuGet Publish |
| `workflow_dispatch` | Any | Manual integration test |

---

## NuGet Packaging

### Package Creation

```bash
# Create package
dotnet pack -c Release -o ./artifacts

# Output: NuciWeb.Steam.{version}.nupkg
```

### Package Contents

| File | Included | Purpose |
|------|----------|---------|
| `NuciWeb.Steam.dll` | Yes | Main assembly |
| `NuciWeb.Steam.pdb` | Yes | Debug symbols |
| `NuciWeb.Steam.xml` | Yes | XML documentation |
| `README.md` | Yes | Package description |
| `LICENSE` | No | Referenced via SPDX |

### Versioning

**Scheme:** Semantic Versioning (MAJOR.MINOR.PATCH)

| Change Type | Version Bump |
|-------------|--------------|
| Breaking API change | MAJOR |
| New feature (backward compatible) | MINOR |
| Bug fix | PATCH |
| Dependency update | PATCH (or MINOR if API affected) |

**Version Location:** `<Version>` in `NuciWeb.Steam.csproj`

### Publishing to NuGet.org

Automated via `nuget-release.yml` workflow on GitHub Release. Manual fallback:

```bash
dotnet nuget push ./artifacts/NuciWeb.Steam.{version}.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

---

## GitHub Release

### Release Process

1. Update version in `NuciWeb.Steam.csproj`
2. Update `CHANGELOG.md`
3. Commit and push to `master`
4. Create GitHub Release (tag: `v{version}`)
5. NuGet publish workflow triggers automatically

### Release Assets

| Asset | Source | Included |
|-------|--------|----------|
| NuGet package | `dotnet pack` | Yes (via workflow) |
| Source code | Git tag | Yes (auto) |
| Documentation | `docs/` | No (in repo) |

---

## Docker (Optional)

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuciWeb.Steam/NuciWeb.Steam.csproj", "NuciWeb.Steam/"]
RUN dotnet restore "NuciWeb.Steam/NuciWeb.Steam.csproj"
COPY . .
WORKDIR "/src/NuciWeb.Steam"
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "NuciWeb.Steam.dll"]
```

**Note:** Library only — no executable entry point. Docker for consumers who embed in containers.

---

## Dependency Management

### Update Process

1. Check for updates: `dotnet list package --outdated`
2. Update `PackageReference` versions in `.csproj`
3. Run `dotnet restore`
4. Build and test
5. Verify selectors still work against live Steam
6. Commit changes

### Security Scanning

```bash
# Check for vulnerabilities
dotnet list package --vulnerable --include-transitive
```

---

## Deployment Checklist

### Pre-Release

- [ ] Version bumped in `.csproj`
- [ ] `CHANGELOG.md` updated
- [ ] All tests pass (manual integration)
- [ ] Selectors verified against live Steam
- [ ] `dotnet pack` produces valid package
- [ ] Package installs and works in test project

### Release

- [ ] Tag created: `git tag v{version}`
- [ ] GitHub Release created
- [ ] NuGet package published (automatic via workflow)
- [ ] Release notes published

### Post-Release

- [ ] Verify package on NuGet.org
- [ ] Update dependent projects (if any)
- [ ] Monitor for issues

---

## Related Documentation

- [Dependencies](../dependencies.md) — Package versions
- [Configuration](../configuration.md) — Build-time config
- [Testing](../testing.md) — Test integration
- [Architecture](../architecture.md) — Build structure