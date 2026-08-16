# Changelog

All notable changes to TradeCopia are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project will use [Semantic Versioning](https://semver.org/) once the
first numbered release exists.

## [Unreleased]

### Added

- Shipped engine runtime hosts the OS named-pipe server; control plane retries attach and surfaces live `engineState` / `copyingEnabled`.
- Protocol session applies pause / disable / resume to the observable snapshot (not a Heartbeat ACK stub).

- Public repository bootstrap with product, architecture, and agent-mandate documents.
- Apache-2.0 license and open-source governance files.
- Agent continuity files and resume script.
- Baseline GitHub Actions, Dependabot, and CodeQL configuration.
- Phase 1 ADRs for naming, event semantics, TFMs, IPC, packaging, and control plane.
- Deterministic domain copy coordinator, sizing, topology validation, and origin registry.
- Length-prefixed IPC framing types.
- Disabled native order executor (no submission).
- FakeNinjaTrader harness and expanding automated tests.
- Native AddOn compiles against local NinjaTrader 8.1.8.2 (`net481`) with order submission disabled.
- Loopback control plane, CSRF/host/origin defenses, demo dashboard SPA.
- Local SQLite persistence using a patched native SQLite package.
- Protocol session handshake, reconnect, and explicit ExecuteOrder rejection.
- Simulation-guarded executor (Unknown is not simulation).
- Engine restart leaves copying disabled.
- Playwright dashboard flow against synthetic demo data.
- `scripts/package.ps1` Alpha packaging (no NinjaTrader binaries).
