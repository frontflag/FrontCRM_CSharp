using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.AiAssistant;

[Table("user_feedback")]
public class UserFeedback : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("category")]
    public string Category { get; set; } = "other";

    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("summary")]
    public string Summary { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("biz_ref")]
    public string? BizRef { get; set; }

    [Column("repro_steps")]
    public string? ReproSteps { get; set; }

    [StringLength(500)]
    [Column("page_url")]
    public string? PageUrl { get; set; }

    [StringLength(100)]
    [Column("route_name")]
    public string? RouteName { get; set; }

    [Column("route_params_json")]
    public string? RouteParamsJson { get; set; }

    [Column("route_query_json")]
    public string? RouteQueryJson { get; set; }

    [StringLength(36)]
    [Column("submit_user_id")]
    public string SubmitUserId { get; set; } = string.Empty;

    [Column("needs_handling")]
    public bool NeedsHandling { get; set; } = true;

    [Column("is_handled")]
    public bool IsHandled { get; set; }

    [Column("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [StringLength(2000)]
    [Column("handle_remark")]
    public string? HandleRemark { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }
}
