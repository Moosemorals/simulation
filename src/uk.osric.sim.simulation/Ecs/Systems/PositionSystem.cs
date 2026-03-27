// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.simulation.Ecs.Components;

namespace uk.osric.sim.simulation.Ecs.Systems;

internal sealed class PositionSystem {
    private readonly EntityStorage storage;
    private readonly float mapSize;

    internal PositionSystem(EntityStorage storage, int mapSize) {
        this.storage = storage;
        this.mapSize = mapSize;
    }

    internal IReadOnlyList<(EntityId Id, Position Location)> Update(float deltaTime) {
        var changes = new List<(EntityId, Position)>();

        foreach (var (id, pos, vel, acc) in storage.Query<Position, Velocity, Acceleration>()) {
            float newVelX = vel.X + acc.X * deltaTime;
            float newVelY = vel.Y + acc.Y * deltaTime;

            float newPosX = pos.X + newVelX * deltaTime;
            float newPosY = pos.Y + newVelY * deltaTime;

            // Toroidal wrap: handles both overrun and negative positions
            newPosX = ((newPosX % mapSize) + mapSize) % mapSize;
            newPosY = ((newPosY % mapSize) + mapSize) % mapSize;

            storage.Set(id, new Position(newPosX, newPosY));
            storage.Set(id, new Velocity(newVelX, newVelY));

            changes.Add((id, new Position(newPosX, newPosY)));
        }

        return changes;
    }
}
