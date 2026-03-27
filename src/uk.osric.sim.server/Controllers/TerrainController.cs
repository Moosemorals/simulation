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
    private readonly ITerrainGenerator terrainGenerator;

    public TerrainController(TerrainSnapshot terrainSnapshot, ITerrainGenerator terrainGenerator) {
        this.terrainSnapshot = terrainSnapshot;
        this.terrainGenerator = terrainGenerator;
    }

    [HttpGet("seed")]
    public ActionResult<TerrainSeedDto> GetSeed() {
        TerrainGenerationOptions options = terrainSnapshot.Options;
        TerrainMap map = terrainSnapshot.Map;

        TerrainSeedDto dto = new(
            options.Seed,
            map.Size,
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

    [HttpGet("hydraulics")]
    public ActionResult<TerrainHydraulicsMapDto> GetHydraulicsMap() {
        TerrainHydraulicsMapDto dto = new(
            terrainSnapshot.Map.Size,
            Convert.ToBase64String(terrainSnapshot.WaterAccumulationBytes),
            Convert.ToBase64String(terrainSnapshot.RiverMaskBytes),
            Convert.ToBase64String(terrainSnapshot.LakeMaskBytes)
        );

        return Ok(dto);
    }

    [HttpGet("tuning-defaults")]
    public ActionResult<TerrainTuningDefaultsDto> GetTuningDefaults() {
        TerrainGenerationOptions options = terrainSnapshot.Options;
        HydraulicErosionTuning tuning = options.HydraulicErosion;

        TerrainTuningDefaultsDto dto = new(
            options.Seed,
            options.Size,
            options.UpscaleFactor,
            options.ErosionPasses,
            tuning.TopologyRefreshInterval,
            tuning.NeighborSampleCount,
            tuning.BaseFlow,
            tuning.ErosionCapFactor,
            tuning.SlopeFlowFactor,
            tuning.DepositionRatio);

        return Ok(dto);
    }

    [HttpPost("render")]
    public ActionResult<TerrainTuningRenderDto> Render([FromBody] TerrainTuningRequestDto request) {
        TerrainGenerationOptions baseOptions = terrainSnapshot.Options;
        TerrainGenerationOptions generationOptions = new() {
            Seed = request.Seed,
            Size = request.ResizeEnabled ? request.SourceSize : baseOptions.Size,
            BaseAlgorithm = baseOptions.BaseAlgorithm,
            ErosionPasses = request.ErosionPasses,
            UpscaleFactor = baseOptions.UpscaleFactor,
            HydraulicErosion = new HydraulicErosionTuning {
                TopologyRefreshInterval = request.TopologyRefreshInterval,
                NeighborSampleCount = request.NeighborSampleCount,
                BaseFlow = request.BaseFlow,
                ErosionCapFactor = request.ErosionCapFactor,
                SlopeFlowFactor = request.SlopeFlowFactor,
                DepositionRatio = request.DepositionRatio,
            },
        };

        try {
            generationOptions.Validate();
        } catch (ArgumentException ex) {
            return BadRequest(new { error = ex.Message });
        }

        TerrainMap map = terrainGenerator.Generate(generationOptions);
        byte[] heightBytes = TerrainMapEncoding.EncodeHeight(map);
        byte[] waterBytes = TerrainMapEncoding.EncodeFloats(map.WaterAccumulationData);
        byte[] riverBytes = TerrainMapEncoding.EncodeMask(map.RiverMask);
        byte[] lakeBytes = TerrainMapEncoding.EncodeMask(map.LakeMask);

        TerrainTuningRenderDto dto = new(
            generationOptions.Seed,
            generationOptions.Size,
            map.Size,
            generationOptions.UpscaleFactor,
            request.ResizeEnabled,
            generationOptions.ErosionPasses,
            generationOptions.HydraulicErosion.TopologyRefreshInterval,
            generationOptions.HydraulicErosion.NeighborSampleCount,
            generationOptions.HydraulicErosion.BaseFlow,
            generationOptions.HydraulicErosion.ErosionCapFactor,
            generationOptions.HydraulicErosion.SlopeFlowFactor,
            generationOptions.HydraulicErosion.DepositionRatio,
            Convert.ToBase64String(heightBytes),
            Convert.ToBase64String(waterBytes),
            Convert.ToBase64String(riverBytes),
            Convert.ToBase64String(lakeBytes));

        return Ok(dto);
    }
}

public sealed record TerrainTuningDefaultsDto(
    int Seed,
    int Size,
    int UpscaleFactor,
    int ErosionPasses,
    int TopologyRefreshInterval,
    int NeighborSampleCount,
    float BaseFlow,
    float ErosionCapFactor,
    float SlopeFlowFactor,
    float DepositionRatio
);

public sealed class TerrainTuningRequestDto {
    public int Seed { get; init; }

    public int SourceSize { get; init; }

    public bool ResizeEnabled { get; init; }

    public int ErosionPasses { get; init; }

    public int TopologyRefreshInterval { get; init; }

    public int NeighborSampleCount { get; init; }

    public float BaseFlow { get; init; }

    public float ErosionCapFactor { get; init; }

    public float SlopeFlowFactor { get; init; }

    public float DepositionRatio { get; init; }
}

public sealed record TerrainTuningRenderDto(
    int Seed,
    int SourceSize,
    int Size,
    int UpscaleFactor,
    bool ResizeEnabled,
    int ErosionPasses,
    int TopologyRefreshInterval,
    int NeighborSampleCount,
    float BaseFlow,
    float ErosionCapFactor,
    float SlopeFlowFactor,
    float DepositionRatio,
    string HeightDataBase64,
    string WaterAccumulationDataBase64,
    string RiverMaskDataBase64,
    string LakeMaskDataBase64
);