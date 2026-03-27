// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using NUnit.Framework;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.tests;

public sealed class DiamondSquareTerrainGeneratorBehaviourTests {
    [Test]
    public void Generate_WithSameSeedAndOptions_IsDeterministic() {
        TerrainGenerationOptions options = CreateDefaultOptions(1729);
        DiamondSquareTerrainGenerator generator = new();

        TerrainMap first = generator.Generate(options);
        TerrainMap second = generator.Generate(options);

        Assert.That(first.HeightData, Is.EqualTo(second.HeightData));
    }

    [Test]
    public void Generate_WithDifferentSeeds_ProducesDifferentHeightData() {
        DiamondSquareTerrainGenerator generator = new();

        TerrainMap first = generator.Generate(CreateDefaultOptions(1729));
        TerrainMap second = generator.Generate(CreateDefaultOptions(1730));

        Assert.That(first.HeightData, Is.Not.EqualTo(second.HeightData));
    }

    [Test]
    public void Generate_ProducesSeamlessToroidalEdges() {
        TerrainGenerationOptions options = CreateDefaultOptions(42);
        DiamondSquareTerrainGenerator generator = new();

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

    private static TerrainGenerationOptions CreateDefaultOptions(int seed) {
        return new TerrainGenerationOptions {
            Seed = seed,
            Size = 257,
        };
    }
}