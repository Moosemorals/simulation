// SPDX-FileCopyrightText: Copyright (c) 2026 Osric Wilkinson <osric@fluffypeople.com>
// SPDX-License-Identifier: ISC

namespace uk.osric.sim.server;

public static class Program {
	public static void Main(string[] args) {
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		builder.WebHost.UseUrls("http://0.0.0.0:5000");
		
		builder.Services.AddControllers();
		builder.Services.AddHealthChecks();

		WebApplication app = builder.Build();

		if (app.Environment.IsDevelopment()) {
			app.UseDeveloperExceptionPage();
		}

		app.UseDefaultFiles();
		app.UseStaticFiles();

		app.MapHealthChecks("/health");
		app.MapControllers();
		app.MapFallbackToFile("index.html");

		app.Run();
	}
}
