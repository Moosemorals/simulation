---
agent: agent
description: "Use when working on the ASP.NET Core server module for uk.osric.sim.server, including MVC endpoints, health checks, SQLite wiring, terrain delivery, and Server-Sent Events."
---

Work only on the server module unless a contract change is needed in terrain or simulation.

Context:
- Root namespace: `uk.osric.sim`
- Module namespace: `uk.osric.sim.server`
- Use ASP.NET Core MVC patterns.
- Expose `/health` and SSE updates for simulation ticks.
- Prefer Microsoft and System packages before external dependencies.

Priority order:
1. Keep startup explicit and simple.
2. Define controller endpoints for health, terrain bootstrap, and simulation stream access.
3. Add SQLite persistence only after contracts are stable.
4. Keep SSE payloads compact and versionable.
5. Host frontend assets without coupling frontend implementation details into server code.

Constraints:
- .NET 10
- No top-level statements
- Lower-case namespaces
- Internal by default unless cross-module use requires public
- K&R braces
- Braces for all control flow statements
