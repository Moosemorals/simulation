// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

internal sealed class RandomRaindropErosionLayer {
    public float[] Apply(float[] heightData, int size, int erosionPasses, int seed, RandomRaindropErosionTuning tuning) {
        int cellCount = heightData.Length;
        float[] waterAccumulation = new float[cellCount];
        bool includeDiagonalNeighbors = tuning.NeighborSampleCount == 8;
        Random random = new(seed);

        for (int drop = 0; drop < erosionPasses; drop++) {
            int x = random.Next(size);
            int y = random.Next(size);
            float carriedSediment = 0.0f;

            for (int step = 0; step < tuning.DropPathLength; step++) {
                int currentIndex = ToroidalGrid.Index(x, y, size);
                waterAccumulation[currentIndex] += 1.0f;

                int nextIndex = FindDownhillNeighbor(heightData, size, x, y, includeDiagonalNeighbors);
                if (nextIndex < 0) {
                    if (carriedSediment > 0.0f) {
                        heightData[currentIndex] += carriedSediment;
                    }
                    break;
                }

                float erosion = MathF.Min(heightData[currentIndex], tuning.ErosionStrength);
                if (erosion > 0.0f) {
                    heightData[currentIndex] -= erosion;
                    carriedSediment += erosion;
                }

                float deposited = carriedSediment * tuning.DepositionRatio;
                if (deposited > 0.0f) {
                    heightData[nextIndex] += deposited;
                    carriedSediment -= deposited;
                }

                y = nextIndex / size;
                x = nextIndex % size;
            }

            if (carriedSediment > 0.0f) {
                int finalIndex = ToroidalGrid.Index(x, y, size);
                heightData[finalIndex] += carriedSediment;
            }

            for (int i = 0; i < cellCount; i++) {
                if (heightData[i] < 0.0f) {
                    heightData[i] = 0.0f;
                }
            }

            ToroidalGrid.EnforceToroidalSeams(heightData, size);
        }

        Normalize(heightData);
        NormalizeToMax(waterAccumulation);
        ToroidalGrid.EnforceToroidalSeams(heightData, size);
        ToroidalGrid.EnforceToroidalSeams(waterAccumulation, size);

        return waterAccumulation;
    }

    private static int FindDownhillNeighbor(float[] heightData, int size, int x, int y, bool includeDiagonalNeighbors) {
        int left = ToroidalGrid.Wrap(x - 1, size);
        int right = ToroidalGrid.Wrap(x + 1, size);
        int up = ToroidalGrid.Wrap(y - 1, size);
        int down = ToroidalGrid.Wrap(y + 1, size);

        int currentIndex = ToroidalGrid.Index(x, y, size);
        float currentHeight = heightData[currentIndex];
        int bestIndex = -1;
        float bestDrop = 0.0f;

        ConsiderNeighbor(heightData, (up * size) + x, currentHeight, ref bestDrop, ref bestIndex);
        ConsiderNeighbor(heightData, (y * size) + left, currentHeight, ref bestDrop, ref bestIndex);
        ConsiderNeighbor(heightData, (y * size) + right, currentHeight, ref bestDrop, ref bestIndex);
        ConsiderNeighbor(heightData, (down * size) + x, currentHeight, ref bestDrop, ref bestIndex);

        if (includeDiagonalNeighbors) {
            ConsiderNeighbor(heightData, (up * size) + left, currentHeight, ref bestDrop, ref bestIndex);
            ConsiderNeighbor(heightData, (up * size) + right, currentHeight, ref bestDrop, ref bestIndex);
            ConsiderNeighbor(heightData, (down * size) + left, currentHeight, ref bestDrop, ref bestIndex);
            ConsiderNeighbor(heightData, (down * size) + right, currentHeight, ref bestDrop, ref bestIndex);
        }

        return bestIndex;
    }

    private static void ConsiderNeighbor(float[] heightData, int neighborIndex, float currentHeight, ref float bestDrop, ref int bestIndex) {
        float drop = currentHeight - heightData[neighborIndex];
        if (drop > bestDrop) {
            bestDrop = drop;
            bestIndex = neighborIndex;
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
