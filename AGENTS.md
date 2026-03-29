# AGENTS.md – AI Coding Guidance for uk.osric.sim

This file accelerates AI agent productivity by documenting essential architectural patterns, workflows, and conventions
specific to this medieval low-fantasy simulation.

## Big Picture: Module Architecture

**Four independent modules**, each with strict internal/public boundaries:

- **`uk.osric.sim.terrain`** – Deterministic toroidal map generation (diamond-square + erosion, hydrology, biome
  assignment, settlement siting). No dependencies.
- **`uk.osric.sim.simulation`** – ECS primitives, entity storage, component queries, and systems (currently: sheep
  spawning and position updates on 10 Hz tick loop).
- **`uk.osric.sim.contracts`** – Shared DTOs and cross-module integration points (e.g., `TerrainSeedDto`,
  `TerrainHeightMapDto`). Minimal surface.
- **`uk.osric.sim.server`** – ASP.NET Core host bridging terrain + simulation. Exposes `/api/terrain/*` (heightmap,
  seed), `/health`, and SSE tick updates. Serves static frontend assets via `wwwroot/`.

**Data Flow**: Terrain generates once at startup → Simulation spawns entities and ticks → Server aggregates both →
Frontend (static assets) renders.

## Critical Patterns & Conventions

### Namespacing & Visibility

- Root namespace: `uk.osric.sim`
- Module namespaces: lowercase (e.g., `uk.osric.sim.simulation`, `uk.osric.sim.simulation.Ecs`)
- **Default to `internal`** – only expose `public` types when another module must consume them.
- Example: `Position`, `Velocity`, `Acceleration` are `public readonly record struct` (used by contracts/tests), but
  `EntityStorage` is `internal` (simulation-only).

### SPDX Headers

All source files must start with:

```csharp
// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC
```

### Project Configuration

- Target: **.NET 10** (see `global.json` version 10.0.104)
- Central Package Management via `Directory.Packages.props` (all package versions defined there)
- `UseArtifactsOutput: true` in `Directory.Build.props` (build outputs to `/artifacts/bin` and `/obj`)
- No top-level statements; always use explicit `Program` class
- ImplicitUsings, Nullable, LangVersion:latest enabled globally

### Component-Query Pattern (ECS)

The simulation uses a hand-rolled ECS with statically-typed queries:

```csharp
// EntityStorage.Query<T1, T2, T3>() returns tuples of (EntityId, T1, T2, T3)
foreach (var (id, pos, vel, acc) in storage.Query<Position, Velocity, Acceleration>()) {
    // Update logic
}
```

- Components are `public readonly record struct`  (value types, immutable)
- EntityId is `public readonly record struct EntityId(int Value)`
- No automatic relationship inference; manually compose what's queried

### Toroidal Wrapping

Both terrain and simulation enforce toroidal (wrap-around) semantics:

```csharp
// Toroidal wrap: handles both overrun and negative positions
newPosX = ((newPosX % mapSize) + mapSize) % mapSize;
```

This is critical for seamless edge scrolling in the frontend and deterministic terrain generation.

## Developer Workflows

### Build & Test

```powershell
# Build all
dotnet build uk.osric.sim.slnx

# Run all tests
dotnet test uk.osric.sim.slnx

# Test a specific project
dotnet test tests/uk.osric.sim.tests/uk.osric.sim.tests.csproj
```

**Important**: Tests are in `/tests/uk.osric.sim.tests` (NUnit 4.3.2). Always run tests after production changes; a task
is incomplete until high-value tests pass.

### Worktree Branching

- Main baseline: commit `main` and create worktrees for module-specific branches
- Example: `git worktree add ../sim-terrain origin/terrain` isolates terrain development
- Use `.github/prompts/{module}.prompt.md` when opening a module worktree in VS Code

### Adding Dependencies

1. New packages go to `Directory.Packages.props` with version pinning (no floating versions)
2. Prefer Microsoft and System packages before external dependencies
3. OpenTelemetry (Prometheus exporter, ASP.NET instrumentation) already included for server observability

## ECS & Simulation Mechanics

### Entity Lifecycle

- `EntityStorage.CreateEntity()` → returns `EntityId(value: int)`
- Set components: `storage.Set(entityId, new Position(x, y))`
- Query entities: `storage.Query<Pos, Vel, Acc>()` returns tuple `(id, pos, vel, acc)`
- Destroy: `storage.DestroyEntity(id)` removes all components

### Systems Pattern

Systems query entities and mutate components. Example: `PositionSystem.Update(deltaTime)` applies velocity/acceleration
and returns location changes for network broadcast.

**Sheep spawner** (in simulation tests) demonstrates entity creation:

```csharp
SheepSpawner spawner = new(storage, terrain.Size, rng);
int count = spawner.SpawnFlock(24);  // returns entity count
```

### Simulation Tick Loop

- Runs every ~100ms (10 Hz) on server via `SimulationHostedService`
- Fires `OnTickUpdate` event with `SimulationTickUpdate(tickSequence, locationChanges)`
- Sequence number increments per tick for SSE client ordering

## Cross-Module Contracts

### Terrain ↔ Simulation

`SimulationWorld(TerrainMap terrain, TerrainConfiguration configuration)` – Simulation reads terrain size for wrapping and
spawn heuristics.

### Simulation ↔ Server

`SimulationTickUpdate(int Sequence, IReadOnlyList<(EntityId, Position)> LocationChanges)` – Server broadcasts position
deltas to clients.

### Server ↔ Frontend

- **REST (terrain)**: `GET /api/terrain/seed` → `TerrainSeedDto`, `GET /api/terrain/heightmap` → heightmap as Base64
- **SSE (ticks)**: Server streams `SimulationTickUpdate` as JSON events; static frontend updates entity positions
- **Static assets**: Server serves HTML, CSS, JS from `wwwroot/` at application root

### Server ↔ Terrain

`TerrainOrchestrator.Generate(TerrainConfiguration) → TerrainMap` – Deterministic output for identical
seeds. Configuration includes: Seed, Size, UpscaleFactor, DiamondSquare, and Erosion.

## Testing Philosophy

- **Behaviour tests over structure tests**: focus on observable outcomes (e.g., "spawned 24 sheep within 20px of center"
  not "component type counts")
- Use `Assert.Multiple(() => { ... })` for grouped assertions
- Tag tests with `[Test]` (NUnit); no classes needed for test fixtures (use constructor parameters or `[Setup]`)
- Example from codebase:
  ```csharp
  [Test]
  public void SpawnFlock_PlacesAllSheepWithinTwentyPixelsOfCenter() { ... }
  ```

- Internal modules expose `InternalsVisibleTo` to tests:
  ```xml
  <ItemGroup>
    <InternalsVisibleTo Include="uk.osric.sim.tests" />
  </ItemGroup>
  ```

## Code Style

- **K&R braces**: `if (x) { y(); }`
- **Always braces** on `if`, `while`, `for`, `foreach` (no single-line)
- **Primary constructors** preferred where they reduce boilerplate
- **Remove unused `using` directives** after edits
- **Explicit public APIs**: narrow surface, no speculative abstractions

## Key Files to Reference

- `/global.json` – .NET 10 version lock
- `/Directory.Build.props` – shared build settings
- `/Directory.Packages.props` – centralized NuGet versions
- `/uk.osric.sim.slnx` – solution structure
- `/src/uk.osric.sim.simulation/Ecs/EntityStorage.cs` – ECS foundation
- `/src/uk.osric.sim.server/Program.cs` – DI container & hosted service setup
- `/tests/uk.osric.sim.tests/TerrainOrchestratorBehaviourTests.cs` – determinism & toroidal edge tests
- `/.github/copilot-instructions.md` – baseline project intent & rules

## When to Escalate

- Architecture changes affecting module boundaries → discuss with team before implementing
- New external dependencies → verify against "Prefer Microsoft and System packages" rule
- Cross-module contract expansions → ensure other modules can import cleanly and remain independently buildable
