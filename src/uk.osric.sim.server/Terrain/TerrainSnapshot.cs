// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;
using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.terrain;

namespace uk.osric.sim.server.Terrain;

public sealed class TerrainSnapshot {
    public TerrainSnapshot(ITerrainGenerator generator, IConfiguration configuration, ILogger<TerrainSnapshot> logger) {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(configuration);

        Options = BuildOptions(configuration);

        logger.LogInformation(
            "Generating terrain: seed={Seed}, size={Size}, raindrops={Raindrops}, upscaleFactor={UpscaleFactor}",
            Options.Seed, Options.Size, Options.Erosion.Raindrops, Options.UpscaleFactor);

        Stopwatch sw = Stopwatch.StartNew();
        Map = generator.Generate(Options);
        sw.Stop();

        logger.LogInformation("Terrain generation complete in {ElapsedMs} ms", sw.ElapsedMilliseconds);

        HeightFloatBytes = TerrainMapEncoding.EncodeFloat32(Map.HeightData);
        WaterAccumulationFloatBytes = TerrainMapEncoding.EncodeFloat32(Map.WaterAccumulationData);
        RiverMaskBytes = TerrainMapEncoding.EncodeMask(Map.RiverMask);
        LakeMaskBytes = TerrainMapEncoding.EncodeMask(Map.LakeMask);
    }

    public TerrainConfiguration Options { get; }

    public TerrainMap Map { get; }

    public byte[] HeightFloatBytes { get; }

    public byte[] WaterAccumulationFloatBytes { get; }

    public byte[] RiverMaskBytes { get; }

    public byte[] LakeMaskBytes { get; }

    private static TerrainConfiguration BuildOptions(IConfiguration configuration) {
        TerrainConfiguration terrainConfiguration = configuration
            .GetSection("Terrain")
            .Get<TerrainConfiguration>()
            ?? new TerrainConfiguration {
                Seed = 1729,
                Size = 64,
            };

        terrainConfiguration.Validate();
        return terrainConfiguration;
    }
}