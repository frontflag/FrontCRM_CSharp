using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>用户个人邮箱（平台/其他）；密码对称加密存库。</summary>
[Table("user_mailbox")]
public class UserMailbox
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>0=platform，1=personal</summary>
    [Column("kind")]
    public short Kind { get; set; }

    [Required]
    [StringLength(256)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [StringLength(128)]
    [Column("local_part")]
    public string? LocalPart { get; set; }

    [StringLength(200)]
    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("password_cipher")]
    public string? PasswordCipher { get; set; }

    [Column("crypto_version")]
    public short CryptoVersion { get; set; } = 1;

    [StringLength(256)]
    [Column("pop_host")]
    public string? PopHost { get; set; }

    [Column("pop_port")]
    public int? PopPort { get; set; }

    [Column("pop_use_ssl")]
    public bool PopUseSsl { get; set; } = true;

    [StringLength(256)]
    [Column("imap_host")]
    public string? ImapHost { get; set; }

    [Column("imap_port")]
    public int? ImapPort { get; set; }

    [Column("imap_use_ssl")]
    public bool ImapUseSsl { get; set; } = true;

    /// <summary>0=未验证，1=成功，2=失败</summary>
    [Column("verify_status")]
    public short VerifyStatus { get; set; }

    [StringLength(1000)]
    [Column("verify_message")]
    public string? VerifyMessage { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    /// <summary>默认发信邮箱；同一用户未删除行中至多一条为 true。</summary>
    [Column("is_default_send")]
    public bool IsDefaultSend { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }
}

public static class UserMailboxKind
{
    public const short Platform = 0;
    public const short Personal = 1;
}

public static class UserMailboxVerifyStatus
{
    public const short None = 0;
    public const short Ok = 1;
    public const short Fail = 2;
}
