# Installation

## Development

- Clone `https://github.com/gattasrikanth/tradecopia`
- `pwsh ./scripts/bootstrap.ps1`
- `pwsh ./scripts/test.ps1`
- `pwsh ./scripts/run-control-plane.ps1`

Native compile (Windows, NinjaTrader installed):

```powershell
dotnet build src/Native/TradeCopia.Native/TradeCopia.Native.csproj
```

The project references local `NinjaTrader.*.dll` files with `Private=false`. Those assemblies are **not** committed.

## End user (Alpha)

Packaging is not live-certified. Run from source as above. A GitHub Release will be marked pre-release until the manual SIM matrix passes.
