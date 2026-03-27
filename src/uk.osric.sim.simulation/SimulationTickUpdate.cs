// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.simulation.Ecs.Components;

namespace uk.osric.sim.simulation;

public sealed record SimulationTickUpdate(
    int TickSequence,
    IReadOnlyList<(Ecs.EntityId Id, Position Location)> LocationChanges);
