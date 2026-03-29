// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.contracts.Terrain;

namespace uk.osric.sim.tests;

public sealed class TerrainConfigurationBehaviourTests {
    [Test]
    public void Defaults_EnableToroidalGenerationAndDiamondSquare() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 512,
        };

        Assert.Multiple(() => {
            Assert.That(configuration.DiamondSquare.Roughness, Is.EqualTo(0.45f));
            Assert.That(configuration.DiamondSquare.InitialDisplacement, Is.EqualTo(1.0f));
            Assert.That(configuration.Erosion.Raindrops, Is.EqualTo(1));
            Assert.That(configuration.DiamondSquare.SmoothnessStopStep, Is.EqualTo(1));
            Assert.That(configuration.Erosion.DropPathLength, Is.EqualTo(64));
            Assert.That(configuration.Erosion.NeighborSampleCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void Configuration_CanOverrideRaindrops() {
        TerrainConfiguration configuration = new() {
            Seed = 42,
            Size = 256,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 3,
            },
        };

        Assert.That(configuration.Erosion.Raindrops, Is.EqualTo(3));
    }

    [Test]
    public void Validate_RejectsNonPositiveSize() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 0,
        };

        ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("Size"));
    }

    [Test]
    public void Validate_RejectsSizesThatAreNotPowerOfTwo() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 300,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => configuration.Validate());
        Assert.That(exception!.Message, Does.Contain("power of two"));
    }

    [Test]
    public void Validate_RejectsDiamondSquareUpscaleFactorThatIsNotPowerOfTwo() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 256,
            UpscaleFactor = 3,
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => configuration.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("UpscaleFactor"));
    }

    [Test]
    public void Validate_RejectsErosionNeighborCountOutsideSupportedValues() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 256,
            Erosion = new TerrainErosionConfiguration {
                NeighborSampleCount = 6,
            },
        };

        ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => configuration.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("NeighborSampleCount"));
    }

    [Test]
    public void Validate_RejectsSmoothnessStopStepThatIsNotPowerOfTwo() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 256,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 3,
            },
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => configuration.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("SmoothnessStopStep"));
    }

    [Test]
    public void Validate_RejectsSmoothnessStopStepLargerThanSize() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 256,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 512,
            },
        };

        ArgumentException? exception = Assert.Throws<ArgumentException>(() => configuration.Validate());
        Assert.That(exception!.ParamName, Is.EqualTo("SmoothnessStopStep"));
    }

    [Test]
    public void Validate_AcceptsErosionConfigurationWithinRange() {
        TerrainConfiguration configuration = new() {
            Seed = 999,
            Size = 256,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 16,
                DropPathLength = 96,
                NeighborSampleCount = 8,
                ErosionStrength = 0.03f,
                DepositionRatio = 0.4f,
            },
        };

        Assert.DoesNotThrow(() => configuration.Validate());
    }
}