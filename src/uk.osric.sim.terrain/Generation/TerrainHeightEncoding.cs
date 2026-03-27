// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public static class TerrainHeightEncoding {
    public static byte[] ToGreyscaleBytes(TerrainMap terrainMap) {
        ArgumentNullException.ThrowIfNull(terrainMap);

        int expectedCellCount = terrainMap.Size * terrainMap.Size;
        if (terrainMap.HeightData.Length != expectedCellCount) {
            throw new ArgumentException("HeightData length must be equal to Size * Size.", nameof(terrainMap));
        }

        byte[] bytes = new byte[terrainMap.HeightData.Length];
        for (int i = 0; i < terrainMap.HeightData.Length; i++) {
            float clampedHeight = Math.Clamp(terrainMap.HeightData[i], 0.0f, 1.0f);
            bytes[i] = (byte)Math.Clamp((int)MathF.Round(clampedHeight * 255.0f), 0, 255);
        }

        return bytes;
    }
}