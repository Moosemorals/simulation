// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Terrain;

internal static class TerrainMapEncoding {
    public static byte[] EncodeHeight(TerrainMap map) {
        return TerrainHeightEncoding.ToGreyscaleBytes(map);
    }

    public static byte[] EncodeFloats(float[] values) {
        byte[] bytes = new byte[values.Length];
        for (int i = 0; i < values.Length; i++) {
            float clamped = Math.Clamp(values[i], 0.0f, 1.0f);
            bytes[i] = (byte)Math.Clamp((int)MathF.Round(clamped * 255.0f), 0, 255);
        }

        return bytes;
    }

    public static byte[] EncodeMask(bool[] values) {
        byte[] bytes = new byte[values.Length];
        for (int i = 0; i < values.Length; i++) {
            bytes[i] = values[i] ? (byte)255 : (byte)0;
        }

        return bytes;
    }
}
