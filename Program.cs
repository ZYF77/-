using BuildingMaterialsAuditAgent.Agents;
using BuildingMaterialsAuditAgent.Data;
using BuildingMaterialsAuditAgent.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<MaterialKnowledgeBase>();
builder.Services.AddSingleton<MaterialParserAgent>();
builder.Services.AddSingleton<ComplianceReviewAgent>();
builder.Services.AddSingleton<CostAnalysisAgent>();
builder.Services.AddSingleton<RiskReviewAgent>();
builder.Services.AddSingleton<MaterialAuditOrchestrator>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "building-materials-audit-agent",
    time = DateTimeOffset.Now
}));

app.MapGet("/api/sample", (MaterialKnowledgeBase kb) => Results.Ok(kb.CreateSampleRequest()));
app.MapGet("/api/rules", (MaterialKnowledgeBase kb) => Results.Ok(kb.Rules));

app.MapPost("/api/audit", async (MaterialAuditRequest request, MaterialAuditOrchestrator orchestrator) =>
{
    var report = await orchestrator.RunAsync(request);
    return Results.Ok(report);
});

var indexFile = Path.Combine(app.Environment.WebRootPath ?? "", "index.html");
if (File.Exists(indexFile))
{
    app.MapFallbackToFile("index.html");
}

app.Run();
