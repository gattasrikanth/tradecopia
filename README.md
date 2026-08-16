# TradeCopia

Modern, local-first, open-source multi-account trade copying for NinjaTrader 8.

**Status: Development.** Automated construction is in progress. This software
is **not** live-certified. It can submit real orders once enabled. Validate
only in NinjaTrader simulation until the published SIM certification matrix
has actually passed.

> Automated tests passing does **not** make TradeCopia live-certified.
> The copier does not guarantee identical fills, zero latency, or compliance
> with any prop-firm rule set.

## What it is

TradeCopia observes eligible leader-account order activity inside NinjaTrader 8
and mirrors it to configured follower accounts according to explicit rules.

- Native execution engine inside NinjaTrader (the only component that may
  submit, change, or cancel follower orders).
- Local control plane on the same Windows machine.
- Modern loopback browser dashboard (`http://127.0.0.1:<port>`).
- Journal, analytics, latency instrumentation, and diagnostics — all local.
- Copying starts **disabled**.

Closing the browser must not stop copying. The dashboard is a control plane,
not the execution path.

## Why local and open source

- No SaaS account, license server, or cloud runtime.
- No trade data uploaded by default.
- Source, tests, and design documents are auditable.

## Architecture

```text
NinjaTrader 8  --named pipe-->  Control plane (.NET)  --loopback HTTP-->  Browser SPA
   execution engine                 config, journal, API                    observe/configure
```

See `docs/architecture/SYSTEM-DESIGN.md` for the locked design.

## Current development status

| Area | Status |
| --- | --- |
| Repository / governance | Done |
| Shared domain / fake engine | Domain coverlet gate met (line >= 95, branch >= 90) |
| Native NinjaTrader AddOn | Compiles locally against NT 8.1.8.2; hosts named pipe; **no order submit** |
| Local control plane | Loopback API + security + retry-attach to engine pipe |
| Browser dashboard | Local SPA at `http://127.0.0.1:17841` (live engine snapshot fields) |
| Manual NT SIM certification | Not started |

## Install (Alpha)

Download `TradeCopia-Setup-*.exe` from GitHub Releases. Close NinjaTrader, run setup, launch NinjaTrader, then open TradeCopia from the Start menu. Copying starts **disabled**. Do not install into an OneDrive-synchronized NinjaTrader Documents folder.

Developer fallback:

```powershell
pwsh ./scripts/test.ps1
pwsh ./scripts/run-control-plane.ps1
```

Then open `http://127.0.0.1:17841`. There is no generic order-entry API.

```powershell
pwsh ./scripts/test-e2e.ps1   # Playwright against demo data
pwsh ./scripts/package.ps1    # Alpha control-plane publish (no NT binaries)
```

**Alpha / automated tests only; manual NinjaTrader SIM certification required before live use.**

## Privacy

TradeCopia stores configuration and journal data on the local machine only
(under `%LOCALAPPDATA%\TradeCopia\` once the control plane exists). Default
telemetry is **none**. Do not commit real account identifiers.

## Supported environment (target)

- Windows x64
- NinjaTrader 8 Desktop (detected locally during Phase 1: 8.1.x)
- Modern Chromium-based or Firefox browser for the dashboard

## Documentation

- Product: `docs/product/PRD.md`
- Architecture: `docs/architecture/SYSTEM-DESIGN.md`
- Contributing: `CONTRIBUTING.md`
- Security: `SECURITY.md`

## Contributing and security

Read `CONTRIBUTING.md` before opening a pull request. Report vulnerabilities
privately as described in `SECURITY.md`. Never attach live account data.

## License

Apache-2.0. See `LICENSE`.

NinjaTrader is a trademark of its owner. TradeCopia is an independent
open-source project and is not affiliated with NinjaTrader, LLC.
