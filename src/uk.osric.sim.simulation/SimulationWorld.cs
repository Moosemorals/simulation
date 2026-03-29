// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.simulation.Ecs;
using uk.osric.sim.simulation.Ecs.Components;
using uk.osric.sim.simulation.Ecs.Systems;
using uk.osric.sim.terrain;

namespace uk.osric.sim.simulation;

public sealed class SimulationWorld {
    private readonly EntityStorage storage;
    private readonly PositionSystem positionSystem;
    private readonly Lock syncRoot = new();
    private int tickSequence;

    public event Action<SimulationTickUpdate>? OnTickUpdate;

    public int EntityCount { get; }

    public SimulationWorld(TerrainMap terrain, TerrainConfiguration terrainConfiguration) {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(terrainConfiguration);

        Random rng = new(terrainConfiguration.Seed);
        storage = new EntityStorage();
        positionSystem = new PositionSystem(storage, terrain.Size);

        SheepSpawner spawner = new(storage, terrain.Size, rng);
        EntityCount = spawner.SpawnFlock(24);
    }

    public void Tick(float deltaTime) {
        SimulationTickUpdate tickUpdate;

        lock (syncRoot) {
            var locationChanges = positionSystem.Update(deltaTime);
            tickUpdate = new SimulationTickUpdate(++tickSequence, locationChanges);
        }

        OnTickUpdate?.Invoke(tickUpdate);
    }

    public IReadOnlyList<(Ecs.EntityId Id, Position Location, float VelocityX, float VelocityY, float Radius)> GetSheepLocations() {
        lock (syncRoot) {
            var sheepLocations = new List<(Ecs.EntityId Id, Position Location, float VelocityX, float VelocityY, float Radius)>();

            foreach (var (id, location, velocity, _) in storage.Query<Position, Velocity, Sheep>()) {
                float radius = storage.Get<Size>(id).Radius;
                sheepLocations.Add((id, location, velocity.X, velocity.Y, radius));
            }

            return sheepLocations;
        }
    }
}
