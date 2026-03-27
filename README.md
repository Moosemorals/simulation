# uk.osric.sim

Initial scaffold for a medieval low-fantasy simulation built as parallel modules.

## Modules

- `src/uk.osric.sim.contracts`: shared cross-module DTOs and small integration contracts.
- `src/uk.osric.sim.terrain`: terrain generation contracts and placeholder domain types.
- `src/uk.osric.sim.simulation`: ECS and tick-timing skeleton for the first simulation slice.
- `src/uk.osric.sim.server`: ASP.NET Core host for health checks, terrain delivery, SSE, and static asset hosting.
- `tests/uk.osric.sim.tests`: top-level NUnit project for behaviour-focused unit tests.

## Working Model

- Start from the baseline scaffold on `main`.
- Create one worktree per module branch to let terrain, simulation, and server progress independently.
- Use the prompt files under `.github/prompts` when opening a module worktree in a new VS Code window.

## Build

```powershell
dotnet build uk.osric.sim.slnx

dotnet test uk.osric.sim.slnx
```

## Lessons Learned

- Keep frontend and server JSON casing aligned for live contracts (especially SSE payloads). The frontend stream handler expects camelCase fields such as `sequence` and `locationChanges`.
- When manually serializing SSE payloads with `System.Text.Json`, use web defaults (`JsonSerializerDefaults.Web`) so payload names match browser-side expectations.
- If simulation appears static in the viewport, verify stream payload shape first before changing movement logic. In this case, sheep were moving server-side, but frontend updates were ignored due to payload field-name mismatch.
