# Manual NinjaTrader SIM certification

Automated tests passing does **not** certify live or SIM trading.

The product owner should run this matrix on NinjaTrader 8 SIM accounts only.

## Preconditions

- NinjaTrader 8 launched at least once
- Two or more SIM accounts
- TradeCopia AddOn loaded
- Control plane running
- Copying starts **disabled** until explicitly enabled

## Scenarios

| ID | Scenario | Expected |
| --- | --- | --- |
| S1 | Market buy on leader | Followers receive one mapped market submit after enable |
| S2 | Limit working then fill | Follower working order then fill tracked |
| S3 | Cancel before fill | Follower remainder canceled |
| S4 | Modify limit price | Follower change intent |
| S5 | Partial fill | Follower working remainder kept |
| S6 | Follower reject | Visible divergence, no silent retry |
| S7 | Close browser | Copying continues if engine has active snapshot |
| S8 | Restart NT | Copying remains disabled until safe restore |
| S9 | Loop attempt | Copier-originated orders are not re-copied |

Do not run this matrix on live accounts as part of development.
