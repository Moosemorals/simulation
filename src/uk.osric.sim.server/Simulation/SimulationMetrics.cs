// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics.Metrics;

namespace uk.osric.sim.server.Simulation;

internal sealed class SimulationMetrics {
    public const string MeterName = "uk.osric.sim.simulation";

    public SimulationMetrics(IMeterFactory meterFactory) {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName, "1.0.0");

        TickDuration = meter.CreateHistogram<double>(
            "simulation.tick.duration",
            unit: "ms",
            description: "Duration of each simulation tick");
    }

    public Histogram<double> TickDuration { get; }
}
