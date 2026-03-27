// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.simulation.Time;

public sealed class SimulationOptions {
    public const int DefaultTickRateHz = 10;

    public int TickRateHz { get; init; } = DefaultTickRateHz;
}