# Installer certification

Automated (this repository):

- Cloud path detection
- Preflight blocks OneDrive NT data, running NinjaTrader, missing payload
- Install copies only `TradeCopia.*` files
- Install refuses `NinjaTrader*.dll`
- Uninstall removes owned files and preserves data
- Backup verifies file counts

Manual (owner, after setup EXE):

- Fresh install via `TradeCopia-Setup-*.exe`
- Start menu launcher
- AddOn loads without F5
- Uninstall/reinstall
