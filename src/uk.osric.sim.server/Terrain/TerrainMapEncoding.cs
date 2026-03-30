// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.terrain;

namespace uk.osric.sim.server.Terrain;

internal static class TerrainMapEncoding {
    public static byte[] EncodeFloat32(Torus<float> values) {
        byte[] bytes = new byte[values.Size * values.Size * sizeof(float)];
        int offset = 0;
        for (int y = 0; y < values.Size; y++) {
            for (int x = 0; x < values.Size; x++) {
                BitConverter.TryWriteBytes(bytes.AsSpan(offset, sizeof(float)), values[x, y]);
                offset += sizeof(float);
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
