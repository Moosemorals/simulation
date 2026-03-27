// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.contracts.Terrain;

public sealed record TerrainHydraulicsMapDto(
    int Size,
    string WaterAccumulationDataBase64,
    string RiverMaskDataBase64,
    string LakeMaskDataBase64
);