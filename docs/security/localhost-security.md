# Localhost security

The control plane binds **only** `127.0.0.1`. It never defaults to `0.0.0.0`.

Defenses:

- Host allow-list (`127.0.0.1`, `localhost`) against DNS rebinding
- Origin check on state-changing requests
- Per-process CSRF token from `/api/v1/system/bootstrap`
- HttpOnly `SameSite=Strict` session cookie
- Restrictive CSP (`default-src 'self'`)
- No generic `POST /api/v1/orders`
- Destructive flatten/reconcile is two-step and cannot submit while the engine is disconnected

The browser cannot place discretionary trades.
