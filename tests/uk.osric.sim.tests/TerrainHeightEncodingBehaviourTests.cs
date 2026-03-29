// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.terrain;

namespace uk.osric.sim.tests;

public sealed class TerrainHeightEncodingBehaviourTests {
    [Test]
    public void ToGreyscaleBytes_MapsNormalizedHeightsToByteRange() {
        TerrainMap map = new() {
            Size = 2,
            HeightData = CreateFloatTorus(2, [0.0f, 0.5f, 1.0f, 0.25f]),
            WaterAccumulationData = new Torus<float>(2),
            RiverMask = new Torus<bool>(2),
            LakeMask = new Torus<bool>(2),
        };

        byte[] bytes = TerrainHeightEncoding.ToGreyscaleBytes(map);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0, 128, 255, 64 }));
    }

    [Test]
    public void ToGreyscaleBytes_ClampsOutOfRangeHeights() {
        TerrainMap map = new() {
            Size = 2,
            HeightData = CreateFloatTorus(2, [-1.0f, 2.0f, 0.0f, 1.0f]),
            WaterAccumulationData = new Torus<float>(2),
            RiverMask = new Torus<bool>(2),
            LakeMask = new Torus<bool>(2),
        };

        byte[] bytes = TerrainHeightEncoding.ToGreyscaleBytes(map);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0, 255, 0, 255 }));
    }

    [Test]
    public void ToGreyscaleBytes_RejectsMismatchedGridLength() {
        TerrainMap map = new() {
            Size = 2,
            HeightData = new Torus<float>(4),
            WaterAccumulationData = new Torus<float>(2),
            RiverMask = new Torus<bool>(2),
            LakeMask = new Torus<bool>(2),
        };

        Assert.Throws<ArgumentException>(() => TerrainHeightEncoding.ToGreyscaleBytes(map));
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