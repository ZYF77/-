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
builder.Services.AddSingleton<AuditHistoryStore>();

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
app.MapGet("/api/audits", async (int? limit, AuditHistoryStore historyStore, CancellationToken cancellationToken) =>
{
    var items = await historyStore.ListAsync(limit ?? 50, cancellationToken);
    return Results.Ok(items);
});

app.MapGet("/api/audits/{reportId}", async (string reportId, AuditHistoryStore historyStore, CancellationToken cancellationToken) =>
{
    var report = await historyStore.GetAsync(reportId, cancellationToken);
    return report is null ? Results.NotFound() : Results.Ok(report);
});

app.MapPost("/api/audit", async (
    MaterialAuditRequest request,
    MaterialAuditOrchestrator orchestrator,
    AuditHistoryStore historyStore,
    CancellationToken cancellationToken) =>
{
    var report = await orchestrator.RunAsync(request);
    await historyStore.SaveAsync(report, cancellationToken);
    return Results.Ok(report);
});

var indexFile = Path.Combine(app.Environment.WebRootPath ?? "", "index.html");
if (File.Exists(indexFile))
{
    app.MapFallbackToFile("index.html");
}

app.Run();
