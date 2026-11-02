# FAQ

## General

### What is NuciWeb.Steam?

A .NET 10 library for automating Steam account operations via browser automation. It provides a synchronous API for login, key activation, profile management, workshop interaction, and chat access.

### What does it depend on?

- **NuciWeb 4.1.0** — Browser automation abstraction (`IWebProcessor`)
- **NuciWeb.Automation 1.0.1** — Selector helpers
- **NuciExtensions 6.0.2** — Extension methods
- **SteamGuard.TOTP 1.1.1** — TOTP code generation

### Is it an official Steam library?

No. It automates the Steam website (store.steampowered.com, steamcommunity.com) using browser automation. It is not affiliated with Valve.

### What .NET version is required?

.NET 10.0 (net10.0 target framework).

### Is it thread-safe?

No. `SteamProcessor` and `IWebProcessor` are single-threaded. Use one instance per thread, or multiple instances each with their own `IWebProcessor`.

---

## Authentication

### How does login work?

1. Navigate to store.steampowered.com/login
2. Enter username/password
3. If Steam Guard enabled: generate TOTP from `TotpKey` (Base32 shared secret)
4. Submit 2FA code
5. Navigate to steamcommunity.com/login to establish Community session
6. Validate session via avatar element

### What if I don't have TOTP?

Set `TotpKey = null` on `SteamAccount`. The processor will wait for email/phone 2FA (manual intervention required) or proceed if 2FA disabled.

### Can I use backup codes?

Not directly. The library only supports TOTP via `TotpKey`. For backup codes, you would need to handle the 2FA page manually or extend `SteamAuthenticationProcessor`.

### Why does login sometimes fail with "captcha"?

Steam presents captchas on suspicious login attempts (new IP, many failures). The library detects captcha presence and throws. Solution: solve manually once, then reuse browser profile (see State and Persistence).

### Can I reuse a logged-in session?

Yes. Configure `IWebProcessor` with a persistent user data directory (Chrome profile). First run logs in; subsequent runs skip login if session valid.

---

## Key Activation

### What key formats are supported?

Standard Steam product keys: `AAAAA-BBBBB-CCCCC` (5-5-5 alphanumeric).

### What errors can occur?

| Code | Meaning |
|------|---------|
| `InvalidProductKey` | Key format wrong or doesn't exist |
| `AlreadyActivatedDifferentAccount` | Key used on another account |
| `AlreadyActivatedCurrentAccount` | Key already on this account |
| `BaseProductRequired` | DLC requires base game owned |
| `RegionLocked` | Key not valid in account region |
| `TooManyAttempts` | Rate limited |
| `Unexpected` | Unmapped error |

### Why does it throw `KeyActivationException`?

All key activation failures throw this typed exception with `Code` property for programmatic handling.

### Can I activate multiple keys?

Yes, call `ActivateKey` sequentially. Add delays (10s) between calls to avoid rate limits.

---

## Profile Management

### What profile fields can I change?

- Display name (`SetProfileName`)
- Custom URL identifier (`SetProfileIdentifier`)
- Avatar image (`SetProfilePicture` — file path)

### Are there limits?

- Name: 1-64 characters, Steam filters profanity
- Identifier: 3-32 alphanumeric/underscore, unique
- Avatar: JPG/PNG, max ~5MB, square recommended

### Why does avatar upload use JavaScript?

The file input is hidden (`display: none`). Library uses `ExecuteScript` to reveal it, then sends file path.

---

## Workshop

### What's the difference between favourite and subscribe?

- **Favourite:** Adds to your "Favourites" collection (heart icon)
- **Subscribe:** Downloads content for games that support it (Subscribe button)

### Why does it check state first?

Idempotency — calling twice has same effect as once. Prevents UI toggles and errors.

### What is "base product required"?

Some workshop items require owning the base game. Library detects modal and throws `KeyActivationErrorCode.BaseProductRequired` (reused for workshop).

---

## Chat

### What does `VisitChat` do?

Navigates to `steamcommunity.com/chat` and waits for `currentUserAvatar` element (indicates chat UI loaded).

### Can I send messages?

No. Only navigation is implemented. Sending messages requires additional DOM interaction not yet supported.

---

## Errors and Troubleshooting

### `NoSuchElementException` — what now?

Steam UI changed. Update selector in relevant processor (see Change Guide).

### `KeyActivationException` with `Unexpected` — what now?

New error message not mapped. Capture `ex.Message`, add to `MapErrorToCode` in `SteamKeyProcessor`.

### Login hangs — why?

- Captcha (check visually)
- 2FA waiting for email/phone (no TOTP key)
- Network/proxy issues
- Selector changed

### "Element not interactable" — why?

Element exists but not clickable (overlay, animation, not visible). Increase wait or check for overlays.

---

## Development

### How do I run tests?

No automated test project yet. Manual integration tests against live Steam (see Testing docs).

### How do I add a new feature?

1. Add method to `ISteamProcessor`
2. Implement in `SteamProcessor` (delegate to processor)
3. Create/extend processor
4. Add selectors, flow, error handling
5. Update documentation
6. Test manually

### Can I mock `IWebProcessor` for unit tests?

Yes. `SteamProcessor` accepts `IWebProcessor` in constructor. Use Moq to mock.

---

## Licensing

### What license?

GPL-3.0-or-later. See LICENSE file.

### Can I use in commercial project?

GPL requires your project to be GPL-compatible. Consult legal counsel.

---

## Related Documentation

- [Quick Start](../quick-start.md) — 5-minute guide
- [Troubleshooting](../troubleshooting.md) — Problem resolution
- [Change Guide](../change-guide.md) — Making changes
- [API Usage Examples](../api-usage-examples.md) — Code samples