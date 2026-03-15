using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 出库申请单
    /// </summary>
    [Table("stockoutrequest")]
    public class StockOutRequest : BaseGuidEntity
    {
        /// <summary>
        /// 申请单号
        /// </summary>
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        /// <summary>
        /// 销售订单ID
        /// </summary>
        [StringLength(36)]
        public string SalesOrderId { get; set; } = string.Empty;

        /// <summary>
        /// 客户ID
        /// </summary>
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 申请人ID
        /// </summary>
        [StringLength(36)]
        public string RequestUserId { get; set; } = string.Empty;

        /// <summary>
        /// 申请日期
        /// </summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// 状态 (0:待出库 1:已出库 2:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}
