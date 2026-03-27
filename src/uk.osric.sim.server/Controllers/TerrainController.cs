// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.contracts.Terrain;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/terrain")]
public sealed class TerrainController : ControllerBase {
    [HttpGet("seed")]
    public ActionResult<TerrainSeedDto> GetSeed() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Width = 512,
            Height = 512,
            WrapHorizontally = true,
            WrapVertically = true,
            BaseAlgorithm = "diamond-square",
            ErosionPasses = 1,
        };

        TerrainSeedDto dto = new(
            options.Seed,
            options.Width,
            options.Height,
            options.WrapHorizontally,
            options.WrapVertically,
            options.BaseAlgorithm,
            options.ErosionPasses
        );

        return Ok(dto);
    }
}