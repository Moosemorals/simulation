// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.tests;

public sealed class TerrainGenerationOrchestratorBehaviourTests {
    [Test]
    public void Generate_WithSameSeedAndOptions_IsDeterministic() {
        TerrainGenerationOptions options = CreateDefaultOptions(1729);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap first = generator.Generate(options);
        TerrainMap second = generator.Generate(options);

        Assert.Multiple(() => {
            Assert.That(first.HeightData, Is.EqualTo(second.HeightData));
            Assert.That(first.WaterAccumulationData, Is.EqualTo(second.WaterAccumulationData));
            Assert.That(first.RiverMask, Is.EqualTo(second.RiverMask));
            Assert.That(first.LakeMask, Is.EqualTo(second.LakeMask));
        });
    }

    [Test]
    public void Generate_WithDifferentSeeds_ProducesDifferentHeightData() {
        TerrainGenerationOrchestrator generator = new();

        TerrainMap first = generator.Generate(CreateDefaultOptions(1729));
        TerrainMap second = generator.Generate(CreateDefaultOptions(1730));

        Assert.That(first.HeightData, Is.Not.EqualTo(second.HeightData));
    }

    [Test]
    public void Generate_ProducesSeamlessToroidalEdges() {
        TerrainGenerationOptions options = CreateDefaultOptions(42);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);
        int size = map.Size;

        for (int i = 0; i < size; i++) {
            float top = map.HeightData[i];
            float bottom = map.HeightData[(size - 1) * size + i];
            float left = map.HeightData[i * size];
            float right = map.HeightData[i * size + (size - 1)];

            Assert.Multiple(() => {
                Assert.That(bottom, Is.EqualTo(top).Within(0.00001f));
                Assert.That(right, Is.EqualTo(left).Within(0.00001f));
            });
        }
    }

    [Test]
    public void Generate_PopulatesWaterAccumulationForEveryTile() {
        TerrainGenerationOptions options = CreateDefaultOptions(2048);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);

        Assert.Multiple(() => {
            Assert.That(map.WaterAccumulationData, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(map.WaterAccumulationData, Has.All.InRange(0.0f, 1.0f));
            Assert.That(map.WaterAccumulationData, Has.Some.GreaterThan(0.05f));
        });
    }

    [Test]
    public void Generate_PopulatesRiverAndLakeMasksForEveryTile() {
        TerrainGenerationOptions options = CreateDefaultOptions(2048);
        TerrainGenerationOrchestrator generator = new();

        TerrainMap map = generator.Generate(options);

        Assert.Multiple(() => {
            Assert.That(map.RiverMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(map.LakeMask, Has.Length.EqualTo(map.Size * map.Size));
            Assert.That(map.RiverMask, Has.Some.True);
            Assert.That(map.LakeMask, Has.Some.True);
        });
    }

    [Test]
    public void Generate_WithErosionPasses_ChangesHeightField() {
        TerrainGenerationOrchestrator generator = new();

        TerrainGenerationOptions withoutErosion = CreateDefaultOptions(99, 0);
        TerrainGenerationOptions withErosion = CreateDefaultOptions(99, 3);

        TerrainMap baseMap = generator.Generate(withoutErosion);
        TerrainMap erodedMap = generator.Generate(withErosion);

        Assert.That(erodedMap.HeightData, Is.Not.EqualTo(baseMap.HeightData));
    }

    [Test]
    public void Generate_MoreErosionPasses_ProducesMoreErosion() {
        TerrainGenerationOrchestrator generator = new();

        TerrainGenerationOptions fewPasses = new() { Seed = 42, Size = 9, ErosionPasses = 1 };
        TerrainGenerationOptions manyPasses = new() { Seed = 42, Size = 9, ErosionPasses = 100 };

        TerrainMap lightlyEroded = generator.Generate(fewPasses);
        TerrainMap heavilyEroded = generator.Generate(manyPasses);

        Assert.That(heavilyEroded.HeightData, Is.Not.EqualTo(lightlyEroded.HeightData));
    }

    private static TerrainGenerationOptions CreateDefaultOptions(int seed, int erosionPasses = 1) {
        return new TerrainGenerationOptions {
            Seed = seed,
            Size = 257,
            ErosionPasses = erosionPasses,
        };
    }
}