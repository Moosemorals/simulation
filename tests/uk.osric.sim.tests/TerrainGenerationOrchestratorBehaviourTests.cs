// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;
using uk.osric.sim.terrain;

namespace uk.osric.sim.tests;

public sealed class TerrainGenerationOrchestratorBehaviourTests {
    [Test]
    public void Generate_WithSameSeedAndOptions_IsDeterministic() {
        TerrainGenerationOptions options = CreateDefaultOptions(1729);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap first = generator.Generate(options);
        TerrainMap second = generator.Generate(options);

        Assert.Multiple(() => {
            Assert.That(Flatten(first.HeightData), Is.EqualTo(Flatten(second.HeightData)));
            Assert.That(Flatten(first.WaterAccumulationData), Is.EqualTo(Flatten(second.WaterAccumulationData)));
            Assert.That(Flatten(first.RiverMask), Is.EqualTo(Flatten(second.RiverMask)));
            Assert.That(Flatten(first.LakeMask), Is.EqualTo(Flatten(second.LakeMask)));
        });
    }

    [Test]
    public void Generate_WithDifferentSeeds_ProducesDifferentHeightData() {
        TerrainGenerationOrchestrator generator = new();

        TerrainMap first = generator.Generate(CreateDefaultOptions(1729));
        TerrainMap second = generator.Generate(CreateDefaultOptions(1730));

        Assert.That(Flatten(first.HeightData), Is.Not.EqualTo(Flatten(second.HeightData)));
    }

    [Test]
    public void Generate_ProducesToroidalHeightWrapping() {
        TerrainGenerationOptions options = CreateDefaultOptions(42);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
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
        TerrainGenerationOptions options = CreateDefaultOptions(42, 4);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
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
        TerrainGenerationOptions options = CreateDefaultOptions(2048);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
        float[] values = Flatten(map.WaterAccumulationData);

        Assert.Multiple(() => {
            Assert.That(values, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(values, Has.All.InRange(0.0f, 1.0f));
            Assert.That(values, Has.Some.GreaterThan(0.05f));
        });
    }

    [Test]
    public void Generate_PopulatesRiverAndLakeMasksForEveryTile() {
        TerrainGenerationOptions options = CreateDefaultOptions(2048);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
        bool[] riverMask = Flatten(map.RiverMask);
        bool[] lakeMask = Flatten(map.LakeMask);

        Assert.Multiple(() => {
            Assert.That(riverMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(lakeMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(riverMask.Any(cell => cell) || lakeMask.Any(cell => cell), Is.True);
        });
    }

    [Test]
    public void Generate_WithErosionPasses_ChangesHeightField() {
        TerrainGenerationOrchestrator generator = new();

        TerrainGenerationOptions withoutErosion = CreateDefaultOptions(99, 0);
        TerrainGenerationOptions withErosion = CreateDefaultOptions(99, 3);

        TerrainMap baseMap = generator.Generate(withoutErosion);
        TerrainMap erodedMap = generator.Generate(withErosion);

        Assert.That(Flatten(erodedMap.HeightData), Is.Not.EqualTo(Flatten(baseMap.HeightData)));
    }

    [Test]
    public void Generate_WithUpscaleFactor_IncreasesOutputSizeAndDataLengths() {
        TerrainGenerationOrchestrator generator = new();
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 8,
            ErosionPasses = 1,
            UpscaleFactor = 8,
        };

        TerrainMap map = generator.Generate(options);

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
        TerrainGenerationOrchestrator generator = new();
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 8,
            ErosionPasses = 4,
            UpscaleFactor = 8,
        };

        TerrainMap map = generator.Generate(options);
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
    public void Generate_MoreErosionPasses_ProducesMoreErosion() {
        TerrainGenerationOrchestrator generator = new();

        TerrainGenerationOptions fewPasses = new() { Seed = 42, Size = 8, ErosionPasses = 1 };
        TerrainGenerationOptions manyPasses = new() { Seed = 42, Size = 8, ErosionPasses = 100 };

        TerrainMap lightlyEroded = generator.Generate(fewPasses);
        TerrainMap heavilyEroded = generator.Generate(manyPasses);

        Assert.That(Flatten(heavilyEroded.HeightData), Is.Not.EqualTo(Flatten(lightlyEroded.HeightData)));
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_ProducesNormalisedHeightInRange() {
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 64,
            ErosionPasses = 0,
            UpscaleFactor = 1,
            SmoothnessStopStep = 8,
        };
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
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
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Size = 64,
            ErosionPasses = 0,
            UpscaleFactor = 1,
            SmoothnessStopStep = 8,
        };
        TerrainGenerationOrchestrator generator = new();

        TerrainMap first = generator.Generate(options);
        TerrainMap second = generator.Generate(options);

        Assert.That(Flatten(first.HeightData), Is.EqualTo(Flatten(second.HeightData)));
    }

    [Test]
    public void Generate_WithSmoothnessStopStep_PreservesToroidalEdges() {
        TerrainGenerationOptions options = new() {
            Seed = 42,
            Size = 64,
            ErosionPasses = 0,
            UpscaleFactor = 1,
            SmoothnessStopStep = 8,
        };
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
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
        TerrainGenerationOptions options = new() {
            Seed = 7,
            Size = 8,
            ErosionPasses = 0,
            UpscaleFactor = 1,
            SmoothnessStopStep = 4,
        };
        TerrainGenerationOrchestrator generator = new();

        // We can only check proportionality after normalisation, so we verify
        // that (1,0) lies on the line between (0,0) and (4,0) in the normalised space.
        TerrainMap map = generator.Generate(options);
        float h00 = map.HeightData[0, 0];
        float h40 = map.HeightData[4, 0];
        float h10 = map.HeightData[1, 0];

        float expected = h00 * 0.75f + h40 * 0.25f;
        Assert.That(h10, Is.EqualTo(expected).Within(0.0001f));
    }

    private static TerrainGenerationOptions CreateDefaultOptions(int seed, int erosionPasses = 1) {
        return new TerrainGenerationOptions {
            Seed = seed,
            Size = 256,
            ErosionPasses = erosionPasses,
            UpscaleFactor = 1,
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
