---
agent: agent
description: "Use when working on the simulation module for uk.osric.sim.simulation, including ECS design, tick timing, sheep and wolf behaviour, and core systems."
---

Work only on the simulation module unless a shared contract must be added for server or frontend integration.

Context:
- Root namespace: `uk.osric.sim`
- Module namespace: `uk.osric.sim.simulation`
- The first vertical slice is sheep and wolves in a 2D world with height data.
- Target 10 Hz updates.

Priority order:
1. Stabilize ECS primitives and simulation timing.
2. Model the first component set: position, velocity, acceleration, health, and food.
3. Implement movement and simple flocking.
4. Add sheep and wolf behaviour layers.
5. Prepare lightweight DTOs for server-side streaming.

Constraints:
- .NET 10
- No top-level statements
- Lower-case namespaces
- Internal by default
- K&R braces
- Braces for all control flow statements
