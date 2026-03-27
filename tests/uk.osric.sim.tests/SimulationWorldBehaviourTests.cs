// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.simulation;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.tests;

public sealed class SimulationWorldBehaviourTests {
    [Test]
    public void GetSheepLocations_OnStartup_ReturnsAllSpawnedSheep() {
        SimulationWorld world = CreateWorld(513, 1729);

        var locations = world.GetSheepLocations();

        Assert.Multiple(() => {
            Assert.That(world.EntityCount, Is.EqualTo(24));
            Assert.That(locations, Has.Count.EqualTo(24));
            Assert.That(locations.Select(location => location.Id.Value).Distinct().Count(), Is.EqualTo(24));
        });
    }

    [Test]
    public void Tick_AfterUpdate_SheepLocationsChange() {
        SimulationWorld world = CreateWorld(513, 1729);
        var before = world.GetSheepLocations().ToDictionary(location => location.Id.Value, location => location.Location);

        world.Tick(1.0f);

        var after = world.GetSheepLocations();

        Assert.That(after.Any(location =>
            before.TryGetValue(location.Id.Value, out var previous) &&
            (Math.Abs(previous.X - location.Location.X) > 0.0001f || Math.Abs(previous.Y - location.Location.Y) > 0.0001f)),
            Is.True);
    }

    private static SimulationWorld CreateWorld(int size, int seed) {
        TerrainMap map = new() {
            Size = size,
            HeightData = new float[size * size],
        };

        TerrainGenerationOptions options = new() {
            Seed = seed,
            Size = size,
        };

        return new SimulationWorld(map, options);
    }
}
