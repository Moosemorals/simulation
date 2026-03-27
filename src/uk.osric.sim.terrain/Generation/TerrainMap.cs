// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public sealed class TerrainMap {
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required float[] HeightData { get; init; }
}