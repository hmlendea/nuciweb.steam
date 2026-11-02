# Ambiguities and Open Questions

## Overview

Known gaps, unresolved design questions, and areas requiring clarification in NuciWeb.Steam.

## Open Questions

### 1. Workshop Base Product Modal Handling

**Issue:** `SteamWorkshopProcessor` detects "base product required" modal but throws `KeyActivationErrorCode.BaseProductRequired` (reused from key activation).

**Questions:**
- Should this be a separate `WorkshopErrorCode` enum?
- Does the modal always appear the same way?
- Can user dismiss and continue, or is it hard block?
- What if user owns base game but Steam doesn't detect it?

**Current State:** Throws reused error code, no modal dismissal logic.

---

### 2. Chat "Loaded" Definition

**Issue:** `VisitChat` waits for `currentUserAvatar` element.

**Questions:**
- Is `currentUserAvatar` a reliable indicator of chat readiness?
- Does chat load asynchronously after this element?
- Should we wait for conversation list, search box, or specific UI?
- What if user has no avatar?

**Current State:** Single element wait, no validation of chat functionality.

---

### 3. Profile Picture Constraints

**Issue:** `SetProfilePicture` accepts file path, uses JS to reveal hidden input.

**Questions:**
- Exact file size limit? (Observed ~5MB, not documented)
- Supported formats? (JPG/PNG confirmed, WebP? GIF?)
- Dimension requirements? (Square? Min/max pixels?)
- Aspect ratio handling? (Crop? Stretch? Letterbox?)
- Animated avatar support?

**Current State:** No validation, relies on Steam server rejection.

---

### 4. TOTP Backup Codes

**Issue:** Library only supports TOTP via `TotpKey` (shared secret).

**Questions:**
- Should backup code support be added?
- How to detect backup code prompt vs TOTP prompt?
- Store used backup codes to prevent reuse?
- Fallback chain: TOTP → backup code → email/phone?

**Current State:** No backup code support. Email/phone 2FA requires manual intervention.

---

### 5. Rate Limit Thresholds

**Issue:** Steam rate limits are undocumented and observed empirically.

**Questions:**
| Operation | Observed Limit | Confirmed? |
|-----------|----------------|------------|
| Login attempts | ~5/hour/IP | No |
| Key activation | ~10/minute | No |
| Profile changes | ~5/minute | No |
| Workshop actions | ~20/minute | No |
| Chat navigation | Unknown | No |

**Current State:** Conservative delays in examples (10s keys, 30s profile). No programmatic rate limit handling.

---

### 6. Selector Stability Strategy

**Issue:** All selectors are hardcoded constants, brittle to Steam UI changes.

**Questions:**
- Should we adopt a selector registry with versioning?
- Multiple fallback selectors per element?
- Automated selector health checks?
- Visual regression testing for Steam pages?
- Community-maintained selector database?

**Current State:** Single selector per element, manual update on breakage.

---

### 7. Multi-Factor Authentication Variants

**Issue:** Steam supports multiple 2FA methods.

**Questions:**
| Method | Supported? | Detection? |
|--------|------------|------------|
| TOTP (Steam Guard Mobile) | Yes | `TotpKey` provided |
| Email code | Partial | No TOTP key → waits |
| Phone/SMS code | Partial | No TOTP key → waits |
| Backup codes | No | — |
| Steam Guard Mobile Confirmations | No | — |

**Current State:** TOTP only. Others require manual/visible browser.

---

### 8. Regional/Localisation Coverage

**Issue:** Error message mapping only covers English and Romanian.

**Questions:**
- Which locales does Steam support?
- Should we crowdsource mappings?
- Machine translation for unmapped locales?
- Fallback strategy beyond `Unexpected`?

**Current State:** English + Romanian only. Others → `Unexpected`.

---

### 9. Session Validation

**Issue:** No method to check if session is still valid without operation.

**Questions:**
- Add `IsSessionValid()` / `CheckSession()` method?
- Lightweight check (single request) vs full navigation?
- Automatic re-login on expiry?
- Event/callback for session expiry?

**Current State:** Operations fail with element not found if session expired.

---

### 10. Proxy and Network Configuration

**Issue:** `IWebProcessor` configuration is external (NuciWeb).

**Questions:**
- Document proxy setup for NuciWeb?
- Per-request proxy rotation?
- Tor/onion support for Steam?
- Custom DNS, certificate handling?

**Current State:** Delegated to NuciWeb configuration.

---

### 11. Headless Detection Avoidance

**Issue:** Steam serves different HTML or captchas in headless mode.

**Questions:**
- Standard stealth arguments for NuciWeb?
- `--disable-blink-features=AutomationControlled` sufficient?
- User agent rotation?
- Canvas/WebGL fingerprinting?
- Behavioral patterns (mouse, scroll, timing)?

**Current State:** Basic headless works sometimes. First login headed recommended.

---

### 12. Concurrent Account Operations

**Issue:** Single-threaded design limits throughput.

**Questions:**
- Official guidance for multi-account automation?
- Shared `IWebProcessor` pool?
- Browser context isolation (profiles)?
- Resource limits (RAM, CPU per browser)?

**Current State:** One `SteamProcessor` + `IWebProcessor` per account, sequential.

---

### 13. Key Activation Result Details

**Issue:** `ActivateKey` returns product name string only.

**Questions:**
- Return structured result (product ID, type, DLC vs game)?
- Include activation timestamp?
- Include remaining activations (for gift keys)?
- Parse "duplicate" vs "already owned" distinction?

**Current State:** String product name from page.

---

### 14. Workshop Item Metadata

**Issue:** No method to query workshop item details before action.

**Questions:**
- Add `GetWorkshopItemInfo(id)` method?
- Return title, author, file size, tags, base game?
- Validate item exists before favourite/subscribe?
- Cache metadata?

**Current State:** Blind operation, fails if item missing.

---

### 15. Profile Custom URL Availability

**Issue:** `SetProfileIdentifier` fails if taken, no pre-check.

**Questions:**
- Add `IsIdentifierAvailable(string)` method?
- Suggest alternatives on conflict?
- Reserve identifier temporarily?

**Current State:** Try and catch failure.

---

## Resolved (Documented Elsewhere)

| Question | Resolution | Location |
|----------|------------|----------|
| Thread safety | Single-threaded by design | Concurrency and Scheduling |
| Browser ownership | Consumer owns `IWebProcessor` | State and Persistence |
| Logging | No internal logging | Logging |
| Configuration | Constructor injection only | Configuration |
| Error codes | `KeyActivationErrorCode` enum | Error Handling |
| Dependencies | 4 NuGet packages | Dependencies |

---

## Priority for Resolution

| Priority | Items |
|----------|-------|
| High | 1, 2, 3, 4, 9 (block common workflows) |
| Medium | 5, 6, 7, 8, 11 (improve reliability) |
| Low | 10, 12, 13, 14, 15 (enhancements) |

---

## Contributing Answers

If you can answer any of these:

1. Test against live Steam
2. Document findings in relevant doc (troubleshooting, behaviour, etc.)
3. Submit PR with code fix if applicable
4. Update this file to mark resolved

---

## Related Documentation

- [Troubleshooting](../troubleshooting.md) — Symptom-based help
- [FAQ](../faq.md) — Common questions
- [Change Guide](../change-guide.md) — Fixing issues
- [Behaviour: Browse and Search](../behaviour/browse-and-search.md) — Selector details