// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

using Microsoft.AspNetCore.ResponseCompression;

using OpenTelemetry.Metrics;

using uk.osric.sim.server.Simulation;
using uk.osric.sim.server.Terrain;
using uk.osric.sim.simulation.Time;
using uk.osric.sim.terrain.Generation;

namespace uk.osric.sim.server;

public static class Program {
	public static void Main(string[] args) {
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		builder.WebHost.UseUrls("http://localhost:5000");

		builder.Services.AddResponseCompression(options => {
			options.EnableForHttps = true;
			options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
		});
		
		builder.Services.AddControllers();
		builder.Services.AddHealthChecks();
		builder.Services.AddSingleton<ITerrainGenerator, TerrainGenerationOrchestrator>();
		builder.Services.AddSingleton<TerrainSnapshot>();
		builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
		builder.Services.AddSingleton<SimulationMetrics>();
		builder.Services.AddHostedService<SimulationHostedService>();

		builder.Services.AddOpenTelemetry()
			.WithMetrics(metrics => {
				metrics
					.AddAspNetCoreInstrumentation()
					.AddRuntimeInstrumentation()
					.AddMeter(SimulationMetrics.MeterName)
					.AddPrometheusExporter();

				string? otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
				if (!string.IsNullOrEmpty(otlpEndpoint)) {
					metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
				}
			});

		WebApplication app = builder.Build();

		if (app.Environment.IsDevelopment()) {
			app.UseDeveloperExceptionPage();
		}

		app.UseResponseCompression();
		app.UseOpenTelemetryPrometheusScrapingEndpoint();
		app.UseDefaultFiles();
		app.UseStaticFiles();

		app.MapHealthChecks("/health");
		app.MapControllers();
		app.MapFallbackToFile("index.html");

		app.Run();
	}
}
