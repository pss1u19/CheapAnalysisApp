// T-002: Minimal host placeholder — FastEndpoints, Serilog, OpenAPI wired in T-004.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Placeholder — replaced by FastEndpoints middleware in T-004
app.MapGet("/", () => "CheapAnalysisApp is running");
app.MapGet("/testManual", () => "CheapAnalysisApp is running test manual");

app.Run();
