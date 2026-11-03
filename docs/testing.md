# Testing

## Overview

This document describes the testing strategy, test types, and testing practices for NuciWeb.Steam.

## Test Pyramid

```
        ┌─────────────┐
        │  E2E /      │  ← Manual only (requires Steam accounts)
        │  Integration│
        ├─────────────┤
        │  Component  │  ← Limited (processor logic with mocked IWebProcessor)
        │  Tests      │
        ├─────────────┤
        │   Unit      │  ← Error mapping, data models, pure logic
        │   Tests     │
        └─────────────┘
```

## Test Types

### Unit Tests (Automated)

**Location:** `NuciWeb.Steam.Tests/` (not yet created)

**Scope:** Pure logic, no browser, no network

**Targets:**
| Component | Test Cases |
|-----------|------------|
| `KeyActivationErrorCode` | Enum values, serialization |
| `KeyActivationException` | Constructors, Code property, serialization |
| `SteamAccount` | Property assignment, null handling |
| `SteamUrls` | URL format, string interpolation |
| `SteamKeyProcessor.MapErrorToCode` | All English/Romanian patterns → correct codes |
| `SteamAuthenticationProcessor.ValidateLogInResult` | Error display, Steam Guard error, success |
| `SteamAuthenticationProcessor.ValidateCurrentSession` | Username match, mismatch |

**Example:**
```csharp
[Test]
public void GivenInvalidProductCode_WhenMappingError_ThenReturnsInvalidProductKey()
{
    var processor = new SteamKeyProcessor(mockWeb.Object);

    var code = processor.MapErrorToCode("Invalid product code");

    Assert.That(code, Is.EqualTo(KeyActivationErrorCode.InvalidProductKey));
}

[Test]
public void GivenRomanianInvalidCode_WhenMappingError_ThenReturnsInvalidProductKey()
{
    var processor = new SteamKeyProcessor(mockWeb.Object);

    var code = processor.MapErrorToCode("Codul de produs este invalid");

    Assert.That(code, Is.EqualTo(KeyActivationErrorCode.InvalidProductKey));
}
```

### Component Tests (Semi-Automated)

**Scope:** Processor logic with mocked `IWebProcessor`

**Targets:**
| Processor | Test Cases |
|-----------|------------|
| `SteamAuthenticationProcessor` | Login flow, TOTP retry, captcha detection, session validation |
| `SteamKeyProcessor` | Key activation flow, error mapping, receipt parsing |
| `SteamProfileProcessor` | Name/identifier/avatar flows, file input reveal |
| `SteamWorkshopProcessor` | Favourite/subscribe, idempotency, required items modal |
| `SteamChatProcessor` | Navigation, wait condition |

**Example:**
```csharp
[Test]
public void GivenValidKey_WhenActivatingKey_ThenReturnsProductName()
{
    // Arrange
    var mockWeb = new Mock<IWebProcessor>();
    mockWeb.Setup(w => w.IsElementVisible(It.IsAny<Selector>()))
           .Returns(false);
    mockWeb.SetupSequence(w => w.IsElementVisible(receiptSelector))
           .Returns(true);
    mockWeb.Setup(w => w.GetText(productNameSelector))
           .Returns("Test Game");

    var processor = new SteamKeyProcessor(mockWeb.Object);

    // Act
    string result = processor.ActivateKey("VALID-KEY");

    // Assert
    Assert.That(result, Is.EqualTo("Test Game"));
    mockWeb.Verify(w => w.GoToUrl(SteamUrls.KeyActivation), Times.Once);
    mockWeb.Verify(w => w.SetText(productKeyInputSelector, "VALID-KEY"), Times.Once);
    mockWeb.Verify(w => w.UpdateCheckbox(ssaCheckboxSelector, true), Times.Once);
    mockWeb.Verify(w => w.Click(registerButtonSelector), Times.Once);
}
```

### Integration Tests (Manual)

**Scope:** Full browser automation against live Steam

**Requirements:**
- Valid Steam account with Steam Guard
- Test product keys (invalid, valid, already owned, DLC, region-locked)
- Workshop items for favourite/subscribe
- Network access to Steam

**Test Scenarios:**

| Scenario | Account | Key/Item | Expected |
|----------|---------|----------|----------|
| Valid login | With 2FA | N/A | Success |
| Invalid password | With 2FA | N/A | AuthenticationException |
| Captcha trigger | With 2FA | N/A | AuthenticationException (captcha) |
| Valid key activation | Logged in | Unused key | Product name |
| Invalid key | Logged in | "INVALID" | KeyActivationException (InvalidProductKey) |
| Already owned key | Logged in | Owned key | KeyActivationException (AlreadyActivatedCurrentAccount) |
| DLC without base | Logged in | DLC key | KeyActivationException (BaseProductRequired) |
| Rate limited | Logged in | Rapid keys | KeyActivationException (TooManyAttempts) |
| Profile name change | Logged in | N/A | Success |
| Profile identifier change | Logged in | N/A | Success |
| Avatar upload | Logged in | Valid image | Success |
| Workshop favourite | Logged in | Valid item ID | Success |
| Workshop subscribe | Logged in | Valid item ID | Success |
| Workshop required base | Logged in | DLC workshop item | Modal handled |
| Chat visit | Logged in | N/A | Success |

---

## Test Infrastructure

### Mocking IWebProcessor

```csharp
public interface IWebProcessor
{
    void GoToUrl(string url);
    void Click(Selector selector);
    void ClickAny(params Selector[] selectors);
    void SetText(Selector selector, string text);
    void AppendText(Selector selector, string text);
    void UpdateCheckbox(Selector selector, bool value);
    string GetText(Selector selector);
    string GetSource(Selector selector);
    void WaitForElementToBeVisible(Selector selector);
    void WaitForElementToExist(Selector selector);
    void WaitForAnyElementToBeVisible(params Selector[] selectors);
    bool IsElementVisible(Selector selector);
    bool AreAllElementsVisible(params Selector[] selectors);
    bool DoesElementExist(Selector selector);
    void ExecuteScript(string script);
    void Wait();
    void Wait(TimeSpan timeout);
    void Dispose();
}
```

**Mock Strategy:** Use `Moq` to setup selector interactions and return values.

### Test Data

| Data Type | Source | Management |
|-----------|--------|------------|
| Steam credentials | Secure store / env vars | Never committed |
| Test product keys | Steam partner / dev keys | Rotated regularly |
| Workshop item IDs | Public workshop items | Stable IDs preferred |
| Avatar images | Test fixtures | Small JPG/PNG |

---

## CI/CD Integration

### Current State

**No automated tests in CI** — all testing is manual.

### Recommended Pipeline

Use the canonical `dotnet.yml` workflow (see `.github/workflows/dotnet.yml`) for unit and component tests. Integration tests require secrets and manual trigger via `workflow_dispatch`.

---

## Test Coverage Goals

| Layer | Target | Current |
|-------|--------|---------|
| Unit (pure logic) | 90% | 0% (no test project) |
| Component (mocked) | 70% | 0% |
| Integration (live) | Key flows | Manual only |

---

## Testing Selectors

**Critical:** Selectors are the most brittle part. Test strategy:

1. **Unit test selector constants** — Verify XPath/ID strings match expected patterns
2. **Component test selector usage** — Verify correct selectors passed to `IWebProcessor`
3. **Integration test against live Steam** — Detect breakage early

```csharp
[Test]
public void GivenStoreLoginSelectors_WhenInspected_ThenAreNotEmpty()
{
    Assert.That(SteamAuthenticationProcessor.UsernameSelector, Is.Not.Empty);
    Assert.That(SteamAuthenticationProcessor.PasswordSelector, Is.Not.Empty);
}
```

---

## Known Testing Gaps

| Gap | Impact | Mitigation |
|-----|--------|------------|
| No test project | No automated regression | Create test project |
| No selector tests | UI changes break silently | Add selector validation tests |
| No integration CI | Regressions found in production | Manual test before release |
| No TOTP mock testing | Time-dependent logic untested | Add FixedSteamGuard mock |
| No error mapping tests | New Steam errors unmapped | Test MapErrorToCode exhaustively |

---

## Related Documentation

- [Error Handling](../error-handling.md) — Exception test cases
- [Data Model](../data-model.md) — Model test cases
- [Components: Application Services](../components/application-services.md) — Processor test targets
- [Build and Deployment](../build-and-deployment.md) — CI/CD integration