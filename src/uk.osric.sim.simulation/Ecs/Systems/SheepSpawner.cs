// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.simulation.Ecs.Components;

namespace uk.osric.sim.simulation.Ecs.Systems;

internal sealed class SheepSpawner {
    private readonly EntityStorage storage;
    private readonly float mapSize;
    private readonly Random rng;

    internal SheepSpawner(EntityStorage storage, int mapSize, Random rng) {
        this.storage = storage;
        this.mapSize = mapSize;
        this.rng = rng;
    }

    internal int SpawnFlock(int count) {
        float cx = mapSize / 2.0f;
        float cy = mapSize / 2.0f;

        for (int i = 0; i < count; i++) {
            EntityId id = storage.CreateEntity();
            storage.Set(id, new Sheep());
            storage.Set(id, new Position(
                cx + (float)(rng.NextDouble() * 40.0 - 20.0),
                cy + (float)(rng.NextDouble() * 40.0 - 20.0)));
            storage.Set(id, new Velocity(
                (float)(rng.NextDouble() * 10.0 - 5.0),
                (float)(rng.NextDouble() * 10.0 - 5.0)));
            storage.Set(id, new Acceleration(0f, 0f));
        }

        return count;
    }
}
