# TradeCopia — OneDrive Remediation, One-Click Installer, Release Engineering, and Owner SIM Certification Plan

**Status:** Execution design / implementation mandate  
**Date:** 2026-08-16  
**Product:** TradeCopia  
**Repository:** `https://github.com/gattasrikanth/tradecopia`  
**Audience:** Coordinating Build Agent and specialized subagents  
**Primary objective:** Remove the OneDrive/NinjaTrader user-data risk, replace the developer-style manual AddOn installation with a production-grade one-click installer, publish repeatable GitHub release artifacts, install TradeCopia on the development PC using the same artifact an end user receives, and leave only genuinely human-required SIM certification actions to the product owner.

---

# 1. Executive Decision

TradeCopia must no longer treat manual DLL copying, NinjaScript Editor compilation, custom PowerShell install commands, or knowledge of NinjaTrader's `bin\Custom` layout as the normal customer installation flow.

The target customer experience is:

```text
GitHub Releases
      ↓
TradeCopia-Setup-<version>.exe
      ↓
Double-click
      ↓
Preflight checks
      ↓
Install / upgrade TradeCopia
      ↓
Launch NinjaTrader
      ↓
TradeCopia loads automatically
      ↓
Open TradeCopia dashboard
```

The installer must handle all supported product files and configuration required for a normal user installation. The user should not need to:

- run PowerShell;
- build source code;
- copy DLLs;
- manually edit NinjaTrader folders;
- press F5 in NinjaScript Editor;
- modify `.csproj` files;
- add DLL references manually;
- know the local control-plane port;
- install the .NET SDK;
- install Node.js;
- install developer tooling.

Developer/manual workflows may remain documented as fallback diagnostics only.

The development PC currently has a second issue that should be corrected before meaningful TradeCopia/NinjaTrader certification:

```text
Current NinjaTrader user-data path:
C:\Users\<user>\OneDrive\Documents\NinjaTrader 8
```

The desired state is a local Windows Documents path, for example:

```text
C:\Users\<user>\Documents\NinjaTrader 8
```

NinjaTrader support documentation identifies OneDrive synchronization of the NinjaTrader Documents folder as a source of file-access/synchronization problems. Therefore TradeCopia should treat cloud-backed NinjaTrader user-data as an unsupported/high-risk configuration by default.

The Build Agent should perform all machine changes it can safely and reversibly perform. It should not stop for routine approval. It may stop only if Windows or OneDrive presents a genuinely interactive user-account confirmation that cannot safely be completed programmatically, or if credentials/terms acceptance are required.

---

# 2. Authoritative Platform Basis

This plan intentionally relies on official platform documentation and our own local tests.

## 2.1 NinjaTrader / OneDrive

NinjaTrader support article:

`https://support.ninjatrader.com/s/article/Unhandled-exception-Access-to-the-path-is-denied-OneDrive-Error`

NinjaTrader support has documented that OneDrive synchronization can interfere with NinjaTrader's Documents folder and produce file-access problems.

The Build Agent must treat this as a platform reliability issue, not merely a TradeCopia script-path bug.

## 2.2 NinjaTrader AddOn installation/distribution

Official NinjaTrader documentation recognizes third-party AddOns and vendor-specific installation approaches, including a custom installer:

`https://ninjatrader.com/support/helpguides/nt8/using_3rd_party_indicators.htm`

NinjaTrader's Export documentation states that a custom installer is advised when a compiled NinjaScript product references external DLLs:

`https://ninjatrader.com/support/helpguides/nt8/export.htm`

NinjaTrader distribution best practices state that a custom installer should not overwrite NinjaTrader-deployed files and should provide an uninstall option:

`https://ninjatrader.com/support/helpguides/nt8/best_practices.htm`

Third-party AddOn installation/update behavior is documented here:

`https://ninjatrader.com/support/helpguides/nt8/using_3rd_party_add-ons.htm`

These official capabilities support the decision to make a proper TradeCopia installer the primary distribution mechanism.

## 2.3 Microsoft OneDrive

Official Microsoft OneDrive guidance for pausing/quitting/unlinking OneDrive:

`https://support.microsoft.com/en-us/onedrive/how-to-cancel-or-stop-sync-in-onedrive`

The Build Agent must use supported Windows/OneDrive mechanisms where possible and must not rely on undocumented destructive registry hacks merely to avoid a one-time confirmation dialog.

---

# 3. Goals

## G1 — Safe local NinjaTrader user-data

Move the effective NinjaTrader user-data location out of the OneDrive-synchronized Documents tree and validate that NinjaTrader operates from a local Documents folder.

## G2 — End-customer installer

Produce a versioned, repeatable Windows installer such as:

```text
TradeCopia-Setup-0.1.0-alpha.1.exe
```

that installs the complete customer-facing product.

## G3 — No developer steps

A normal customer installation must not require source compilation or manual DLL placement.

## G4 — Self-contained companion runtime

End users should not need the .NET SDK. Prefer self-contained publishing of the TradeCopia control plane so the installer can deploy everything required to run it.

## G5 — Correct NinjaTrader integration

The installer must install the native NinjaTrader integration using a method verified against the locally installed supported NinjaTrader version and consistent with official NinjaTrader distribution guidance.

## G6 — Safe upgrade and uninstall

The installer must support clean upgrade, repair where feasible, uninstall, and rollback/failure recovery.

## G7 — Public GitHub release

GitHub Actions or an equivalent reproducible release workflow should build, test, package, checksum, and attach release artifacts to GitHub Releases.

## G8 — Dogfood the real artifact

The development PC must ultimately install TradeCopia using the generated release/setup artifact rather than relying on a developer-only copy/build procedure.

## G9 — Preserve fail-closed trading behavior

Installation, dashboard launch, and control-plane startup must never imply that copying is enabled.

After installation/restart:

```text
copyingEnabled = false
```

until explicitly and safely enabled through the product's approved workflow.

---

# 4. Non-Goals and Safety Boundaries

This goal does not authorize:

- real-money order submission;
- storing broker credentials;
- modifying NinjaTrader proprietary binaries;
- suppressing NinjaTrader safety checks;
- patching NinjaTrader executables;
- disabling Windows security protections;
- turning off antivirus;
- weakening TradeCopia's localhost protections;
- publishing a “stable/live-certified” release before manual SIM certification;
- deleting OneDrive cloud files;
- destroying backups to simplify migration;
- silently changing unrelated user folders.

The Build Agent may disable OneDrive Documents backup because the product owner has explicitly stated that OneDrive backup is not required for this machine. It should still preserve user files and use the smallest system change needed to make NinjaTrader local and reliable.

---

# 5. Current Known State

At the time this plan was created, the development PC reported:

```text
NinjaTrader:
8.1.8.2

NinjaTrader user data:
<OneDrive>\Documents\NinjaTrader 8

Control plane:
http://127.0.0.1:17841

Current reported product state:
- deterministic domain/copier logic exists;
- native AddOn compiles against the installed NinjaTrader assemblies;
- local dashboard/control plane exists;
- automated tests exist;
- native integration and packaging remain incomplete;
- copying remains fail-closed;
- manual SIM certification has not been completed.
```

The Build Agent must verify current repository and machine truth before relying on any value above.

---

# 6. Phase A — Repository and Machine Preflight

## A1. Repository checkpoint

Run and record:

```text
git fetch --all --prune
git status
git branch --show-current
git log --oneline -20
```

Read:

```text
docs/product/PRD.md
docs/architecture/SYSTEM-DESIGN.md
docs/agent/AUTONOMOUS-BUILD-MANDATE.md
docs/agent/STATE.md
docs/agent/NEXT.md
docs/agent/TASKS.md
docs/agent/DECISIONS.md
docs/agent/BLOCKERS.md
```

Commit this plan into the repository at:

```text
docs/architecture/ONEDRIVE-INSTALLER-RELEASE-PLAN.md
```

unless a clearer existing location is already established by repository conventions.

Update agent state before beginning system changes.

## A2. Machine inventory

Collect without exposing secrets:

- Windows version/build;
- PowerShell version;
- current Windows Documents known-folder path;
- OneDrive installation/running state;
- OneDrive root;
- whether Documents Known Folder Backup is active;
- installed NinjaTrader version;
- NinjaTrader executable location;
- running NinjaTrader processes;
- current NinjaTrader user-data location;
- available disk space;
- TradeCopia repo location;
- installed .NET SDKs/runtimes;
- installer build tooling availability.

Do not commit the literal Windows username or account numbers to public docs. Normalize/redact machine-specific information in committed reports.

## A3. Backup before mutation

Before changing OneDrive/Documents/NinjaTrader state:

1. Close NinjaTrader gracefully.
2. Verify all NinjaTrader processes are stopped.
3. Stop/quit OneDrive synchronization if currently running.
4. Create a timestamped local backup outside OneDrive of the entire current NinjaTrader user-data directory.
5. Produce a backup manifest containing source, destination, timestamp, file count, directory count, total bytes, and errors.
6. Verify backup readability.
7. Hash critical configuration artifacts where useful.
8. Never delete the source tree during the first migration pass.

Recommended backup root:

```text
C:\TradeCopia-Backups\NinjaTrader8\<timestamp>\
```

or another explicit local non-OneDrive directory.

---

# 7. Phase B — OneDrive / Documents Remediation

## B1. Desired end state

The Windows Documents known folder should resolve to a local path, preferably:

```text
%USERPROFILE%\Documents
```

and NinjaTrader user data should exist at:

```text
%USERPROFILE%\Documents\NinjaTrader 8
```

The path must not resolve under OneDrive or another cloud-synchronized root.

## B2. Safe migration principles

The Build Agent must:

- prioritize preservation over speed;
- copy before delete;
- validate before switching;
- stop NinjaTrader during the move;
- stop OneDrive syncing during the move;
- avoid unsupported direct changes to OneDrive internals;
- use supported Windows known-folder behavior where feasible;
- retain a rollback copy;
- not delete cloud data;
- not assume a path based solely on `%USERPROFILE%\Documents`;
- query the actual Windows Documents Known Folder.

## B3. Turn off OneDrive Documents backup

The product owner has explicitly stated that OneDrive backup is not needed for now.

Preferred order:

1. Quit OneDrive.
2. Determine whether OneDrive Known Folder Backup for Documents can be disabled safely through supported mechanisms on this Windows installation.
3. If Windows/OneDrive requires a GUI confirmation such as “Stop backup” / “Keep files on this PC,” use safe automation only if each choice can be verified and is unambiguous.
4. If an account-security or ambiguous destructive dialog cannot be safely automated, this is an acceptable owner-intervention point.
5. Do not uninstall OneDrive merely because Documents backup is enabled unless normal supported backup disablement fails and uninstall is clearly the safer reversible option.
6. Do not delete OneDrive cloud content.

The goal is not “remove OneDrive at all costs.” The goal is “NinjaTrader user data must be local and unsynchronized.”

## B4. Establish local Documents

Create/verify:

```text
%USERPROFILE%\Documents
```

Then copy/migrate the NinjaTrader subtree:

```text
OneDrive\Documents\NinjaTrader 8
→
Documents\NinjaTrader 8
```

Use a robust copy mechanism and preserve timestamps/attributes where appropriate.

A safe migration must:

1. copy all files;
2. review copy errors;
3. compare file counts and sizes;
4. verify expected critical subdirectories;
5. preserve the old OneDrive copy temporarily;
6. set the Windows Documents known-folder location to local through supported Windows folder-redirection behavior;
7. notify Explorer/shell of the change if required;
8. verify the effective known-folder path in a new process/session.

## B5. NinjaTrader post-migration verification

After migration:

1. Start NinjaTrader normally.
2. Confirm the Control Center opens.
3. Confirm there is no OneDrive path warning.
4. Confirm NinjaTrader creates/updates files under `%USERPROFILE%\Documents\NinjaTrader 8`.
5. Confirm it does not actively write runtime state into `%OneDrive%\Documents\NinjaTrader 8`.
6. Close NinjaTrader.
7. Confirm expected log/database/workspace timestamps changed only in the local location.
8. Reopen NinjaTrader once more to validate stable startup.

If NinjaTrader itself requires reinstall/reconfiguration to adopt the new known-folder path, the Build Agent may perform that only after backup and only using the official installer. Do not wipe the local user-data backup.

## B6. Rollback

If NinjaTrader fails after migration:

1. stop NinjaTrader;
2. preserve logs from the failed attempt;
3. restore the prior known-folder configuration;
4. restore from the verified backup if needed;
5. document the failure;
6. diagnose before retrying.

Never keep iterating destructively on the only copy of NinjaTrader user data.

---

# 8. Phase C — Product-Level Path Handling

TradeCopia must not repeat the original bug of assuming:

```text
%USERPROFILE%\Documents\NinjaTrader 8
```

without asking Windows where Documents actually is.

## C1. Known-folder resolver

Implement a single production path-resolution component that obtains the real Windows Documents known-folder location through supported Windows/.NET APIs.

All installer/dev scripts should use that abstraction.

Do not independently reimplement path guessing across scripts.

## C2. Cloud-path risk detection

TradeCopia installer/preflight should detect common cloud-backed Documents paths including at least OneDrive and OneDrive for Business-style roots. Other deterministic cloud/redirection signals may also be detected.

## C3. Installer policy on cloud-backed NT data

Recommended V1 behavior:

```text
If NinjaTrader user-data is cloud-backed:
    BLOCK normal install
    explain why
    offer remediation documentation/helper
    allow Retry after remediation
```

Do not silently install into a known high-risk synchronized NT tree.

## C4. Developer scripts

Update `scripts/install-local.ps1` and related tooling so that they:

- query the actual Windows Documents known folder;
- detect the NinjaTrader user-data folder;
- accept explicit overrides only for developer/testing scenarios;
- identify OneDrive/cloud paths;
- block/warn according to policy;
- no longer require the developer to manually supply a OneDrive path under normal operation.

---

# 9. Phase D — Installer Architecture

## D1. Customer-facing artifact

Primary artifact:

```text
TradeCopia-Setup-<semver>.exe
```

Examples:

```text
TradeCopia-Setup-0.1.0-alpha.1.exe
TradeCopia-Setup-0.1.0-beta.1.exe
TradeCopia-Setup-1.0.0.exe
```

## D2. Installer technology decision

Select a production-grade Windows installer technology that:

- emits a single `setup.exe`;
- supports per-user installation when feasible;
- supports upgrades;
- supports uninstall;
- supports rollback/failure recovery;
- integrates with Windows Installed Apps where appropriate;
- builds in CI;
- has licensing compatible with a free open-source product;
- supports code signing;
- supports deterministic/versioned inputs.

A WiX/Burn-based installer is one viable class of solution because Burn can produce a single EXE bundle, but the Build Agent must evaluate the current licensing/tooling implications before locking it. Another mature Windows installer framework may be preferable. Record the choice in an ADR.

Do not choose an installer solely because it is easiest to script.

## D3. Prefer per-user installation

Prefer a per-user installation if it satisfies NinjaTrader integration requirements.

Suggested application root:

```text
%LOCALAPPDATA%\TradeCopia\
```

Possible layout:

```text
%LOCALAPPDATA%\TradeCopia\
  app\
    TradeCopia.ControlPlane.exe
    dashboard\
  config\
  logs\
  data\
  version.json
```

Keep executable product files out of Documents except the NinjaTrader integration pieces that must live under NinjaTrader's user-data tree.

## D4. NinjaTrader integration files

Install only TradeCopia-owned files required by the supported NinjaTrader integration into:

```text
<Documents Known Folder>\NinjaTrader 8\bin\Custom\
```

or exact verified subdirectories.

Never overwrite NinjaTrader-owned binaries.

Use unique TradeCopia names/namespaces.

## D5. No developer SDK required

Publish the control plane as a production Windows artifact, preferably:

```text
win-x64
Release
self-contained
```

End users should not require:

- .NET SDK;
- Visual Studio;
- Node.js;
- pnpm/npm;
- PowerShell build scripts.

If a runtime is intentionally external, setup must detect/install it cleanly. Self-contained is preferred for support simplicity.

## D6. Dashboard assets

Compile the frontend during release and bundle the generated static assets with the control plane.

No Vite/npm development server on customer machines.

## D7. Companion lifecycle

Production requirements:

- one instance per Windows user;
- no terminal window required;
- loopback-only server;
- deterministic port strategy / collision handling;
- graceful shutdown;
- reconnection to native engine;
- browser closing does not stop copying;
- companion restart never silently enables copying;
- startup/launch mechanism is documented and testable.

Choose one production lifecycle model and record an ADR, for example a per-user scheduled startup task or an AddOn-launched companion process with single-instance semantics. Avoid an unnecessary LocalSystem service if per-user execution is sufficient.

## D8. Start menu / launcher

Install a customer entry such as:

```text
Start → TradeCopia → Open TradeCopia
```

The launcher should start/reuse the companion and open the correct local dashboard URL automatically.

The user should never need to remember `127.0.0.1:17841`.

## D9. Expected normal startup

After setup:

```text
Launch NinjaTrader
→ TradeCopia native AddOn loads
→ companion is running/reconnects
→ TradeCopia dashboard available
→ copying remains Disabled
```

No NinjaScript Editor/F5 step.

---

# 10. Phase E — Native AddOn Distribution Validation

This is critical because “copy DLLs and press F5” is not a product installation experience.

## E1. Determine the supported no-F5 mechanism

Empirically determine the cleanest supported installation form for the current TradeCopia architecture using:

- official NinjaTrader documentation;
- the locally installed supported NinjaTrader environment;
- minimal reproducible experiments;
- source-controlled installer inputs.

Potential forms may include:

- compiled NinjaScript/AddOn assembly;
- source bootstrap/shim plus compiled product assemblies;
- NinjaScript archive as part of the installation pipeline;
- another officially supported custom-installer arrangement.

Do not derive behavior from third-party copier products.

## E2. Acceptance criterion

After installation while NinjaTrader is closed:

```text
1. Run TradeCopia-Setup-*.exe
2. Launch NinjaTrader
3. TradeCopia native component loads
4. No F5/manual compile required
5. No manual DLL reference editing required
6. Engine named pipe comes online
7. Dashboard reports engineConnected=true
8. copyingEnabled=false
```

A one-time NinjaTrader restart after initial install is acceptable if setup communicates it. Manual compilation is not acceptable as the standard path.

## E3. Compile/runtime isolation

TradeCopia installation must not leave NinjaTrader's global NinjaScript environment broken.

Test on:

- clean synthetic/profile state;
- existing profile with unrelated custom scripts where practical;
- uninstall/reinstall;
- upgrade.

## E4. Safe uninstall

Uninstall removes only TradeCopia-owned files/registration. It must not remove NinjaTrader data, unrelated AddOns, workspaces, or account configuration.

---

# 11. Phase F — Installer Preflight UX

Before installation, setup should check:

## F1. Operating system

- supported Windows version;
- x64 architecture as required.

## F2. NinjaTrader

Detect:

- installed NinjaTrader version/location;
- running NinjaTrader process;
- effective user-data path;
- expected `bin\Custom` presence/shape.

If NinjaTrader is running, request a graceful close and verify shutdown before modifying native files.

## F3. OneDrive/cloud-backed Documents

If the effective NT user-data path is cloud-backed, block normal installation with a clear explanation and remediation link/helper. Do not offer a casual “Install Anyway” button in V1.

## F4. Existing TradeCopia

Detect:

- fresh install;
- same version;
- older version;
- newer version;
- partial/broken install.

Support install, repair, upgrade, uninstall as appropriate.

## F5. Port availability

Validate the local server's port strategy and collision behavior.

## F6. Disk and permissions

Check required free space and write access before mutating files.

---

# 12. Phase G — Upgrade, Repair, Rollback, and Uninstall

## G1. Upgrade

Upgrade must:

1. detect installed version;
2. stop TradeCopia companion gracefully;
3. require NinjaTrader closed if native files change;
4. back up config/state needed for migration;
5. deploy new version;
6. run schema/config migrations;
7. verify health;
8. roll back on installation failure where practical.

## G2. Configuration compatibility

Version configuration schemas explicitly. Never silently discard unsupported configuration fields.

## G3. Database migration

Journal/analytics DB migrations must be versioned and tested. Back up before migrations that can mutate data materially. Failure must not corrupt the only data copy.

## G4. Uninstall

Remove:

- app binaries;
- companion startup registration;
- TradeCopia NT integration files;
- shortcuts;
- installer registration.

Preserve user-generated journal/history by default unless product requirements specify a different policy; optionally offer “remove local TradeCopia data.”

---

# 13. Phase H — Release Pipeline

## H1. CI gates

Before packaging/release, require applicable success for:

- dependency restore;
- build;
- unit tests;
- integration tests;
- security/static checks;
- formatting/lint;
- coverage gates;
- frontend build;
- Playwright/browser E2E;
- installer build;
- artifact-contents validation.

NinjaTrader runtime tests that cannot run on GitHub-hosted CI remain a separate certification lane.

## H2. Reproducible release build

Release from:

```text
clean git checkout
+ tagged commit
+ locked dependencies
+ Release configuration
```

No release artifact may depend on uncommitted local files.

## H3. GitHub release assets

A pre-release should attach at least:

```text
TradeCopia-Setup-<version>.exe
TradeCopia-Setup-<version>.exe.sha256
release notes
SBOM/dependency manifest where practical
```

The README should direct normal users to the setup executable.

## H4. Checksums

Generate SHA-256 automatically.

## H5. Code signing

Early Alpha may be unsigned. Build signing hooks now so later versions can sign setup and executable/DLL artifacts appropriately.

Never commit signing credentials.

Until signed, documentation must honestly explain that Windows SmartScreen may show an unknown-publisher warning.

## H6. Version progression

Recommended:

```text
v0.1.0-alpha.1
v0.1.0-alpha.2
...
v0.1.0-beta.1
...
v1.0.0
```

Do not mark Stable/live-certified until the required manual SIM certification passes.

---

# 14. Phase I — Automated Installer Testing

Installer quality is product quality.

## I1. Package-content tests

Verify:

- expected files only;
- no secrets;
- no absolute developer paths;
- no NinjaTrader proprietary DLLs;
- consistent version metadata;
- license/notices included;
- intended self-contained runtime included.

## I2. Synthetic profile tests

Create fake Windows/NinjaTrader layouts for:

- local Documents/NT8;
- OneDrive Documents/NT8;
- missing NT user-data;
- read-only path;
- existing older TradeCopia;
- corrupt partial installation.

Preflight behavior must be deterministic.

## I3. Install/uninstall roundtrip

Automate:

```text
install
→ verify files/registration
→ launch companion
→ health request
→ uninstall
→ verify product removal
```

Use synthetic data only.

## I4. Upgrade tests

Automate at least one N-1 → N upgrade path once multiple release artifacts exist.

## I5. Failure injection

Test:

- NinjaTrader running;
- locked file;
- companion already running;
- port collision;
- inadequate permissions;
- DB migration failure;
- invalid artifact;
- cloud-backed path;
- partial install recovery.

## I6. Security tests

Verify installer cannot be used to:

- traverse arbitrary paths;
- overwrite unrelated NT files;
- load binaries from unintended writable search paths;
- expose dashboard on LAN;
- persist secrets.

---

# 15. Phase J — Dogfood the Production Artifact

The development PC must be treated like an end-customer machine for final Alpha install verification.

## J1. Remove developer-style remnants

After preserving diagnostics:

- remove manually copied TradeCopia AddOn artifacts;
- stop development companion processes;
- leave source repo intact;
- do not delete NinjaTrader user data.

## J2. Produce an Alpha setup artifact

Example:

```text
TradeCopia-Setup-0.1.0-alpha.1.exe
```

from a clean commit.

## J3. Install only via setup

Run the setup artifact.

Do not manually copy DLLs afterward to make the test pass.

If setup does not install the native component correctly, fix the setup/integration and generate a new artifact.

## J4. Validate customer experience

Verify:

- setup completes;
- Installed Apps entry exists if designed;
- Start menu launcher works;
- companion launches without a terminal;
- dashboard opens;
- NinjaTrader launches;
- AddOn loads without F5;
- named pipe connects;
- status API reports expected engine state;
- copying remains disabled;
- browser close does not alter copying;
- restart leaves fail-closed state.

## J5. Screenshots

If safe and useful, capture setup/dashboard screenshots with synthetic/demo data only.

Do not expose Windows username, real accounts, broker identifiers, tokens, personal paths, or private logs.

---

# 16. Phase K — Native SIM Execution Completion

The previous Alpha checkpoint indicated actual follower submission remained disabled. A trade copier is not code-complete until a safe SIM execution path exists.

## K1. Positive SIM detection

Implement a deterministic account-safety gate using official NinjaTrader account metadata/behavior where available.

Requirements:

- live account must never pass;
- unknown classification fails closed;
- gate is enforced inside native execution boundary;
- browser/control-plane cannot bypass it;
- spoofed/ambiguous account names are tested;
- if NinjaTrader lacks a perfect simulation flag, the selected approach is documented in an ADR and verified through local SIM experiments.

Do not rely on a fragile name substring alone without explicit platform evidence and additional safeguards.

## K2. Native submission path

Complete:

```text
Leader event
→ normalize
→ eligibility
→ idempotency
→ follower mapping
→ SIM safety gate
→ native follower Submit/Change/Cancel
→ mapping state
→ asynchronous telemetry
```

No synchronous persistence/browser operation in the hot path.

## K3. Fail closed

On lost state, malformed mapping, unknown account class, disconnected follower, unsupported instrument, or invalid quantity, do not guess. Emit structured diagnostics and remain safe.

## K4. Automated tests

Before owner SIM, test:

- native-adapter fake submit;
- rejection;
- duplicate leader event;
- partial fill;
- cancel;
- modify;
- disconnect;
- live-account hard block;
- unknown-account hard block;
- companion disconnected;
- journal unavailable.

---

# 17. Phase L — Manual Owner SIM Certification

This is the portion that genuinely benefits from the product owner observing NinjaTrader.

## L1. Potential unavoidable human actions

### A. OneDrive confirmation

Only if Windows/OneDrive requires an account-bound GUI confirmation that cannot be safely automated:

- Stop Documents backup;
- choose to keep files locally where offered.

The Build Agent should reduce this to one precise instruction.

### B. UAC / SmartScreen

If the early unsigned Alpha setup triggers Windows protection, the owner may need to approve the locally built/released installer.

### C. NinjaTrader/broker authentication

Owner handles credentials, MFA, terms, prop/broker connection login, and first-run dialogs. The agent must not collect/store credentials.

### D. Manual SIM trades

Owner places deliberate SIM leader actions and visually confirms follower behavior against the certification checklist.

## L2. Desired owner experience

After this goal, the manual flow should ideally be:

```text
1. Run TradeCopia Setup.
2. Launch NinjaTrader.
3. Confirm Engine Connected / Copying Disabled.
4. Select SIM leader + SIM follower.
5. Enable SIM copying.
6. Place one small SIM leader market order.
7. Confirm follower mirrors it.
8. Continue the prepared SIM certification checklist.
```

No compiling or DLL copying.

---

# 18. Manual SIM Certification Matrix

At minimum certify:

## S1 — Basic market entry

- leader SIM buy/sell;
- follower receives expected quantity;
- no duplicate;
- event timeline correct.

## S2 — Market exit

- leader exit;
- follower exits;
- final positions converge.

## S3 — Limit order

- submit;
- modify;
- cancel;
- fill where practical.

## S4 — Stop order

- submit;
- modify;
- cancel/fill.

## S5 — Partial fill

If SIM environment permits a controlled scenario, validate quantity propagation.

## S6 — Scale in/out

Validate follower quantity remains deterministic and never reverses accidentally.

## S7 — Protection/OCO

Validate supported stop/target behavior and OCO sibling cancellation.

## S8 — Failure/reconnect

Simulate a safe connection loss/reconnect and confirm divergence/health behavior.

## S9 — Restart

Restart browser, companion, and NinjaTrader as separate scenarios. Verify no unsafe automatic resume.

## S10 — Live-account negative test

Without placing a live order, expose/select a live-capable account and verify the current Alpha's safety gate rejects execution.

No live order should be sent.

---

# 19. Installer UX Requirements

The setup UI should be conventional and simple.

## Welcome

```text
TradeCopia Setup

Modern, local-first multi-account trade copying for NinjaTrader 8.
```

## Preflight

Example:

```text
✓ Windows supported
✓ NinjaTrader 8 detected
✓ NinjaTrader user-data is local
✓ Sufficient disk space
✓ Existing TradeCopia version: none / x.y.z
```

If NinjaTrader is open:

```text
NinjaTrader must be closed to install or update TradeCopia.
[Retry]
```

## OneDrive block

```text
NinjaTrader's user-data folder is currently synchronized by OneDrive.

TradeCopia will not install into a cloud-synchronized NinjaTrader folder
because it can cause file-access and reliability problems.

[Open Help] [Retry]
```

Do not add “Install Anyway” in V1.

## Finish

```text
TradeCopia installed successfully.

Next:
1. Launch NinjaTrader 8.
2. Open TradeCopia.
3. Start with SIM accounts.

Copying is disabled by default.

[Open TradeCopia] [Finish]
```

---

# 20. Runtime UX After Install

Dashboard must clearly distinguish:

```text
NinjaTrader: Connected / Disconnected
Engine: Connected / Disconnected
Copying: Disabled / Paused / Enabled
Environment: SIM-safe / Mixed / Unsafe / Unknown
```

Do not report green “ready” merely because HTTP health is good.

Before enablement, require all applicable product safety predicates such as valid topology, connected engine, active validated configuration, allowed environment/account state, and no blocking divergence.

---

# 21. Security Requirements

## 21.1 Installer

- no embedded secrets;
- least privilege;
- avoid Administrator if not needed;
- no unnecessary PATH changes;
- no firewall opening;
- no LAN listener;
- validate packaged/downloaded payloads;
- safe temporary-file handling;
- avoid DLL search-order vulnerabilities;
- uninstall only TradeCopia-owned files.

## 21.2 Control plane

Retain:

- loopback-only binding;
- Host validation;
- Origin validation;
- CSRF protection;
- restrictive CORS;
- versioned/authenticated IPC;
- malformed-request rejection.

## 21.3 Native engine

- fail closed;
- SIM gate at the execution boundary;
- no browser trust;
- no arbitrary raw order endpoint;
- bounded queues;
- no persistent secrets.

## 21.4 Release

- SHA-256 checksum;
- dependency/SBOM review;
- no proprietary NinjaTrader assemblies;
- no personal machine artifacts;
- signing-ready pipeline.

---

# 22. Performance Requirements

Installer/product packaging work must not compromise execution latency.

Native hot path remains:

```text
leader callback
→ deterministic in-memory decision
→ follower native API invocation
```

Do not route follower execution through HTTP, SQLite, browser UI, journal persistence, installer services, or filesystem polling.

---

# 23. Test Coverage / Quality Gates

Use existing repository gates if stricter.

## OneDrive remediation tests

- local Documents detection;
- OneDrive Documents detection;
- backup failure aborts mutation;
- copy failure aborts switch;
- target already exists;
- path with spaces;
- rollback path;
- NinjaTrader running;
- OneDrive running.

## Installer tests

- fresh install;
- repair;
- upgrade;
- uninstall;
- NT-running block;
- OneDrive block;
- missing NT;
- port collision;
- companion health;
- no-F5 startup;
- native load after restart;
- engineConnected;
- copying disabled.

## Release tests

- artifact exists;
- checksum correct;
- no secrets;
- no NT proprietary DLLs;
- version consistency;
- source commit clean/tagged;
- CI green.

---

# 24. Documentation Deliverables

Commit/update as appropriate:

```text
docs/
  architecture/
    ONEDRIVE-INSTALLER-RELEASE-PLAN.md
  operations/
    installation.md
    upgrade.md
    uninstall.md
    onedrive-remediation.md
    recovery.md
  testing/
    installer-certification.md
    manual-sim-certification.md
    release-certification.md
```

README should direct end users to GitHub Releases and the setup EXE. Developer build steps belong in a separate developer section/document.

---

# 25. Git / Agent SDLC Rules

Continue the established TradeCopia discipline:

```text
small slice
→ implementation
→ tests
→ diff review
→ commit
→ push
→ state update
→ next slice
```

Possible commit progression:

```text
docs: add installer and OneDrive remediation plan
fix: resolve NinjaTrader user data via Windows known folder
feat: add cloud-backed user-data preflight
build: publish self-contained control plane
feat: productionize companion lifecycle
build: add TradeCopia Windows installer
test: add installer and upgrade certification
feat: complete native no-F5 AddOn deployment
feat: complete fail-closed SIM native executor
ci: publish checksummed pre-release artifacts
docs: add customer installation and SIM certification
release: prepare TradeCopia alpha installer
```

Do not combine the entire goal into one commit.

---

# 26. Subagent Plan

Parallelize where safe.

## Agent A — Windows / OneDrive

Own known-folder detection, backup/migration helper, rollback, machine remediation, path tests.

## Agent B — Installer

Own installer ADR, setup project, install/upgrade/uninstall, setup UX, installer tests.

## Agent C — Native NinjaTrader integration

Own no-F5 deployment, native load, SIM gate, named-pipe lifecycle, integration tests.

## Agent D — Release engineering

Own GitHub Actions, self-contained publish, checksum/SBOM, release artifacts.

## Agent E — E2E / docs / UX

Own installer E2E, dashboard smoke, safe screenshots, customer docs, manual SIM checklist.

The coordinator owns `main` integration.

Only one agent at a time should control machine-level OneDrive/NinjaTrader mutation.

---

# 27. Stop Conditions for `/goal`

Do not stop merely because:

- OneDrive migration completed;
- setup.exe builds;
- a release exists;
- dashboard works;
- tests pass;
- a commit was pushed.

Continue until all independent work is complete.

A legitimate completion state should include, where the environment permits:

- [x] NinjaTrader user data local, not OneDrive-backed;
- [x] migration backup retained and verified;
- [x] path resolver uses actual Windows Documents known folder;
- [x] cloud-backed NT path blocked/warned by setup;
- [x] production customer installer implemented;
- [x] a single versioned setup EXE is produced;
- [x] customer does not need .NET SDK/Node/PowerShell;
- [x] no manual DLL-copy workflow required;
- [x] no F5 required for normal install;
- [x] companion lifecycle productionized;
- [x] install/upgrade/uninstall tested;
- [x] native AddOn loads from installer;
- [x] named pipe connects;
- [x] dashboard opens;
- [x] copying disabled by default;
- [x] native SIM executor code complete and fail-closed;
- [x] live/unknown account negative protections tested;
- [x] automated tests and coverage green;
- [x] installer tests green;
- [ ] CI green;
- [x] setup artifact checksummed;
- [ ] GitHub pre-release created if release gates permit;
- [x] exact artifact installed on development PC;
- [x] customer installation docs complete;
- [x] manual SIM checklist simplified;
- [ ] all completed code/docs pushed;
- [ ] `main` green;
- [ ] working tree clean.

If one human interaction is required, document the exact step and continue every independent task that does not depend on it.

---

# 28. What the Build Agent Can Do vs. Owner Actions

## Build Agent should do

- inspect OneDrive/NinjaTrader state;
- quit processes safely;
- back up NT user data;
- build migration tooling;
- move/copy user data safely;
- change supported local path configuration where programmatically possible;
- verify the new path;
- update TradeCopia path resolution;
- implement the installer;
- build setup.exe;
- publish self-contained runtime;
- package dashboard;
- implement AddOn deployment;
- implement SIM safety gate;
- run automated tests;
- run installer smoke tests;
- install/uninstall TradeCopia;
- launch NinjaTrader;
- inspect logs/processes/status;
- verify engine pipe;
- publish GitHub pre-release;
- generate checksums;
- write docs;
- commit/push all completed work.

## Owner may need to do only when unavoidable

1. Click a OneDrive “Stop backup” / “Keep files locally” confirmation if OneDrive exposes no safe programmable route.
2. Approve UAC/SmartScreen for the unsigned Alpha setup.
3. Enter NinjaTrader/broker credentials, MFA, or accept terms.
4. Place deliberate manual SIM trades for final certification.

These are the intended human boundaries.

---

# 29. Final Definition of Success

The result must feel like a customer product, not a developer prototype.

A clean Windows/NinjaTrader user should be able to:

```text
download setup.exe
→ install
→ launch NinjaTrader
→ open TradeCopia
→ configure leader/followers
→ start with SIM
```

without knowing how NinjaScript compilation or `bin\Custom` works.

For the product owner, after this goal completes, the remaining manual work should ideally be only:

```text
open NinjaTrader
→ log in if needed
→ use SIM accounts
→ execute the certification checklist
```

---

# 30. Final Implementation Report

When the goal reaches a legitimate stopping point, commit a detailed report containing:

- starting commit;
- final commit;
- OneDrive remediation outcome;
- original/final Documents state, redacted;
- backup status/location, redacted;
- NinjaTrader version tested;
- installer technology and ADR;
- setup artifact filename;
- SHA-256;
- customer install flow;
- upgrade/uninstall results;
- AddOn load result;
- named-pipe result;
- dashboard result;
- SIM executor implementation state;
- automated test counts;
- coverage;
- installer matrix;
- security checks;
- CI status;
- GitHub release tag/URL if created;
- manual owner actions remaining;
- defects/deferred scope;
- final `git status`.

A progress report is not completion.

---

**End of plan**
