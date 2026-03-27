// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.simulation.Ecs;
using uk.osric.sim.simulation.Ecs.Components;
using uk.osric.sim.simulation.Ecs.Systems;

namespace uk.osric.sim.tests;

public sealed class PositionSystemBehaviourTests {
    [Test]
    public void Update_WithConstantVelocityAndZeroAcceleration_MovesEntityByVelocityTimesDeltaTime() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 1000);
        EntityId entity = storage.CreateEntity();
        storage.Set(entity, new Position(100f, 200f));
        storage.Set(entity, new Velocity(10f, 20f));
        storage.Set(entity, new Acceleration(0f, 0f));

        // Semi-implicit Euler with zero acceleration:
        // newVel = (10, 20) + (0, 0) * 0.1 = (10, 20)
        // newPos = (100, 200) + (10, 20) * 0.1 = (101, 202)
        var changes = system.Update(0.1f);

        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Location.X, Is.EqualTo(101f).Within(0.0001f));
        Assert.That(changes[0].Location.Y, Is.EqualTo(202f).Within(0.0001f));
    }

    [Test]
    public void Update_WithAcceleration_UpdatesVelocityBeforeMoving() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 1000);
        EntityId entity = storage.CreateEntity();
        storage.Set(entity, new Position(0f, 0f));
        storage.Set(entity, new Velocity(0f, 0f));
        storage.Set(entity, new Acceleration(10f, 0f));

        // Semi-implicit Euler:
        // newVel = (0, 0) + (10, 0) * 1.0 = (10, 0)
        // newPos = (0, 0) + (10, 0) * 1.0 = (10, 0)
        var changes = system.Update(1.0f);

        Assert.That(changes[0].Location.X, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(changes[0].Location.Y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void Update_WhenPositionExceedsMapSize_WrapsToroidally() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 100);
        EntityId entity = storage.CreateEntity();
        storage.Set(entity, new Position(99f, 0f));
        storage.Set(entity, new Velocity(5f, 0f));
        storage.Set(entity, new Acceleration(0f, 0f));

        // newPos.X = 99 + 5 * 1.0 = 104 → wraps to 4
        var changes = system.Update(1.0f);

        Assert.That(changes[0].Location.X, Is.EqualTo(4f).Within(0.0001f));
    }

    [Test]
    public void Update_WhenPositionGoesNegative_WrapsToroidally() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 100);
        EntityId entity = storage.CreateEntity();
        storage.Set(entity, new Position(1f, 0f));
        storage.Set(entity, new Velocity(-5f, 0f));
        storage.Set(entity, new Acceleration(0f, 0f));

        // newPos.X = 1 + (-5) * 1.0 = -4 → wraps to 96
        var changes = system.Update(1.0f);

        Assert.That(changes[0].Location.X, Is.EqualTo(96f).Within(0.0001f));
    }

    [Test]
    public void Update_ReturnsOneChangePerMovingEntity() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 1000);

        for (int i = 0; i < 5; i++) {
            EntityId id = storage.CreateEntity();
            storage.Set(id, new Position(i * 10f, 0f));
            storage.Set(id, new Velocity(1f, 0f));
            storage.Set(id, new Acceleration(0f, 0f));
        }

        var changes = system.Update(0.1f);

        Assert.That(changes, Has.Count.EqualTo(5));
    }

    [Test]
    public void Update_ReturnsEntityIdMatchingStoredEntity() {
        EntityStorage storage = new();
        PositionSystem system = new(storage, 1000);
        EntityId entity = storage.CreateEntity();
        storage.Set(entity, new Position(0f, 0f));
        storage.Set(entity, new Velocity(1f, 0f));
        storage.Set(entity, new Acceleration(0f, 0f));

        var changes = system.Update(1.0f);

        Assert.That(changes[0].Id, Is.EqualTo(entity));
    }
}
