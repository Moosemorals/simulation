// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

internal sealed class RandomRaindropErosionLayer {
    public static Torus<float> Apply(Torus<float> heightData, int size, int erosionPasses, int seed, RandomRaindropErosionTuning tuning) {
        Torus<float> waterAccumulation = new(size);
        bool includeDiagonalNeighbors = tuning.NeighborSampleCount == 8;
        Random random = new(seed);

        for (int drop = 0; drop < erosionPasses; drop++) {
            int x = random.Next(size);
            int y = random.Next(size);
            float carriedSediment = 0.0f;

            for (int step = 0; step < tuning.DropPathLength; step++) {
                waterAccumulation[x, y] += 1.0f;

                bool hasNeighbor = TryFindDownhillNeighbor(heightData, x, y, includeDiagonalNeighbors, out int nextX, out int nextY);
                if (!hasNeighbor) {
                    if (carriedSediment > 0.0f) {
                        heightData[x, y] += carriedSediment;
                    }
                    break;
                }

                float erosion = MathF.Min(heightData[x, y], tuning.ErosionStrength);
                if (erosion > 0.0f) {
                    heightData[x, y] -= erosion;
                    carriedSediment += erosion;
                }

                float deposited = carriedSediment * tuning.DepositionRatio;
                if (deposited > 0.0f) {
                    heightData[nextX, nextY] += deposited;
                    carriedSediment -= deposited;
                }

                x = nextX;
                y = nextY;
            }

            if (carriedSediment > 0.0f) {
                heightData[x, y] += carriedSediment;
            }

            for (int row = 0; row < size; row++) {
                for (int col = 0; col < size; col++) {
                    if (heightData[col, row] < 0.0f) {
                        heightData[col, row] = 0.0f;
                    }
                }
            }
        }

        Normalize(heightData);
        NormalizeToMax(waterAccumulation);

        return waterAccumulation;
    }

    private static bool TryFindDownhillNeighbor(Torus<float> heightData, int x, int y, bool includeDiagonalNeighbors, out int bestX, out int bestY) {
        float currentHeight = heightData[x, y];
        float bestDrop = 0.0f;
        bestX = x;
        bestY = y;

        ConsiderNeighbor(heightData, x, y, x, y - 1, currentHeight, ref bestDrop, ref bestX, ref bestY);
        ConsiderNeighbor(heightData, x, y, x - 1, y, currentHeight, ref bestDrop, ref bestX, ref bestY);
        ConsiderNeighbor(heightData, x, y, x + 1, y, currentHeight, ref bestDrop, ref bestX, ref bestY);
        ConsiderNeighbor(heightData, x, y, x, y + 1, currentHeight, ref bestDrop, ref bestX, ref bestY);

        if (includeDiagonalNeighbors) {
            ConsiderNeighbor(heightData, x, y, x - 1, y - 1, currentHeight, ref bestDrop, ref bestX, ref bestY);
            ConsiderNeighbor(heightData, x, y, x + 1, y - 1, currentHeight, ref bestDrop, ref bestX, ref bestY);
            ConsiderNeighbor(heightData, x, y, x - 1, y + 1, currentHeight, ref bestDrop, ref bestX, ref bestY);
            ConsiderNeighbor(heightData, x, y, x + 1, y + 1, currentHeight, ref bestDrop, ref bestX, ref bestY);
        }

        return bestDrop > 0.0f;
    }

    private static void ConsiderNeighbor(Torus<float> heightData, int currentX, int currentY, int candidateX, int candidateY, float currentHeight, ref float bestDrop, ref int bestX, ref int bestY) {
        float drop = currentHeight - heightData[candidateX, candidateY];
        if (drop > bestDrop) {
            bestDrop = drop;
            bestX = candidateX;
            bestY = candidateY;
        }
    }

    private static void Normalize(Torus<float> values) {
        float min = float.MaxValue;
        float max = float.MinValue;
        int size = values.Size;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float value = values[x, y];
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
                    values[x, y] = 0.5f;
                }
            }

            return;
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                values[x, y] = (values[x, y] - min) / range;
            }
        }
    }

    private static void NormalizeToMax(Torus<float> values) {
        float max = 0.0f;

        int size = values.Size;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                if (values[x, y] > max) {
                    max = values[x, y];
                }
            }
        }

        if (max <= 0.0f) {
            return;
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                values[x, y] /= max;
            }
        }
    }

}
