// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.contracts.Simulation;
using uk.osric.sim.server.Simulation;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase {
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SimulationTickBroadcaster tickBroadcaster;
    private readonly SimulationHostedService simulationHostedService;

    public SimulationController(
        SimulationTickBroadcaster tickBroadcaster,
        SimulationHostedService simulationHostedService) {
        this.tickBroadcaster = tickBroadcaster;
        this.simulationHostedService = simulationHostedService;
    }

    [HttpGet("actors")]
    public ActionResult<IReadOnlyList<SimulationActorLocationDto>> GetActors() {
        if (!simulationHostedService.TryGetActorSnapshot(out var actors)) {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Simulation is starting.");
        }

        return Ok(actors);
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken) {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";
        var (reader, subscription) = tickBroadcaster.Subscribe();

        try {
            while (await reader.WaitToReadAsync(cancellationToken)) {
                while (reader.TryRead(out SimulationTickDto? dto)) {
                    string payload = JsonSerializer.Serialize(dto, SseJsonOptions);

                    await Response.WriteAsync("event: tick\n", cancellationToken);
                    await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Client disconnect/cancellation is expected for long-lived SSE streams.
        }
        finally {
            subscription.Dispose();

            if (!HttpContext.RequestAborted.IsCancellationRequested) {
                await Response.CompleteAsync();
            }
        }
    }
}