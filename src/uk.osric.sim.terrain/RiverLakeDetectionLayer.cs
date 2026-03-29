// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

internal sealed class RiverLakeDetectionLayer {
    private const float RiverFlowThreshold = 0.12f;
    private const float RiverSlopeThreshold = 0.0035f;
    private const float LakeFlowThreshold = 0.2f;
    private const float LakeSlopeEpsilon = 0.00075f;

    public static (Torus<bool> riverMask, Torus<bool> lakeMask) Build(Torus<float> heightData, Torus<float> waterAccumulationData, int size) {
        Torus<bool> riverMask = new(size);
        Torus<bool> lakeMask = new(size);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float flow = waterAccumulationData[x, y];

                bool hasDownhill = TryFindSteepestDownhill(heightData, x, y, out float downhillDrop);

                bool isRiver = flow >= RiverFlowThreshold && hasDownhill && downhillDrop >= RiverSlopeThreshold;
                riverMask[x, y] = isRiver;

                bool isLake = !isRiver && flow >= LakeFlowThreshold && (!hasDownhill || downhillDrop <= LakeSlopeEpsilon);
                lakeMask[x, y] = isLake;
            }
        }

        return (riverMask, lakeMask);
    }

    private static bool TryFindSteepestDownhill(Torus<float> heightData, int x, int y, out float bestDrop) {
        bool found = false;
        float currentHeight = heightData[x, y];
        bestDrop = 0.0f;

        for (int offsetY = -1; offsetY <= 1; offsetY++) {
            for (int offsetX = -1; offsetX <= 1; offsetX++) {
                if (offsetX == 0 && offsetY == 0) {
                    continue;
                }

                float drop = currentHeight - heightData[x + offsetX, y + offsetY];

                if (drop > bestDrop) {
                    bestDrop = drop;
                    found = true;
                }
            }
        }

        return found;
    }
}
