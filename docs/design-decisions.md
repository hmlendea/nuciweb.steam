# Design Decisions

## Overview

This document records significant architectural and design decisions for NuciWeb.Steam, their rationale, and trade-offs.

## Decision Log

### 1. Facade Pattern with Composed Processors

**Decision:** `SteamProcessor` implements `ISteamProcessor` and composes five internal processors (`SteamAuthenticationProcessor`, `SteamProfileProcessor`, `SteamKeyProcessor`, `SteamChatProcessor`, `SteamWorkshopProcessor`), all sharing a single `IWebProcessor`.

**Rationale:**
- Single public API surface (`ISteamProcessor`)
- Clear separation of concerns per domain
- Shared browser session enables cookie transfer between Store and Community
- Easy to test individual processors in isolation
- Extensible for new domains

**Trade-offs:**
- More classes than a monolithic implementation
- Internal processors not publicly accessible (by design)
- All processors instantiated eagerly at construction

**Alternatives Considered:**
- Single `SteamProcessor` with all logic — rejected (violates SRP, hard to test)
- Separate public interfaces per domain — rejected (fragmented API, session sharing complex)

---

### 2. Synchronous API Only

**Decision:** All public methods are synchronous (`void` or `string` return), no `async`/`await`.

**Rationale:**
- Browser automation is inherently sequential and blocking
- Simplifies consumer code (no async context management)
- `IWebProcessor` operations are synchronous
- Single-threaded design matches automation use case

**Trade-offs:**
- Blocks calling thread during long operations (10-30s)
- Cannot easily parallelise independent operations
- Not suitable for high-throughput server scenarios

**Alternatives Considered:**
- Async wrappers over sync calls — rejected (fake async, no benefit)
- Full async `IWebProcessor` — would require NuciWeb changes

---

### 3. Browser Automation Over Steam Web API

**Decision:** Use `IWebProcessor` (Selenium/WebDriver) to automate Steam website, not official Steam Web API.

**Rationale:**
- Steam Web API requires API key, limited endpoints
- Website has all functionality (login, 2FA, key activation, profile, workshop, chat)
- No rate limits beyond website's own
- Single integration point (browser) vs multiple API endpoints

**Trade-offs:**
- Brittle selectors (HTML structure changes)
- Slower than API calls
- Requires browser/WebDriver infrastructure
- Captcha/2FA handling complex
- No official support

**Alternatives Considered:**
- Hybrid (API for reads, browser for writes) — rejected (complexity)
- Steam Web API only — rejected (insufficient coverage)

---

### 4. Typed Key Activation Errors

**Decision:** `KeyActivationException` with `KeyActivationErrorCode` enum for all key activation failures.

**Rationale:**
- Enables programmatic handling (retry, user messaging, logging)
- Maps Steam's localised error messages to stable codes
- Serializable for distributed scenarios
- Clear contract for consumers

**Trade-offs:**
- Requires maintaining error message mappings (English/Romanian only)
- New Steam errors fall back to `Unexpected`
- String matching is fragile

**Alternatives Considered:**
- Generic `Exception` with message parsing — rejected (error-prone for consumers)
- HTTP status codes — rejected (Steam returns 200 with error in HTML)

---

### 5. Steam Guard TOTP via Interface

**Decision:** `ISteamGuard` interface injected into `SteamAuthenticationProcessor`, default implementation from `SteamGuard.TOTP` library.

**Rationale:**
- Testability (mock for deterministic codes)
- Separation of crypto logic
- Allows custom implementations (hardware tokens, etc.)
- Library handles RFC 6238 correctly

**Trade-offs:**
- Additional dependency (`SteamGuard.TOTP`)
- Interface adds indirection

**Alternatives Considered:**
- Direct `SteamGuard.TOTP.SteamGuard` usage — rejected (hard to test)
- Built-in TOTP — rejected (crypto best practice: use vetted library)

---

### 6. Idempotent Workshop Operations

**Decision:** `FavouriteWorkshopItem` and `SubscribeToWorkshopItem` check current state and return early if already in target state.

**Rationale:**
- Safe to call repeatedly in automation scripts
- Reduces unnecessary network requests
- Matches user expectation (favouriting twice = still favourited)

**Trade-offs:**
- Extra DOM checks before action
- Slightly slower for first call

**Alternatives Considered:**
- Always click — rejected (causes UI toggles, errors)
- Separate "check" methods — rejected (API bloat)

---

### 7. No Internal Logging

**Decision:** Library emits zero log output. Consumers implement logging around calls.

**Rationale:**
- No logging dependency
- Consumer controls log level, format, destination
- Avoids secret leakage (credentials, TOTP, cookies)
- Library has no context for meaningful log levels

**Trade-offs:**
- Consumers must remember to add logging
- No built-in diagnostics for troubleshooting

**Alternatives Considered:**
- `ILogger` injection — rejected (dependency, configuration burden)
- `TraceSource` — rejected (legacy, same issues)

---

### 8. Single Shared IWebProcessor

**Decision:** One `IWebProcessor` instance shared by all internal processors, owned by consumer.

**Rationale:**
- Single browser session = shared cookies = seamless Store ↔ Community auth
- Consumer controls browser lifecycle (headless, proxy, profile)
- Consumer disposes to clear session
- Enables testing with mocked `IWebProcessor`

**Trade-offs:**
- Not thread-safe (single-threaded design)
- Consumer must manage disposal
- All operations sequential

**Alternatives Considered:**
- Per-processor `IWebProcessor` — rejected (cookie sharing complex)
- Internal `IWebProcessor` creation — rejected (consumer loses control)

---

### 9. English/Romanian Error Localisation Only

**Decision:** `MapErrorToCode` only handles English and Romanian Steam error messages.

**Rationale:**
- Developer's known locales
- Covers primary test environments
- Extensible for new locales

**Trade-offs:**
- Other locales fall back to `Unexpected`
- Requires manual addition for new locales

**Alternatives Considered:**
- Machine translation — rejected (unreliable, adds dependency)
- All Steam locales — rejected (maintenance burden)

---

### 10. No Configuration Files

**Decision:** Configuration via constructor injection only. No `appsettings.json`, environment variables, or options pattern.

**Rationale:**
- Library is a component, not an application
- Consumer controls all configuration
- Simple, explicit dependencies
- No configuration parsing code

**Trade-offs:**
- Consumer must wire up dependencies manually
- No default configuration discovery

**Alternatives Considered:**
- `IOptions<SteamOptions>` — rejected (overhead for library)
- Environment variables — rejected (implicit, hard to test)

---

## Related Documentation

- [Architecture](../architecture.md) — Overall structure
- [Components: Host and Composition](../components/host-and-composition.md) — Processor composition
- [Components: Application Services](../components/application-services.md) — Processor details
- [Configuration](../configuration.md) — Injection patterns