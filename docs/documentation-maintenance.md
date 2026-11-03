# Documentation Maintenance

## Overview

This document describes how to maintain the documentation in `docs/` and keep it synchronized with code.

## Documentation Structure

```
docs/
├── INDEX.md                      # Navigation hub
├── repository-overview.md        # High-level summary
├── repository-structure.md       # File tree, conventions
├── architecture.md               # System architecture
├── components/
│   ├── presentation.md           # Public API (ISteamProcessor)
│   ├── host-and-composition.md   # SteamProcessor, DI
│   ├── application-services.md   # Internal processors
│   └── integration-models.md     # Data models, external contracts
├── flows/
│   ├── startup-and-rendering.md  # Initialization
│   ├── authentication.md         # Login, 2FA, session
│   ├── key-activation.md         # Key activation flow
│   ├── profile-management.md     # Profile updates
│   ├── workshop-interaction.md   # Favourite/subscribe
│   └── chat-access.md            # Chat navigation
├── api-reference/
│   ├── INDEX.md                  # API index
│   └── steam-processor.md        # ISteamProcessor reference
├── behaviour/
│   ├── browse-and-search.md      # Navigation, waits
│   └── inspect-edit-delete.md    # DOM interaction
├── data-model.md                 # SteamAccount, enums, exceptions
├── dependencies.md               # Package references
├── configuration.md              # Injection, options
├── error-handling.md             # Exceptions, codes
├── logging.md                    # Logging approach
├── security.md                   # Credentials, secrets
├── testing.md                    # Test strategy
├── build-and-deployment.md       # CI/CD, packaging
├── change-guide.md               # How to make changes
├── design-decisions.md           # ADR log
├── invariants.md                 # Must-hold conditions
├── integrations.md               # External systems
├── state-and-persistence.md      # Session, cookies, memory
├── concurrency-and-scheduling.md # Threading, scheduling
├── documentation-maintenance.md  # This file
├── faq.md                        # Common questions
├── troubleshooting.md            # Problem resolution
├── quick-start.md                # 5-minute guide
├── api-usage-examples.md         # Code samples
└── ambiguities-and-open-questions.md # Known gaps
```

## Maintenance Rules

### 1. Code Changes Require Doc Updates

| Code Change | Required Doc Updates |
|-------------|---------------------|
| New public method | `api-reference/steam-processor.md`, `components/presentation.md` |
| New processor | `components/application-services.md`, `architecture.md` |
| New error code | `error-handling.md`, `data-model.md`, `api-reference/steam-processor.md` |
| Selector change | `behaviour/browse-and-search.md`, relevant flow doc |
| New dependency | `dependencies.md`, `integrations.md` |
| Config change | `configuration.md` |
| Breaking change | `change-guide.md`, `architecture.md`, release notes |

### 2. Documentation Standards

- **Format:** Markdown (`.md`)
- **Naming:** kebab-case (e.g., `key-activation.md`)
- **Headings:** ATX style (`#`, `##`, `###`)
- **Code blocks:** Language-tagged (```csharp, ```bash, ```yaml)
- **Links:** Repository-relative (`../architecture.md`)
- **Tables:** For structured data (APIs, versions, checklists)

### 3. Cross-Reference Integrity

- Every file linked from `INDEX.md` must exist
- No dead internal links
- `INDEX.md` updated when files added/removed
- Section anchors stable (lowercase, hyphens)

---

## Update Workflow

### When Adding a File

1. Create file in correct directory
2. Add entry to `INDEX.md` (appropriate section)
3. Add cross-references from related files
4. Verify links: `markdown-link-check` or manual

### When Modifying a File

1. Update content
2. Check cross-references still valid
3. Update `INDEX.md` if structure changed
4. Update `CHANGELOG.md` if user-facing

### When Removing a File

1. Remove file
2. Remove from `INDEX.md`
3. Remove cross-references from other files
4. Update `CHANGELOG.md`

---

## Automation

### Link Checking

```bash
# Install
npm install -g markdown-link-check

# Run
markdown-link-check docs/**/*.md
```

### Spell Checking

```bash
# Install
npm install -g cspell

# Run
cspell "docs/**/*.md"
```

### Linting

```bash
# Install
npm install -g markdownlint-cli

# Run
markdownlint "docs/**/*.md"
```

---

## Review Checklist

### Per PR

- [ ] New/changed code has corresponding doc updates
- [ ] `INDEX.md` reflects current structure
- [ ] No broken internal links
- [ ] Code samples compile (copy-paste test)
- [ ] Version numbers updated if applicable
- [ ] Cross-references use relative paths

### Quarterly

- [ ] Run link checker
- [ ] Run spell checker
- [ ] Review `ambiguities-and-open-questions.md` for resolution
- [ ] Verify `quick-start.md` works end-to-end
- [ ] Check `api-usage-examples.md` against current API

---

## Documentation Debt

Track in `ambiguities-and-open-questions.md`:

- [ ] Workshop flow: required base product modal handling details
- [ ] Chat flow: what "chat loaded" means precisely
- [ ] Profile picture: file format/size limits
- [ ] TOTP: backup code handling
- [ ] Rate limits: exact Steam thresholds

---

## Related Documentation

- [INDEX](../INDEX.md) — Navigation
- [Change Guide](../change-guide.md) — Change process
- [Architecture](../architecture.md) — System context
- [Repository Structure](../repository-structure.md) — File conventions