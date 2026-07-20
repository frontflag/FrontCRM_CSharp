using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.AiAssistant;

[Table("ai_assistant_message")]
public class AiAssistantMessage : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [StringLength(36)]
    [Column("attachment_document_id")]
    public string? AttachmentDocumentId { get; set; }
}
