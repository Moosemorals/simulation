# Project Intent

- Build a medieval low-fantasy simulation with a clear split between terrain generation, simulation, and server modules.
- Favour clean interfaces between modules. Cross-module contracts should stay small and deliberate.

# Baseline Technical Rules

- Target .NET 10 for all C# projects.
- Use Central Package Management via `Directory.Packages.props`.
- Set `UseArtifactsOutput` to `true` in shared build properties.
- Do not use top-level statements.
- Use `uk.osric.sim` as the root namespace.
- Use lower-case module namespaces such as `uk.osric.sim.terrain`, `uk.osric.sim.simulation`, and `uk.osric.sim.server`.
- Default classes, records, and interfaces to `internal`. Make types `public` only when another module must consume them.
- Prefer Microsoft and System packages before adding external dependencies.
- Add SPDX headers to new source files:
  - `SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>`
  - `SPDX-License-Identifier: ISC`

# Environment and Tooling

- The primary development environment is Windows.
- Prefer PowerShell-native commands and tooling for automation.
- Do not assume `rg` is available; use PowerShell alternatives such as `Select-String` and `Get-ChildItem` for search operations.

# Testing and Completion Criteria

- Add or update unit tests for behaviour whenever production code changes.
- Prioritize high-value behavioural tests over low-value structure tests.
- A task is not complete until relevant high-value tests pass locally.
- For each completed task, run and report the exact test command used.

# Code Style

- Follow K&R brace style.
- Always use braces for `if`, `while`, `for`, and `foreach`.
- When changing code, always check for and remove unused imports/`using` directives.
- Prefer primary constructors where they make the code clearer.
- Keep public APIs narrow and explicit.
- Avoid speculative abstractions. Start with thin seams that support the current milestone.

# Architecture Boundaries

- `uk.osric.sim.terrain`
  - Owns toroidal map generation, diamond-square terrain shaping, raindrop erosion tuning, river and lake placement, biome assignment, and settlement siting heuristics.
- `uk.osric.sim.simulation`
  - Owns ECS primitives, simulation timing, entity state, systems, and AI layers.
  - The first playable slice is sheep and wolves with movement, flocking, hunger, sleep, and predator avoidance.
- `uk.osric.sim.server`
  - Hosts the backend with ASP.NET Core.
  - Exposes `/health`.
  - Serves terrain data to clients.
  - Serves static frontend assets (HTML, CSS, JS) from `wwwroot/`.
  - Uses Server-Sent Events for tick updates to clients.
  - Uses SQLite for persistence.

# Delivery Guidance

- Scaffold and stabilize shared contracts first, then parallelize work by module.
- Rebase active module branches from `main` frequently, especially after shared contract changes.
- For long-lived module branches, merge small, reviewable slices back into `main` regularly so progress stays visible to the team.
- Keep terrain, simulation, and server changes independently buildable whenever possible.
- Document assumptions in the relevant module prompt or plan file before widening interfaces.
