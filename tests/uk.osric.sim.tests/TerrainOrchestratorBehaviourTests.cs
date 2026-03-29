// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.terrain;

namespace uk.osric.sim.tests;

public sealed class TerrainOrchestratorBehaviourTests {
    [Test]
    public void Generate_WithSameSeedAndOptions_IsDeterministic() {
        TerrainConfiguration configuration = CreateDefaultConfiguration(1729);
        TerrainOrchestrator generator = new();

        TerrainMap first = generator.Generate(configuration);
        TerrainMap second = generator.Generate(configuration);

        Assert.Multiple(() => {
            Assert.That(Flatten(first.HeightData), Is.EqualTo(Flatten(second.HeightData)));
            Assert.That(Flatten(first.WaterAccumulationData), Is.EqualTo(Flatten(second.WaterAccumulationData)));
            Assert.That(Flatten(first.RiverMask), Is.EqualTo(Flatten(second.RiverMask)));
            Assert.That(Flatten(first.LakeMask), Is.EqualTo(Flatten(second.LakeMask)));
        });
    }

    [Test]
    public void Generate_WithDifferentSeeds_ProducesDifferentHeightData() {
        TerrainOrchestrator generator = new();

        TerrainMap first = generator.Generate(CreateDefaultConfiguration(1729));
        TerrainMap second = generator.Generate(CreateDefaultConfiguration(1730));

        Assert.That(Flatten(first.HeightData), Is.Not.EqualTo(Flatten(second.HeightData)));
    }

    [Test]
    public void Generate_ProducesToroidalHeightWrapping() {
        TerrainConfiguration configuration = CreateDefaultConfiguration(42);
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        int size = map.Size;

        for (int i = 0; i < size; i++) {
            float top = map.HeightData[i, 0];
            float wrappedTop = map.HeightData[i, size];
            float left = map.HeightData[0, i];
            float wrappedLeft = map.HeightData[size, i];

            Assert.Multiple(() => {
                Assert.That(wrappedTop, Is.EqualTo(top).Within(0.00001f));
                Assert.That(wrappedLeft, Is.EqualTo(left).Within(0.00001f));
            });
        }
    }

    [Test]
    public void Generate_ProducesToroidalWaterAccumulationWrapping() {
        TerrainConfiguration configuration = CreateDefaultConfiguration(42, 4);
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        int size = map.Size;

        for (int i = 0; i < size; i++) {
            float top = map.WaterAccumulationData[i, 0];
            float wrappedTop = map.WaterAccumulationData[i, size];
            float left = map.WaterAccumulationData[0, i];
            float wrappedLeft = map.WaterAccumulationData[size, i];

            Assert.Multiple(() => {
                Assert.That(wrappedTop, Is.EqualTo(top).Within(0.00001f));
                Assert.That(wrappedLeft, Is.EqualTo(left).Within(0.00001f));
            });
        }
    }

    [Test]
    public void Generate_PopulatesWaterAccumulationForEveryTile() {
        TerrainConfiguration configuration = CreateDefaultConfiguration(2048);
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        float[] values = Flatten(map.WaterAccumulationData);

        Assert.Multiple(() => {
            Assert.That(values, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(values, Has.All.InRange(0.0f, 1.0f));
            Assert.That(values, Has.Some.GreaterThan(0.05f));
        });
    }

    [Test]
    public void Generate_PopulatesRiverAndLakeMasksForEveryTile() {
        TerrainConfiguration configuration = CreateDefaultConfiguration(2048);
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        bool[] riverMask = Flatten(map.RiverMask);
        bool[] lakeMask = Flatten(map.LakeMask);

        Assert.Multiple(() => {
            Assert.That(riverMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(lakeMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(riverMask.Any(cell => cell) || lakeMask.Any(cell => cell), Is.True);
        });
    }

    [Test]
    public void Generate_WithRaindrops_ChangesHeightField() {
        TerrainOrchestrator generator = new();

        TerrainConfiguration withoutErosion = CreateDefaultConfiguration(99, 0);
        TerrainConfiguration withErosion = CreateDefaultConfiguration(99, 3);

        TerrainMap baseMap = generator.Generate(withoutErosion);
        TerrainMap erodedMap = generator.Generate(withErosion);

        Assert.That(Flatten(erodedMap.HeightData), Is.Not.EqualTo(Flatten(baseMap.HeightData)));
    }

    [Test]
    public void Generate_WithUpscaleFactor_IncreasesOutputSizeAndDataLengths() {
        TerrainOrchestrator generator = new();
        TerrainConfiguration configuration = new() {
            Seed = 42,
            Size = 8,
            UpscaleFactor = 8,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 1,
            },
        };

        TerrainMap map = generator.Generate(configuration);

        Assert.Multiple(() => {
            Assert.That(map.Size, Is.EqualTo(64));
            Assert.That(map.HeightData.Size, Is.EqualTo(64));
            Assert.That(map.WaterAccumulationData.Size, Is.EqualTo(64));
            Assert.That(map.RiverMask.Size, Is.EqualTo(64));
            Assert.That(map.LakeMask.Size, Is.EqualTo(64));
        });
    }

    [Test]
    public void Generate_WithUpscaleFactor_PreservesToroidalEdges() {
        TerrainOrchestrator generator = new();
        TerrainConfiguration configuration = new() {
            Seed = 42,
            Size = 8,
            UpscaleFactor = 8,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 4,
            },
        };

        TerrainMap map = generator.Generate(configuration);
        int size = map.Size;

        for (int i = 0; i < size; i++) {
            float top = map.HeightData[i, 0];
            float wrappedTop = map.HeightData[i, size];
            float left = map.HeightData[0, i];
            float wrappedLeft = map.HeightData[size, i];

            Assert.Multiple(() => {
                Assert.That(wrappedTop, Is.EqualTo(top).Within(0.00001f));
                Assert.That(wrappedLeft, Is.EqualTo(left).Within(0.00001f));
            });
        }
    }

    [Test]
    public void Generate_MoreRaindrops_ProducesMoreErosion() {
        TerrainOrchestrator generator = new();

        TerrainConfiguration fewPasses = new() {
            Seed = 42,
            Size = 8,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 1,
            },
        };
        TerrainConfiguration manyPasses = new() {
            Seed = 42,
            Size = 8,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 100,
            },
        };

        TerrainMap lightlyEroded = generator.Generate(fewPasses);
        TerrainMap heavilyEroded = generator.Generate(manyPasses);

        Assert.That(Flatten(heavilyEroded.HeightData), Is.Not.EqualTo(Flatten(lightlyEroded.HeightData)));
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_ProducesNormalisedHeightInRange() {
        TerrainConfiguration configuration = new() {
            Seed = 42,
            Size = 64,
            UpscaleFactor = 1,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 8,
            },
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 0,
            },
        };
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        float[] values = Flatten(map.HeightData);

        Assert.Multiple(() => {
            Assert.That(values, Has.Length.EqualTo(64 * 64));
            Assert.That(values, Has.All.InRange(0.0f, 1.0f));
            Assert.That(values.Min(), Is.EqualTo(0.0f).Within(0.00001f));
            Assert.That(values.Max(), Is.EqualTo(1.0f).Within(0.00001f));
        });
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_IsDeterministic() {
        TerrainConfiguration configuration = new() {
            Seed = 1729,
            Size = 64,
            UpscaleFactor = 1,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 8,
            },
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 0,
            },
        };
        TerrainOrchestrator generator = new();

        TerrainMap first = generator.Generate(configuration);
        TerrainMap second = generator.Generate(configuration);

        Assert.That(Flatten(first.HeightData), Is.EqualTo(Flatten(second.HeightData)));
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_PreservesToroidalEdges() {
        TerrainConfiguration configuration = new() {
            Seed = 42,
            Size = 64,
            UpscaleFactor = 1,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 8,
            },
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 0,
            },
        };
        TerrainOrchestrator generator = new();

        TerrainMap map = generator.Generate(configuration);
        int size = map.Size;

        for (int i = 0; i < size; i++) {
            float top = map.HeightData[i, 0];
            float wrappedTop = map.HeightData[i, size];
            float left = map.HeightData[0, i];
            float wrappedLeft = map.HeightData[size, i];

            Assert.Multiple(() => {
                Assert.That(wrappedTop, Is.EqualTo(top).Within(0.00001f));
                Assert.That(wrappedLeft, Is.EqualTo(left).Within(0.00001f));
            });
        }
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_InteriorCellsAreBilinearlyInterpolated() {
        // Use a minimal map (Size=8) and stop after the very first diamond-square
        // pass so that the only filled cells are the multiples-of-4 grid.
        // A cell at (1, 0) must equal 0.75*h[0,0] + 0.25*h[4,0] (tx=0.25, ty=0).
        TerrainConfiguration configuration = new() {
            Seed = 7,
            Size = 8,
            UpscaleFactor = 1,
            DiamondSquare = new DiamondSquareConfiguration {
                SmoothnessStopStep = 4,
            },
            Erosion = new TerrainErosionConfiguration {
                Raindrops = 0,
            },
        };
        TerrainOrchestrator generator = new();

        // We can only check proportionality after normalisation, so we verify
        // that (1,0) lies on the line between (0,0) and (4,0) in the normalised space.
        TerrainMap map = generator.Generate(configuration);
        float h00 = map.HeightData[0, 0];
        float h40 = map.HeightData[4, 0];
        float h10 = map.HeightData[1, 0];

        float expected = h00 * 0.75f + h40 * 0.25f;
        Assert.That(h10, Is.EqualTo(expected).Within(0.0001f));
    }

    private static TerrainConfiguration CreateDefaultConfiguration(int seed, int raindrops = 1) {
        return new TerrainConfiguration {
            Seed = seed,
            Size = 256,
            UpscaleFactor = 1,
            Erosion = new TerrainErosionConfiguration {
                Raindrops = raindrops,
            },
        };
    }

    private static float[] Flatten(Torus<float> torus) {
        float[] values = new float[torus.Size * torus.Size];

        for (int y = 0; y < torus.Size; y++) {
            for (int x = 0; x < torus.Size; x++) {
                values[(y * torus.Size) + x] = torus[x, y];
            }
        }

        return values;
    }

    private static bool[] Flatten(Torus<bool> torus) {
        bool[] values = new bool[torus.Size * torus.Size];

        for (int y = 0; y < torus.Size; y++) {
            for (int x = 0; x < torus.Size; x++) {
                values[(y * torus.Size) + x] = torus[x, y];
            }
        }

        return values;
    }
}
