# Security Policy

## Supported versions

TradeCopia is under active development and has **not** completed NinjaTrader simulation certification. Treat every build as pre-release software that can submit real brokerage orders once enabled.

| Version | Supported |
| --- | --- |
| `main` (Development / Alpha) | Security reports accepted |
| Tagged Alpha / pre-release | Security reports accepted |
| Stable 1.0+ | Supported after that release exists |

## Report a vulnerability

Do **not** open a public GitHub issue for security problems that could cause unauthorized order submission, privilege escalation, secret exposure, or localhost control-plane bypass.

Email or privately message the repository owner (`gattasrikanth` on GitHub) with:

- affected commit SHA or release tag;
- component (`Native`, `ControlPlane`, `Web`, IPC, install scripts);
- reproduction steps using **synthetic / SIM** accounts only;
- expected vs actual behavior;
- impact assessment.

We will acknowledge reports as quickly as practical and coordinate a fix before public disclosure.

## Product safety rules

- Copying starts **disabled**.
- The browser is a control plane, not an execution engine. There is no generic web API for placing discretionary trades.
- Default HTTP bind is loopback only (`127.0.0.1`).
- Automated tests must never place orders in a live account.
- Do not attach real account numbers, credentials, brokerage identifiers, or personal data to issues, PRs, logs, or screenshots.

## Local data

Runtime data belongs under the per-user application root (see `docs/architecture/SYSTEM-DESIGN.md`). Never commit `.env` files, SQLite databases, diagnostic bundles, or NinjaTrader proprietary assemblies.
