# API Reference Index

## Overview

This section documents the public API surface of NuciWeb.Steam. The library exposes a single public interface `ISteamProcessor` implemented by `SteamProcessor`, along with supporting types for accounts, errors, and exceptions.

## Entry Point

| Type | Kind | Description |
|------|------|-------------|
| `ISteamProcessor` | Interface | Public contract for all Steam operations |
| `SteamProcessor` | Class | Facade implementation composing internal processors |

## Core Types

| Type | Kind | Description |
|------|------|-------------|
| `SteamAccount` | Class | Credentials container (username, password, TOTP key) |
| `KeyActivationErrorCode` | Enum | Typed error codes for key activation failures |
| `KeyActivationException` | Exception | Typed exception for key activation with error code |
| `AuthenticationException` | Exception | Exception for authentication failures |

## Method Summary

### Authentication

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `LogIn` | `void` | `SteamAccount account` | Authenticate on Steam Store and Community |

### Profile Management

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `SetProfileName` | `void` | `string name` | Update display name |
| `SetProfileIdentifier` | `void` | `string identifier` | Update custom profile URL |
| `SetProfilePicture` | `void` | `string imagePath` | Upload new avatar image |

### Cookie Preferences

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `AcceptCookies` | `void` | — | Accept all cookies on Steam Store |
| `RejectCookies` | `void` | — | Reject all cookies on Steam Store |

### Chat

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `VisitChat` | `void` | — | Navigate to Steam Chat |

### Key Activation

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `ActivateKey` | `string` | `string key` | Redeem product key, returns product name |

### Workshop

| Method | Return | Parameters | Description |
|--------|--------|------------|-------------|
| `FavouriteWorkshopItem` | `void` | `string workshopItemId` | Favourite a workshop item |
| `SubscribeToWorkshopItem` | `void` | `string workshopItemId` | Subscribe to a workshop item |

## Error Code Reference

### KeyActivationErrorCode

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Unexpected` | Unrecognised error |
| 1 | `InvalidProductKey` | Key format invalid or doesn't exist |
| 2 | `AlreadyActivatedDifferentAccount` | Key used on another account |
| 3 | `AlreadyActivatedCurrentAccount` | Key already owned by this account |
| 4 | `BaseProductRequired` | DLC requires base game ownership |
| 5 | `RegionLocked` | Key not valid for account region |
| 6 | `TooManyAttempts` | Rate limited by Steam |

## Navigation

- [SteamProcessor Detailed Reference](./steam-processor.md) — Per-method documentation with parameters, returns, exceptions, and examples
- [KeyActivationErrorCode](./steam-processor.md#keyactivationerrorcode) — Enum values and mappings
- [KeyActivationException](./steam-processor.md#keyactivationexception) — Exception details
- [SteamAccount](./steam-processor.md#steamaccount) — Credentials model
- [AuthenticationException](./steam-processor.md#authenticationexception) — Auth exception details

## Related Documentation

- [Architecture](../architecture.md) — Facade pattern and processor composition
- [Components: Application Services](../components/application-services.md) — Internal processor implementations
- [Flows](../flows/) — End-to-end execution sequences
- [Error Handling](../error-handling.md) — Exception taxonomy and handling strategies