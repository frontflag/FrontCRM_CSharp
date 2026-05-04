using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>统一操作日志表 log_operation（只读列表映射，写入仍走 <see cref="ILogOperationAppendService"/> 等 SQL）。</summary>
[Table("log_operation")]
public class OperationLog
{
    [Key]
    [StringLength(36)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string BizType { get; set; } = string.Empty;

    [Required]
    public string RecordId { get; set; } = string.Empty;

    [StringLength(128)]
    public string? RecordCode { get; set; }

    [Required]
    [StringLength(100)]
    public string ActionType { get; set; } = string.Empty;

    public DateTime OperationTime { get; set; }

    public string? OperatorUserId { get; set; }

    [StringLength(100)]
    public string? OperatorUserName { get; set; }

    public string? Reason { get; set; }

    public string? ExtraInfo { get; set; }

    public string? SysRemark { get; set; }

    public string? OperationDesc { get; set; }
}
