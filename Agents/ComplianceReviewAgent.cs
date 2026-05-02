using System.Text.RegularExpressions;
using BuildingMaterialsAuditAgent.Data;
using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Agents;

public sealed class ComplianceReviewAgent(MaterialKnowledgeBase knowledgeBase)
{
    public List<AuditFinding> Review(MaterialProfile profile)
    {
        var rule = knowledgeBase.ResolveRule(profile.Category);
        var findings = new List<AuditFinding>();

        CheckBasicFields(profile, findings);
        CheckDocuments(profile, rule, findings);
        CheckStandards(profile, rule, findings);
        CheckRequiredParameters(profile, rule, findings);
        CheckDesignRequirement(profile, findings);
        CheckCategorySpecificRules(profile, rule, findings);

        if (findings.Count == 0)
        {
            findings.Add(new AuditFinding
            {
                Title = "未发现明显合规问题",
                Severity = "Low",
                Evidence = "资料字段、必要文件、标准声明和核心参数均已覆盖。",
                Recommendation = "进入人工复核和留档流程。",
                OwnerAgent = "合规审核 Agent"
            });
        }

        return findings;
    }

    private static void CheckBasicFields(MaterialProfile profile, List<AuditFinding> findings)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.MaterialName)) missing.Add("材料名称");
        if (string.IsNullOrWhiteSpace(profile.Specification)) missing.Add("规格型号");
        if (string.IsNullOrWhiteSpace(profile.Supplier)) missing.Add("供应商");
        if (string.IsNullOrWhiteSpace(profile.BatchNo)) missing.Add("批次/批号");

        if (missing.Count > 0)
        {
            findings.Add(new AuditFinding
            {
                Title = "基础信息不完整",
                Severity = missing.Count >= 3 ? "High" : "Medium",
                Evidence = $"缺少：{string.Join("、", missing)}。",
                Recommendation = "补齐材料报审单中的基础字段后再进入合规审批。",
                OwnerAgent = "合规审核 Agent"
            });
        }
    }

    private static void CheckDocuments(MaterialProfile profile, MaterialRule rule, List<AuditFinding> findings)
    {
        var missingDocs = rule.RequiredDocuments
            .Where(required => !profile.ProvidedDocuments.Any(doc => doc.Contains(required, StringComparison.OrdinalIgnoreCase) || required.Contains(doc, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingDocs.Count > 0)
        {
            findings.Add(new AuditFinding
            {
                Title = "必要报审资料缺失",
                Severity = missingDocs.Any(doc => doc.Contains("复试") || doc.Contains("认证")) ? "High" : "Medium",
                Evidence = $"{rule.DisplayName}通常需要：{string.Join("、", rule.RequiredDocuments)}；当前缺少：{string.Join("、", missingDocs)}。",
                Recommendation = "要求供应商补交缺失资料，并核验报告编号、检测机构资质和批次一致性。",
                OwnerAgent = "文档解析 Agent"
            });
        }
    }

    private static void CheckStandards(MaterialProfile profile, MaterialRule rule, List<AuditFinding> findings)
    {
        var source = string.Join(' ', profile.DeclaredStandards) + " " + profile.SubmittedText + " " + profile.DesignRequirement;
        var matched = rule.RecommendedStandards
            .Where(standard => source.Contains(standard, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
        {
            findings.Add(new AuditFinding
            {
                Title = "未识别到推荐规范依据",
                Severity = "Medium",
                Evidence = $"资料中未匹配到 {rule.DisplayName} 常用规范：{string.Join("、", rule.RecommendedStandards)}。",
                Recommendation = "补充适用规范条款，或说明使用企业标准/地方标准的等效依据。",
                OwnerAgent = "合规审核 Agent"
            });
        }
    }

    private static void CheckRequiredParameters(MaterialProfile profile, MaterialRule rule, List<AuditFinding> findings)
    {
        var text = profile.SubmittedText + " " + profile.Specification + " " + profile.DesignRequirement;
        var missing = rule.RequiredParameterKeywords
            .Where(keyword => !text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count > 0)
        {
            findings.Add(new AuditFinding
            {
                Title = "关键性能参数覆盖不足",
                Severity = missing.Count >= 3 ? "High" : "Medium",
                Evidence = $"未在资料中识别到：{string.Join("、", missing)}。",
                Recommendation = "要求检测报告或产品说明书补充对应性能指标，避免仅凭合格证通过。",
                OwnerAgent = "合规审核 Agent"
            });
        }
    }

    private static void CheckDesignRequirement(MaterialProfile profile, List<AuditFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(profile.DesignRequirement))
        {
            findings.Add(new AuditFinding
            {
                Title = "缺少设计要求对照",
                Severity = "Medium",
                Evidence = "请求中未提供设计说明、图纸材料表或招标清单要求。",
                Recommendation = "补充设计要求后进行规格、性能和品牌替代核对。",
                OwnerAgent = "合规审核 Agent"
            });
            return;
        }

        var designThickness = ExtractNumberAfter(profile.DesignRequirement, "厚度");
        var submittedThickness = ExtractNumberAfter(profile.Specification + " " + profile.SubmittedText, "厚度");

        if (designThickness.HasValue && submittedThickness.HasValue && submittedThickness.Value < designThickness.Value)
        {
            findings.Add(new AuditFinding
            {
                Title = "规格参数低于设计要求",
                Severity = "High",
                Evidence = $"设计厚度约为 {designThickness.Value}，报审资料识别厚度约为 {submittedThickness.Value}。",
                Recommendation = "核实是否存在材料替代；如需替代，应提交设计变更或技术核定。",
                OwnerAgent = "合规审核 Agent"
            });
        }
    }

    private static void CheckCategorySpecificRules(MaterialProfile profile, MaterialRule rule, List<AuditFinding> findings)
    {
        var text = profile.SubmittedText + " " + profile.Specification + " " + profile.DesignRequirement;

        if (rule.Category == "insulation")
        {
            if (Regex.IsMatch(text, @"B2\s*级|B3\s*级|可燃", RegexOptions.IgnoreCase))
            {
                findings.Add(new AuditFinding
                {
                    Title = "保温材料燃烧性能风险",
                    Severity = "Critical",
                    Evidence = "资料中出现 B2/B3 或可燃描述，与外墙/防火场景通常存在重大冲突。",
                    Recommendation = "暂停通过，要求提供燃烧性能检测报告并由设计/消防专业复核。",
                    OwnerAgent = "风险复核 Agent"
                });
            }

            if (profile.ExtractedNumericParameters.TryGetValue("导热系数", out var thermal) &&
                profile.DesignRequirement.Contains("导热系数", StringComparison.OrdinalIgnoreCase))
            {
                var designThermal = ExtractNumberAfter(profile.DesignRequirement, "导热系数");
                if (designThermal.HasValue && thermal > designThermal.Value)
                {
                    findings.Add(new AuditFinding
                    {
                        Title = "导热系数不满足设计上限",
                        Severity = "High",
                        Evidence = $"报审导热系数为 {thermal}，设计要求不大于 {designThermal.Value}。",
                        Recommendation = "要求供应商更换满足设计热工指标的材料或提交节能复核意见。",
                        OwnerAgent = "合规审核 Agent"
                    });
                }
            }
        }

        if (rule.Category == "concrete")
        {
            var designStrength = ExtractConcreteStrength(profile.DesignRequirement);
            var submittedStrength = ExtractConcreteStrength(profile.Specification + " " + profile.SubmittedText);
            if (designStrength.HasValue && submittedStrength.HasValue && submittedStrength.Value < designStrength.Value)
            {
                findings.Add(new AuditFinding
                {
                    Title = "混凝土强度等级低于设计要求",
                    Severity = "Critical",
                    Evidence = $"设计要求 C{designStrength.Value}，报审资料为 C{submittedStrength.Value}。",
                    Recommendation = "不得进场使用，需按设计等级重新报审。",
                    OwnerAgent = "风险复核 Agent"
                });
            }
        }

        if (rule.Category == "rebar")
        {
            var joined = profile.Specification + " " + profile.SubmittedText;
            if (joined.Contains("HRB400", StringComparison.OrdinalIgnoreCase) &&
                profile.DesignRequirement.Contains("HRB500", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new AuditFinding
                {
                    Title = "钢筋级别与设计要求不一致",
                    Severity = "Critical",
                    Evidence = "设计要求识别为 HRB500，报审资料包含 HRB400。",
                    Recommendation = "暂停通过，提交设计复核或重新报审符合等级的钢筋。",
                    OwnerAgent = "风险复核 Agent"
                });
            }
        }

        var highRiskHit = rule.HighRiskKeywords.FirstOrDefault(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (highRiskHit is not null)
        {
            findings.Add(new AuditFinding
            {
                Title = "命中重点部位或高风险场景",
                Severity = "Medium",
                Evidence = $"资料中出现“{highRiskHit}”，该场景建议提高审核等级。",
                Recommendation = "增加监理/设计/成本三方复核节点，并保留可追溯审核记录。",
                OwnerAgent = "风险复核 Agent"
            });
        }
    }

    private static decimal? ExtractNumberAfter(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var tail = text[index..];
        var match = Regex.Match(tail, @"(\d+(\.\d+)?)");
        return match.Success && decimal.TryParse(match.Groups[1].Value, out var value) ? value : null;
    }

    private static int? ExtractConcreteStrength(string text)
    {
        var match = Regex.Match(text, @"C\s*(\d{2})", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : null;
    }
}
