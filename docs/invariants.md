# Invariants

## Overview

This document defines the invariants (conditions that must always hold) for NuciWeb.Steam.

## Core Invariants

### I1: Single Browser Session

**Statement:** All processors share exactly one `IWebProcessor` instance for the lifetime of `SteamProcessor`.

**Enforcement:**
- `SteamProcessor` constructor accepts `IWebProcessor` and passes to all processors
- No processor creates its own `IWebProcessor`
- Consumer owns and disposes `IWebProcessor`

**Violation Consequences:**
- Cookie isolation between Store and Community
- Authentication state loss
- Resource leaks

---

### I2: Sequential Execution

**Statement:** Only one public method executes at a time on a `SteamProcessor` instance.

**Enforcement:**
- No `async`/`await` in public API
- No internal threading
- Consumer must not call concurrently

**Violation Consequences:**
- Race conditions on shared browser state
- Selector interference
- Undefined behaviour

---

### I3: Valid SteamAccount for Authenticated Operations

**Statement:** `LogIn` must be called with a valid `SteamAccount` before any operation requiring authentication.

**Enforcement:**
- `SteamAuthenticationProcessor.LogIn` validates credentials
- Other processors assume authenticated session
- No automatic re-login

**Violation Consequences:**
- Operations fail with `NoSuchElementException` or unexpected page
- Silent failures possible

---

### I4: TOTP Code Reuse Prevention

**Statement:** A generated TOTP code is never reused for a login attempt.

**Enforcement:**
- `UsedSteamGuardCodes` list tracks used codes per session
- New code generated if previous attempt failed
- List cleared on successful login

**Violation Consequences:**
- Steam rejects reused codes
- Login loops

---

### I5: Key Activation Error Mapping Completeness

**Statement:** Every observed Steam key activation error message maps to a `KeyActivationErrorCode` (falls back to `Unexpected`).

**Enforcement:**
- `MapErrorToCode` covers known English/Romanian messages
- Default case returns `KeyActivationErrorCode.Unexpected`
- New messages added via change guide process

**Violation Consequences:**
- Consumers cannot programmatically handle new errors
- All new errors appear as `Unexpected`

---

### I6: Idempotent Workshop Operations

**Statement:** `FavouriteWorkshopItem` and `SubscribeToWorkshopItem` are idempotent — calling multiple times with same `workshopId` has same effect as calling once.

**Enforcement:**
- Pre-check current state via DOM
- Return early if already in target state
- Only act if state differs

**Violation Consequences:**
- Duplicate favourites/subscriptions
- UI toggles
- Rate limiting

---

### I7: No Credentials in Exceptions

**Statement:** No exception message, stack trace, or `ToString()` output contains passwords, TOTP keys, or session cookies.

**Enforcement:**
- `SteamAccount` does not override `ToString()`
- Exceptions constructed with sanitized messages
- `KeyActivationException` only contains product key (not credentials)

**Violation Consequences:**
- Credential leakage in logs
- Security incident

---

### I8: Selector Stability

**Statement:** All element selectors target stable attributes (ID, data attributes) over fragile ones (position, generated classes).

**Enforcement:**
- Code review for selector changes
- Prefer `ById` > `ByCssSelector` > `ByXPath`
- Document selector rationale in comments

**Violation Consequences:**
- Frequent breakage on Steam UI updates
- High maintenance burden

---

### I9: Single Responsibility per Processor

**Statement:** Each internal processor handles exactly one domain (auth, profile, keys, chat, workshop).

**Enforcement:**
- No cross-domain method calls between processors
- Shared state only via `IWebProcessor` session
- New domains get new processors

**Violation Consequences:**
- Tight coupling
- Testing difficulty
- Unclear ownership

---

### I10: Consumer Controls Browser Lifecycle

**Statement:** `SteamProcessor` never creates, navigates initially, or disposes the `IWebProcessor`.

**Enforcement:**
- `IWebProcessor` injected via constructor
- No `IDisposable` on `SteamProcessor`
- Consumer calls `IWebProcessor.Dispose()`

**Violation Consequences:**
- Consumer cannot configure browser (headless, proxy, profile)
- Session persistence impossible
- Resource leaks

---

## Invariant Verification

### At Build Time

- Compiler enforces single `IWebProcessor` parameter
- No `async` in public interface
- `SteamAccount` has no `ToString` override

### At Runtime (Manual Integration Tests)

- Login → multiple operations → logout sequence
- Concurrent call attempt (should fail visibly)
- Invalid account rejection
- TOTP reuse attempt
- Unknown key error → `Unexpected`
- Duplicate workshop favourite/subscribe
- Exception message inspection
- Selector stability across Steam updates
- Processor domain boundaries
- Consumer disposal pattern

---

## Related Documentation

- [Architecture](../architecture.md) — Invariant context
- [Design Decisions](../design-decisions.md) — Rationale
- [Testing](../testing.md) — Verification approach
- [Error Handling](../error-handling.md) — Exception invariants