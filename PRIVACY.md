# Privacy and Personal Data

This document describes how NuciWeb.Steam at https://github.com/hmlendea/nuciweb.steam handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 📑 Table of Contents

- [What This Document Covers](#what-this-document-covers)
- [Self-Hosted Deployments](#self-hosted-deployments)
- [Data We Handle](#data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#processing-and-use)
- [Storage, Retention, and Deletion](#storage-retention-and-deletion)
- [External Processing and Integrations](#external-processing-and-integrations)
- [Data Protection and Security](#data-protection-and-security)
- [Document Changes](#document-changes)
- [Contact](#contact)

## 🔎 What This Document Covers

This document describes how NuciWeb.Steam at https://github.com/hmlendea/nuciweb.steam handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

NuciWeb.Steam is a .NET library distributed as a NuGet package. Developers integrate it into their own applications and control the deployment, configuration, and execution of those applications. This document covers the library behaviour only. Instance operators control their instance's configuration, local storage, logs, backups, access controls, retention, and request handling.

The library does not send any data from a self-hosted instance to project maintainers. All network communication is directed to Steam services as required by the Steam features being automated. No telemetry, update checks, crash reports, email, authentication, reverse-proxy, object-storage, or monitoring data are transmitted to project maintainers or any third party by the library itself.

## 📥 Data We Handle

### Data Provided to the Application

- Steam account credentials: username, password, and Steam Guard shared secret (for TOTP generation) — provided by users, administrators, or connected applications to authenticate with Steam.
- Profile data: display name, custom profile URL, and avatar image — provided by users, administrators, or connected applications when updating a Steam profile.
- Product keys — provided by users, administrators, or connected applications when activating Steam product keys.
- Workshop item identifiers — provided by users, administrators, or connected applications when favoriting or subscribing to Workshop items.

### Data Generated or Collected by the Application

- HTTP request and response data exchanged with Steam services during automation flows — generated automatically during application operation.
- Steam Guard TOTP codes — generated locally from the provided shared secret using the SteamGuard.TOTP library; not transmitted outside the Steam authentication flow.
- No personal data is generated or collected automatically beyond the transient in-memory objects required for each operation.

### Data Received from Integrations

- Steam account data returned by Steam services (profile information, inventory, chat messages, Workshop details) — received from Steam during automation flows. No personal data is received from integrations or third parties other than Steam.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Steam authentication (sign-in, Steam Guard TOTP) — Steam account credentials, Steam Guard shared secret
- Cookie preference handling — HTTP cookies from Steam
- Steam Chat access — Steam account credentials, chat data returned by Steam
- Product key activation — Steam account credentials, product keys
- Profile updates (name, custom URL, avatar) — Steam account credentials, profile data
- Workshop interactions (favorite, subscribe) — Steam account credentials, Workshop item identifiers

## 🗄️ Storage, Retention, and Deletion

The application does not persist any personal data. All relevant data categories are stored only as transient in-memory objects during operation execution. No local databases, files, logs, caches, backups, or configured storage services are populated with personal data by the library. For self-hosted deployments, the instance operator controls storage, deletion, and backups.

## 🔗 External Processing and Integrations

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Steam Community (steamcommunity.com) | Web-based Steam features (login, chat, profile, workshop) | Steam account credentials, profile data, chat data, Workshop data | https://steamcommunity.com |
| Steam Store (store.steampowered.com) | Product key activation | Steam account credentials, product keys | https://store.steampowered.com |
| Steam Web API (api.steampowered.com) | Official Steam Web API endpoints | Steam account credentials, API responses | https://steamcommunity.com/dev |

The application has no built-in external data transfer to any service other than Steam.

## 🛡️ Data Protection and Security

The application uses technical safeguards including HTTPS for all communication with Steam services. Credentials and secrets are handled as in-memory data structures and are not logged, serialised, or persisted by the library. For self-hosted deployments, the operator is responsible for application updates, secrets management, access controls, backups, network exposure, and log protection. Absolute security is not promised.

The original maintainers are not responsible for changes, modifications, or privacy issues introduced by downstream forks.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/nuciweb.steam/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers via GitHub issues at https://github.com/hmlendea/nuciweb.steam/issues. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the library version and the Steam feature being used; do not send passwords, access tokens, or other secrets.