# Troubleshooting

## Overview

Common issues, causes, and resolutions for NuciWeb.Steam.

## Quick Diagnosis

| Symptom | Likely Cause | First Check |
|---------|--------------|-------------|
| `NoSuchElementException` | Selector outdated | Steam UI in browser dev tools |
| `KeyActivationException` (Unexpected) | New error message | `ex.Message` content |
| Login hangs | Captcha / 2FA wait | Visual inspection |
| `WebDriverException` | Browser/driver mismatch | Driver version, browser version |
| Timeout | Slow page load | Network, increase waits |
| Element not interactable | Overlay / animation | Wait longer, check overlays |

---

## Authentication Issues

### Problem: Login fails with "captcha detected"

**Cause:** Steam presented a captcha challenge.

**Resolution:**
1. Run once with visible browser to solve captcha manually
2. Use persistent Chrome profile (user data dir) to retain session
3. Avoid rapid login attempts from same IP

**Code:**
```csharp
var options = new WebProcessorOptions
{
    UserDataDir = @"C:\steam-profile",
    Headless = false // First run visible
};
using var web = new WebProcessor(options);
var steam = new SteamProcessor(web);
steam.LogIn(account); // Solve captcha manually
// Subsequent runs: Headless = true, same UserDataDir
```

### Problem: Login hangs indefinitely

**Cause:** Waiting for email/phone 2FA (no TOTP key provided).

**Resolution:**
- Provide `TotpKey` (Base32 shared secret) in `SteamAccount`
- Or run visible and complete 2FA manually

### Problem: "Invalid credentials" but credentials are correct

**Cause:**
- Account locked
- Steam Guard email/phone code required (no TOTP)
- Captcha not detected but present

**Resolution:**
- Check account status in browser
- Ensure `TotpKey` is correct Base32
- Try visible browser to see actual error

### Problem: TOTP code rejected

**Cause:**
- Code reused (library prevents this per session)
- Clock skew > 30s
- Wrong shared secret

**Resolution:**
- Verify system clock (NTP)
- Regenerate `TotpKey` from Steam
- Check `UsedSteamGuardCodes` logic

---

## Key Activation Issues

### Problem: `KeyActivationException` with `Unexpected`

**Cause:** Steam returned error message not in mapping table.

**Resolution:**
1. Catch exception, log `ex.Message`
2. Add mapping to `SteamKeyProcessor.MapErrorToCode`
3. Submit PR or update local fork

**Example:**
```csharp
try
{
    steam.ActivateKey(key);
}
catch (KeyActivationException ex)
{
    Console.WriteLine($"Unmapped: {ex.Message}");
    // Add to MapErrorToCode
}
```

### Problem: `AlreadyActivatedCurrentAccount` but key not in library

**Cause:** Key activated previously, or family sharing.

**Resolution:** Check Steam library, family sharing settings.

### Problem: `TooManyAttempts` error

**Cause:** Rate limited by Steam.

**Resolution:**
- Wait 15-30 minutes
- Add delays between activations (10s minimum)
- Use different IP if persistent

### Problem: `BaseProductRequired` for DLC

**Cause:** Account doesn't own base game.

**Resolution:** Activate base game key first, or purchase base game.

---

## Profile Management Issues

### Problem: `SetProfileName` fails silently

**Cause:** Name filtered (profanity, reserved, too long).

**Resolution:**
- Check name length (1-64 chars)
- Avoid filtered words
- Try in browser first

### Problem: `SetProfileIdentifier` fails

**Cause:** Identifier taken, invalid chars, too short.

**Resolution:**
- 3-32 chars, alphanumeric + underscore
- Check availability in browser
- Must be unique globally

### Problem: `SetProfilePicture` fails

**Cause:**
- File not found
- Invalid format (not JPG/PNG)
- File too large (>5MB)
- Hidden file input not revealed

**Resolution:**
- Verify file path exists
- Use JPG/PNG, square, <5MB
- Check JavaScript execution permission

---

## Workshop Issues

### Problem: `FavouriteWorkshopItem` doesn't work

**Cause:**
- Wrong workshop ID
- Item removed/hidden
- Already favourited (idempotent returns early)
- Selector changed

**Resolution:**
- Verify ID in URL: `steamcommunity.com/sharedfiles/filedetails/?id=12345`
- Check item exists in browser
- Update selector if needed

### Problem: `SubscribeToWorkshopItem` throws `BaseProductRequired`

**Cause:** Workshop item requires base game ownership.

**Resolution:** Own the base game first.

### Problem: Modal appears but not handled

**Cause:** New modal type (age gate, region, etc.)

**Resolution:**
- Run visible to see modal
- Add handler in `SteamWorkshopProcessor`
- Update selectors

---

## Chat Issues

### Problem: `VisitChat` times out

**Cause:**
- Chat UI slow to load
- Not logged into Community
- Selector `currentUserAvatar` changed

**Resolution:**
- Ensure `LogIn` completed successfully
- Increase wait timeout
- Check selector in browser

### Problem: Chat loads but avatar not found

**Cause:** Selector changed or chat UI redesigned.

**Resolution:** Update selector in `SteamChatProcessor`.

---

## Browser/WebDriver Issues

### Problem: `WebDriverException: session not created`

**Cause:** ChromeDriver version mismatch with Chrome browser.

**Resolution:**
- Update NuciWeb (manages driver)
- Or ensure Chrome and ChromeDriver versions match

### Problem: `WebDriverException: unknown error: cannot find Chrome binary`

**Cause:** Chrome not installed or not in PATH.

**Resolution:**
- Install Google Chrome
- Or configure `WebProcessorOptions.BrowserPath`

### Problem: Headless mode fails but headed works

**Cause:** Steam detects headless, serves different HTML, or captcha.

**Resolution:**
- Use headed for first login (solve captcha)
- Use persistent profile
- Add `--disable-blink-features=AutomationControlled`

### Problem: Elements not found in headless

**Cause:** Different DOM in headless (lazy loading, different CSS).

**Resolution:**
- Add explicit waits
- Scroll into view before interaction
- Compare headed vs headless DOM

---

## Network/Proxy Issues

### Problem: Navigation timeout

**Cause:** Slow network, proxy, firewall.

**Resolution:**
- Increase `IWebProcessor` navigation timeout
- Check proxy settings
- Test connectivity to steam domains

### Problem: Intermittent failures

**Cause:** Flaky network, Steam CDN issues.

**Resolution:**
- Add retry logic (consumer-side)
- Exponential backoff
- Log failures for pattern analysis

---

## Debugging Techniques

### 1. Run Visible

```csharp
var options = new WebProcessorOptions { Headless = false };
using var web = new WebProcessor(options);
// Watch browser, see what happens
```

### 2. Capture Page Source

```csharp
var html = webProcessor.GetPageSource();
File.WriteAllText("debug.html", html);
// Open in browser, inspect
```

### 3. Screenshot on Failure

```csharp
try
{
    steam.ActivateKey(key);
}
catch
{
    webProcessor.TakeScreenshot("failure.png");
    throw;
}
```

### 4. Enable WebDriver Logs

```csharp
var options = new WebProcessorOptions
{
    DriverLogPath = "chromedriver.log",
    VerboseLogging = true
};
```

---

## Getting Help

1. Check [FAQ](../faq.md)
2. Search existing issues
3. Create issue with:
   - Exception type and message
   - Stack trace
   - `ex.Message` for `KeyActivationException`
   - Page source / screenshot
   - Steps to reproduce
   - Environment (.NET version, OS, Chrome version)

---

## Related Documentation

- [FAQ](../faq.md) — Common questions
- [Change Guide](../change-guide.md) — Fixing selectors
- [Error Handling](../error-handling.md) — Exception types
- [Behaviour: Browse and Search](../behaviour/browse-and-search.md) — Selector details