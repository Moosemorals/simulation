// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.tests;

public sealed class TerrainGenerationOptionsBehaviourTests {
    [Test]
    public void Defaults_EnableToroidalGenerationAndDiamondSquare() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Width = 512,
            Height = 512,
        };

        Assert.Multiple(() => {
            Assert.That(options.WrapHorizontally, Is.True);
            Assert.That(options.WrapVertically, Is.True);
            Assert.That(options.BaseAlgorithm, Is.EqualTo("diamond-square"));
            Assert.That(options.ErosionPasses, Is.EqualTo(1));
        });
    }

    [Test]
    public void Options_CanOverrideAlgorithmAndErosionPasses() {
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Width = 256,
            Height = 256,
            BaseAlgorithm = "test-noise",
            ErosionPasses = 3,
        };

        Assert.Multiple(() => {
            Assert.That(options.BaseAlgorithm, Is.EqualTo("test-noise"));
            Assert.That(options.ErosionPasses, Is.EqualTo(3));
        });
    }
}