using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Component
{
    /// <summary>
    /// 物料数据缓存表 - 存储从外部 API（Nexar/Octopart）获取的物料信息
    /// 24小时内重复查询直接读取此表，超过24小时重新调用外部接口
    /// </summary>
    [Table("component_cache")]
    public class ComponentCache
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 物料型号（MPN - Manufacturer Part Number），查询的主键
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Mpn { get; set; } = string.Empty;

        /// <summary>
        /// 制造商名称
        /// </summary>
        [MaxLength(200)]
        public string? ManufacturerName { get; set; }

        /// <summary>
        /// 物料简短描述
        /// </summary>
        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        /// <summary>
        /// 物料详细描述
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// 生命周期状态（Active / Obsolete / NRND 等）
        /// </summary>
        [MaxLength(50)]
        public string? LifecycleStatus { get; set; }

        /// <summary>
        /// 封装类型（DIP-8 / SOIC-8 等）
        /// </summary>
        [MaxLength(100)]
        public string? PackageType { get; set; }

        /// <summary>
        /// 是否符合 RoHS
        /// </summary>
        public bool? IsRoHSCompliant { get; set; }

        /// <summary>
        /// 规格参数 JSON（序列化的 List&lt;ComponentSpec&gt;）
        /// </summary>
        public string? SpecsJson { get; set; }

        /// <summary>
        /// 分销商价格 JSON（序列化的 List&lt;DistributorPricing&gt;）
        /// </summary>
        public string? SellersJson { get; set; }

        /// <summary>
        /// 可替代料 JSON（序列化的 List&lt;AlternativeComponent&gt;）
        /// </summary>
        public string? AlternativesJson { get; set; }

        /// <summary>
        /// 应用场景 JSON（序列化的 List&lt;ApplicationScenario&gt;）
        /// </summary>
        public string? ApplicationsJson { get; set; }

        /// <summary>
        /// 历史价格趋势 JSON（序列化的 List&lt;PriceTrendPoint&gt;）
        /// </summary>
        public string? PriceTrendJson { get; set; }

        /// <summary>
        /// 相关新闻 JSON（序列化的 List&lt;ComponentNews&gt;）
        /// </summary>
        public string? NewsJson { get; set; }

        /// <summary>
        /// 数据来源（Mock / Nexar / DigiKey 等）
        /// </summary>
        [MaxLength(50)]
        public string DataSource { get; set; } = "Mock";

        /// <summary>
        /// 数据获取时间（UTC），用于判断是否超过24小时缓存期
        /// </summary>
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 查询次数统计
        /// </summary>
        public int QueryCount { get; set; } = 1;
    }
}
