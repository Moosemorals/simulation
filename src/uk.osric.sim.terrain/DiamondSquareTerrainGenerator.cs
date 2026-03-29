// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.contracts.Terrain;

namespace uk.osric.sim.terrain;

internal sealed class DiamondSquareTerrainGenerator {
    public static Torus<float> GenerateHeightData(TerrainConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration);

        return GenerateHeightField(
            configuration.Size,
            configuration.Seed,
            configuration.DiamondSquare.InitialDisplacement,
            configuration.DiamondSquare.Roughness,
            configuration.DiamondSquare.SmoothnessStopStep);
    }

    private static Torus<float> GenerateHeightField(int size, int seed, float initialDisplacement, float roughness, int smoothnessStopStep) {
        Torus<float> heightData = new(size);
        Random rng = new(seed);

        heightData[0, 0] = NextDisplacement(rng, initialDisplacement);

        int step = size;
        float displacement = initialDisplacement;

        while (step > 1) {
            int halfStep = step / 2;

            for (int y = 0; y < size; y += step) {
                for (int x = 0; x < size; x += step) {
                    int centerX = x + halfStep;
                    int centerY = y + halfStep;

                    float a = heightData[x, y];
                    float b = heightData[x + step, y];
                    float c = heightData[x, y + step];
                    float d = heightData[x + step, y + step];
                    float average = (a + b + c + d) * 0.25f;
                    float value = average + NextDisplacement(rng, displacement);
                    heightData[centerX, centerY] = value;
                }
            }

            for (int y = 0; y < size; y += halfStep) {
                int startX = ((y / halfStep) % 2 == 0) ? 0 : halfStep;
                for (int x = startX; x < size; x += step) {
                    float left = heightData[x - halfStep, y];
                    float right = heightData[x + halfStep, y];
                    float up = heightData[x, y - halfStep];
                    float down = heightData[x, y + halfStep];
                    float average = (left + right + up + down) * 0.25f;
                    float value = average + NextDisplacement(rng, displacement);
                    heightData[x, y] = value;
                }
            }

            step /= 2;
            displacement *= roughness;

            // Stop adding fine detail when below the requested threshold.
            // Any unfilled cells will be interpolated below.
            if (step <= smoothnessStopStep) {
                break;
            }
        }

        // When we stopped early the grid is only filled at multiples of `step`.
        // Bilinearly interpolate everything in between so no cell is left at its
        // default zero value.
        if (step > 1) {
            BilinearFill(heightData, size, step);
        }

        Normalize(heightData);
        return heightData;
    }

    // Fills every cell that is not on the coarse grid (multiples of gridSpacing)
    // using bilinear interpolation between its four enclosing grid corners.
    private static void BilinearFill(Torus<float> heightData, int size, int gridSpacing) {
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                if (x % gridSpacing == 0 && y % gridSpacing == 0) {
                    continue;
                }

                int x0 = (x / gridSpacing) * gridSpacing;
                int y0 = (y / gridSpacing) * gridSpacing;

                float tx = (float)(x - x0) / gridSpacing;
                float ty = (float)(y - y0) / gridSpacing;

                float tl = heightData[x0, y0];
                float tr = heightData[x0 + gridSpacing, y0];
                float bl = heightData[x0, y0 + gridSpacing];
                float br = heightData[x0 + gridSpacing, y0 + gridSpacing];

                heightData[x, y] = tl * (1 - tx) * (1 - ty)
                                  + tr * tx * (1 - ty)
                                  + bl * (1 - tx) * ty
                                  + br * tx * ty;
            }
        }
    }

    private static float NextDisplacement(Random rng, float amplitude) {
        return ((float)rng.NextDouble() * 2.0f - 1.0f) * amplitude;
    }

    private static void Normalize(Torus<float> heightData) {
        float min = float.MaxValue;
        float max = float.MinValue;
        int size = heightData.Size;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float value = heightData[x, y];
                if (value < min) {
                    min = value;
                }

                if (value > max) {
                    max = value;
                }
            }
        }

        float range = max - min;
        if (range <= 0.0f) {
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    heightData[x, y] = 0.5f;
                }
            }

            return;
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                heightData[x, y] = (heightData[x, y] - min) / range;
            }
        }
    }
}