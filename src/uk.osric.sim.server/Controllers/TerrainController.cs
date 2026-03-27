// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.server.Terrain;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/terrain")]
public sealed class TerrainController : ControllerBase {
    private readonly TerrainSnapshot terrainSnapshot;

    public TerrainController(TerrainSnapshot terrainSnapshot) {
        this.terrainSnapshot = terrainSnapshot;
    }

    [HttpGet("seed")]
    public ActionResult<TerrainSeedDto> GetSeed() {
        TerrainGenerationOptions options = terrainSnapshot.Options;

        TerrainSeedDto dto = new(
            options.Seed,
            options.Size,
            options.BaseAlgorithm,
            options.ErosionPasses
        );

        return Ok(dto);
    }

    [HttpGet("heightmap")]
    public ActionResult<TerrainHeightMapDto> GetHeightMap() {
        TerrainHeightMapDto dto = new(
            terrainSnapshot.Map.Size,
            Convert.ToBase64String(terrainSnapshot.HeightBytes)
        );

        return Ok(dto);
    }
}