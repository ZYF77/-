using BuildingMaterialsAuditAgent.Data;
using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Agents;

public sealed class CostAnalysisAgent(MaterialKnowledgeBase knowledgeBase)
{
    public (CostAnalysis Cost, List<AuditFinding> Findings) Analyze(MaterialProfile profile)
    {
        var rule = knowledgeBase.ResolveRule(profile.Category);
        var signals = new List<CostSignal>();
        var findings = new List<AuditFinding>();
        var totalAmount = profile.UnitPrice * profile.Quantity;

        decimal? budgetVariance = profile.BudgetUnitPrice.HasValue && profile.BudgetUnitPrice.Value > 0
            ? (profile.UnitPrice - profile.BudgetUnitPrice.Value) / profile.BudgetUnitPrice.Value
            : null;

        decimal? historicalVariance = profile.HistoricalUnitPrice.HasValue && profile.HistoricalUnitPrice.Value > 0
            ? (profile.UnitPrice - profile.HistoricalUnitPrice.Value) / profile.HistoricalUnitPrice.Value
            : null;

        if (budgetVariance.HasValue)
        {
            AddVarianceSignal("预算价", budgetVariance.Value, profile.BudgetUnitPrice!.Value, profile.UnitPrice, signals, findings);
        }

        if (historicalVariance.HasValue)
        {
            AddVarianceSignal("历史采购价", historicalVariance.Value, profile.HistoricalUnitPrice!.Value, profile.UnitPrice, signals, findings);
        }

        if (rule.MinReferencePrice.HasValue && profile.UnitPrice < rule.MinReferencePrice.Value)
        {
            var detail = $"单价 {profile.UnitPrice:0.##} 低于参考区间下限 {rule.MinReferencePrice:0.##} {rule.PriceUnit}，存在低价替代或质量缩水风险。";
            signals.Add(new CostSignal { Name = "低价异常", Level = "Warning", Detail = detail });
            findings.Add(new AuditFinding
            {
                Title = "报价低于材料参考区间",
                Severity = "Medium",
                Evidence = detail,
                Recommendation = "核验品牌、规格、检测报告批次和供货承诺，避免以低配材料替代。",
                OwnerAgent = "成本分析 Agent"
            });
        }

        if (rule.MaxReferencePrice.HasValue && profile.UnitPrice > rule.MaxReferencePrice.Value)
        {
            var detail = $"单价 {profile.UnitPrice:0.##} 高于参考区间上限 {rule.MaxReferencePrice:0.##} {rule.PriceUnit}，需解释溢价来源。";
            signals.Add(new CostSignal { Name = "高价异常", Level = "Risk", Detail = detail });
            findings.Add(new AuditFinding
            {
                Title = "报价高于材料参考区间",
                Severity = "High",
                Evidence = detail,
                Recommendation = "要求供应商拆分材料价、运输费、损耗、税费和特殊认证成本，并进行二次询价。",
                OwnerAgent = "成本分析 Agent"
            });
        }

        if (signals.Count == 0)
        {
            signals.Add(new CostSignal
            {
                Name = "价格未见明显异常",
                Level = "Info",
                Detail = "报价未明显偏离预算价、历史价或内置参考区间。"
            });
        }

        var referenceBand = rule.MinReferencePrice.HasValue && rule.MaxReferencePrice.HasValue
            ? $"{rule.MinReferencePrice:0.##}-{rule.MaxReferencePrice:0.##} {rule.PriceUnit}"
            : "未配置";

        var summary = BuildSummary(profile, totalAmount, budgetVariance, historicalVariance, referenceBand);
        var cost = new CostAnalysis
        {
            TotalAmount = totalAmount,
            BudgetVarianceRate = budgetVariance,
            HistoricalVarianceRate = historicalVariance,
            ReferenceBand = referenceBand,
            Summary = summary,
            Signals = signals
        };

        return (cost, findings);
    }

    private static void AddVarianceSignal(
        string baselineName,
        decimal variance,
        decimal baselinePrice,
        decimal actualPrice,
        List<CostSignal> signals,
        List<AuditFinding> findings)
    {
        var abs = Math.Abs(variance);
        var direction = variance >= 0 ? "高于" : "低于";
        var detail = $"单价 {actualPrice:0.##} {direction}{baselineName} {baselinePrice:0.##}，偏差 {variance:P1}。";

        var level = abs switch
        {
            >= 0.20m => "Risk",
            >= 0.10m => "Warning",
            _ => "Info"
        };

        signals.Add(new CostSignal
        {
            Name = $"{baselineName}偏差",
            Level = level,
            Detail = detail
        });

        if (abs >= 0.10m)
        {
            findings.Add(new AuditFinding
            {
                Title = $"{baselineName}偏差超过阈值",
                Severity = abs >= 0.20m ? "High" : "Medium",
                Evidence = detail,
                Recommendation = variance > 0
                    ? "发起二次询价或要求供应商提交溢价说明。"
                    : "核实低价原因，重点排查品牌、规格、检测报告和供货范围是否一致。",
                OwnerAgent = "成本分析 Agent"
            });
        }
    }

    private static string BuildSummary(
        MaterialProfile profile,
        decimal totalAmount,
        decimal? budgetVariance,
        decimal? historicalVariance,
        string referenceBand)
    {
        var pieces = new List<string>
        {
            $"本批预计金额 {totalAmount:0.##} 元"
        };

        if (budgetVariance.HasValue)
        {
            pieces.Add($"较预算价偏差 {budgetVariance.Value:P1}");
        }

        if (historicalVariance.HasValue)
        {
            pieces.Add($"较历史价偏差 {historicalVariance.Value:P1}");
        }

        pieces.Add($"参考区间：{referenceBand}");

        if (profile.Quantity <= 0 || profile.UnitPrice <= 0)
        {
            pieces.Add("数量或单价未完整录入，金额测算可能不完整");
        }

        return string.Join("；", pieces) + "。";
    }
}
