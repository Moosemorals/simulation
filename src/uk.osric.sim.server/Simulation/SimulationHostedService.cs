// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.Extensions.Options;

using uk.osric.sim.server.Terrain;
using uk.osric.sim.simulation;
using uk.osric.sim.simulation.Time;

namespace uk.osric.sim.server.Simulation;

internal sealed class SimulationHostedService : BackgroundService {
    private readonly TerrainSnapshot terrainSnapshot;
    private readonly SimulationOptions options;
    private readonly ILogger<SimulationHostedService> logger;

    public SimulationHostedService(
        TerrainSnapshot terrainSnapshot,
        IOptions<SimulationOptions> options,
        ILogger<SimulationHostedService> logger) {
        ArgumentNullException.ThrowIfNull(terrainSnapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this.terrainSnapshot = terrainSnapshot;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        SimulationWorld world = new();

        logger.LogInformation(
            "Simulation started on terrain {Size}x{Size} at {TickRateHz} Hz",
            terrainSnapshot.Map.Size,
            terrainSnapshot.Map.Size,
            options.TickRateHz);

        TimeSpan tickInterval = TimeSpan.FromSeconds(1.0 / options.TickRateHz);

        while (!stoppingToken.IsCancellationRequested) {
            world.Tick();
            await Task.Delay(tickInterval, stoppingToken);
        }

        logger.LogInformation("Simulation stopped");
    }
}
