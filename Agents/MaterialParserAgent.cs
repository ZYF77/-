using System.Text.RegularExpressions;
using BuildingMaterialsAuditAgent.Data;
using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Agents;

public sealed class MaterialParserAgent(MaterialKnowledgeBase knowledgeBase)
{
    private static readonly Regex KeyValueRegex = new(@"(?<key>[\u4e00-\u9fa5A-Za-z0-9/()（）.]{2,18})\s*[:：]\s*(?<value>[^\r\n；;]+)", RegexOptions.Compiled);
    private static readonly Regex NumberWithUnitRegex = new(@"(?<name>导热系数|压缩强度|抗压强度|屈服强度|抗拉强度|厚度|密度|坍落度|公称压力|壁厚|环刚度|导体电阻)\s*[:：]?\s*(?<value>\d+(\.\d+)?)", RegexOptions.Compiled);

    public MaterialProfile Parse(MaterialAuditRequest request)
    {
        var text = request.SubmittedText ?? "";
        var extracted = ExtractKeyValues(text);
        var numeric = ExtractNumericParameters(text);

        var materialName = FirstNonEmpty(request.MaterialName, GetAny(extracted, "材料名称", "名称", "产品名称"));
        var specification = FirstNonEmpty(request.Specification, GetAny(extracted, "规格型号", "规格", "型号"));
        var supplier = FirstNonEmpty(request.Supplier, GetAny(extracted, "供应商", "生产厂家", "厂家"));
        var brand = FirstNonEmpty(request.Brand, GetAny(extracted, "品牌", "商标"));
        var batch = FirstNonEmpty(request.BatchNo, GetAny(extracted, "批次", "批号", "生产批号"));

        var categorySeed = string.Join(' ', request.Category, materialName, specification, text);
        var rule = knowledgeBase.ResolveRule(categorySeed);

        return new MaterialProfile
        {
            ProjectName = request.ProjectName.Trim(),
            MaterialName = materialName.Trim(),
            Category = rule.Category,
            Specification = specification.Trim(),
            Supplier = supplier.Trim(),
            Brand = brand.Trim(),
            BatchNo = batch.Trim(),
            UnitPrice = request.UnitPrice,
            Quantity = request.Quantity,
            Unit = request.Unit.Trim(),
            BudgetUnitPrice = request.BudgetUnitPrice,
            HistoricalUnitPrice = request.HistoricalUnitPrice,
            DesignRequirement = request.DesignRequirement.Trim(),
            SubmittedText = text.Trim(),
            ProvidedDocuments = request.ProvidedDocuments
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            DeclaredStandards = request.DeclaredStandards
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ExtractedFields = extracted,
            ExtractedNumericParameters = numeric
        };
    }

    private static Dictionary<string, string> ExtractKeyValues(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in KeyValueRegex.Matches(text))
        {
            var key = match.Groups["key"].Value.Trim();
            var value = match.Groups["value"].Value.Trim();
            if (!result.ContainsKey(key) && !string.IsNullOrWhiteSpace(value))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static Dictionary<string, decimal> ExtractNumericParameters(string text)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in NumberWithUnitRegex.Matches(text))
        {
            var name = match.Groups["name"].Value.Trim();
            var raw = match.Groups["value"].Value.Trim();
            if (decimal.TryParse(raw, out var value) && !result.ContainsKey(name))
            {
                result[name] = value;
            }
        }

        return result;
    }

    private static string GetAny(Dictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return "";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}
