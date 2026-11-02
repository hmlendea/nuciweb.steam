# Inspect, Edit, Delete Behaviours

## Overview

This document describes user-facing behaviours for inspecting current state, editing existing data, and deleting/removing content through the NuciWeb.Steam API.

## Inspect Behaviours

### Inspect Authentication State

**Entry Point:** Implicit via `LogIn` early-return behaviour

**Behaviour:**
- `LogIn` checks if already authenticated on Steam Store
- If avatar + account pulldown visible, validates username matches
- Returns immediately if same user (no exception)
- Throws `AuthenticationException` if different user

**Limitations:**
- No standalone "check if logged in" method
- No way to inspect current session without attempting login
- No access to session cookies, expiry, or permissions

### Inspect Profile State

**Entry Point:** None (no read API)

**Behaviour:**
- No methods to read current profile name, identifier, or avatar
- Profile edits are write-only operations
- User must use browser or Steam Web API to inspect current state

### Inspect Key Activation History

**Entry Point:** None

**Behaviour:**
- No method to list previously activated keys
- No method to check if a key was already activated
- `ActivateKey` only reveals this via `AlreadyActivatedCurrentAccount` error

### Inspect Workshop State

**Entry Point:** Implicit via `FavouriteWorkshopItem` / `SubscribeToWorkshopItem` early returns

**Behaviour:**
- Both methods check current state before acting
- If already favourited/subscribed, return immediately (idempotent)
- No standalone "check if favourited" or "check if subscribed" methods
- No method to list favourited or subscribed items

### Inspect Chat State

**Entry Point:** None

**Behaviour:**
- `VisitChat` only navigates; no inspection of chat rooms, messages, or contacts
- No programmatic access to chat data

---

## Edit Behaviours

### Edit Profile Name

**Entry Point:** `SteamProcessor.SetProfileName(string name)`

**Behaviour:**
- Navigates to edit profile page
- Overwrites current name with new value
- No validation of name format, length, or uniqueness (delegates to Steam)
- No diff or preview — direct overwrite

**Constraints:**
- Steam enforces: max 32 chars, no profanity, uniqueness not required
- No rollback — previous name lost unless user remembers it

### Edit Profile Identifier (Custom URL)

**Entry Point:** `SteamProcessor.SetProfileIdentifier(string identifier)`

**Behaviour:**
- Navigates to edit profile page
- Overwrites current custom URL identifier
- No validation of format (alphanumeric, underscore, 2-32 chars)
- No availability check before submit (Steam validates server-side)

**Constraints:**
- Must be unique across Steam
- Changing breaks existing `steamcommunity.com/id/{identifier}` links
- No redirect from old identifier

### Edit Profile Picture (Avatar)

**Entry Point:** `SteamProcessor.SetProfilePicture(string imagePath)`

**Behaviour:**
- Navigates to edit profile → avatar tab
- Uploads new image, replacing current avatar
- Polls for upload completion (image URL change)
- No preview, no crop, no format conversion

**Constraints:**
- Steam enforces: JPG/PNG, max ~1MB, recommended 184x184px
- Animated GIFs not supported
- No rollback — previous avatar lost

### Edit Cookie Preferences

**Entry Points:** `SteamProcessor.AcceptCookies()` / `RejectCookies()`

**Behaviour:**
- Navigates to cookie preferences page
- Clicks "Accept All" or "Reject All"
- No granular control (individual cookie categories)
- No inspection of current preference state

---

## Delete/Remove Behaviours

### Remove Workshop Favourite

**Entry Point:** None

**Behaviour:**
- No method to unfavourite a workshop item
- `FavouriteWorkshopItem` is one-way (add only)
- User must manually unfavourite via Steam website

### Remove Workshop Subscription

**Entry Point:** None

**Behaviour:**
- No method to unsubscribe from a workshop item
- `SubscribeToWorkshopItem` is one-way (add only)
- User must manually unsubscribe via Steam website

### Delete Profile Picture

**Entry Point:** None

**Behaviour:**
- No method to remove avatar (revert to default)
- Only `SetProfilePicture` to replace
- User must manually delete via Steam website

### Clear Authentication Session

**Entry Point:** None

**Behaviour:**
- No logout method
- Session persists in `IWebProcessor` browser instance
- User must dispose `IWebProcessor` to clear cookies
- No programmatic cookie clearing

### Revoke Key Activation

**Entry Point:** None

**Behaviour:**
- No method to refund or revoke activated key
- Keys are permanently bound to account on activation
- Steam Support required for refunds (within policy)

---

## Summary Matrix

| Capability | Inspect | Edit | Delete |
|------------|---------|------|--------|
| Authentication state | Partial (via LogIn) | — | — |
| Profile name | ❌ | ✅ SetProfileName | ❌ |
| Profile identifier | ❌ | ✅ SetProfileIdentifier | ❌ |
| Profile picture | ❌ | ✅ SetProfilePicture | ❌ |
| Cookie preferences | ❌ | ✅ Accept/RejectCookies | ❌ |
| Key activation history | ❌ | — | ❌ |
| Workshop favourite | Partial (via method) | ✅ FavouriteWorkshopItem | ❌ |
| Workshop subscription | Partial (via method) | ✅ SubscribeToWorkshopItem | ❌ |
| Chat state | ❌ | — | — |
| Authentication session | ❌ | — | ❌ (dispose browser) |

---

## Design Rationale

### Write-Heavy API

The library is designed for **automation** (writing state) not **inspection** (reading state). This reflects the underlying browser automation approach:
- Reading requires parsing HTML, which is brittle
- Writing uses form submissions, which are more stable
- Steam Web API exists for read operations (not used by this library)

### Idempotent Writes

Workshop operations are idempotent (safe to repeat):
- `FavouriteWorkshopItem` checks before acting
- `SubscribeToWorkshopItem` checks before acting
- Profile operations are not idempotent (always overwrite)

### No Delete Operations

Deletion requires:
- Different UI flows (confirmation dialogs, different pages)
- Higher risk (irreversible)
- Less common in automation scenarios

### Session Management

Authentication session tied to `IWebProcessor` lifetime:
- No explicit logout — dispose browser to clear
- No session inspection — not needed for automation
- Single-threaded design assumes one session per processor

---

## Workarounds for Missing Capabilities

| Missing Capability | Workaround |
|-------------------|------------|
| Check if logged in | Call `LogIn` with dummy account, catch `AuthenticationException` |
| Read current profile name | Use browser automation directly via `IWebProcessor` |
| List activated keys | Use Steam Web API `IPlayerService/GetOwnedGames` |
| List workshop favourites | Use Steam Web API `IPublishedFileService/GetUserFiles` |
| Unfavourite workshop | Use `IWebProcessor` to navigate and click unfavourite |
| Unsubscribe workshop | Use `IWebProcessor` to navigate and click unsubscribe |
| Logout | `webProcessor.Dispose()` and create new `SteamProcessor` |

---

## Related Documentation

- [Browse and Search Behaviours](./browse-and-search.md) — Discovery operations
- [API Reference](../api-reference/steam-processor.md) — Method signatures
- [Profile Management Flow](../flows/profile-management.md) — Profile edit details
- [Workshop Interaction Flow](../flows/workshop-interaction.md) — Workshop operations
- [Integration Models](../components/integration-models.md#iwebprocessor-nuciweb) — Direct browser access