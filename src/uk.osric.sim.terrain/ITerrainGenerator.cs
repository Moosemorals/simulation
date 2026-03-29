// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using uk.osric.sim.contracts.Terrain;

namespace uk.osric.sim.terrain;

public interface ITerrainGenerator {
    public TerrainMap Generate(TerrainConfiguration configuration);
}