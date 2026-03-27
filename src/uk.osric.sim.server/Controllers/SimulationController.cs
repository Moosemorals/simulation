// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.Mvc;

using uk.osric.sim.simulation.Time;

namespace uk.osric.sim.server.Controllers;

[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase {
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken) {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";

        int sequence = 0;

        while (!cancellationToken.IsCancellationRequested) {
            string payload = $"{{\"sequence\":{sequence},\"tickRateHz\":{SimulationOptions.DefaultTickRateHz}}}";

            await Response.WriteAsync($"event: tick\n", cancellationToken);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            sequence++;

            await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
        }
    }
}