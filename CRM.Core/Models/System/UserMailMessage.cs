using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>用户邮箱同步落库的邮件（一期 INBOX）。</summary>
[Table("user_mail_message")]
public class UserMailMessage
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("mailbox_id")]
    public string MailboxId { get; set; } = string.Empty;

    [Column("imap_uid")]
    public long ImapUid { get; set; }

    [Required]
    [StringLength(128)]
    [Column("folder")]
    public string Folder { get; set; } = "INBOX";

    [StringLength(998)]
    [Column("message_id")]
    public string? MessageId { get; set; }

    [StringLength(1000)]
    [Column("subject")]
    public string? Subject { get; set; }

    [StringLength(512)]
    [Column("from_address")]
    public string? FromAddress { get; set; }

    [StringLength(256)]
    [Column("from_name")]
    public string? FromName { get; set; }

    [Column("to_addresses")]
    public string? ToAddresses { get; set; }

    [Column("received_at")]
    public DateTime? ReceivedAt { get; set; }

    /// <summary>本地未读；查看后可标为已读，不写回服务器。</summary>
    [Column("is_unread")]
    public bool IsUnread { get; set; } = true;

    /// <summary>本地星标；同步不覆盖，不写回 IMAP。</summary>
    [Column("is_starred")]
    public bool IsStarred { get; set; }

    /// <summary>本地备注；同步不覆盖，不写回 IMAP。</summary>
    [StringLength(2000)]
    [Column("remark")]
    public string? Remark { get; set; }

    [StringLength(500)]
    [Column("snippet")]
    public string? Snippet { get; set; }

    [Column("body_text")]
    public string? BodyText { get; set; }

    [Column("body_html")]
    public string? BodyHtml { get; set; }

    [Column("has_attachments")]
    public bool HasAttachments { get; set; }

    [Column("size_bytes")]
    public int SizeBytes { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }
}

/// <summary>每邮箱同步状态。</summary>
[Table("user_mailbox_sync_state")]
public class UserMailboxSyncState
{
    [Key]
    [StringLength(36)]
    [Column("mailbox_id")]
    public string MailboxId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("last_sync_at")]
    public DateTime? LastSyncAt { get; set; }

    [Column("last_success_at")]
    public DateTime? LastSuccessAt { get; set; }

    [StringLength(2000)]
    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("last_uid_validity")]
    public uint? LastUidValidity { get; set; }
}

/// <summary>每日自动同步跑批标记（多实例防重）。</summary>
[Table("mail_sync_daily_run")]
public class MailSyncDailyRun
{
    /// <summary>Asia/Shanghai 日历日，格式 yyyy-MM-dd。</summary>
    [Key]
    [StringLength(10)]
    [Column("run_date")]
    public string RunDate { get; set; } = string.Empty;

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("ok_count")]
    public int OkCount { get; set; }

    [Column("fail_count")]
    public int FailCount { get; set; }
}
