# Blockers

A blocker does not stop independent work.

---

## BLOCKER-ENV-001

Scope affected: local `dotnet build` of domain/control-plane until SDK is present; public CI is unaffected once GitHub runners have an SDK.

Why blocked: bootstrap machine had .NET 8/10 **runtimes** but no SDK (`dotnet --list-sdks` empty).

Evidence: `dotnet --info` reported "No SDKs were found" on 2026-08-16.

Workaround attempted: `winget install --id Microsoft.DotNet.SDK.10` started during Phase 0.

What can continue: documentation, repository governance, CI YAML, ADRs, source authoring.

What a human eventually must provide: nothing if the SDK install succeeds; otherwise install the .NET 10 SDK and Visual Studio Build Tools /.NET Framework 4.8 targeting pack.

---

## BLOCKER-ENV-002

Scope affected: local native AddOn compile against NinjaTrader assemblies in public CI; end-user install-local until a user-data directory exists.

Why blocked: NinjaTrader Desktop 8.1.8.2 is installed, but `%USERPROFILE%\Documents\NinjaTrader 8` was not present. Proprietary NT assemblies must not be committed, so GitHub-hosted Linux/macOS CI cannot compile the native adapter.

Evidence: `C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe` FileVersion `8.1.8.2`; Documents user-data path missing.

Workaround attempted: none yet. Phase 1 will probe assemblies locally and keep references machine-local.

What can continue: FakeNinjaTrader, domain, control plane, web, docs, CI of non-native projects.

What a human eventually must provide: a NinjaTrader user-data directory / first-run NT profile for install-local and SIM certification.

---

## BLOCKER-ENV-003

Scope affected: invoking `msbuild` by short name.

Why blocked: `msbuild` was not on PATH. Visual Studio 2022 Build Tools are installed at the vswhere path.

Evidence: `where.exe msbuild` found nothing; vswhere returned `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools`.

Workaround attempted: scripts will resolve MSBuild via vswhere.

What can continue: all non-native work.

What a human eventually must provide: nothing if Build Tools remain installed.
