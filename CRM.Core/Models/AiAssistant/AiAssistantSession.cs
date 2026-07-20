using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.AiAssistant;

[Table("ai_assistant_session")]
public class AiAssistantSession : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [StringLength(32)]
    [Column("active_skill")]
    public string ActiveSkill { get; set; } = "feedback";

    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "open";

    [StringLength(20)]
    [Column("preferred_category")]
    public string? PreferredCategory { get; set; }

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

    [StringLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("consecutive_off_topic_count")]
    public int ConsecutiveOffTopicCount { get; set; }

    [Column("user_turn_count")]
    public int UserTurnCount { get; set; }

    [StringLength(200)]
    [Column("inferred_biz_ref")]
    public string? InferredBizRef { get; set; }
}
