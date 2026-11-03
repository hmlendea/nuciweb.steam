# NuciWeb.Steam Documentation

Master index and navigation for the NuciWeb.Steam documentation.

## Repository Summary

NuciWeb.Steam is a .NET library that provides high-level Steam account automation flows on top of NuciWeb (`IWebProcessor`). It automates common Steam operations including authentication (with Steam Guard TOTP), cookie preference management, chat access, product key activation (with typed error handling), profile updates (name, custom URL, avatar), and Workshop interactions (favorite, subscribe). The library targets .NET 10.0+ and is distributed as a NuGet package.

## Root Document Map

- [ARCHITECTURE.md](../ARCHITECTURE.md) — High-level architecture, components, flows, and design decisions
- [SECURITY.md](../SECURITY.md) — Security policy, vulnerability reporting, supported versions
- [PRIVACY.md](../PRIVACY.md) — Data handling, retention, privacy details
- [CONTRIBUTING.md](../CONTRIBUTING.md) — Contribution guidelines, coding standards
- [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) — Community standards
- [LICENSE](../LICENSE) — GNU GPL v3.0 or later
- [README.md](../README.md) — Project overview, installation, usage

## Documentation Catalogue

### Getting Started
- [Quick Start](./quick-start.md) — Getting started guide for newcomers
- [Repository Overview](./repository-overview.md) — Repository purpose, scope, entry points
- [Repository Structure](./repository-structure.md) — Source tree layout, module organisation

### Architecture & Design
- [Architecture](./architecture.md) — High-level architecture (complements root ARCHITECTURE.md)
- [Components](./components/) — Per-component deep dives
  - [Presentation](./components/presentation.md) — Public API facade and models
  - [Host and Composition](./components/host-and-composition.md) — SteamProcessor composition and lifetime
  - [Application Services](./components/application-services.md) — Domain processors (auth, profile, keys, chat, workshop)
  - [Integration Models](./components/integration-models.md) — External integration boundaries
- [Design Decisions](./design-decisions.md) — Key architectural/design choices and rationale
- [Dependencies](./dependencies.md) — External and internal dependencies

### Execution Flows
- [Flows](./flows/) — End-to-end execution flows
  - [Startup and Initialisation](./flows/startup-and-rendering.md) — Processor construction and first operation
  - [Authentication Flow](./flows/authentication.md) — Login, Steam Guard, session validation
  - [Key Activation Flow](./flows/key-activation.md) — Product key activation with error mapping
  - [Profile Management Flow](./flows/profile-management.md) — Name, identifier, avatar updates
  - [Workshop Interaction Flow](./flows/workshop-interaction.md) — Favourite and subscribe operations
  - [Chat Access Flow](./flows/chat-access.md) — Steam Chat navigation

### API Reference
- [API Reference Index](./api-reference/INDEX.md) — Endpoint summary table and navigation
- [SteamProcessor API](./api-reference/steam-processor.md) — Public API methods and contracts

### Data & State
- [Data Model](./data-model.md) — Domain entities, relationships, persistence
- [State and Persistence](./state-and-persistence.md) — State management, stores, caches, migrations
- [Configuration](./configuration.md) — Configuration schema, sources, precedence

### Cross-Cutting Concerns
- [Error Handling](./error-handling.md) — Error taxonomy, handling patterns, recovery
- [Logging](./logging.md) — Logging framework, levels, structured fields, correlation, sinks
- [Security](./security.md) — Security model, threats, mitigations (complements root SECURITY.md)
- [Concurrency and Scheduling](./concurrency-and-scheduling.md) — Threading, async, schedulers, locks
- [Integrations](./integrations.md) — External system integrations
- [Invariants](./invariants.md) — System-wide invariants and contracts

### Operations
- [Build and Deployment](./build-and-deployment.md) — Build pipeline, deployment, environments
- [Testing](./testing.md) — Test strategy, organisation, coverage
- [Change Guide](./change-guide.md) — How to modify common areas safely
- [Documentation Maintenance](./documentation-maintenance.md) — How to keep docs current, ownership
- [Troubleshooting](./troubleshooting.md) — Common issues and solutions
- [FAQ](./faq.md) — Frequently asked questions

### Examples & Reference
- [API Usage Examples](./api-usage-examples.md) — Example requests/responses for API usage
- [Ambiguities and Open Questions](./ambiguities-and-open-questions.md) — Unresolved items, TODOs, known gaps

## Navigation Aids

| Audience | Start Here |
|----------|------------|
| Newcomers | [Quick Start](./quick-start.md) → [Repository Overview](./repository-overview.md) → [Architecture](./architecture.md) |
| Component Owners | [Components](./components/) → [Design Decisions](./design-decisions.md) → [Change Guide](./change-guide.md) |
| Debuggers | [Flows](./flows/) → [Error Handling](./error-handling.md) → [Troubleshooting](./troubleshooting.md) |
| API Consumers | [API Reference Index](./api-reference/INDEX.md) → [API Usage Examples](./api-usage-examples.md) |

## Maintenance Metadata

- **Last Reviewed:** 2026-11-02
- **Owner:** Horațiu Mlendea
- **Coverage Status:** Initial generation — comprehensive coverage of all components, flows, and cross-cutting concerns
- **Related Repository:** https://github.com/hmlendea/nuciweb.steam