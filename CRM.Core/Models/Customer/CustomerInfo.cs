using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.Core.Models.Customer
{
    /// <summary>
    /// 客户主表
    /// </summary>
    [Table("customerinfo")]
    public class CustomerInfo : BaseGuidEntity
    {
        /// <summary>
        /// 客户ID (主键)
        /// </summary>
        [StringLength(36)]
        [Column("CustomerId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 客户编码
        /// </summary>
        [Required]
        [StringLength(16)]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// 公司全称
        /// </summary>
        [StringLength(128)]
        public string? OfficialName { get; set; }

        /// <summary>
        /// 客户名称（OfficialName的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? CustomerName 
        { 
            get => OfficialName; 
            set => OfficialName = value; 
        }

        /// <summary>
        /// 公司标准全称
        /// </summary>
        [StringLength(128)]
        public string? StandardOfficialName { get; set; }

        /// <summary>
        /// 公司英文全称
        /// </summary>
        [StringLength(128)]
        public string? EnglishOfficialName { get; set; }

        /// <summary>
        /// 公司简称
        /// </summary>
        [StringLength(64)]
        public string? NickName { get; set; }

        /// <summary>
        /// 客户简称（NickName的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? CustomerShortName
        {
            get => NickName;
            set => NickName = value;
        }

        /// <summary>
        /// 客户等级 (1:D 2:C 3:B 4:BPO 5:VIP 6:VPO)
        /// </summary>
        public short Level { get; set; } = 1;

        /// <summary>
        /// 客户等级字符串（Level的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? CustomerLevel
        {
            get => Level switch
            {
                1 => "D",
                2 => "C",
                3 => "B",
                4 => "BPO",
                5 => "VIP",
                6 => "VPO",
                _ => "Normal"
            };
            set => Level = value?.ToUpper() switch
            {
                "D" => (short)1,
                "C" => (short)2,
                "B" => (short)3,
                "BPO" => (short)4,
                "VIP" => (short)5,
                "VPO" => (short)6,
                _ => (short)1
            };
        }

        /// <summary>
        /// 客户类型：1 OEM（历史）；2 ODM；3 终端；4 IDH；5 贸易商；6 代理商；7 EMS；8 非行业；9 科研机构；10 供应链；11 原厂
        /// </summary>
        public short? Type { get; set; }

        /// <summary>
        /// 客户类型（Type的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public short? CustomerType
        {
            get => Type;
            set => Type = value;
        }

        /// <summary>
        /// 规模
        /// </summary>
        public short? Scale { get; set; }

        /// <summary>
        /// 背景
        /// </summary>
        public short? Background { get; set; }

        /// <summary>
        /// 交易模式
        /// </summary>
        public short? DealMode { get; set; }

        /// <summary>
        /// 公司性质
        /// </summary>
        public short? CompanyNature { get; set; }

        /// <summary>
        /// 国家类型
        /// </summary>
        public short? Country { get; set; }

        /// <summary>
        /// 国家名称（用于前端兼容）
        /// </summary>
        [StringLength(50)]
        [NotMapped]
        public string? CountryName { get; set; }

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
        /// 主营产品2
        /// </summary>
        [StringLength(200)]
        public string? Product2 { get; set; }

        /// <summary>
        /// 应用领域
        /// </summary>
        [StringLength(200)]
        public string? Application { get; set; }

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
        /// 授信额度
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal CreditLine { get; set; } = 0.00m;

        /// <summary>
        /// 授信额度剩余
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal CreditLineRemain { get; set; } = 0.00m;

        /// <summary>
        /// 账户余额（CreditLineRemain的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public decimal Balance
        {
            get => CreditLineRemain;
            set => CreditLineRemain = value;
        }

        /// <summary>
        /// 地区（Province + City 的组合，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? Region
        {
            get => !string.IsNullOrEmpty(Province) && !string.IsNullOrEmpty(City) 
                ? $"{Province} {City}" 
                : Province ?? City;
        }

        /// <summary>
        /// 客户归属 (1:专属 2:公海)。由服务端维护，不对 API/前端 JSON 暴露。
        /// </summary>
        [JsonIgnore]
        public short AscriptionType { get; set; } = 1;

        /// <summary>
        /// 保护状态
        /// </summary>
        public bool ProtectStatus { get; set; } = false;

        /// <summary>
        /// 保护人ID
        /// </summary>
        [StringLength(36)]
        public string? ProtectFromUserId { get; set; }

        /// <summary>
        /// 保护时间
        /// </summary>
        public DateTime? ProtectTime { get; set; }

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
        /// 黑名单
        /// </summary>
        public bool BlackList { get; set; } = false;

        /// <summary>
        /// 禁用状态
        /// </summary>
        public bool DisenableStatus { get; set; } = false;

        /// <summary>
        /// 禁用类型
        /// </summary>
        public short? DisenableType { get; set; }

        /// <summary>
        /// 公海审核状态
        /// </summary>
        public short? CommonSeaAuditStatus { get; set; }

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
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 邓白氏码
        /// </summary>
        [StringLength(20)]
        public string? DUNS { get; set; }

        /// <summary>
        /// 是否管控
        /// </summary>
        public bool IsControl { get; set; } = false;

        /// <summary>
        /// 统一社会信用代码（含带分隔符录入时可超过 18 位；与 vendorinfo.CreditCode 对齐为 50）
        /// </summary>
        [StringLength(50)]
        public string? CreditCode { get; set; }

        /// <summary>
        /// 统一社会信用代码（CreditCode的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? UnifiedSocialCreditCode
        {
            get => CreditCode;
            set => CreditCode = value;
        }

        /// <summary>
        /// 客户身份类型
        /// </summary>
        public short? IdentityType { get; set; }

        /// <summary>
        /// 业务员ID
        /// </summary>
        [StringLength(36)]
        public string? SalesUserId { get; set; }

        /// <summary>
        /// 业务员ID（SalesUserId的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? SalesPersonId
        {
            get => SalesUserId;
            set => SalesUserId = value;
        }

        /// <summary>
        /// 业务员名称（用于前端展示）
        /// </summary>
        [NotMapped]
        public string? SalesPersonName { get; set; }

        /// <summary>
        /// 省/州
        /// </summary>
        [StringLength(50)]
        public string? Province { get; set; }

        /// <summary>
        /// 市
        /// </summary>
        [StringLength(50)]
        public string? City { get; set; }

        /// <summary>
        /// 区/县
        /// </summary>
        [StringLength(50)]
        public string? District { get; set; }

        /// <summary>
        /// 详细地址
        /// </summary>
        [StringLength(200)]
        [NotMapped]
        public string? Address { get; set; }

        /// <summary>
        /// 信用额度（CreditLine的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public decimal CreditLimit
        {
            get => CreditLine;
            set => CreditLine = value;
        }

        /// <summary>
        /// 账期天数（Payment的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public short PaymentTerms
        {
            get => Payment ?? 30;
            set => Payment = value;
        }

        /// <summary>
        /// 结算货币（TradeCurrency的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public short Currency
        {
            get => TradeCurrency ?? 1;
            set => TradeCurrency = value;
        }

        /// <summary>
        /// 税率
        /// </summary>
        [NotMapped]
        public decimal TaxRate { get; set; } = 13;

        /// <summary>
        /// 发票类型
        /// </summary>
        [NotMapped]
        public short InvoiceType { get; set; } = 2;

        /// <summary>
        /// 是否启用（Status的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public bool IsActive
        {
            // 历史上 IsActive 表示“启用”，在新状态体系下等价于“已审核及以后”
            get => Status >= 10;
            set => Status = value ? (short)10 : (short)1;
        }

        /// <summary>
        /// 备注（Remark的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? Remarks
        {
            get => Remark;
            set => Remark = value;
        }

        // ===== 软删除字段 =====

        /// <summary>
        /// 是否已删除（软删除标志）
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// 删除操作人ID
        /// </summary>
        [StringLength(36)]
        public string? DeletedByUserId { get; set; }

        /// <summary>
        /// 删除操作人姓名
        /// </summary>
        [StringLength(64)]
        public string? DeletedByUserName { get; set; }

        /// <summary>
        /// 删除理由
        /// </summary>
        [StringLength(500)]
        public string? DeleteReason { get; set; }

        /// <summary>
        /// 黑名单理由
        /// </summary>
        [StringLength(500)]
        public string? BlackListReason { get; set; }

        /// <summary>
        /// 加入黑名单时间
        /// </summary>
        public DateTime? BlackListAt { get; set; }

        /// <summary>
        /// 加入黑名单操作人ID
        /// </summary>
        [StringLength(36)]
        public string? BlackListByUserId { get; set; }

        /// <summary>
        /// 加入黑名单操作人姓名
        /// </summary>
        [StringLength(64)]
        public string? BlackListByUserName { get; set; }

        /// <summary>创建客户时的登录用户 ID（GUID）</summary>
        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        /// <summary>最后修改时的登录用户 ID（GUID）</summary>
        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        // 导航属性
        public virtual ICollection<CustomerContactInfo> Contacts { get; set; } = new List<CustomerContactInfo>();
        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
        public virtual ICollection<CustomerBankInfo> BankAccounts { get; set; } = new List<CustomerBankInfo>();
    }
}
