# ADR-0010: Account safety classification

- Status: Accepted
- Date: 2026-08-16

## Decision

Alpha classifies NinjaTrader accounts using official `NinjaTrader.Cbi` metadata only:

- `Account.Provider`
- `Connection.Options.Mode` (`Live` or `Simulation` on NT 8.1.8.2)
- `Connection.Options.IsDemo`

Rules:

| Evidence | Class |
| --- | --- |
| Mode = Simulation **or** Provider = Simulator | Simulation |
| IsDemo = true **or** Provider = Playback | Demo/Paper |
| Provider empty/Unknown **or** Mode missing | Unknown |
| Mode = Live (and not Simulator/IsDemo/Playback) | Live |

Display names (`Sim101`, `DEMO…`) are never the sole safety signal.

Stable identity is `Provider + "|" + Account.Id` (`Id` is `Int64` on NT 8.1.8.2). Display name is not the durable key.

Alpha may **select and enable** only Simulation and Demo/Paper. Live and Unknown stay visible and are blocked at the native execution boundary.

## Empirical evidence (NT 8.1.8.2)

Inspected `NinjaTrader.Core.dll` on this machine:

- `Account.All`, `Account.Id` (`Int64`), `Account.Name`, `Account.DisplayName`, `Account.Provider`, `Account.Connection`
- `ConnectOptions.IsDemo` (`Boolean`), `ConnectOptions.Mode` (`NinjaTrader.Cbi.Mode`), `ConnectOptions.Provider`
- `Mode` enum names: `Live`, `Simulation`
- `Provider` includes `Simulator`, `Playback`, `Unknown`, live brokers, and numbered `ProviderN` slots (Tradovate-class connections appear as a numbered provider, not a display-name token)

No display-name substring is used for classification.

## Consequences

- Browser classification is informational.
- Native submit still requires Simulation/Demo-Paper via this classifier plus the existing submit gate.
- Installed runtime account lists come from the engine snapshot, never from `DemoCatalog` fixtures.
