// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainGenerationOptions {
    public required int Seed { get; init; }

    public required int Size { get; init; }

    public int UpscaleFactor { get; init; } = 1;

    public string BaseAlgorithm { get; init; } = "diamond-square";

    public float Roughness { get; init; } = 0.55f;

    public float InitialDisplacement { get; init; } = 1.0f;

    public int ErosionPasses { get; init; } = 1;

    public HydraulicErosionTuning HydraulicErosion { get; init; } = HydraulicErosionTuning.Default;

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

        if (UpscaleFactor <= 0) {
            throw new ArgumentOutOfRangeException(nameof(UpscaleFactor), "UpscaleFactor must be greater than 0.");
        }

        HydraulicErosion.Validate();
    }

    private static bool IsPowerOfTwo(int value) {
        return value > 0 && (value & (value - 1)) == 0;
    }
}

public sealed class HydraulicErosionTuning {
    public static HydraulicErosionTuning Default { get; } = new();

    public int TopologyRefreshInterval { get; init; } = 4;

    public int NeighborSampleCount { get; init; } = 4;

    public float BaseFlow { get; init; } = 1.0f;

    public float ErosionCapFactor { get; init; } = 0.15f;

    public float SlopeFlowFactor { get; init; } = 0.35f;

    public float DepositionRatio { get; init; } = 0.25f;

    public void Validate() {
        if (TopologyRefreshInterval <= 0) {
            throw new ArgumentOutOfRangeException(nameof(TopologyRefreshInterval), "TopologyRefreshInterval must be greater than 0.");
        }

        if (NeighborSampleCount is not 4 and not 8) {
            throw new ArgumentOutOfRangeException(nameof(NeighborSampleCount), "NeighborSampleCount must be 4 or 8.");
        }

        if (BaseFlow <= 0.0f) {
            throw new ArgumentOutOfRangeException(nameof(BaseFlow), "BaseFlow must be greater than 0.");
        }

        if (ErosionCapFactor <= 0.0f || ErosionCapFactor > 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(ErosionCapFactor), "ErosionCapFactor must be in range (0, 1].");
        }

        if (SlopeFlowFactor <= 0.0f || SlopeFlowFactor > 4.0f) {
            throw new ArgumentOutOfRangeException(nameof(SlopeFlowFactor), "SlopeFlowFactor must be in range (0, 4].");
        }

        if (DepositionRatio < 0.0f || DepositionRatio > 1.0f) {
            throw new ArgumentOutOfRangeException(nameof(DepositionRatio), "DepositionRatio must be in range [0, 1].");
        }
    }
}