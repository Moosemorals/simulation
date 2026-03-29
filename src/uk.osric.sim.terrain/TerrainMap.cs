// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain;

public sealed class TerrainMap {
    public required int Size { get; init; }

    public required float[] HeightData { get; init; }

    public float[] WaterAccumulationData { get; init; } = [];

    public bool[] RiverMask { get; init; } = [];

    public bool[] LakeMask { get; init; } = [];

    public byte[] BiomeData { get; init; } = [];

    public IReadOnlyList<SettlementCandidate> SettlementCandidates { get; init; } = [];
}