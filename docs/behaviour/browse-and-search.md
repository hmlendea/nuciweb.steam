# Browse and Search Behaviours

## Overview

This document describes user-facing browse and search behaviours available through the NuciWeb.Steam API. These are high-level operations that users perform to discover and interact with Steam content.

## Browse Behaviours

### Browse Workshop Items

**Entry Point:** `SteamProcessor.FavouriteWorkshopItem` / `SubscribeToWorkshopItem`

**User Flow:**
1. User discovers workshop item ID externally (Steam website, community links, etc.)
2. User calls `FavouriteWorkshopItem(id)` or `SubscribeToWorkshopItem(id)`
3. Library navigates to `https://steamcommunity.com/sharedfiles/filedetails/?id={id}`
4. Library checks current state (already favourited/subscribed)
5. If not in target state, performs action and waits for confirmation
6. Returns control to user

**Discoverability:** No built-in search or listing — user must provide workshop item ID.

**Idempotency:** Both operations are idempotent — safe to call repeatedly.

**State Visibility:** No return value indicating previous state. User must track externally.

### Browse Chat

**Entry Point:** `SteamProcessor.VisitChat`

**User Flow:**
1. User calls `VisitChat()`
2. Library navigates to `https://steamcommunity.com/chat`
3. Waits for chat interface to load (`#currentUserAvatar` visible)
3. Returns control — browser now at chat interface

**Discoverability:** Direct navigation only. No chat room listing, search, or history.

**Limitations:**
- No programmatic chat interaction (send messages, join rooms, etc.)
- Only navigates; user must use browser for actual chat

### Browse Profile

**Entry Point:** Implicit via profile management methods

**User Flow:**
1. User calls `SetProfileName`, `SetProfileIdentifier`, or `SetProfilePicture`
2. Library navigates to `https://store.steampowered.com/account`
3. Clicks through to edit profile page
4. Performs specific edit
5. Returns control

**Discoverability:** No profile viewing API. User must use browser to view current profile.

---

## Search Behaviours

### Key Activation Search (Implicit)

**Entry Point:** `SteamProcessor.ActivateKey`

**User Flow:**
1. User has product key (from purchase, gift, etc.)
2. User calls `ActivateKey(key)`
3. Library submits key to Steam activation endpoint
4. Steam validates key and returns result
5. On success: returns product name (implicit "search" result)
6. On failure: throws typed exception with error code

**Search Semantics:**
- Key acts as lookup identifier
- No fuzzy search, partial matching, or suggestions
- Exact key required

**Result Information:**
- Success: Product name only (no app ID, no metadata)
- Failure: Error code + localised message

### Workshop Item Search (External)

**Entry Point:** None (external)

**User Flow:**
1. User searches workshop via Steam website or community
2. User extracts workshop item ID from URL
3. User calls `FavouriteWorkshopItem(id)` or `SubscribeToWorkshopItem(id)`

**Library Support:** None — purely external discovery.

---

## Navigation Patterns

### Direct Navigation (All Operations)

| Operation | Target URL | Wait Condition |
|-----------|------------|----------------|
| `LogIn` | `store.steampowered.com/login` | Username/password inputs |
| `LogIn` (community) | `steamcommunity.com/login/home` | Account pulldown username |
| `ActivateKey` | `store.steampowered.com/account/registerkey` | Register button |
| `SetProfileName/Identifier` | `store.steampowered.com/account` → edit profile | Profile name input |
| `SetProfilePicture` | `store.steampowered.com/account` → edit profile → avatar tab | Avatar image |
| `FavouriteWorkshopItem` | `steamcommunity.com/sharedfiles/filedetails/?id={id}` | `#mainContents` |
| `SubscribeToWorkshopItem` | `steamcommunity.com/sharedfiles/filedetails/?id={id}` | `#mainContents` |
| `VisitChat` | `steamcommunity.com/chat` | `#currentUserAvatar` |
| `AcceptCookies` | `store.steampowered.com/account/cookiepreferences` | Accept button |
| `RejectCookies` | `store.steampowered.com/account/cookiepreferences` | Reject button |

### Navigation Idempotency

| Operation | Idempotent Navigation | Notes |
|-----------|----------------------|-------|
| `ActivateKey` | Yes | Checks for register button before navigating |
| `SetProfileName` | No | Always navigates full path |
| `SetProfileIdentifier` | No | Always navigates full path |
| `SetProfilePicture` | No | Always navigates full path |
| `FavouriteWorkshopItem` | No | Always navigates to item page |
| `SubscribeToWorkshopItem` | No | Always navigates to item page |
| `VisitChat` | No | Always navigates to chat |
| `AcceptCookies` | No | Always navigates to cookie page |
| `RejectCookies` | No | Always navigates to cookie page |

---

## Result Presentation

### Success Results

| Operation | Return Type | Success Indicator |
|-----------|-------------|-------------------|
| `LogIn` | `void` | No exception |
| `SetProfileName` | `void` | No exception |
| `SetProfileIdentifier` | `void` | No exception |
| `SetProfilePicture` | `void` | No exception |
| `ActivateKey` | `string` | Returns product name |
| `FavouriteWorkshopItem` | `void` | No exception |
| `SubscribeToWorkshopItem` | `void` | No exception |
| `VisitChat` | `void` | No exception |
| `AcceptCookies` | `void` | No exception |
| `RejectCookies` | `void` | No exception |

### Failure Results

All failures expressed as exceptions:

| Exception Type | Operations | Programmatic Handling |
|----------------|------------|----------------------|
| `AuthenticationException` | `LogIn` | Catch, inspect `Message` |
| `KeyActivationException` | `ActivateKey` | Catch, inspect `Code` (enum) |
| `ArgumentNullException` | All (null params) | Validate before call |
| `IWebProcessor` exceptions | All (browser) | Catch base `Exception` |

---

## Pagination and Filtering

**Not Supported:** The library provides no pagination, filtering, sorting, or listing capabilities for any Steam content (workshop items, chat rooms, profile history, etc.).

**Workaround:** Users must perform discovery externally (Steam website, Steam Web API) and pass identifiers to library methods.

---

## Related Documentation

- [API Reference](../api-reference/steam-processor.md) — Method signatures
- [Workshop Interaction Flow](../flows/workshop-interaction.md) — Workshop operations detail
- [Chat Access Flow](../flows/chat-access.md) — Chat navigation detail
- [Profile Management Flow](../flows/profile-management.md) — Profile operations detail
- [Key Activation Flow](../flows/key-activation.md) — Key activation detail