// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.simulation.Time;

namespace uk.osric.sim.tests;

public sealed class SimulationOptionsBehaviourTests {
    [Test]
    public void DefaultTickRateHz_IsTenForBaselineSimulationPacing() {
        SimulationOptions options = new();

        Assert.That(options.TickRateHz, Is.EqualTo(10));
    }

    [Test]
    public void TickRateHz_CanBeOverriddenForScenarioTuning() {
        SimulationOptions options = new() {
            TickRateHz = 20,
        };

        Assert.That(options.TickRateHz, Is.EqualTo(20));
    }
}