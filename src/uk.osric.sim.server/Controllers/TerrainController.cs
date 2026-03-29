// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.server.Terrain;
using uk.osric.sim.terrain;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/terrain")]
public sealed class TerrainController : ControllerBase {
    private readonly TerrainSnapshot terrainSnapshot;
    private readonly ITerrainGenerator terrainGenerator;

    public TerrainController(TerrainSnapshot terrainSnapshot, ITerrainGenerator terrainGenerator) {
        this.terrainSnapshot = terrainSnapshot;
        this.terrainGenerator = terrainGenerator;
    }

    [HttpGet("seed")]
    public ActionResult<TerrainSeedDto> GetSeed() {
        TerrainConfiguration configuration = terrainSnapshot.Options;
        TerrainMap map = terrainSnapshot.Map;

        TerrainSeedDto dto = new(
            configuration.Seed,
            map.Size,
            configuration.Erosion.Raindrops
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

    [HttpGet("watermap")]
    public ActionResult<TerrainWaterMapDto> GetWaterMap() {
        TerrainWaterMapDto dto = new(
            terrainSnapshot.Map.Size,
            Convert.ToBase64String(terrainSnapshot.WaterAccumulationBytes),
            Convert.ToBase64String(terrainSnapshot.RiverMaskBytes),
            Convert.ToBase64String(terrainSnapshot.LakeMaskBytes)
        );

        return Ok(dto);
    }

    [HttpGet("tuning-defaults")]
    public ActionResult<TerrainConfiguration> GetTuningDefaults() {
        return Ok(terrainSnapshot.Options);
    }

    [HttpPost("render")]
    public ActionResult<TerrainRenderDto> Render([FromBody] TerrainConfiguration configuration) {
        try {
            configuration.Validate();
        } catch (ArgumentException ex) {
            return BadRequest(new { error = ex.Message });
        }

        TerrainMap map = terrainGenerator.Generate(configuration);
        byte[] heightBytes = TerrainMapEncoding.EncodeHeight(map);
        byte[] waterBytes = TerrainMapEncoding.EncodeFloats(map.WaterAccumulationData);
        byte[] riverBytes = TerrainMapEncoding.EncodeMask(map.RiverMask);
        byte[] lakeBytes = TerrainMapEncoding.EncodeMask(map.LakeMask);

        TerrainRenderDto dto = new(
            configuration,
            map.Size,
            Convert.ToBase64String(heightBytes),
            Convert.ToBase64String(waterBytes),
            Convert.ToBase64String(riverBytes),
            Convert.ToBase64String(lakeBytes));

        return Ok(dto);
    }
}