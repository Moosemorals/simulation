// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Diagnostics;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.calibrate;

internal static class Program {
    private const int Size = 129;
    private const int Seed = 42;
    private const int MaxPasses = 1_000_000;
    private static readonly TimeSpan LowBound = TimeSpan.FromSeconds(9);
    private static readonly TimeSpan HighBound = TimeSpan.FromSeconds(11);
    private static readonly TimeSpan Target = TimeSpan.FromSeconds(10);

    internal static void Main() {
        Console.WriteLine("=== Erosion Pass Calibrator ===");
        Console.WriteLine($"Target: 10s +/-10% (9-11s), grid size: {Size}x{Size}");
        Console.WriteLine();

        Warmup();

        (int lo, TimeSpan loTime, int hi, TimeSpan hiTime) = ExpandBounds();

        if (IsInTolerance(loTime)) {
            PrintResult(lo, loTime);
            return;
        }

        if (IsInTolerance(hiTime)) {
            PrintResult(hi, hiTime);
            return;
        }

        if (hi >= MaxPasses) {
            Console.WriteLine($"[warn] Reached MaxPasses={MaxPasses}. Reporting best result.");
            PrintResult(hi, hiTime);
            return;
        }

        BinarySearch(lo, loTime, hi, hiTime);
    }

    private static void Warmup() {
        Console.Write("Warming up JIT... ");
        TimeGeneration(65, 1, 1);
        Console.WriteLine("done.");
        Console.WriteLine();
    }

    private static (int lo, TimeSpan loTime, int hi, TimeSpan hiTime) ExpandBounds() {
        Console.WriteLine("--- Phase 1: Finding bounds ---");

        int lo = 0;
        TimeSpan loTime = TimeSpan.Zero;
        int hi = 1;

        while (true) {
            TimeSpan t = TimeGeneration(Size, hi, Seed);
            PrintProbe(hi, t);

            if (IsInTolerance(t)) {
                return (lo, loTime, hi, t);
            }

            if (t > HighBound || hi >= MaxPasses) {
                return (lo, loTime, hi, t);
            }

            lo = hi;
            loTime = t;
            hi = Math.Min(hi * 2, MaxPasses);
        }
    }

    private static void BinarySearch(int lo, TimeSpan loTime, int hi, TimeSpan hiTime) {
        Console.WriteLine();
        Console.WriteLine($"--- Phase 2: Binary search between {lo} and {hi} ---");

        while (hi - lo > 1) {
            int mid = lo + (hi - lo) / 2;
            TimeSpan t = TimeGeneration(Size, mid, Seed);
            PrintProbe(mid, t);

            if (IsInTolerance(t)) {
                PrintResult(mid, t);
                return;
            }

            if (t < LowBound) {
                lo = mid;
                loTime = t;
            } else {
                hi = mid;
                hiTime = t;
            }
        }

        // hi - lo == 1: pick whichever is closer to 10s (timings already captured)
        int best = Math.Abs((loTime - Target).Ticks) <= Math.Abs((hiTime - Target).Ticks) ? lo : hi;
        TimeSpan bestTime = best == lo ? loTime : hiTime;
        PrintResult(best, bestTime);
    }

    private static TimeSpan TimeGeneration(int size, int passes, int seed) {
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

    private static bool IsInTolerance(TimeSpan t) => t >= LowBound && t <= HighBound;

    private static void PrintProbe(int passes, TimeSpan t) {
        string label = t < LowBound ? "too fast" : t > HighBound ? "too slow" : "in range";
        Console.WriteLine($"  {passes,8} passes -> {t.TotalSeconds,7:F2}s  [{label}]");
    }

    private static void PrintResult(int passes, TimeSpan t) {
        Console.WriteLine();
        Console.WriteLine("=== Result ===");
        Console.WriteLine($"  ErosionPasses = {passes}");
        Console.WriteLine($"  Elapsed       = {t.TotalSeconds:F2}s");
        Console.WriteLine("  Target        = 10s +/-10%");
        Console.WriteLine();
        Console.WriteLine("Apply to appsettings.json:");
        Console.WriteLine($"  \"ErosionPasses\": {passes}");
    }
}
