using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services.Implementations;

public sealed class MailboxSendService : IMailboxSendService
{
    private readonly ApplicationDbContext _db;
    private readonly IMailboxPasswordCipher _cipher;

    public MailboxSendService(ApplicationDbContext db, IMailboxPasswordCipher cipher)
    {
        _db = db;
        _cipher = cipher;
    }

    public async Task<MailboxSendReadyDto> GetSendReadyAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, cancellationToken);
        if (tenant == null || !tenant.Enabled)
            return Block(MailboxSendErrorCodes.SmtpDisabled);
        if (string.IsNullOrWhiteSpace(tenant.SmtpHost))
            return Block(MailboxSendErrorCodes.SmtpHostMissing);

        var box = await LoadDefaultAsync(userId, cancellationToken);
        if (box == null)
            return Block(MailboxSendErrorCodes.NoDefaultMailbox);
        if (box.Kind != UserMailboxKind.Platform
            || box.VerifyStatus != UserMailboxVerifyStatus.Ok
            || string.IsNullOrWhiteSpace(box.PasswordCipher))
            return Block(MailboxSendErrorCodes.DefaultNotVerified);

        return new MailboxSendReadyDto { Ready = true };
    }

    public async Task<(CompanySmtpEmailSettingsDto Tenant, UserMailbox Mailbox, string PlainPassword)> ResolveSenderAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, cancellationToken);
        if (tenant == null || !tenant.Enabled)
            throw new EmailSendException(MailboxSendErrorCodes.SmtpDisabled, "管理员未启用 SMTP 发信。");
        if (string.IsNullOrWhiteSpace(tenant.SmtpHost))
            throw new EmailSendException(MailboxSendErrorCodes.SmtpHostMissing, "管理员未配置 SMTP 服务器。");
        if (tenant.SmtpPort is < 1 or > 65535)
            throw new EmailSendException(MailboxSendErrorCodes.SmtpHostMissing, "SMTP 端口无效。");

        var box = await LoadDefaultAsync(userId, cancellationToken);
        if (box == null)
            throw new EmailSendException(MailboxSendErrorCodes.NoDefaultMailbox, MailboxSendErrorCodes.DefaultUserHint);
        if (box.Kind != UserMailboxKind.Platform
            || box.VerifyStatus != UserMailboxVerifyStatus.Ok
            || string.IsNullOrWhiteSpace(box.PasswordCipher))
            throw new EmailSendException(MailboxSendErrorCodes.DefaultNotVerified, MailboxSendErrorCodes.DefaultUserHint);

        try
        {
            var plain = _cipher.Decrypt(box.PasswordCipher, box.CryptoVersion);
            if (string.IsNullOrWhiteSpace(plain))
                throw new EmailSendException(MailboxSendErrorCodes.DefaultNotVerified, MailboxSendErrorCodes.DefaultUserHint);
            return (tenant, box, plain);
        }
        catch (EmailSendException)
        {
            throw;
        }
        catch
        {
            throw new EmailSendException(MailboxSendErrorCodes.DefaultNotVerified, MailboxSendErrorCodes.DefaultUserHint);
        }
    }

    public async Task SetDefaultSendAsync(string userId, string mailboxId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.UserMailboxes.FirstOrDefaultAsync(
            x => x.Id == mailboxId && x.UserId == userId && !x.IsDeleted, cancellationToken);
        if (entity == null)
            throw new InvalidOperationException("邮箱不存在");
        if (entity.Kind != UserMailboxKind.Platform || entity.VerifyStatus != UserMailboxVerifyStatus.Ok)
            throw new InvalidOperationException("仅「平台 + 验证成功」的邮箱可设为默认发信。");
        if (string.IsNullOrWhiteSpace(entity.PasswordCipher))
            throw new InvalidOperationException("请先设置邮箱密码并完成验证。");

        var others = await _db.UserMailboxes
            .Where(x => x.UserId == userId && !x.IsDeleted && x.IsDefaultSend && x.Id != entity.Id)
            .ToListAsync(cancellationToken);
        foreach (var o in others)
            o.IsDefaultSend = false;

        entity.IsDefaultSend = true;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TryAutoDefaultAfterVerifyOkAsync(UserMailbox entity, CancellationToken cancellationToken = default)
    {
        if (entity.Kind != UserMailboxKind.Platform || entity.VerifyStatus != UserMailboxVerifyStatus.Ok)
            return;
        if (string.IsNullOrWhiteSpace(entity.PasswordCipher))
            return;

        var hasDefault = await _db.UserMailboxes.AnyAsync(
            x => x.UserId == entity.UserId
                 && !x.IsDeleted
                 && x.IsDefaultSend
                 && x.Id != entity.Id, cancellationToken);
        if (hasDefault)
            return;

        // 本条已是默认则保持；否则设为默认并清掉异常残留
        var siblings = await _db.UserMailboxes
            .Where(x => x.UserId == entity.UserId && !x.IsDeleted && x.IsDefaultSend && x.Id != entity.Id)
            .ToListAsync(cancellationToken);
        foreach (var s in siblings)
            s.IsDefaultSend = false;

        entity.IsDefaultSend = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public void ClearDefaultSend(UserMailbox entity)
    {
        entity.IsDefaultSend = false;
    }

    private async Task<UserMailbox?> LoadDefaultAsync(string userId, CancellationToken cancellationToken)
    {
        return await _db.UserMailboxes.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && !x.IsDeleted && x.IsDefaultSend,
                cancellationToken);
    }

    private static MailboxSendReadyDto Block(string code) => new()
    {
        Ready = false,
        BlockReason = code
    };
}
