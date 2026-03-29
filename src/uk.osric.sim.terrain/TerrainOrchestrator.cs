// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.terrain;

public sealed class TerrainOrchestrator : ITerrainGenerator {
    public TerrainMap Generate(TerrainConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        Torus<float> heightData = DiamondSquareTerrainGenerator.GenerateHeightData(configuration);
        Torus<float> waterAccumulationData = ErosionLayer.Apply(
            heightData,
            configuration.Size,
            configuration.Seed,
            configuration.Erosion);
        (Torus<bool> riverMask, Torus<bool> lakeMask) = RiverLakeDetectionLayer.Build(heightData, waterAccumulationData, configuration.Size);

        UpscaledTerrainData upscaled = BicubicUpscaleLayer.Apply(
            heightData,
            waterAccumulationData,
            riverMask,
            lakeMask,
            configuration.Size,
            configuration.UpscaleFactor);

        return new TerrainMap {
            Size = upscaled.Size,
            HeightData = upscaled.HeightData,
            WaterAccumulationData = upscaled.WaterAccumulationData,
            RiverMask = upscaled.RiverMask,
            LakeMask = upscaled.LakeMask,
        };
    }
}
