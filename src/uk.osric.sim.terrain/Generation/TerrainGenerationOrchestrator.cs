// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainGenerationOrchestrator : ITerrainGenerator {
    private readonly DiamondSquareTerrainGenerator diamondSquareLayer;
    private readonly HydraulicErosionLayer hydraulicErosionLayer;

    public TerrainGenerationOrchestrator() {
        diamondSquareLayer = new DiamondSquareTerrainGenerator();
        hydraulicErosionLayer = new HydraulicErosionLayer();
    }

    public TerrainMap Generate(TerrainGenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!string.Equals(options.BaseAlgorithm, "diamond-square", StringComparison.OrdinalIgnoreCase)) {
            throw new NotSupportedException($"Unsupported terrain algorithm '{options.BaseAlgorithm}'.");
        }

        float[] heightData = diamondSquareLayer.GenerateHeightData(options);
        float[] waterAccumulationData = hydraulicErosionLayer.Apply(heightData, options.Size, options.ErosionPasses);

        return new TerrainMap {
            Size = options.Size,
            HeightData = heightData,
            WaterAccumulationData = waterAccumulationData,
        };
    }
}
