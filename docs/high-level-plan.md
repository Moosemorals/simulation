# High-Level Plan

## Immediate Goal

Create a stable baseline that all module work can branch from without forcing early coupling.

## Phase 1: Baseline Scaffold

1. Lock in shared conventions: .NET 10, central package management, artifacts output, SPDX headers, lower-case module namespaces, and no top-level statements.
2. Establish the initial module split:
   - `uk.osric.sim.terrain`
   - `uk.osric.sim.simulation`
   - `uk.osric.sim.server`
   - `uk.osric.sim.frontend`
3. Commit the baseline and create one worktree per module branch.

## Phase 2: First Vertical Slice

1. Terrain: deterministic toroidal heightmap generation with seed-based outputs.
2. Simulation: 10 Hz ECS loop with the first components and one movement system.
3. Server: terrain bootstrap endpoint, `/health`, and a stable SSE envelope for tick data.
4. Frontend: top-down WebGL scene with terrain bootstrap, pan and zoom, and tick visualisation hooks.

## Phase 3: Behaviour and World Fidelity

1. Terrain: erosion, hydrology, biome assignment, and settlement scoring.
2. Simulation: sheep and wolves, flocking, hunting, hunger, sleep, and simple terrain-aware movement.
3. Server: SQLite-backed persistence for seeds, snapshots, and simulation metadata.
4. Frontend: tile rendering, shaders, minimap decision, and simulation overlays.

## Parallel Work Guidance

1. Treat server DTOs and terrain or simulation contracts as explicit negotiation points.
2. Avoid reaching across modules for convenience methods.
3. Stabilize contracts in small increments and then fan out implementation work.

## Recommended Worktree Split

1. `module/terrain`
2. `module/simulation`
3. `module/server`
4. `module/frontend`
