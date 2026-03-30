// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.contracts.Terrain;

public sealed record TerrainRenderDto(
    TerrainConfiguration Configuration,
    int RenderedSize,
    string HeightFloatDataBase64,
    string WaterAccumulationFloatDataBase64,
    string RiverMaskDataBase64,
    string LakeMaskDataBase64
);
