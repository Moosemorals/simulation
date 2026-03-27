// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

internal sealed class HydraulicErosionLayer {
    public float[] Apply(float[] heightData, int size, int erosionPasses) {
        float[] finalAccumulation = BuildFlowAccumulation(heightData, size);

        for (int pass = 0; pass < erosionPasses; pass++) {
            ErodeAndDeposit(heightData, finalAccumulation, size);
            EnforceToroidalSeams(heightData, size);
            finalAccumulation = BuildFlowAccumulation(heightData, size);
        }

        Normalize(heightData);
        NormalizeToMax(finalAccumulation);
        EnforceToroidalSeams(finalAccumulation, size);
        return finalAccumulation;
    }

    private static float[] BuildFlowAccumulation(float[] heightData, int size) {
        int cellCount = heightData.Length;
        float[] accumulation = new float[cellCount];
        int[] indices = new int[cellCount];

        for (int i = 0; i < cellCount; i++) {
            accumulation[i] = 1.0f;
            indices[i] = i;
        }

        Array.Sort(indices, (left, right) => heightData[right].CompareTo(heightData[left]));

        for (int i = 0; i < indices.Length; i++) {
            int currentIndex = indices[i];
            int x = currentIndex % size;
            int y = currentIndex / size;
            int downhillIndex = FindSteepestDownhillIndex(heightData, size, x, y);

            if (downhillIndex >= 0) {
                accumulation[downhillIndex] += accumulation[currentIndex];
            }
        }

        return accumulation;
    }

    private static void ErodeAndDeposit(float[] heightData, float[] accumulation, int size) {
        float[] delta = new float[heightData.Length];

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                int index = y * size + x;
                int downhillIndex = FindSteepestDownhillIndex(heightData, size, x, y);
                if (downhillIndex < 0) {
                    continue;
                }

                float slope = heightData[index] - heightData[downhillIndex];
                if (slope <= 0.0f) {
                    continue;
                }

                float flowFactor = accumulation[index] / (size * size);
                float erosion = MathF.Min(heightData[index] * 0.15f, slope * flowFactor * 0.35f);
                float deposition = erosion * 0.25f;

                delta[index] -= erosion;
                delta[downhillIndex] += deposition;
            }
        }

        for (int i = 0; i < heightData.Length; i++) {
            heightData[i] += delta[i];
        }
    }

    private static int FindSteepestDownhillIndex(float[] heightData, int size, int x, int y) {
        int bestIndex = -1;
        float currentHeight = heightData[y * size + x];
        float bestDrop = 0.0f;

        for (int offsetY = -1; offsetY <= 1; offsetY++) {
            for (int offsetX = -1; offsetX <= 1; offsetX++) {
                if (offsetX == 0 && offsetY == 0) {
                    continue;
                }

                int neighborX = ToroidalGrid.Wrap(x + offsetX, size);
                int neighborY = ToroidalGrid.Wrap(y + offsetY, size);
                int neighborIndex = neighborY * size + neighborX;
                float drop = currentHeight - heightData[neighborIndex];

                if (drop > bestDrop) {
                    bestDrop = drop;
                    bestIndex = neighborIndex;
                }
            }
        }

        return bestIndex;
    }

    private static void EnforceToroidalSeams(float[] grid, int size) {
        for (int i = 0; i < size; i++) {
            float topValue = ToroidalGrid.Get(grid, i, 0, size);
            ToroidalGrid.Set(grid, i, size - 1, size, topValue);

            float leftValue = ToroidalGrid.Get(grid, 0, i, size);
            ToroidalGrid.Set(grid, size - 1, i, size, leftValue);
        }
    }

    private static void Normalize(float[] values) {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < values.Length; i++) {
            float value = values[i];
            if (value < min) {
                min = value;
            }

            if (value > max) {
                max = value;
            }
        }

        float range = max - min;
        if (range <= 0.0f) {
            for (int i = 0; i < values.Length; i++) {
                values[i] = 0.5f;
            }

            return;
        }

        for (int i = 0; i < values.Length; i++) {
            values[i] = (values[i] - min) / range;
        }
    }

    private static void NormalizeToMax(float[] values) {
        float max = 0.0f;
        for (int i = 0; i < values.Length; i++) {
            if (values[i] > max) {
                max = values[i];
            }
        }

        if (max <= 0.0f) {
            return;
        }

        for (int i = 0; i < values.Length; i++) {
            values[i] /= max;
        }
    }
}