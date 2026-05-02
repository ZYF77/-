using BuildingMaterialsAuditAgent.Models;

namespace BuildingMaterialsAuditAgent.Data;

public sealed class MaterialKnowledgeBase
{
    private readonly List<MaterialRule> _rules =
    [
        new()
        {
            Category = "waterproof",
            DisplayName = "防水材料",
            Keywords = ["防水", "卷材", "涂膜", "聚氨酯", "SBS", "自粘"],
            RequiredDocuments = ["材料报审单", "出厂合格证", "检测报告", "复试报告"],
            RecommendedStandards = ["GB 18242", "GB/T 23445", "GB 55030"],
            RequiredParameterKeywords = ["不透水性", "拉伸强度", "低温柔性", "厚度"],
            MinReferencePrice = 18,
            MaxReferencePrice = 95,
            PriceUnit = "元/平方米",
            HighRiskKeywords = ["地下室", "屋面", "卫生间", "渗漏", "一级防水"]
        },
        new()
        {
            Category = "insulation",
            DisplayName = "保温材料",
            Keywords = ["保温", "岩棉", "挤塑板", "XPS", "EPS", "玻璃棉"],
            RequiredDocuments = ["材料报审单", "出厂合格证", "检测报告", "燃烧性能报告"],
            RecommendedStandards = ["GB 8624", "GB/T 10801", "GB 55037"],
            RequiredParameterKeywords = ["燃烧性能", "导热系数", "密度", "压缩强度"],
            MinReferencePrice = 40,
            MaxReferencePrice = 180,
            PriceUnit = "元/平方米",
            HighRiskKeywords = ["外墙", "防火隔离带", "幕墙", "A级", "B1级"]
        },
        new()
        {
            Category = "rebar",
            DisplayName = "钢筋",
            Keywords = ["钢筋", "HRB", "HPB", "螺纹钢"],
            RequiredDocuments = ["材料报审单", "出厂合格证", "质量证明书", "复试报告"],
            RecommendedStandards = ["GB/T 1499.2", "GB 55008"],
            RequiredParameterKeywords = ["屈服强度", "抗拉强度", "伸长率", "重量偏差"],
            MinReferencePrice = 3200,
            MaxReferencePrice = 5200,
            PriceUnit = "元/吨",
            HighRiskKeywords = ["主体结构", "基础", "梁", "柱", "抗震"]
        },
        new()
        {
            Category = "concrete",
            DisplayName = "混凝土",
            Keywords = ["混凝土", "C30", "C35", "C40", "商砼"],
            RequiredDocuments = ["配合比通知单", "出厂合格证", "强度报告", "开盘鉴定"],
            RecommendedStandards = ["GB 50164", "GB/T 14902", "GB 50204"],
            RequiredParameterKeywords = ["强度等级", "坍落度", "抗渗等级", "氯离子"],
            MinReferencePrice = 360,
            MaxReferencePrice = 780,
            PriceUnit = "元/立方米",
            HighRiskKeywords = ["主体结构", "基础", "地下室", "抗渗", "大体积"]
        },
        new()
        {
            Category = "cable",
            DisplayName = "电线电缆",
            Keywords = ["电线", "电缆", "YJV", "BV", "阻燃", "耐火"],
            RequiredDocuments = ["材料报审单", "出厂合格证", "检测报告", "CCC认证"],
            RecommendedStandards = ["GB/T 12706", "GB/T 5023", "GB 31247"],
            RequiredParameterKeywords = ["导体电阻", "绝缘厚度", "阻燃等级", "耐火性能"],
            MinReferencePrice = 8,
            MaxReferencePrice = 260,
            PriceUnit = "元/米",
            HighRiskKeywords = ["消防", "应急照明", "配电干线", "耐火"]
        },
        new()
        {
            Category = "pipe",
            DisplayName = "管材",
            Keywords = ["管材", "PPR", "PVC", "PE管", "镀锌钢管", "HDPE"],
            RequiredDocuments = ["材料报审单", "出厂合格证", "检测报告", "卫生许可批件"],
            RecommendedStandards = ["GB/T 13663", "GB/T 18742", "GB/T 5836"],
            RequiredParameterKeywords = ["公称压力", "壁厚", "环刚度", "卫生性能"],
            MinReferencePrice = 5,
            MaxReferencePrice = 180,
            PriceUnit = "元/米",
            HighRiskKeywords = ["给水", "消防", "压力管", "埋地"]
        }
    ];

    public IReadOnlyList<MaterialRule> Rules => _rules;

    public MaterialRule ResolveRule(string categoryOrText)
    {
        if (string.IsNullOrWhiteSpace(categoryOrText))
        {
            return _rules[0];
        }

        var normalized = categoryOrText.Trim();
        var exact = _rules.FirstOrDefault(rule =>
            rule.Category.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            rule.DisplayName.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            return exact;
        }

        return _rules
            .OrderByDescending(rule => rule.Keywords.Count(keyword =>
                normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(rule => rule.Keywords.Any(keyword =>
                normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            ?? _rules[0];
    }

    public MaterialAuditRequest CreateSampleRequest() => new()
    {
        ProjectName = "星河湾二期商业综合体",
        MaterialName = "外墙岩棉保温板",
        Category = "insulation",
        Specification = "1200*600*80mm，容重 120kg/m3，A级不燃",
        Supplier = "华北节能建材有限公司",
        Brand = "北辰",
        BatchNo = "BC-2026-0421-RW",
        UnitPrice = 156,
        Quantity = 4800,
        Unit = "m2",
        BudgetUnitPrice = 145,
        HistoricalUnitPrice = 138,
        ProvidedDocuments = ["材料报审单", "出厂合格证", "检测报告"],
        DeclaredStandards = ["GB 8624", "GB/T 25975"],
        DesignRequirement = "外墙保温材料应采用A级不燃岩棉板，厚度80mm，导热系数不大于0.040 W/(m.K)，燃烧性能应满足GB 8624。",
        SubmittedText = """
        材料名称：外墙岩棉保温板
        规格型号：1200*600*80mm
        品牌：北辰
        批次：BC-2026-0421-RW
        检测结论：所检项目符合标准要求
        燃烧性能：A级
        导热系数：0.041 W/(m.K)
        密度：120 kg/m3
        压缩强度：42 kPa
        适用部位：外墙保温系统，含防火隔离带
        """
    };
}
