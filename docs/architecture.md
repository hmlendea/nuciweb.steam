# Architecture

This document elaborates on the root [ARCHITECTURE.md](../ARCHITECTURE.md) with implementation-level detail for component owners and debuggers.

## Architectural Style

**Facade with Composed Processors.** `SteamProcessor` implements the public `ISteamProcessor` interface and delegates to five internal processor classes, each owning a distinct Steam domain. This isolates page-specific selectors, navigation logic, and error handling per domain while presenting a unified synchronous API.

```
┌─────────────────────────────────────────────────────────────┐
│                    ISteamProcessor                          │
│  (Public API — 9 synchronous methods)                       │
└─────────────────────────┬───────────────────────────────────┘
                          │ implements
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    SteamProcessor                           │
│  - IWebProcessor webProcessor                               │
│  - SteamAuthenticationProcessor authenticationProcessor     │
│  - SteamChatProcessor chatProcessor                         │
│  - SteamKeyProcessor keyProcessor                           │
│  - SteamProfileProcessor profileProcessor                   │
│  - SteamWorkshopProcessor workshopProcessor                 │
└─────────────────────────┬───────────────────────────────────┘
                          │ composes & delegates
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌──────────────┐
│ Authentication  │ │   Profile   │ │    Key       │
│ Processor       │ │  Processor  │ │  Processor   │
└─────────────────┘ └─────────────┘ └──────────────┘
          │               │               │
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌──────────────┐
│    Chat         │ │  Workshop   │ │  SteamUrls   │
│  Processor      │ │  Processor  │ │  (constants) │
└─────────────────┘ └─────────────┘ └──────────────┘
          │               │
          └───────────────┼───────────────┐
                          ▼               ▼
                 ┌─────────────────┐ ┌──────────────┐
                 │  IWebProcessor  │ │ISteamGuard   │
                 │  (NuciWeb)      │ │(SteamGuard.  │
                 │                 │ │ TOTP)        │
                 └─────────────────┘ └──────────────┘
```

## Component Responsibilities

### SteamProcessor (Facade)

**Location:** `NuciWeb.Steam/SteamProcessor.cs`

**Responsibilities:**
- Implements `ISteamProcessor` public interface
- Constructs and owns all internal processor instances
- Delegates each public method to the appropriate internal processor
- Manages `IWebProcessor` lifetime (injected or default)

**Key Design Decisions:**
- Synchronous API only — no `async`/`Task` in public contract
- Single-threaded — processors maintain in-memory state (used TOTP codes)
- Constructor injection for `IWebProcessor` enables testing and backend flexibility

### SteamAuthenticationProcessor

**Location:** `NuciWeb.Steam/Processors/SteamAuthenticationProcessor.cs`

**Responsibilities:**
- Dual login flow: Steam Store → Steam Community
- Steam Guard TOTP code generation and input
- Session validation (username match on both domains)
- Captcha detection and failure handling
- Used TOTP code tracking to prevent reuse

**Dependencies:**
- `IWebProcessor` — browser automation
- `ISteamGuard` — TOTP code generation (injected, default: `SteamGuard.TOTP.SteamGuard`)
- `SteamUrls` — endpoint constants

**Key Implementation Details:**
- `LogIn` calls `LogInOnPage` twice (store then community)
- TOTP codes are 5 characters, input individually into separate input fields
- `UsedSteamGuardCodes` list prevents code reuse within a single `LogIn` call
- Waits 10 seconds between TOTP generations if code already used (TODO: remove hardcoded sleep)

### SteamProfileProcessor

**Location:** `NuciWeb.Steam/Processors/SteamProfileProcessor.cs`

**Responsibilities:**
- Display name update via edit profile page
- Custom URL (identifier) update via edit profile page
- Avatar upload via file input (revealed via JavaScript)
- Navigation: account page → profile page → edit profile page

**Dependencies:**
- `IWebProcessor` — browser automation
- `SteamUrls` — endpoint constants

**Key Implementation Details:**
- Uses JavaScript to reveal hidden file input for avatar upload
- Compares image URLs before/after to confirm upload success
- Polls up to 5 times with waits for image URL changes

### SteamKeyProcessor

**Location:** `NuciWeb.Steam/Processors/SteamKeyProcessor.cs`

**Responsibilities:**
- Navigate to key activation page
- Submit product key with SSA agreement checkbox
- Map localised error messages to `KeyActivationErrorCode`
- Return product name on success

**Dependencies:**
- `IWebProcessor` — browser automation
- `SteamUrls` — endpoint constants
- `KeyActivationException` / `KeyActivationErrorCode` — typed errors

**Key Implementation Details:**
- Supports English and Romanian error message detection
- Error mapping covers 7 specific error codes plus unexpected
- Waits for either receipt (success) or error element

### SteamChatProcessor

**Location:** `NuciWeb.Steam/Processors/SteamChatProcessor.cs`

**Responsibilities:**
- Navigate to Steam Chat URL
- Verify chat loaded by waiting for current user avatar element

**Dependencies:**
- `IWebProcessor` — browser automation
- `SteamUrls` — endpoint constants

### SteamWorkshopProcessor

**Location:** `NuciWeb.Steam/Processors/SteamWorkshopProcessor.cs`

**Responsibilities:**
- Navigate to Workshop item page
- Favorite item (idempotent — checks if already favorited)
- Subscribe to item (idempotent — checks if already subscribed)
- Handle modal dialog for required base products during subscribe

**Dependencies:**
- `IWebProcessor` — browser automation
- `SteamUrls` — endpoint constants (format string for workshop URLs)

**Key Implementation Details:**
- Uses `ClickAny` to handle multiple possible button selectors
- Waits for confirmation notices (`JustFavorited`, `JustSubscribed`)
- Handles modal dialog with "Continue" button for required base products

## Data Flow

```
Client Application
       │
       ▼
SteamProcessor (facade)
       │
       ├── LogIn(SteamAccount) ──────────────────► SteamAuthenticationProcessor
       │                                              │
       │                                              ├── GoToUrl(StoreLogin)
       │                                              ├── Fill credentials
       │                                              ├── Submit
       │                                              ├── Wait for Steam Guard
       │                                              ├── Generate TOTP (loop until unused)
       │                                              ├── Input 5 chars individually
       │                                              ├── Validate session (store)
       │                                              ├── GoToUrl(CommunityLogin)
       │                                              └── Validate session (community)
       │
       ├── SetProfileName/Identifier/Picture ──────► SteamProfileProcessor
       │                                              │
       │                                              ├── GoToProfilePage
       │                                              ├── GoToEditProfilePage
       │                                              ├── Set form fields
       │                                              ├── Click save
       │                                              └── (avatar) Upload file, verify URL change
       │
       ├── ActivateKey(key) ───────────────────────► SteamKeyProcessor
       │                                              │
       │                                              ├── GoToUrl(KeyActivation) if needed
       │                                              ├── Set key, check SSA, click register
       │                                              ├── Wait for receipt or error
       │                                              ├── Map error to KeyActivationErrorCode
       │                                              └── Return product name or throw
       │
       ├── VisitChat() ────────────────────────────► SteamChatProcessor
       │                                              │
       │                                              ├── GoToUrl(Chat)
       │                                              └── Wait for currentUserAvatar
       │
       ├── FavouriteWorkshopItem/Subscribe ────────► SteamWorkshopProcessor
       │                                              │
       │                                              ├── GoToWorkshopItemPage(id)
       │                                              ├── Check current state (buttons)
       │                                              ├── Click appropriate button
       │                                              ├── Wait for confirmation
       │                                              └── (subscribe) Handle modal if needed
       │
       └── AcceptCookies/RejectCookies ────────────► SteamProcessor (direct)
                                                      │
                                                      ├── GoToUrl(CookiePreferences)
                                                      └── Click accept/reject button
```

## Dependency Rules

1. **Public API** (`ISteamProcessor`, `SteamAccount`, `KeyActivationErrorCode`, `KeyActivationException`) — no dependencies on NuciWeb or automation types
2. **Internal Processors** — depend on `IWebProcessor` (abstraction) and `SteamUrls` (constants)
3. **SteamAuthenticationProcessor** — additionally depends on `ISteamGuard` abstraction for TOTP
4. **SteamUrls** — leaf dependency, no outbound dependencies
5. **No circular dependencies** — strict layering: Client → Facade → Processors → Abstractions/Utilities

## Extension Points

### Custom Authentication Flow
1. Implement `ISteamAuthenticationProcessor` with custom logic
2. Construct `SteamProcessor` using internal constructor or fork
3. Add verification for session validation invariants

### Additional Processor Domains
1. Create `ISteamXxxProcessor` interface and `SteamXxxProcessor` implementation
2. Add field and delegation in `SteamProcessor`
3. Expose method on `ISteamProcessor` (breaking) or new interface (non-breaking)

### Alternative TOTP Provider
1. Implement `ISteamGuard` (from SteamGuard.TOTP or custom)
2. Inject via `SteamAuthenticationProcessor` constructor overload

## Related Documentation

- [Root ARCHITECTURE.md](../ARCHITECTURE.md) — High-level architecture overview
- [Components](./components/) — Per-component deep dives
- [Flows](./flows/) — End-to-end execution flows
- [Dependencies](./dependencies.md) — External and internal dependencies