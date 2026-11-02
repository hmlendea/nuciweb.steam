# Data Model

## Overview

This document describes the data structures, invariants, and relationships in NuciWeb.Steam. The model is intentionally minimal — focused on automation inputs and typed error outputs.

## Core Types

### SteamAccount

```csharp
public sealed class SteamAccount
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TotpKey { get; set; }
}
```

**Purpose:** Credentials container for authentication.

**Fields:**
| Field | Type | Required | Constraints | Sensitivity |
|-------|------|----------|-------------|-------------|
| `Username` | `string` | Yes | Email or username format | High (PII) |
| `Password` | `string` | Yes | Any string | Critical (secret) |
| `TotpKey` | `string` | Conditional | Base32 encoded shared secret | Critical (secret) |

**Invariants:**
- `Username` and `Password` must be non-null for `LogIn`
- `TotpKey` must be valid Base32 if provided (validated by `ISteamGuard`)
- Instance should be short-lived; not persisted, logged, or serialized

**Lifecycle:**
1. Created by caller
2. Passed to `SteamProcessor.LogIn`
3. Consumed by `SteamAuthenticationProcessor`
4. Eligible for GC after `LogIn` returns

---

### KeyActivationErrorCode

```csharp
public enum KeyActivationErrorCode
{
    Unexpected = 0,
    InvalidProductKey = 1,
    AlreadyActivatedDifferentAccount = 2,
    AlreadyActivatedCurrentAccount = 3,
    BaseProductRequired = 4,
    RegionLocked = 5,
    TooManyAttempts = 6
}
```

**Purpose:** Typed error classification for key activation failures.

**Values:**
| Value | Name | Category | Retryable | User Action |
|-------|------|----------|-----------|-------------|
| 0 | `Unexpected` | Unknown | No | Report issue |
| 1 | `InvalidProductKey` | Client error | No | Verify key |
| 2 | `AlreadyActivatedDifferentAccount` | Conflict | No | Key consumed |
| 3 | `AlreadyActivatedCurrentAccount` | Conflict | No | Already owned |
| 4 | `BaseProductRequired` | Precondition | No | Buy base game |
| 5 | `RegionLocked` | Policy | No | Region mismatch |
| 6 | `TooManyAttempts` | Rate limit | Yes (delay) | Wait and retry |

**Mapping:** Performed by `SteamKeyProcessor.MapErrorToCode` via string matching (English/Romanian).

---

### KeyActivationException

```csharp
[Serializable]
public class KeyActivationException : Exception
{
    public KeyActivationErrorCode Code { get; }

    public KeyActivationException(KeyActivationErrorCode code, string message);
    public KeyActivationException(KeyActivationErrorCode code, string message, Exception inner);
    protected KeyActivationException(SerializationInfo info, StreamingContext context);
    public override void GetObjectData(SerializationInfo info, StreamingContext context);
}
```

**Purpose:** Typed exception carrying both machine-readable code and human-readable message.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `Code` | `KeyActivationErrorCode` | Programmatic error classification |
| `Message` | `string` | Original Steam error text (localised) |
| `InnerException` | `Exception` | Optional wrapped exception |

**Serialization:** Implements `ISerializable` for binary serialization support.

---

### AuthenticationException

```csharp
public class AuthenticationException : Exception
{
    public AuthenticationException(string message);
    public AuthenticationException(string message, Exception inner);
}
```

**Purpose:** Generic authentication failure exception.

**Common Messages:**
| Message | Cause |
|---------|-------|
| "Captcha input required." | Steam presented captcha |
| "The provided Steam Guard code is not valid." | TOTP rejected |
| "Authentication failure." | No success indicator |
| "Already logged in as a different user ('X')." | Session mismatch |
| Steam error text | Invalid credentials, locked account |

---

## Internal State

### SteamAuthenticationProcessor.UsedSteamGuardCodes

```csharp
private readonly List<string> UsedSteamGuardCodes = new();
```

**Purpose:** Tracks TOTP codes used within a single `LogIn` call to prevent reuse.

**Lifecycle:**
- Initialised empty at processor construction
- Codes added during `InputSteamGuardCode`
- Cleared implicitly when processor is GC'd (per `SteamProcessor` instance)
- Not shared across `LogIn` calls (new processor per `SteamProcessor`)

**Invariant:** No duplicate codes within one `LogIn` invocation.

---

## Data Flow Types

### Steam Guard TOTP Code

**Format:** 5-character alphanumeric string (e.g., "R7K3M")

**Generation:** `ISteamGuard.GenerateAuthenticationCode(string sharedSecret)`

**Algorithm:** RFC 6238 TOTP (SHA-1, 30-second window, 5 digits)

**Validity:** Single-use per Steam login challenge; 30-second window

---

### Product Key

**Format:** Variable (typically `XXXXX-XXXXX-XXXXX` or `XXXXX-XXXXX`)

**Validation:** Performed by Steam server; library does no client-side validation

**Consumption:** One-time use; bound to account on successful activation

---

### Workshop Item ID

**Format:** Numeric string (e.g., "123456789")

**Source:** Steam Community workshop item URL: `https://steamcommunity.com/sharedfiles/filedetails/?id=123456789`

**Usage:** Passed directly to `FavouriteWorkshopItem` / `SubscribeToWorkshopItem`

---

### Profile Fields

| Field | Steam Limits | Library Validation |
|-------|--------------|-------------------|
| Display Name | 32 chars max | None |
| Custom URL | 2-32 chars, alphanumeric + underscore | None |
| Avatar | JPG/PNG, ~1MB, 184x184px recommended | None |

---

## Relationships

```
SteamProcessor (1) ─────► (1) IWebProcessor
       │
       ├─► (1) SteamAuthenticationProcessor ──► (1) ISteamGuard
       ├─► (1) SteamProfileProcessor
       ├─► (1) SteamKeyProcessor
       ├─► (1) SteamChatProcessor
       └─► (1) SteamWorkshopProcessor

SteamAccount ──► LogIn(SteamAccount)
KeyActivationException ◄── ActivateKey(string) ──► KeyActivationErrorCode
```

---

## Serialisation

**Supported:**
- `KeyActivationException` — Implements `ISerializable`
- `KeyActivationErrorCode` — Standard enum serialisation

**Not Supported / Not Intended:**
- `SteamAccount` — Contains secrets, should not be serialised
- `SteamProcessor` — Holds browser session, not serialisable
- Internal processors — Implementation details

---

## Validation Rules

### Input Validation (Library)

| Input | Validation | Exception |
|-------|------------|-----------|
| `SteamAccount` in `LogIn` | Not null | `ArgumentNullException` |
| `SteamAccount.Username` | Not null/empty | `ArgumentNullException` (via `SetText`) |
| `SteamAccount.Password` | Not null/empty | `ArgumentNullException` (via `SetText`) |
| `SteamAccount.TotpKey` | Not null/empty when needed | `ArgumentNullException` in `InputSteamGuardCode` |
| `name` in `SetProfileName` | Not null | `ArgumentNullException` |
| `identifier` in `SetProfileIdentifier` | Not null | `ArgumentNullException` |
| `imagePath` in `SetProfilePicture` | Not null | `ArgumentNullException` |
| `key` in `ActivateKey` | Not null | `ArgumentNullException` |
| `workshopItemId` in workshop methods | Not null | `ArgumentNullException` |

### Server-Side Validation (Steam)

| Operation | Validated By | Failure Mode |
|-----------|--------------|--------------|
| Login credentials | Steam Store | `AuthenticationException` |
| Steam Guard code | Steam Store | `AuthenticationException` |
| Profile name | Steam Community | HTTP error (not caught) |
| Custom URL | Steam Community | HTTP error (not caught) |
| Avatar image | Steam Community | HTTP error (not caught) |
| Product key | Steam Store | `KeyActivationException` |
| Workshop actions | Steam Community | HTTP error (not caught) |

---

## Privacy Classification

| Data | Classification | Handling |
|------|----------------|----------|
| `SteamAccount.Username` | PII | Not logged, not persisted |
| `SteamAccount.Password` | Secret | Not logged, not persisted, not exposed |
| `SteamAccount.TotpKey` | Secret | Not logged, not persisted, not exposed |
| `KeyActivationException.Message` | Operational | Contains Steam error text (may include product name) |
| Workshop item IDs | Public | No sensitivity |
| Profile name/identifier | Public (user-chosen) | No sensitivity |

---

## Related Documentation

- [API Reference](../api-reference/steam-processor.md) — Type usage in methods
- [Error Handling](../error-handling.md) — Exception taxonomy
- [Security](../security.md) — Secret handling
- [Authentication Flow](../flows/authentication.md) — `SteamAccount` and TOTP usage
- [Key Activation Flow](../flows/key-activation.md) — Error code mapping