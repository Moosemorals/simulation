// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Terrain;

public sealed class TerrainSnapshot {
    private const int FallbackSeed = 1729;
    private const int FallbackSize = 513;
    private const int FallbackErosionPasses = 1;

    public TerrainSnapshot(ITerrainGenerator generator, IConfiguration configuration, ILogger<TerrainSnapshot> logger) {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(configuration);

        Options = BuildOptions(configuration);

        logger.LogInformation(
            "Generating terrain: seed={Seed}, size={Size}, erosionPasses={ErosionPasses}",
            Options.Seed, Options.Size, Options.ErosionPasses);

        Stopwatch sw = Stopwatch.StartNew();
        Map = generator.Generate(Options);
        sw.Stop();

        logger.LogInformation("Terrain generation complete in {ElapsedMs} ms", sw.ElapsedMilliseconds);

        HeightBytes = TerrainHeightEncoding.ToGreyscaleBytes(Map);
    }

    public TerrainGenerationOptions Options { get; }

    public TerrainMap Map { get; }

    public byte[] HeightBytes { get; }

    private static TerrainGenerationOptions BuildOptions(IConfiguration configuration) {
        return new TerrainGenerationOptions {
            Seed = configuration.GetValue<int?>("Terrain:DefaultSeed") ?? FallbackSeed,
            Size = configuration.GetValue<int?>("Terrain:DefaultSize") ?? FallbackSize,
            BaseAlgorithm = "diamond-square",
            ErosionPasses = configuration.GetValue<int?>("Terrain:ErosionPasses") ?? FallbackErosionPasses,
        };
    }
}