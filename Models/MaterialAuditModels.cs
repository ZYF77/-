namespace BuildingMaterialsAuditAgent.Models;

public sealed record MaterialAuditRequest
{
    public string ProjectName { get; init; } = "";
    public string MaterialName { get; init; } = "";
    public string Category { get; init; } = "";
    public string Specification { get; init; } = "";
    public string Supplier { get; init; } = "";
    public string Brand { get; init; } = "";
    public string BatchNo { get; init; } = "";
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = "";
    public decimal? BudgetUnitPrice { get; init; }
    public decimal? HistoricalUnitPrice { get; init; }
    public string DesignRequirement { get; init; } = "";
    public string SubmittedText { get; init; } = "";
    public List<string> ProvidedDocuments { get; init; } = [];
    public List<string> DeclaredStandards { get; init; } = [];
}

public sealed record MaterialProfile
{
    public string ProjectName { get; init; } = "";
    public string MaterialName { get; init; } = "";
    public string Category { get; init; } = "";
    public string Specification { get; init; } = "";
    public string Supplier { get; init; } = "";
    public string Brand { get; init; } = "";
    public string BatchNo { get; init; } = "";
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = "";
    public decimal? BudgetUnitPrice { get; init; }
    public decimal? HistoricalUnitPrice { get; init; }
    public string DesignRequirement { get; init; } = "";
    public string SubmittedText { get; init; } = "";
    public List<string> ProvidedDocuments { get; init; } = [];
    public List<string> DeclaredStandards { get; init; } = [];
    public Dictionary<string, string> ExtractedFields { get; init; } = [];
    public Dictionary<string, decimal> ExtractedNumericParameters { get; init; } = [];
}

public sealed record MaterialRule
{
    public string Category { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string[] Keywords { get; init; } = [];
    public string[] RequiredDocuments { get; init; } = [];
    public string[] RecommendedStandards { get; init; } = [];
    public string[] RequiredParameterKeywords { get; init; } = [];
    public decimal? MinReferencePrice { get; init; }
    public decimal? MaxReferencePrice { get; init; }
    public string PriceUnit { get; init; } = "";
    public string[] HighRiskKeywords { get; init; } = [];
}

public sealed record AuditFinding
{
    public string Title { get; init; } = "";
    public string Severity { get; init; } = "Low";
    public string Evidence { get; init; } = "";
    public string Recommendation { get; init; } = "";
    public string OwnerAgent { get; init; } = "";
}

public sealed record CostAnalysis
{
    public decimal TotalAmount { get; init; }
    public decimal? BudgetVarianceRate { get; init; }
    public decimal? HistoricalVarianceRate { get; init; }
    public string ReferenceBand { get; init; } = "";
    public string Summary { get; init; } = "";
    public List<CostSignal> Signals { get; init; } = [];
}

public sealed record CostSignal
{
    public string Name { get; init; } = "";
    public string Level { get; init; } = "Info";
    public string Detail { get; init; } = "";
}

public sealed record AgentStep
{
    public string Agent { get; init; } = "";
    public string Status { get; init; } = "Done";
    public string Output { get; init; } = "";
}

public sealed record MaterialAuditReport
{
    public string ReportId { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public MaterialProfile Profile { get; init; } = new();
    public string Conclusion { get; init; } = "";
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = "";
    public string ExecutiveSummary { get; init; } = "";
    public List<AuditFinding> Findings { get; init; } = [];
    public CostAnalysis Cost { get; init; } = new();
    public List<string> RequiredManualChecks { get; init; } = [];
    public List<string> NextActions { get; init; } = [];
    public List<AgentStep> Trace { get; init; } = [];
}
