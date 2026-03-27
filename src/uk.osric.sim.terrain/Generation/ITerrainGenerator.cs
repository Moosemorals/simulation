// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.terrain.Generation;

public interface ITerrainGenerator {
    TerrainMap Generate(TerrainGenerationOptions options);
}