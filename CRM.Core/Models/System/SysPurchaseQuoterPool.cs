using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>
/// RFQ 轮询报价员池（仅持久化已勾选的采购员）。
/// </summary>
[Table("sys_purchase_quoter_pool")]
public class SysPurchaseQuoterPool
{
    [Key]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("update_time")]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
}
