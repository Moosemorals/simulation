// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Terrain;

public sealed class TerrainSnapshot {
    private const int FallbackSeed = 1729;
    private const int FallbackSize = 513;
    private const int FallbackErosionPasses = 1;
    private const int FallbackUpscaleFactor = 1;

    public TerrainSnapshot(ITerrainGenerator generator, IConfiguration configuration, ILogger<TerrainSnapshot> logger) {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(configuration);

        Options = BuildOptions(configuration);

        logger.LogInformation(
            "Generating terrain: seed={Seed}, size={Size}, erosionPasses={ErosionPasses}, upscaleFactor={UpscaleFactor}",
            Options.Seed, Options.Size, Options.ErosionPasses, Options.UpscaleFactor);

        Stopwatch sw = Stopwatch.StartNew();
        Map = generator.Generate(Options);
        sw.Stop();

        logger.LogInformation("Terrain generation complete in {ElapsedMs} ms", sw.ElapsedMilliseconds);

        HeightBytes = TerrainMapEncoding.EncodeHeight(Map);
        WaterAccumulationBytes = TerrainMapEncoding.EncodeFloats(Map.WaterAccumulationData);
        RiverMaskBytes = TerrainMapEncoding.EncodeMask(Map.RiverMask);
        LakeMaskBytes = TerrainMapEncoding.EncodeMask(Map.LakeMask);
    }

    public TerrainGenerationOptions Options { get; }

    public TerrainMap Map { get; }

    public byte[] HeightBytes { get; }

    public byte[] WaterAccumulationBytes { get; }

    public byte[] RiverMaskBytes { get; }

    public byte[] LakeMaskBytes { get; }

    private static TerrainGenerationOptions BuildOptions(IConfiguration configuration) {
        RandomRaindropErosionTuning defaults = RandomRaindropErosionTuning.Default;

        return new TerrainGenerationOptions {
            Seed = configuration.GetValue<int?>("Terrain:DefaultSeed") ?? FallbackSeed,
            Size = configuration.GetValue<int?>("Terrain:DefaultSize") ?? FallbackSize,
            BaseAlgorithm = "diamond-square",
            ErosionPasses = configuration.GetValue<int?>("Terrain:ErosionPasses") ?? FallbackErosionPasses,
            UpscaleFactor = configuration.GetValue<int?>("Terrain:UpscaleFactor") ?? FallbackUpscaleFactor,
            RaindropErosion = new RandomRaindropErosionTuning {
                DropPathLength = configuration.GetValue<int?>("Terrain:RaindropErosion:DropPathLength") ?? defaults.DropPathLength,
                NeighborSampleCount = configuration.GetValue<int?>("Terrain:RaindropErosion:NeighborSampleCount") ?? defaults.NeighborSampleCount,
                ErosionStrength = configuration.GetValue<float?>("Terrain:RaindropErosion:ErosionStrength") ?? defaults.ErosionStrength,
                DepositionRatio = configuration.GetValue<float?>("Terrain:RaindropErosion:DepositionRatio") ?? defaults.DepositionRatio,
            },
        };
    }
}