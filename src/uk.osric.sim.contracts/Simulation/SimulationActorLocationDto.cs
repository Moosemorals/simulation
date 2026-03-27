// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.contracts.Simulation;

public sealed record SimulationActorLocationDto(
    int EntityId,
    float X,
    float Y
);
