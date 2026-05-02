using BuildingMaterialsAuditAgent.Data;
using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Agents;

public sealed class RiskReviewAgent(MaterialKnowledgeBase knowledgeBase)
{
    public RiskDecision Decide(MaterialProfile profile, IReadOnlyList<AuditFinding> findings, CostAnalysis cost)
    {
        var rule = knowledgeBase.ResolveRule(profile.Category);
        var score = 0;

        foreach (var finding in findings)
        {
            score += finding.Severity switch
            {
                "Critical" => 35,
                "High" => 24,
                "Medium" => 12,
                "Low" => 4,
                _ => 6
            };
        }

        if (rule.HighRiskKeywords.Any(keyword => (profile.SubmittedText + profile.DesignRequirement).Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            score += 8;
        }

        if (cost.TotalAmount >= 1_000_000)
        {
            score += 8;
        }
        else if (cost.TotalAmount >= 300_000)
        {
            score += 4;
        }

        score = Math.Clamp(score, 0, 100);

        var hasCritical = findings.Any(item => item.Severity == "Critical");
        var hasHigh = findings.Any(item => item.Severity == "High");
        var missingRequiredDocument = findings.Any(item => item.Title.Contains("资料缺失"));

        var conclusion = (hasCritical, hasHigh, missingRequiredDocument, score) switch
        {
            (true, _, _, _) => "不建议通过",
            (_, true, true, _) => "暂缓通过",
            (_, true, _, _) => "需整改后复核",
            (_, _, _, >= 55) => "需整改后复核",
            (_, _, _, >= 30) => "有条件通过",
            _ => "建议通过"
        };

        var riskLevel = score switch
        {
            >= 75 => "重大风险",
            >= 55 => "高风险",
            >= 30 => "中风险",
            _ => "低风险"
        };

        var manualChecks = BuildManualChecks(profile, findings, rule);
        var nextActions = BuildNextActions(conclusion, findings);
        var summary = BuildExecutiveSummary(profile, rule, conclusion, riskLevel, score, findings, cost);

        return new RiskDecision(conclusion, score, riskLevel, summary, manualChecks, nextActions);
    }

    private static List<string> BuildManualChecks(MaterialProfile profile, IReadOnlyList<AuditFinding> findings, MaterialRule rule)
    {
        var checks = new List<string>();

        if (findings.Any(item => item.Severity is "Critical" or "High"))
        {
            checks.Add("由项目工程、成本和监理共同复核高风险项。");
        }

        if (findings.Any(item => item.Title.Contains("资料缺失") || item.Title.Contains("参数")))
        {
            checks.Add("核对检测报告原件、报告编号、检测机构资质和材料批次。");
        }

        if (findings.Any(item => item.OwnerAgent == "成本分析 Agent"))
        {
            checks.Add("对供应商报价进行二次询价，并留存比价记录。");
        }

        if (rule.Category is "insulation" or "waterproof" or "rebar" or "concrete")
        {
            checks.Add($"该材料属于{rule.DisplayName}重点审核对象，建议保留设计/监理确认意见。");
        }

        if (string.IsNullOrWhiteSpace(profile.DesignRequirement))
        {
            checks.Add("补充图纸说明、材料表或招标清单后再次运行审核。");
        }

        return checks.Distinct().ToList();
    }

    private static List<string> BuildNextActions(string conclusion, IReadOnlyList<AuditFinding> findings)
    {
        var actions = new List<string>();

        if (conclusion is "建议通过" or "有条件通过")
        {
            actions.Add("生成审核记录并进入材料样品/封样流程。");
        }

        if (findings.Any(item => item.Title.Contains("资料缺失")))
        {
            actions.Add("向供应商发起补资料清单，补齐后重新审核。");
        }

        if (findings.Any(item => item.Title.Contains("设计") || item.Title.Contains("规格")))
        {
            actions.Add("提交设计单位或专业工程师确认是否允许替代。");
        }

        if (findings.Any(item => item.OwnerAgent == "成本分析 Agent"))
        {
            actions.Add("冻结当前报价结论，完成市场询价或历史价复核后再定标。");
        }

        if (actions.Count == 0)
        {
            actions.Add("归档本次审核报告，按项目制度进行人工签认。");
        }

        return actions.Distinct().ToList();
    }

    private static string BuildExecutiveSummary(
        MaterialProfile profile,
        MaterialRule rule,
        string conclusion,
        string riskLevel,
        int score,
        IReadOnlyList<AuditFinding> findings,
        CostAnalysis cost)
    {
        var highCount = findings.Count(item => item.Severity is "High" or "Critical");
        var mediumCount = findings.Count(item => item.Severity == "Medium");
        var material = string.IsNullOrWhiteSpace(profile.MaterialName) ? rule.DisplayName : profile.MaterialName;

        return $"{material}审核结论为“{conclusion}”，风险等级为{riskLevel}（{score}/100）。" +
               $"本次识别高危问题 {highCount} 项、中风险问题 {mediumCount} 项。{cost.Summary}";
    }
}

public sealed record RiskDecision(
    string Conclusion,
    int Score,
    string RiskLevel,
    string ExecutiveSummary,
    List<string> RequiredManualChecks,
    List<string> NextActions);
