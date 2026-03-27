// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.simulation;

public sealed class SimulationWorld {
    private readonly List<string> systems = [];

    public IReadOnlyList<string> Systems => this.systems;

    public void RegisterSystem(string systemName) {
        this.systems.Add(systemName);
    }

    public void Tick() {
        // stub: systems will be driven here
    }
}