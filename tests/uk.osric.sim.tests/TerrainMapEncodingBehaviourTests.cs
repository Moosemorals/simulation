// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.server.Terrain;
using uk.osric.sim.terrain;

namespace uk.osric.sim.tests;

public sealed class TerrainMapEncodingBehaviourTests {
    [Test]
    public void EncodeFloat32_PreservesRawFloatValuesWithoutNormalization() {
        Torus<float> values = CreateFloatTorus(2, [-1.25f, 0.0f, 1.5f, 42.25f]);

        byte[] encoded = TerrainMapEncoding.EncodeFloat32(values);

        Assert.That(encoded.Length, Is.EqualTo(2 * 2 * sizeof(float)));
        Assert.Multiple(() => {
            Assert.That(BitConverter.ToSingle(encoded, 0), Is.EqualTo(-1.25f));
            Assert.That(BitConverter.ToSingle(encoded, 4), Is.EqualTo(0.0f));
            Assert.That(BitConverter.ToSingle(encoded, 8), Is.EqualTo(1.5f));
            Assert.That(BitConverter.ToSingle(encoded, 12), Is.EqualTo(42.25f));
        });
    }

    [Test]
    public void EncodeMask_MapsBooleanValuesToOpaqueBytes() {
        Torus<bool> values = new(2);
        values[0, 0] = true;
        values[1, 0] = false;
        values[0, 1] = false;
        values[1, 1] = true;

        byte[] encoded = TerrainMapEncoding.EncodeMask(values);

        Assert.That(encoded, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
    }

    private static Torus<float> CreateFloatTorus(int size, float[] values) {
        Torus<float> torus = new(size);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                torus[x, y] = values[(y * size) + x];
            }
        }

        return torus;
    }
}
