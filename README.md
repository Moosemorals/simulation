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
