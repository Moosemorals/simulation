// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

internal sealed class DiamondSquareTerrainGenerator {
    public static float[] GenerateHeightData(TerrainGenerationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return GenerateHeightField(options.Size, options.Seed, options.InitialDisplacement, options.Roughness);
    }

    private static float[] GenerateHeightField(int size, int seed, float initialDisplacement, float roughness) {
        float[] heightData = new float[size * size];
        Random rng = new(seed);

        float cornerValue = NextDisplacement(rng, initialDisplacement);
        ToroidalGrid.Set(heightData, 0, 0, size, cornerValue);
        ToroidalGrid.Set(heightData, size - 1, 0, size, cornerValue);
        ToroidalGrid.Set(heightData, 0, size - 1, size, cornerValue);
        ToroidalGrid.Set(heightData, size - 1, size - 1, size, cornerValue);

        int step = size - 1;
        float displacement = initialDisplacement;

        while (step > 1) {
            int halfStep = step / 2;

            for (int y = halfStep; y < size; y += step) {
                for (int x = halfStep; x < size; x += step) {
                    float a = ToroidalGrid.Get(heightData, x - halfStep, y - halfStep, size);
                    float b = ToroidalGrid.Get(heightData, x + halfStep, y - halfStep, size);
                    float c = ToroidalGrid.Get(heightData, x - halfStep, y + halfStep, size);
                    float d = ToroidalGrid.Get(heightData, x + halfStep, y + halfStep, size);
                    float average = (a + b + c + d) * 0.25f;
                    float value = average + NextDisplacement(rng, displacement);
                    ToroidalGrid.Set(heightData, x, y, size, value);
                }
            }

            for (int y = 0; y < size; y += halfStep) {
                int startX = ((y / halfStep) % 2 == 0) ? halfStep : 0;
                for (int x = startX; x < size; x += step) {
                    float left = ToroidalGrid.Get(heightData, x - halfStep, y, size);
                    float right = ToroidalGrid.Get(heightData, x + halfStep, y, size);
                    float up = ToroidalGrid.Get(heightData, x, y - halfStep, size);
                    float down = ToroidalGrid.Get(heightData, x, y + halfStep, size);
                    float average = (left + right + up + down) * 0.25f;
                    float value = average + NextDisplacement(rng, displacement);
                    ToroidalGrid.Set(heightData, x, y, size, value);
                }
            }

            step /= 2;
            displacement *= roughness;
        }

        // Force explicit seam continuity for toroidal edge tiles.
        ToroidalGrid.EnforceToroidalSeams(heightData, size);

        Normalize(heightData);
        return heightData;
    }

    private static float NextDisplacement(Random rng, float amplitude) {
        return ((float)rng.NextDouble() * 2.0f - 1.0f) * amplitude;
    }

    private static void Normalize(float[] heightData) {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < heightData.Length; i++) {
            float value = heightData[i];
            if (value < min) {
                min = value;
            }

            if (value > max) {
                max = value;
            }
        }

        float range = max - min;
        if (range <= 0.0f) {
            for (int i = 0; i < heightData.Length; i++) {
                heightData[i] = 0.5f;
            }

            return;
        }

        for (int i = 0; i < heightData.Length; i++) {
            heightData[i] = (heightData[i] - min) / range;
        }
    }
}