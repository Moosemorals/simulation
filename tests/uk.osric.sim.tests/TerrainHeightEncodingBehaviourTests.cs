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
            HeightData = [0.0f, 0.5f, 1.0f, 0.25f],
        };

        byte[] bytes = TerrainHeightEncoding.ToGreyscaleBytes(map);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0, 128, 255, 64 }));
    }

    [Test]
    public void ToGreyscaleBytes_ClampsOutOfRangeHeights() {
        TerrainMap map = new() {
            Size = 2,
            HeightData = [-1.0f, 2.0f, 0.0f, 1.0f],
        };

        byte[] bytes = TerrainHeightEncoding.ToGreyscaleBytes(map);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0, 255, 0, 255 }));
    }

    [Test]
    public void ToGreyscaleBytes_RejectsMismatchedGridLength() {
        TerrainMap map = new() {
            Size = 2,
            HeightData = [0.0f],
        };

        Assert.Throws<ArgumentException>(() => TerrainHeightEncoding.ToGreyscaleBytes(map));
    }
}