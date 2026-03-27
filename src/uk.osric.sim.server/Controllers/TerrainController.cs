// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/terrain")]
public sealed class TerrainController : ControllerBase {
    [HttpGet("seed")]
    public ActionResult<TerrainGenerationOptions> GetSeed() {
        TerrainGenerationOptions options = new() {
            Seed = 1729,
            Width = 512,
            Height = 512,
            WrapHorizontally = true,
            WrapVertically = true,
            BaseAlgorithm = "diamond-square",
            ErosionPasses = 1,
        };

        return Ok(options);
    }
}