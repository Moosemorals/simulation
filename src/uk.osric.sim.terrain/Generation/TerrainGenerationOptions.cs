// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainGenerationOptions {
    public required int Seed { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public bool WrapHorizontally { get; init; } = true;

    public bool WrapVertically { get; init; } = true;

    public string BaseAlgorithm { get; init; } = "diamond-square";

    public int ErosionPasses { get; init; } = 1;
}