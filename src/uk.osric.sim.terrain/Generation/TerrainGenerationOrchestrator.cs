// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainGenerationOrchestrator : ITerrainGenerator {
    private readonly DiamondSquareTerrainGenerator diamondSquareLayer;
    private readonly RandomRaindropErosionLayer randomRaindropErosionLayer;
    private readonly RiverLakeDetectionLayer riverLakeDetectionLayer;
    private readonly BicubicUpscaleLayer bicubicUpscaleLayer;

    public TerrainGenerationOrchestrator() {
        diamondSquareLayer = new DiamondSquareTerrainGenerator();
        randomRaindropErosionLayer = new RandomRaindropErosionLayer();
        riverLakeDetectionLayer = new RiverLakeDetectionLayer();
        bicubicUpscaleLayer = new BicubicUpscaleLayer();
    }

    public TerrainMap Generate(TerrainGenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!string.Equals(options.BaseAlgorithm, "diamond-square", StringComparison.OrdinalIgnoreCase)) {
            throw new NotSupportedException($"Unsupported terrain algorithm '{options.BaseAlgorithm}'.");
        }

        float[] heightData = diamondSquareLayer.GenerateHeightData(options);
        float[] waterAccumulationData = randomRaindropErosionLayer.Apply(
            heightData,
            options.Size,
            options.ErosionPasses,
            options.Seed,
            options.RaindropErosion);
        (bool[] riverMask, bool[] lakeMask) = riverLakeDetectionLayer.Build(heightData, waterAccumulationData, options.Size);

        UpscaledTerrainData upscaled = bicubicUpscaleLayer.Apply(
            heightData,
            waterAccumulationData,
            riverMask,
            lakeMask,
            options.Size,
            options.UpscaleFactor);

        return new TerrainMap {
            Size = upscaled.Size,
            HeightData = upscaled.HeightData,
            WaterAccumulationData = upscaled.WaterAccumulationData,
            RiverMask = upscaled.RiverMask,
            LakeMask = upscaled.LakeMask,
        };
    }
}
