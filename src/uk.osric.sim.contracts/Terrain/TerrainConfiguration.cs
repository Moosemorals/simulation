// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.contracts.Terrain;

public sealed class TerrainConfiguration {
    public required int Seed { get; init; }

    public required int Size { get; init; }

    public int UpscaleFactor { get; init; } = 1;

    public DiamondSquareConfiguration DiamondSquare { get; init; } = new();

    public TerrainErosionConfiguration Erosion { get; init; } = TerrainErosionConfiguration.Default;

    public void Validate() {
        if (Size <= 0) {
            throw new ArgumentOutOfRangeException(nameof(Size), "Map size must be positive.");
        }

        if (!IsPowerOfTwo(Size)) {
            throw new ArgumentException("Map size must be a power of two.", nameof(Size));
        }

        if (UpscaleFactor <= 0) {
            throw new ArgumentOutOfRangeException(nameof(UpscaleFactor), "UpscaleFactor must be greater than 0.");
        }

        if (!IsPowerOfTwo(UpscaleFactor)) {
            throw new ArgumentException("Diamond-square upscaling requires UpscaleFactor to be a power of two.", nameof(UpscaleFactor));
        }

        DiamondSquare.Validate(Size);
        Erosion.Validate();
    }

    private static bool IsPowerOfTwo(int value) {
        return value > 0 && (value & (value - 1)) == 0;
    }
}

public sealed class DiamondSquareConfiguration {
    public static DiamondSquareConfiguration Default { get; } = new();

    public int SmoothnessStopStep { get; init; } = 1;

    public float Roughness { get; init; } = 0.45f;

    public float InitialDisplacement { get; init; } = 1.0f;

    public void Validate(int size) {
        if (Roughness <= 0.0f || Roughness >= 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(Roughness), "Roughness must be between 0 and 1.");
        }

        if (InitialDisplacement <= 0.0f) {
            throw new ArgumentOutOfRangeException(nameof(InitialDisplacement), "InitialDisplacement must be greater than 0.");
        }

        if (!IsPowerOfTwo(SmoothnessStopStep) || SmoothnessStopStep > size) {
            throw new ArgumentException(
                "SmoothnessStopStep must be a power of two no greater than Size.",
                nameof(SmoothnessStopStep));
        }
    }

    private static bool IsPowerOfTwo(int value) {
        return value > 0 && (value & (value - 1)) == 0;
    }
}

public sealed class TerrainErosionConfiguration {
    public static TerrainErosionConfiguration Default { get; } = new();

    public int Raindrops { get; init; } = 1;

    public int DropPathLength { get; init; } = 64;

    public int NeighborSampleCount { get; init; } = 4;

    public float ErosionStrength { get; init; } = 0.02f;

    public float DepositionRatio { get; init; } = 0.25f;

    public void Validate() {
        if (Raindrops < 0) {
            throw new ArgumentOutOfRangeException(nameof(Raindrops), "Raindrops cannot be negative.");
        }

        if (DropPathLength <= 0) {
            throw new ArgumentOutOfRangeException(nameof(DropPathLength), "DropPathLength must be greater than 0.");
        }

        if (NeighborSampleCount is not 4 and not 8) {
            throw new ArgumentOutOfRangeException(nameof(NeighborSampleCount), "NeighborSampleCount must be 4 or 8.");
        }

        if (ErosionStrength <= 0.0f || ErosionStrength > 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(ErosionStrength), "ErosionStrength must be in range (0, 1].");
        }

        if (DepositionRatio < 0.0f || DepositionRatio > 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(DepositionRatio), "DepositionRatio must be in range [0, 1].");
        }
    }
}