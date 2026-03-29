// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

internal sealed class BicubicUpscaleLayer {
    public static UpscaledTerrainData Apply(
        Torus<float> heightData,
        Torus<float> waterAccumulationData,
        Torus<bool> riverMask,
        Torus<bool> lakeMask,
        int sourceSize,
        int upscaleFactor) {
        ArgumentNullException.ThrowIfNull(heightData);
        ArgumentNullException.ThrowIfNull(waterAccumulationData);
        ArgumentNullException.ThrowIfNull(riverMask);
        ArgumentNullException.ThrowIfNull(lakeMask);

        if (upscaleFactor <= 1) {
            return new UpscaledTerrainData(sourceSize, heightData, waterAccumulationData, riverMask, lakeMask);
        }

        int targetSize = sourceSize * upscaleFactor;

        Torus<float> upscaledHeightData = UpscaleFloatGrid(heightData, sourceSize, targetSize, upscaleFactor);
        Torus<float> upscaledWaterAccumulationData = UpscaleFloatGrid(waterAccumulationData, sourceSize, targetSize, upscaleFactor);
        Torus<bool> upscaledRiverMask = UpscaleMask(riverMask, sourceSize, targetSize, upscaleFactor);
        Torus<bool> upscaledLakeMask = UpscaleMask(lakeMask, sourceSize, targetSize, upscaleFactor);

        return new UpscaledTerrainData(targetSize, upscaledHeightData, upscaledWaterAccumulationData, upscaledRiverMask, upscaledLakeMask);
    }

    private static Torus<float> UpscaleFloatGrid(Torus<float> source, int sourceSize, int targetSize, int upscaleFactor) {
        Torus<float> target = new(targetSize);

        for (int y = 0; y < targetSize; y++) {
            float sourceY = (float)y / upscaleFactor;
            int sourceYFloor = (int)MathF.Floor(sourceY);
            float yFraction = sourceY - sourceYFloor;

            for (int x = 0; x < targetSize; x++) {
                float sourceX = (float)x / upscaleFactor;
                int sourceXFloor = (int)MathF.Floor(sourceX);
                float xFraction = sourceX - sourceXFloor;

                float[] rowSamples = new float[4];
                for (int rowOffset = -1; rowOffset <= 2; rowOffset++) {
                    int sampleY = sourceYFloor + rowOffset;
                    rowSamples[rowOffset + 1] = CubicInterpolate(
                        source[sourceXFloor - 1, sampleY],
                        source[sourceXFloor, sampleY],
                        source[sourceXFloor + 1, sampleY],
                        source[sourceXFloor + 2, sampleY],
                        xFraction);
                }

                target[x, y] = Math.Clamp(
                    CubicInterpolate(rowSamples[0], rowSamples[1], rowSamples[2], rowSamples[3], yFraction),
                    0.0f,
                    1.0f);
            }
        }

        return target;
    }

    private static Torus<bool> UpscaleMask(Torus<bool> source, int sourceSize, int targetSize, int upscaleFactor) {
        Torus<bool> target = new(targetSize);

        for (int y = 0; y < targetSize; y++) {
            int sourceY = Math.Min(y / upscaleFactor, sourceSize - 1);

            for (int x = 0; x < targetSize; x++) {
                int sourceX = Math.Min(x / upscaleFactor, sourceSize - 1);
                target[x, y] = source[sourceX, sourceY];
            }
        }

        return target;
    }

    private static float CubicInterpolate(float p0, float p1, float p2, float p3, float t) {
        float a = (-0.5f * p0) + (1.5f * p1) - (1.5f * p2) + (0.5f * p3);
        float b = p0 - (2.5f * p1) + (2.0f * p2) - (0.5f * p3);
        float c = (-0.5f * p0) + (0.5f * p2);
        float d = p1;
        return ((a * t + b) * t + c) * t + d;
    }

}

internal readonly record struct UpscaledTerrainData(
    int Size,
    Torus<float> HeightData,
    Torus<float> WaterAccumulationData,
    Torus<bool> RiverMask,
    Torus<bool> LakeMask
);