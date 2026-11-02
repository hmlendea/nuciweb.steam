# Security Policy

This document describes the security policy for NuciWeb.Steam, including how to report vulnerabilities, which versions receive security maintenance, and the coordinated disclosure expectations.

## 📑 Table of Contents

- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | NuGet | ✅ |
| Latest version | GitHub Releases | ✅ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/nuciweb.steam/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

The subsequent report categories are in scope for this repository:
- Vulnerabilities in the NuciWeb.Steam library code that affect the confidentiality, integrity, or availability of Steam account automation flows.
- Security-relevant defects in the library's handling of credentials, secrets, or session data.

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in Steam services or third-party dependencies.
- Issues in downstream applications that integrate this library.

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.