// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.terrain;

namespace uk.osric.sim.server.Terrain;

internal static class TerrainMapEncoding {
    public static byte[] EncodeHeight(TerrainMap map) {
        return TerrainHeightEncoding.ToGreyscaleBytes(map);
    }

    public static byte[] EncodeFloats(Torus<float> values) {
        byte[] bytes = new byte[values.Size * values.Size];
        for (int y = 0; y < values.Size; y++) {
            for (int x = 0; x < values.Size; x++) {
                float clamped = Math.Clamp(values[x, y], 0.0f, 1.0f);
                bytes[(y * values.Size) + x] = (byte)Math.Clamp((int)MathF.Round(clamped * 255.0f), 0, 255);
            }
        }

        return bytes;
    }

    public static byte[] EncodeMask(Torus<bool> values) {
        byte[] bytes = new byte[values.Size * values.Size];
        for (int y = 0; y < values.Size; y++) {
            for (int x = 0; x < values.Size; x++) {
                bytes[(y * values.Size) + x] = values[x, y] ? (byte)255 : (byte)0;
            }
        }

        return bytes;
    }
}
