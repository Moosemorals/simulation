// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

internal sealed class RiverLakeDetectionLayer {
    private const float RiverFlowThreshold = 0.12f;
    private const float RiverSlopeThreshold = 0.0035f;
    private const float LakeFlowThreshold = 0.2f;
    private const float LakeSlopeEpsilon = 0.00075f;

    public (bool[] riverMask, bool[] lakeMask) Build(float[] heightData, float[] waterAccumulationData, int size) {
        int cellCount = size * size;
        bool[] riverMask = new bool[cellCount];
        bool[] lakeMask = new bool[cellCount];

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                int index = y * size + x;
                float flow = waterAccumulationData[index];

                int downhillIndex = FindSteepestDownhillIndex(heightData, size, x, y, out float downhillDrop);
                bool hasDownhill = downhillIndex >= 0;

                bool isRiver = flow >= RiverFlowThreshold && hasDownhill && downhillDrop >= RiverSlopeThreshold;
                riverMask[index] = isRiver;

                bool isLake = !isRiver && flow >= LakeFlowThreshold && (!hasDownhill || downhillDrop <= LakeSlopeEpsilon);
                lakeMask[index] = isLake;
            }
        }

        EnforceToroidalSeams(riverMask, size);
        EnforceToroidalSeams(lakeMask, size);

        return (riverMask, lakeMask);
    }

    private static int FindSteepestDownhillIndex(float[] heightData, int size, int x, int y, out float bestDrop) {
        int bestIndex = -1;
        float currentHeight = heightData[y * size + x];
        bestDrop = 0.0f;

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

    private static void EnforceToroidalSeams(bool[] grid, int size) {
        for (int i = 0; i < size; i++) {
            bool topValue = grid[i];
            grid[(size - 1) * size + i] = topValue;

            bool leftValue = grid[i * size];
            grid[i * size + (size - 1)] = leftValue;
        }
    }
}
