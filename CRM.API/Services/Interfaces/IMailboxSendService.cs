using CRM.API.Models.DTOs;
using CRM.Core.Models.System;

namespace CRM.API.Services.Interfaces;

public interface IMailboxSendService
{
    Task<MailboxSendReadyDto> GetSendReadyAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>解析发信用默认平台邮箱；不可用时抛 <see cref="EmailSendException"/>。</summary>
    Task<(CompanySmtpEmailSettingsDto Tenant, UserMailbox Mailbox, string PlainPassword)> ResolveSenderAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task SetDefaultSendAsync(string userId, string mailboxId, CancellationToken cancellationToken = default);

    /// <summary>验证成功且当前无默认时，将本条设为默认。</summary>
    Task TryAutoDefaultAfterVerifyOkAsync(UserMailbox entity, CancellationToken cancellationToken = default);

    void ClearDefaultSend(UserMailbox entity);
}
