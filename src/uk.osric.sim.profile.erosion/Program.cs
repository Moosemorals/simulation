// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.profile.erosion;

internal static class Program {
    private const int Size = 513;
    private const int Seed = 42;
    private const int ErosionPasses = 40;

    internal static void Main() {
        Console.WriteLine($"=== Erosion Profiler: size={Size}, seed={Seed}, passes={ErosionPasses} ===");
        Console.WriteLine();

        Warmup();

        Console.WriteLine("Running timed generation...");
        TimeSpan elapsed = RunGeneration(Size, Seed, ErosionPasses);
        Console.WriteLine($"  Elapsed: {elapsed.TotalSeconds:F3}s");
        Console.WriteLine();
        Console.WriteLine("Done.");
    }

    private static void Warmup() {
        Console.Write("Warming up JIT... ");
        RunGeneration(65, Seed, 1);
        Console.WriteLine("done.");
        Console.WriteLine();
    }

    private static TimeSpan RunGeneration(int size, int seed, int passes) {
        TerrainGenerationOrchestrator generator = new();
        TerrainGenerationOptions options = new() {
            Seed = seed,
            Size = size,
            ErosionPasses = passes,
        };
        Stopwatch sw = Stopwatch.StartNew();
        generator.Generate(options);
        sw.Stop();
        return sw.Elapsed;
    }
}
