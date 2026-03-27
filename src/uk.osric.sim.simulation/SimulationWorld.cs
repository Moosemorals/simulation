// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.simulation.Ecs;
using uk.osric.sim.simulation.Ecs.Systems;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.simulation;

public sealed class SimulationWorld {
    private readonly EntityStorage storage;
    private readonly PositionSystem positionSystem;
    private int tickSequence;

    public event Action<SimulationTickUpdate>? OnTickUpdate;

    public int EntityCount { get; }

    public SimulationWorld(TerrainMap terrain, TerrainGenerationOptions terrainOptions) {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(terrainOptions);

        Random rng = new(terrainOptions.Seed);
        storage = new EntityStorage();
        positionSystem = new PositionSystem(storage, terrain.Size);

        SheepSpawner spawner = new(storage, terrain.Size, rng);
        EntityCount = spawner.SpawnFlock(24);
    }

    public void Tick(float deltaTime) {
        var locationChanges = positionSystem.Update(deltaTime);
        OnTickUpdate?.Invoke(new SimulationTickUpdate(++tickSequence, locationChanges));
    }
}
