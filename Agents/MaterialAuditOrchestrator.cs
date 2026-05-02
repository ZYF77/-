using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Agents;

public sealed class MaterialAuditOrchestrator(
    MaterialParserAgent parserAgent,
    ComplianceReviewAgent complianceReviewAgent,
    CostAnalysisAgent costAnalysisAgent,
    RiskReviewAgent riskReviewAgent)
{
    public Task<MaterialAuditReport> RunAsync(MaterialAuditRequest request)
    {
        var trace = new List<AgentStep>();

        var profile = parserAgent.Parse(request);
        trace.Add(new AgentStep
        {
            Agent = "文档解析 Agent",
            Output = $"识别材料类别为 {profile.Category}，抽取字段 {profile.ExtractedFields.Count} 个、数值参数 {profile.ExtractedNumericParameters.Count} 个。"
        });

        var findings = complianceReviewAgent.Review(profile);
        trace.Add(new AgentStep
        {
            Agent = "合规审核 Agent",
            Output = $"完成资料完整性、规范依据、设计要求和关键性能参数核对，产生 {findings.Count} 条审核发现。"
        });

        var (cost, costFindings) = costAnalysisAgent.Analyze(profile);
        findings.AddRange(costFindings);
        trace.Add(new AgentStep
        {
            Agent = "成本分析 Agent",
            Output = $"完成预算价、历史价和参考区间比对，预计金额 {cost.TotalAmount:0.##} 元。"
        });

        var decision = riskReviewAgent.Decide(profile, findings, cost);
        trace.Add(new AgentStep
        {
            Agent = "风险复核 Agent",
            Output = $"综合风险分 {decision.Score}/100，结论：{decision.Conclusion}。"
        });

        var report = new MaterialAuditReport
        {
            ReportId = $"BMA-{DateTimeOffset.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            CreatedAt = DateTimeOffset.Now,
            Profile = profile,
            Conclusion = decision.Conclusion,
            RiskScore = decision.Score,
            RiskLevel = decision.RiskLevel,
            ExecutiveSummary = decision.ExecutiveSummary,
            Findings = findings
                .OrderByDescending(item => SeverityRank(item.Severity))
                .ThenBy(item => item.OwnerAgent)
                .ToList(),
            Cost = cost,
            RequiredManualChecks = decision.RequiredManualChecks,
            NextActions = decision.NextActions,
            Trace = trace
        };

        return Task.FromResult(report);
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "Critical" => 4,
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };
}
