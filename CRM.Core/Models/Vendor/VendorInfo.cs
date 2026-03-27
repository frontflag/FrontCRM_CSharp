using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商主表
    /// </summary>
    [Table("vendorinfo")]
    public class VendorInfo : BaseGuidEntity
    {
        /// <summary>
        /// 供应商ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("VendorId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 供应商编码
        /// </summary>
        [Required]
        [StringLength(16)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 公司全称
        /// </summary>
        [StringLength(64)]
        public string? OfficialName { get; set; }

        /// <summary>
        /// 公司简称
        /// </summary>
        [StringLength(64)]
        public string? NickName { get; set; }

        /// <summary>
        /// CRM系统供应商ID
        /// </summary>
        [StringLength(50)]
        public string? VendorIdCrm { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        public short? Level { get; set; }

        /// <summary>
        /// 规模
        /// </summary>
        public short? Scale { get; set; }

        /// <summary>
        /// 背景
        /// </summary>
        public short? Background { get; set; }

        /// <summary>
        /// 公司分类
        /// </summary>
        public short? CompanyClass { get; set; }

        /// <summary>
        /// 国家
        /// </summary>
        public short? Country { get; set; }

        /// <summary>
        /// 地域类型
        /// </summary>
        public short? LocationType { get; set; }

        /// <summary>
        /// 行业
        /// </summary>
        [StringLength(50)]
        public string? Industry { get; set; }

        /// <summary>
        /// 主营产品
        /// </summary>
        [StringLength(200)]
        public string? Product { get; set; }

        /// <summary>
        /// 办公地址
        /// </summary>
        [StringLength(200)]
        public string? OfficeAddress { get; set; }

        /// <summary>
        /// 交易币别
        /// </summary>
        public short? TradeCurrency { get; set; }

        /// <summary>
        /// 交易方式
        /// </summary>
        public short? TradeType { get; set; }

        /// <summary>
        /// 账期
        /// </summary>
        public short? Payment { get; set; }

        /// <summary>
        /// 外部系统编号
        /// </summary>
        [StringLength(50)]
        public string? ExternalNumber { get; set; }

        /// <summary>
        /// 信用评级
        /// </summary>
        public short? Credit { get; set; }

        /// <summary>
        /// 质量预判
        /// </summary>
        public short? QualityPrejudgement { get; set; }

        /// <summary>
        /// 可追溯性
        /// </summary>
        public short? Traceability { get; set; }

        /// <summary>
        /// 售后服务
        /// </summary>
        public short? AfterSalesService { get; set; }

        /// <summary>
        /// 适应程度
        /// </summary>
        public short? DegreeAdaptability { get; set; }

        /// <summary>
        /// ISCP标志
        /// </summary>
        public bool ISCPFlag { get; set; } = false;

        /// <summary>
        /// 战略属性
        /// </summary>
        public short? Strategy { get; set; }

        /// <summary>
        /// 自营
        /// </summary>
        public bool SelfSupport { get; set; } = false;

        /// <summary>
        /// 黑名单
        /// </summary>
        public bool BlackList { get; set; } = false;

        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool IsDisenable { get; set; } = false;

        /// <summary>
        /// 经度
        /// </summary>
        [Column(TypeName = "numeric(10,6)")]
        public decimal? Longitude { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        [Column(TypeName = "numeric(10,6)")]
        public decimal? Latitude { get; set; }

        /// <summary>
        /// 公司简介
        /// </summary>
        public string? CompanyInfo { get; set; }

        /// <summary>
        /// 其他备注（与公司简介区分）
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 官方网站
        /// </summary>
        [StringLength(300)]
        public string? Website { get; set; }

        /// <summary>
        /// 负责采购员（展示名，非用户表关联时可填）
        /// </summary>
        [StringLength(64)]
        public string? PurchaserName { get; set; }

        /// <summary>
        /// 付款方式（如 Prepaid、TT 等，与账期天数区分）
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// 上市代码
        /// </summary>
        [StringLength(50)]
        public string? ListingCode { get; set; }

        /// <summary>
        /// 经营范围
        /// </summary>
        [StringLength(200)]
        public string? VendorScope { get; set; }

        /// <summary>
        /// 是否管控
        /// </summary>
        public bool IsControl { get; set; } = false;

        /// <summary>
        /// 统一社会信用代码
        /// </summary>
        [StringLength(50)]
        public string? CreditCode { get; set; }

        /// <summary>
        /// 供应商归属 (1:专属 2:公海)
        /// </summary>
        public short AscriptionType { get; set; } = 1;

        /// <summary>
        /// 采购员ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseUserId { get; set; }

        /// <summary>
        /// 采购员组ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseGroupId { get; set; }

        /// <summary>
        /// 状态
        /// 1=新建 2=待审核 10=已审核 12=待财务审核 20=财务建档 -1=审核失败
        /// </summary>
        public short Status { get; set; } = 1;

        /// <summary>
        /// 审核拒绝原因（审核失败时由审批人填写）
        /// </summary>
        [StringLength(500)]
        public string? AuditRemark { get; set; }

        /// <summary>
        /// 是否已删除（软删除）
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime? DeleteTime { get; set; }

        /// <summary>
        /// 删除原因
        /// </summary>
        [StringLength(200)]
        public string? DeleteReason { get; set; }

        // 导航属性
        public virtual ICollection<VendorContactInfo> Contacts { get; set; } = new List<VendorContactInfo>();
        public virtual ICollection<VendorAddress> Addresses { get; set; } = new List<VendorAddress>();
        public virtual ICollection<VendorBankInfo> BankAccounts { get; set; } = new List<VendorBankInfo>();
    }
}
