using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Work;

[Table("work_task")]
public class WorkTask : ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string CreateByUserId { get; set; } = string.Empty;

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }

    [Column("status")]
    public short Status { get; set; } = WorkTaskStatuses.Pending;

    [StringLength(32)]
    [Column("object_type")]
    public string ObjectType { get; set; } = WorkTaskObjectTypes.Customer;

    [StringLength(36)]
    [Column("object_id")]
    public string ObjectId { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Column("content")]
    public string? Content { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("priority")]
    public short Priority { get; set; } = WorkTaskPriorities.P2;

    [StringLength(36)]
    [Column("assignee_user_id")]
    public string AssigneeUserId { get; set; } = string.Empty;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [StringLength(36)]
    [Column("completed_by_user_id")]
    public string? CompletedByUserId { get; set; }

    [StringLength(36)]
    [Column("contact_history_id")]
    public string? ContactHistoryId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
