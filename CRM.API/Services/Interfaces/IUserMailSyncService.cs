namespace CRM.API.Services.Interfaces;

public interface IUserMailSyncService
{
    /// <summary>同步指定用户已验证 IMAP 邮箱；mailboxId 为空表示全部。</summary>
    Task<UserMailSyncResultDto> SyncUserAsync(
        string userId,
        string? mailboxId = null,
        CancellationToken cancellationToken = default);

    /// <summary>每日任务：同步所有拥有已验证 IMAP 邮箱的用户。</summary>
    Task<UserMailDailySyncResultDto> SyncAllUsersAsync(CancellationToken cancellationToken = default);
}

public sealed class UserMailSyncResultDto
{
    public int MailboxCount { get; set; }
    public int FetchedCount { get; set; }
    public int UpsertedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class UserMailDailySyncResultDto
{
    public int UserCount { get; set; }
    public int OkCount { get; set; }
    public int FailCount { get; set; }
}
