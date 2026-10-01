using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

/// <summary>一次投票选中的选项（多选多行）。</summary>
[Table("bbs_poll_vote_item")]
public class BbsPollVoteItem
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("vote_id")]
    public string VoteId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("option_id")]
    public string OptionId { get; set; } = string.Empty;
}
