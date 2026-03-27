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

## Terrain Layer Plan (Toroidal Tile Map)

Goal: build terrain generation as deterministic layers with clear handoff data between steps.

### Layer 0: Generation Contract and Determinism

1. Define a single terrain generation request model:
   - seed
   - single map size (square maps only; width equals height)
   - roughness and sea-level tuning knobs
   - erosion and river tuning knobs
2. Define a generation result model:
   - elevation grid
   - water flow or accumulation grid
   - river and lake masks
   - biome grid
   - settlement candidate list
3. Lock deterministic behavior:
   - all randomness from a seeded RNG
   - deterministic step order
   - reproducibility test for same seed and options.

### Layer 1: Diamond-Square Base Elevation (Toroidal)

1. Implement toroidal indexing helpers first so edge reads and writes wrap in both axes.
2. Generate a square base height field with diamond-square:
   - start from seeded corner values
   - perform alternating diamond and square steps
   - decay displacement each iteration by roughness.
   - constrain map size to $2^n + 1$ for baseline implementation.
3. Remove seam artifacts:
   - mirror or wrap sample neighborhoods during each step
   - enforce equal values along opposite edges when needed.
4. Normalize output to a stable range for downstream systems.

### Layer 2: Hydrology and Erosion Pass

1. Compute per-tile downhill direction using wrapped neighbors.
2. Build flow accumulation:
   - route rainfall or moisture downhill
   - sum upstream contributions.
3. Apply a simple erosion model:
   - erode high-flow steep tiles
   - deposit sediment in low-slope low-velocity areas.
4. Run several small iterations instead of one aggressive pass.
5. Re-normalize elevation while preserving relative basin shape.

### Layer 3: Resize or Resample for Target Tile Resolution

1. Decide canonical pipeline order:
   - generate at lower resolution for speed
   - upsample to target tile grid before river extraction.
2. Use wrapped bilinear interpolation first for simplicity.
3. Keep an extension seam for later upgrade to bicubic or noise-guided resampling.
4. Validate no seams appear across wrap boundaries after interpolation.

### Layer 4: River and Lake Detection

1. Mark river tiles from flow accumulation threshold and local slope.
2. Trace river paths to local minima or ocean-connected cells using wrapped traversal.
3. Detect lakes as closed depressions where outflow is below threshold.
4. Optionally carve shallow riverbeds into elevation for visual coherence.
5. Produce stable river and lake masks for biome and settlement layers.

### Layer 5: Biome Assignment

1. Derive climate inputs per tile:
   - elevation band
   - moisture proxy from flow and distance to water
   - temperature proxy from latitude band and altitude.
2. Classify each tile with a small biome lookup table.
3. Smooth isolated biome speckles with a lightweight neighborhood majority pass.
4. Keep rule table data-driven so tuning does not require core algorithm changes.

### Layer 6: Settlement Candidate Scoring

1. Build a settlement score per tile from weighted factors:
   - fresh water proximity
   - flood risk
   - slope buildability
   - biome suitability
   - strategic spacing from existing candidates.
2. Select top candidates with a minimum wrapped distance constraint.
3. Tag candidates by type potential (hamlet, village, stronghold) for simulation bootstrap.

### Validation and Milestones

1. Milestone A: same seed and options always produce byte-identical elevation output.
2. Milestone B: no visible seams in elevation, rivers, or biomes across toroidal edges.
3. Milestone C: hydrology produces connected river networks and plausible lakes.
4. Milestone D: settlement candidates cluster near water and avoid extreme slopes.
5. Milestone E: generation completes within target budget for baseline map sizes.

## Parallel Work Guidance

1. Treat server DTOs and terrain or simulation contracts as explicit negotiation points.
2. Avoid reaching across modules for convenience methods.
3. Stabilize contracts in small increments and then fan out implementation work.

## Recommended Worktree Split

1. `module/terrain`
2. `module/simulation`
3. `module/server`
4. `module/frontend`
