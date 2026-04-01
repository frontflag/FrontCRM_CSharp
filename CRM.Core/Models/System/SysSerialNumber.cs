using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 系统流水号管理表
    /// 每个业务板块维护独立的流水号序列
    /// </summary>
    [Table("sys_serial_number")]
    public class SysSerialNumber
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 业务模块代码（唯一标识）
        /// 如：Customer, Vendor, Inquiry, Quotation, SalesOrder, PurchaseOrder,
        ///     StockIn, StockOut, Inventory, Receipt, Payment, InputInvoice, OutputInvoice
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        /// <summary>
        /// 业务模块名称（中文描述）
        /// </summary>
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// 编号前缀（2～16 字符，如 CUS、SO、PAY_DEL、FNP 等；与 sys_serial_number 配置一致）
        /// </summary>
        [Required]
        [StringLength(16)]
        public string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// 流水号位数（不足时前面补0）
        /// </summary>
        public int SequenceLength { get; set; } = 4;

        /// <summary>
        /// 当前最大流水号（每次使用后 +1）
        /// </summary>
        public int CurrentSequence { get; set; } = 0;

        /// <summary>
        /// 是否按年重置流水号
        /// </summary>
        public bool ResetByYear { get; set; } = false;

        /// <summary>
        /// 是否按月重置流水号
        /// </summary>
        public bool ResetByMonth { get; set; } = false;

        /// <summary>
        /// 最后重置年份
        /// </summary>
        public int? LastResetYear { get; set; }

        /// <summary>
        /// 最后重置月份
        /// </summary>
        public int? LastResetMonth { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(200)]
        public string? Remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}
