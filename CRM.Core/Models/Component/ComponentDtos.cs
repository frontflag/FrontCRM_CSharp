namespace CRM.Core.Models.Component
{
    /// <summary>
    /// 物料完整数据 DTO（从缓存或外部 API 返回给前端的统一格式）
    /// </summary>
    public class ComponentDetailDto
    {
        public string Mpn { get; set; } = string.Empty;
        public string? ManufacturerName { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? LifecycleStatus { get; set; }
        public string? PackageType { get; set; }
        public bool? IsRoHSCompliant { get; set; }
        public List<ComponentSpec> Specs { get; set; } = new();
        public List<DistributorPricing> Sellers { get; set; } = new();
        public List<AlternativeComponent> Alternatives { get; set; } = new();
        public List<ApplicationScenario> Applications { get; set; } = new();
        public List<PriceTrendPoint> PriceTrend { get; set; } = new();
        public List<ComponentNews> News { get; set; } = new();
        public string DataSource { get; set; } = "Mock";
        public DateTime FetchedAt { get; set; }
        public bool IsFromCache { get; set; }
    }

    /// <summary>
    /// 规格参数
    /// </summary>
    public class ComponentSpec
    {
        public string AttributeName { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 分销商报价
    /// </summary>
    public class DistributorPricing
    {
        public string DistributorName { get; set; } = string.Empty;
        public string? DistributorUrl { get; set; }
        public int InStock { get; set; }
        public bool IsAuthorized { get; set; }
        public string? PartNumber { get; set; }
        public string? MinOrderQty { get; set; }
        public string? LeadTime { get; set; }
        public List<PriceBreak> PriceBreaks { get; set; } = new();
    }

    /// <summary>
    /// 阶梯价格
    /// </summary>
    public class PriceBreak
    {
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
    }

    /// <summary>
    /// 可替代料
    /// </summary>
    public class AlternativeComponent
    {
        public string Mpn { get; set; } = string.Empty;
        public string? ManufacturerName { get; set; }
        public string? Description { get; set; }
        public string? RelationType { get; set; }  // FunctionalEquivalent / PinCompatible / Similar
        public string? PriceRange { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 应用场景
    /// </summary>
    public class ApplicationScenario
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
    }

    /// <summary>
    /// 价格趋势数据点
    /// </summary>
    public class PriceTrendPoint
    {
        public string Month { get; set; } = string.Empty;  // e.g. "Jan 2025"
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
    }

    /// <summary>
    /// 相关新闻
    /// </summary>
    public class ComponentNews
    {
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Source { get; set; }
        public string? Url { get; set; }
        public string? Tag { get; set; }  // e.g. "Product Launch" / "Market Trend"
        public DateTime? PublishedAt { get; set; }
    }
}
