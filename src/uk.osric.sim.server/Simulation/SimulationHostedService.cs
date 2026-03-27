// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;

using Microsoft.Extensions.Options;

using uk.osric.sim.contracts.Simulation;
using uk.osric.sim.server.Terrain;
using uk.osric.sim.simulation;
using uk.osric.sim.simulation.Time;

namespace uk.osric.sim.server.Simulation;

public sealed class SimulationHostedService : BackgroundService {
    private readonly TerrainSnapshot terrainSnapshot;
    private readonly SimulationOptions options;
    private readonly SimulationMetrics metrics;
    private readonly SimulationTickBroadcaster tickBroadcaster;
    private readonly ILogger<SimulationHostedService> logger;
    private readonly Lock worldLock = new();
    private SimulationWorld? world;

    public SimulationHostedService(
        TerrainSnapshot terrainSnapshot,
        IOptions<SimulationOptions> options,
        SimulationMetrics metrics,
        SimulationTickBroadcaster tickBroadcaster,
        ILogger<SimulationHostedService> logger) {
        ArgumentNullException.ThrowIfNull(terrainSnapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);

        this.terrainSnapshot = terrainSnapshot;
        this.options = options.Value;
        this.metrics = metrics;
        this.tickBroadcaster = tickBroadcaster;
        this.logger = logger;
    }

    internal bool TryGetActorSnapshot(out IReadOnlyList<SimulationActorSnapshotDto> actors) {
        SimulationWorld? currentWorld;
        lock (worldLock) {
            currentWorld = world;
        }

        if (currentWorld is null) {
            actors = [];
            return false;
        }

        actors = currentWorld
            .GetSheepLocations()
            .Select(location => new SimulationActorSnapshotDto(
                location.Id.Value,
                location.Location.X,
                location.Location.Y,
                location.VelocityX,
                location.VelocityY,
                location.Radius))
            .ToArray();

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        SimulationWorld simulationWorld = new(terrainSnapshot.Map, terrainSnapshot.Options);
        simulationWorld.OnTickUpdate += PublishTickUpdate;

        lock (worldLock) {
            world = simulationWorld;
        }

        logger.LogInformation(
            "Simulation started on terrain {Size}x{Size} at {TickRateHz} Hz with {EntityCount} entities",
            terrainSnapshot.Map.Size,
            terrainSnapshot.Map.Size,
            options.TickRateHz,
            simulationWorld.EntityCount);

        TimeSpan tickInterval = TimeSpan.FromSeconds(1.0 / options.TickRateHz);
        float deltaTime = (float)tickInterval.TotalSeconds;

        try {
            while (!stoppingToken.IsCancellationRequested) {
                long startTimestamp = Stopwatch.GetTimestamp();
                simulationWorld.Tick(deltaTime);
                TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                metrics.TickDuration.Record(elapsed.TotalMilliseconds);

                await Task.Delay(tickInterval, stoppingToken);
            }
        }
        finally {
            simulationWorld.OnTickUpdate -= PublishTickUpdate;

            lock (worldLock) {
                world = null;
            }
        }

        logger.LogInformation("Simulation stopped");
    }

    private void PublishTickUpdate(SimulationTickUpdate update) {
        SimulationTickDto tick = new(
            update.TickSequence,
            options.TickRateHz,
            DateTimeOffset.UtcNow,
            update.LocationChanges
                .Select(change => new SimulationActorLocationDto(
                    change.Id.Value,
                    change.Location.X,
                    change.Location.Y,
                    change.VelocityX,
                    change.VelocityY))
                .ToArray());

        tickBroadcaster.Publish(tick);
    }
}
