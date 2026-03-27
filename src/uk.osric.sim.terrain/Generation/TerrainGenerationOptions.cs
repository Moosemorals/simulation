// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainGenerationOptions {
    public required int Seed { get; init; }

    public required int Size { get; init; }

    public string BaseAlgorithm { get; init; } = "diamond-square";

    public float Roughness { get; init; } = 0.55f;

    public float InitialDisplacement { get; init; } = 1.0f;

    public int ErosionPasses { get; init; } = 1;

    public void Validate() {
        if (Size <= 0) {
            throw new ArgumentOutOfRangeException(nameof(Size), "Map size must be positive.");
        }

        if (string.Equals(BaseAlgorithm, "diamond-square", StringComparison.OrdinalIgnoreCase)) {
            int edgeLength = Size - 1;
            if (!IsPowerOfTwo(edgeLength)) {
                throw new ArgumentException("Diamond-square requires map dimensions of 2^n + 1.");
            }
        }

        if (Roughness <= 0.0f || Roughness >= 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(Roughness), "Roughness must be between 0 and 1.");
        }

        if (InitialDisplacement <= 0.0f) {
            throw new ArgumentOutOfRangeException(nameof(InitialDisplacement), "InitialDisplacement must be greater than 0.");
        }

        if (ErosionPasses < 0) {
            throw new ArgumentOutOfRangeException(nameof(ErosionPasses), "ErosionPasses cannot be negative.");
        }
    }

    private static bool IsPowerOfTwo(int value) {
        return value > 0 && (value & (value - 1)) == 0;
    }
}