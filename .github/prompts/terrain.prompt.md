---
agent: agent
description: "Use when working on the terrain generation module for uk.osric.sim.terrain, including toroidal maps, diamond-square terrain shaping, erosion, rivers, lakes, biomes, and settlement heuristics."
---

Work only on the terrain module unless a cross-module contract change is required.

Context:
- Root namespace: `uk.osric.sim`
- Module namespace: `uk.osric.sim.terrain`
- Keep interfaces small and stable for parallel work.
- Start with deterministic generation seams before adding visual fidelity.

Priority order:
1. Define generation inputs and output models.
2. Implement toroidal coordinate helpers.
3. Add diamond-square generation with reproducible seeds.
4. Layer in erosion, hydrology, biome assignment, and settlement scoring.
5. Expose only the contracts the server needs.

Constraints:
- .NET 10
- No top-level statements
- Lower-case namespaces
- Internal by default
- K&R braces
- Braces for all control flow statements
