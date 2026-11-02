# Change Guide

## Overview

This document describes how to evaluate, implement, and verify changes to NuciWeb.Steam.

## Change Categories

### 1. Selector Updates (Most Common)

**Trigger:** Steam UI changes break element location

**Process:**
1. Identify broken selector from error logs (`NoSuchElementException`)
2. Open Steam page in browser dev tools
3. Find new stable selector (prefer ID > class > XPath)
4. Update selector constant in relevant processor
5. Test against live Steam
6. Commit with message: `fix: update {selector} selector for Steam UI change`

**Files to Modify:**
| Processor | Selector File |
|-----------|---------------|
| Authentication | `SteamAuthenticationProcessor.cs` |
| Profile | `SteamProfileProcessor.cs` |
| Key | `SteamKeyProcessor.cs` |
| Workshop | `SteamWorkshopProcessor.cs` |
| Chat | `SteamChatProcessor.cs` |
| URLs | `SteamUrls.cs` |

### 2. Error Message Mapping Updates

**Trigger:** New Steam error message not mapped to `KeyActivationErrorCode`

**Process:**
1. Capture error message from `KeyActivationException.Message`
2. Determine appropriate `KeyActivationErrorCode`
3. Add English pattern to `MapErrorToCode` in `SteamKeyProcessor.cs`
4. Add Romanian pattern if applicable
5. Add unit test for new mapping
6. Commit with message: `feat: map new error "{message}" to {ErrorCode}`

### 3. New Feature Addition

**Trigger:** New Steam functionality to automate

**Process:**
1. Design API addition to `ISteamProcessor`
2. Implement in `SteamProcessor` (delegate to new/existing processor)
3. Create new processor or extend existing
4. Add selectors, flow logic, error handling
5. Add integration test scenario
6. Update documentation (API reference, flows, behaviour)
7. Commit with message: `feat: add {feature} automation`

### 4. Dependency Updates

**Trigger:** New version of NuciWeb, SteamGuard.TOTP, etc.

**Process:**
1. Update `PackageReference` in `.csproj`
2. Run `dotnet restore`
3. Build: `dotnet build`
4. Run integration tests (manual)
5. Check for breaking changes in dependency
6. Update selectors if NuciWeb changed APIs
7. Commit with message: `chore: update {package} to {version}`

### 5. Breaking API Changes

**Trigger:** Fundamental redesign needed

**Process:**
1. Plan new API in `ISteamProcessor`
2. Implement alongside old API (if possible)
3. Mark old API `[Obsolete]` with migration guidance
4. Major version bump (MAJOR)
6. Update all documentation
7. Release notes with migration guide

---

## Verification Checklist

### For All Changes

- [ ] `dotnet build` succeeds
- [ ] `dotnet pack` produces valid package
- [ ] Manual integration test against live Steam
- [ ] Selectors work on current Steam UI
- [ ] Error mappings cover observed errors
- [ ] No credentials in logs/output
- [ ] Documentation updated

### For Selector Changes

- [ ] Old selector removed
- [ ] New selector tested in browser dev tools
- [ ] Works for both success and error paths
- [ ] No regression on other selectors

### For Error Mapping Changes

- [ ] English pattern added
- [ ] Romanian pattern added (if known)
- [ ] Unit test added
- [ ] Fallback to `Unexpected` still works

### For New Features

- [ ] Added to `ISteamProcessor`
- [ ] Implemented in `SteamProcessor`
- [ ] Processor logic complete
- [ ] Integration test scenario documented
- [ ] API reference updated
- [ ] Flow documentation created
- [ ] Behaviour documentation updated

---

## Release Process

### Version Bump Rules

| Change Type | Version | Example |
|-------------|---------|---------|
| Selector fix | PATCH | 1.0.0 → 1.0.1 |
| Error mapping | PATCH | 1.0.1 → 1.0.2 |
| New feature | MINOR | 1.0.2 → 1.1.0 |
| Dependency update | PATCH/MINOR | 1.1.0 → 1.1.1 |
| Breaking change | MAJOR | 1.1.1 → 2.0.0 |

### Pre-Release Checklist

- [ ] Version updated in `.csproj`
- [ ] `CHANGELOG.md` updated with all changes
- [ ] All manual integration tests pass
- [ ] `dotnet pack` verified
- [ ] Package installs in test project
- [ ] Git tag created: `v{version}`
- [ ] GitHub Release created
- [ ] NuGet publish workflow triggered

---

## Rollback Procedure

If release has critical issue:

1. **NuGet:** Cannot unlist easily — publish patch version immediately
2. **GitHub Release:** Edit release notes, mark as pre-release if needed
3. **Code:** Revert commit, bump version, re-release

---

## Related Documentation

- [Build and Deployment](../build-and-deployment.md) — Release process
- [Testing](../testing.md) — Verification
- [Dependencies](../dependencies.md) — Dependency updates
- [Architecture](../architecture.md) — API design