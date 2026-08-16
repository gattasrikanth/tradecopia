# NinjaTrader local build

## Detected environment (this workstation)

- NinjaTrader Desktop **8.1.8.2**
- Official assemblies live next to `NinjaTrader.exe` under the default
  Program Files install. **Do not commit them.**
- A NinjaTrader user-data directory may be missing until NT has been launched
  at least once. `scripts/install-local.ps1` must tolerate that.

Machine-specific absolute paths are not recorded here.

## Official API baseline

Documented AddOn surface used by TradeCopia:

- [AddOn development](https://ninjatrader.com/support/helpguides/nt8/add_on.htm)
- [Account](https://ninjatrader.com/support/helpguides/nt8/account_class.htm)
- [CreateOrder](https://ninjatrader.com/support/helpguides/nt8/createorder.htm)
- [Developing AddOns](https://ninjatrader.com/support/helpguides/nt8/developing_add_ons.htm)

Confirmed methods/events: `Account.All` (enumerate under `lock`),
`AccountStatusUpdate`, `OrderUpdate`, `ExecutionUpdate`, `PositionUpdate`,
`CreateOrder`, `Submit`, `Change`, `Cancel`, `CancelAllOrders`, `Flatten`.

`CreateOrder` parameters include instrument, action, type (Market / Limit /
StopMarket / StopLimit / MIT), TIF, quantity, limit, stop, **OCO string**,
**name (max 50 chars)**, GTD, custom order. Submit via `Account.Submit`.

## Local compile

```powershell
pwsh ./scripts/verify-ninjatrader.ps1
# then, only if net48 targeting pack + NT assemblies are present:
dotnet build src/Native/TradeCopia.Native/TradeCopia.Native.csproj
```

Public CI does **not** compile the NT-referenced project.

## Safety

The Phase 1 native facade must not call `Submit`, `Change`, `Cancel`, or
`Flatten` except behind an explicit executor that remains disabled by default.
Automated submission is allowed later only against a positively identified
simulation account.
