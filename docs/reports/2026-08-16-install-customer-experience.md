# TradeCopia install and customer-experience certification

**Status language:** Alpha. Not Stable. Not live-certified. No live order was submitted.

Repository: https://github.com/gattasrikanth/tradecopia  
Release: https://github.com/gattasrikanth/tradecopia/releases/tag/v0.1.0-alpha.1

## Artifact

- File: `TradeCopia-Setup-0.1.0-alpha.1.exe`
- SHA-256: `E4BC287500F8198730A3BC815A0B50AE2A6C58BAA0A0657CBD27278CA6131F4E`
- Source: GitHub Release `v0.1.0-alpha.1` (downloaded and hashed before run)
- The earlier `EF818851…` asset could not start the companion: installed `TradeCopia.Launcher.exe` was a 162KB apphost missing `TradeCopia.Launcher.dll`. That is a product defect. `package.ps1` now publishes a single-file launcher (`4499ff6`). The release asset was replaced. Certification used the new published file, not a local/dev EXE.

## Happy path used

1. Stop companion. Close NinjaTrader (it was Control Center, not Welcome).
2. Run the downloaded setup with `--silent` (exit 0).
3. Launch only via Start Menu `Open TradeCopia.cmd`, which starts `%LOCALAPPDATA%\TradeCopia\app\TradeCopia.Launcher.exe`.
4. Not used: `scripts/run-control-plane.ps1`, `dotnet run`, manual `bin\Custom` copy, NinjaScript F5.

## Results

| Check | Result |
| --- | --- |
| Documents known folder | local, not OneDrive |
| Setup preflight | all ok, including local NT user-data |
| Start Menu target | installed `TradeCopia.Launcher.exe` (not a `.url`) |
| Installed launcher size | 73,561,444 bytes (runnable single-file) |
| Status ×2 | bind `127.0.0.1`, `copyingEnabled=false` |
| `engineConnected` | false — NinjaTrader not restarted (do not start `NinjaTrader.exe` raw) |
| POST pause without CSRF | 403 |
| POST `/api/v1/orders` with CSRF | 404 `no-generic-order-entry` |
| Companion restart | Stopped installed `TradeCopia.ControlPlane` (pid 29644); status became unreachable; Start Menu `Open TradeCopia.cmd` started launcher only; companion returned as pid 31224; `bindAddress=127.0.0.1`, `copyingEnabled=false` |

## Owner-only next step

If install stays healthy, the first manual SIM action is:

1. Start NinjaTrader via **Windows Trading Backbone** (not a raw `NinjaTrader.exe` launch).
2. Confirm dashboard `engineConnected=true` and copying still disabled.
3. Enable SIM copying only for a SIM leader → SIM follower.
4. Place **one** small MNQ (or equivalent) SIM leader market order and confirm the follower mirrors it.

Do not run the full S1–S10 matrix as part of this certification slice.

## Confirmation

- Copying starts disabled.
- Product is not live-certified.
- TradeCopia does not fill NinjaTrader passwords.
