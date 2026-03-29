// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

public static class TerrainHeightEncoding {
    public static byte[] ToGreyscaleBytes(TerrainMap terrainMap) {
        ArgumentNullException.ThrowIfNull(terrainMap);

        int expectedCellCount = terrainMap.Size * terrainMap.Size;
        if (terrainMap.HeightData.Size != terrainMap.Size) {
            throw new ArgumentException("HeightData length must be equal to Size * Size.", nameof(terrainMap));
        }

        byte[] bytes = new byte[expectedCellCount];
        for (int y = 0; y < terrainMap.Size; y++) {
            for (int x = 0; x < terrainMap.Size; x++) {
                float clampedHeight = Math.Clamp(terrainMap.HeightData[x, y], 0.0f, 1.0f);
                bytes[(y * terrainMap.Size) + x] = (byte)Math.Clamp((int)MathF.Round(clampedHeight * 255.0f), 0, 255);
            }
        }

        return bytes;
    }
}