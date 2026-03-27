// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.simulation.Ecs;
using uk.osric.sim.simulation.Ecs.Components;
using uk.osric.sim.simulation.Ecs.Systems;

namespace uk.osric.sim.tests;

public sealed class SheepSpawnerBehaviourTests {
    [Test]
    public void SpawnFlock_CreatesExactRequestedNumberOfEntities() {
        EntityStorage storage = new();
        SheepSpawner spawner = new(storage, 1000, new Random(42));

        int count = spawner.SpawnFlock(24);

        Assert.That(count, Is.EqualTo(24));
    }

    [Test]
    public void SpawnFlock_PlacesAllSheepWithinTwentyPixelsOfCenter() {
        EntityStorage storage = new();
        int mapSize = 1000;
        SheepSpawner spawner = new(storage, mapSize, new Random(42));
        spawner.SpawnFlock(24);

        float cx = mapSize / 2.0f;
        float cy = mapSize / 2.0f;

        foreach (var (id, pos, _, _) in storage.Query<Position, Velocity, Acceleration>()) {
            Assert.That(Math.Abs(pos.X - cx), Is.LessThanOrEqualTo(20f),
                $"Entity {id.Value} X position {pos.X} is more than 20px from center {cx}");
            Assert.That(Math.Abs(pos.Y - cy), Is.LessThanOrEqualTo(20f),
                $"Entity {id.Value} Y position {pos.Y} is more than 20px from center {cy}");
        }
    }

    [Test]
    public void SpawnFlock_AssignsZeroAccelerationToAllSheep() {
        EntityStorage storage = new();
        SheepSpawner spawner = new(storage, 1000, new Random(42));
        spawner.SpawnFlock(24);

        foreach (var (_, _, _, acc) in storage.Query<Position, Velocity, Acceleration>()) {
            Assert.That(acc.X, Is.EqualTo(0f));
            Assert.That(acc.Y, Is.EqualTo(0f));
        }
    }

    [Test]
    public void SpawnFlock_AssignsLowVelocityWithinExpectedRange() {
        EntityStorage storage = new();
        SheepSpawner spawner = new(storage, 1000, new Random(42));
        spawner.SpawnFlock(24);

        foreach (var (id, _, vel, _) in storage.Query<Position, Velocity, Acceleration>()) {
            Assert.That(vel.X, Is.InRange(-2.5f, 2.5f),
                $"Entity {id.Value} velocity X {vel.X} is outside [-2.5, 2.5]");
            Assert.That(vel.Y, Is.InRange(-2.5f, 2.5f),
                $"Entity {id.Value} velocity Y {vel.Y} is outside [-2.5, 2.5]");
        }
    }

    [Test]
    public void SpawnFlock_AssignsExpectedSizeRadiusToAllSheep() {
        EntityStorage storage = new();
        SheepSpawner spawner = new(storage, 1000, new Random(42));
        spawner.SpawnFlock(24);

        foreach (var (id, _, _, _) in storage.Query<Position, Velocity, Sheep>()) {
            Size size = storage.Get<Size>(id);
            Assert.That(size.Radius, Is.EqualTo(SheepSpawner.SheepRadius));
        }
    }

    [Test]
    public void SpawnFlock_TagsAllEntitiesAsSheep() {
        EntityStorage storage = new();
        SheepSpawner spawner = new(storage, 1000, new Random(42));
        spawner.SpawnFlock(24);

        int sheepCount = storage.Query<Position, Sheep>().Count();

        Assert.That(sheepCount, Is.EqualTo(24));
    }

    [Test]
    public void SpawnFlock_WithSameSeed_ProducesDeterministicPositions() {
        EntityStorage storage1 = new();
        EntityStorage storage2 = new();
        new SheepSpawner(storage1, 1000, new Random(42)).SpawnFlock(24);
        new SheepSpawner(storage2, 1000, new Random(42)).SpawnFlock(24);

        var positions1 = storage1.Query<Position, Velocity, Acceleration>()
            .Select(t => t.Item2)
            .OrderBy(p => p.X)
            .ToList();
        var positions2 = storage2.Query<Position, Velocity, Acceleration>()
            .Select(t => t.Item2)
            .OrderBy(p => p.X)
            .ToList();

        Assert.That(positions1, Is.EqualTo(positions2));
    }
}
