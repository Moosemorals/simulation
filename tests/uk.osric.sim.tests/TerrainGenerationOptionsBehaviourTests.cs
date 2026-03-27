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
            Size = 513,
        };

        Assert.Multiple(() => {
            Assert.That(options.BaseAlgorithm, Is.EqualTo("diamond-square"));
            Assert.That(options.Roughness, Is.EqualTo(0.55f));
            Assert.That(options.InitialDisplacement, Is.EqualTo(1.0f));
            Assert.That(options.ErosionPasses, Is.EqualTo(1));
        });
    }

    [Test]
    public void Options_CanOverrideAlgorithmAndErosionPasses() {
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 257,
            BaseAlgorithm = "test-noise",
            ErosionPasses = 3,
        };

        Assert.Multiple(() => {
            Assert.That(options.BaseAlgorithm, Is.EqualTo("test-noise"));
            Assert.That(options.ErosionPasses, Is.EqualTo(3));
        });
    }

    [Test]
    public void Validate_RejectsNonPositiveSize() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 0,
        };

        ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("Size"));
    }

    [Test]
    public void Validate_RejectsDiamondSquareSizesThatAreNotPowerOfTwoPlusOne() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 300,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.That(exception!.Message, Does.Contain("2^n + 1"));
    }
}