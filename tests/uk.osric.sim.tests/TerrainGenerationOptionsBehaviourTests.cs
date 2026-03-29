// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.terrain;


namespace uk.osric.sim.tests;

public sealed class TerrainGenerationOptionsBehaviourTests {
    [Test]
    public void Defaults_EnableToroidalGenerationAndDiamondSquare() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 512,
        };

        Assert.Multiple(() => {
            Assert.That(options.BaseAlgorithm, Is.EqualTo("diamond-square"));
            Assert.That(options.Roughness, Is.EqualTo(0.45f));
            Assert.That(options.InitialDisplacement, Is.EqualTo(1.0f));
            Assert.That(options.ErosionPasses, Is.EqualTo(1));
            Assert.That(options.SmoothnessStopStep, Is.EqualTo(1));
            Assert.That(options.RaindropErosion.DropPathLength, Is.EqualTo(64));
            Assert.That(options.RaindropErosion.NeighborSampleCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void Options_CanOverrideAlgorithmAndErosionPasses() {
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 256,
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
    public void Validate_RejectsSizesThatAreNotPowerOfTwo() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 300,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.That(exception!.Message, Does.Contain("power of two"));
    }

    [Test]
    public void Validate_RejectsDiamondSquareUpscaleFactorThatIsNotPowerOfTwo() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 256,
            UpscaleFactor = 3,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("UpscaleFactor"));
    }

    [Test]
    public void Validate_RejectsRaindropNeighborCountOutsideSupportedValues() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 256,
            RaindropErosion = new RandomRaindropErosionTuning {
                NeighborSampleCount = 6,
            },
        };

        ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("NeighborSampleCount"));
    }

    [Test]
    public void Validate_RejectsSmoothnessStopStepThatIsNotPowerOfTwo() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 256,
            SmoothnessStopStep = 3,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("SmoothnessStopStep"));
    }

    [Test]
    public void Validate_RejectsSmoothnessStopStepLargerThanSize() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 256,
            SmoothnessStopStep = 512,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("SmoothnessStopStep"));
    }

    [Test]
    public void Validate_AcceptsRaindropTuningWithinRange() {
        TerrainGenerationOptions options = new() {
            Seed = 999,
            Size = 256,
            ErosionPasses = 16,
            RaindropErosion = new RandomRaindropErosionTuning {
                DropPathLength = 96,
                NeighborSampleCount = 8,
                ErosionStrength = 0.03f,
                DepositionRatio = 0.4f,
            },
        };

        Assert.DoesNotThrow(() => options.Validate());
    }
}