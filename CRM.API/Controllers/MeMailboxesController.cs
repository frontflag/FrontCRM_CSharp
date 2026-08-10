using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me/mailboxes")]
public class MeMailboxesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMailboxPasswordCipher _cipher;
    private readonly IMailboxVerifyService _verify;
    private readonly IMailboxSendService _send;
    private readonly ILogger<MeMailboxesController> _logger;

    public MeMailboxesController(
        ApplicationDbContext db,
        IMailboxPasswordCipher cipher,
        IMailboxVerifyService verify,
        IMailboxSendService send,
        ILogger<MeMailboxesController> logger)
    {
        _db = db;
        _cipher = cipher;
        _verify = verify;
        _send = send;
        _logger = logger;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserMailboxDto>>>> List(CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<List<UserMailboxDto>>.Fail("未登录", 401));

        await EnsurePlatformDraftAsync(uid, ct);

        var rows = await _db.UserMailboxes.AsNoTracking()
            .Where(x => x.UserId == uid && !x.IsDeleted)
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.CreateTime)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<UserMailboxDto>>.Ok(rows.Select(ToDto).ToList()));
    }

    [HttpGet("send-ready")]
    public async Task<ActionResult<ApiResponse<MailboxSendReadyDto>>> SendReady(CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<MailboxSendReadyDto>.Fail("未登录", 401));

        var dto = await _send.GetSendReadyAsync(uid, ct);
        return Ok(ApiResponse<MailboxSendReadyDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserMailboxDto>>> Create(
        [FromBody] UserMailboxWriteRequest? body,
        CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<UserMailboxDto>.Fail("未登录", 401));
        body ??= new UserMailboxWriteRequest();

        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, ct)
                     ?? new CompanySmtpEmailSettingsDto();
        var err = await ApplyWriteAsync(null, uid, body, tenant, ct);
        if (err != null)
            return BadRequest(ApiResponse<UserMailboxDto>.Fail(err, 400));

        var entity = await BuildNewEntityAsync(uid, body, tenant, ct);
        _db.UserMailboxes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<UserMailboxDto>.Ok(ToDto(entity), "已创建"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserMailboxDto>>> Update(
        string id,
        [FromBody] UserMailboxWriteRequest? body,
        CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<UserMailboxDto>.Fail("未登录", 401));
        body ??= new UserMailboxWriteRequest();

        var entity = await _db.UserMailboxes.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<UserMailboxDto>.Fail("邮箱不存在", 404));

        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, ct)
                     ?? new CompanySmtpEmailSettingsDto();
        var err = await ApplyWriteAsync(entity, uid, body, tenant, ct);
        if (err != null)
            return BadRequest(ApiResponse<UserMailboxDto>.Fail(err, 400));

        await MapOntoEntityAsync(entity, body, tenant, resetVerifyOnSensitive: true, ct);
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyByUserId = uid;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<UserMailboxDto>.Ok(ToDto(entity), "已保存"));
    }

    [HttpPut("{id}/default-send")]
    public async Task<ActionResult<ApiResponse<UserMailboxDto>>> SetDefaultSend(string id, CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<UserMailboxDto>.Fail("未登录", 401));

        try
        {
            await _send.SetDefaultSendAsync(uid, id, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserMailboxDto>.Fail(ex.Message, 400));
        }

        var entity = await _db.UserMailboxes.AsNoTracking()
            .FirstAsync(x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        return Ok(ApiResponse<UserMailboxDto>.Ok(ToDto(entity), "已设为默认发信邮箱"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailboxes.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮箱不存在", 404));

        entity.IsDeleted = true;
        entity.IsDefaultSend = false;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyByUserId = uid;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "已删除"));
    }

    [HttpGet("{id}/password")]
    public async Task<ActionResult<ApiResponse<MailboxPasswordRevealDto>>> RevealPassword(
        string id,
        CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<MailboxPasswordRevealDto>.Fail("未登录", 401));

        var entity = await _db.UserMailboxes.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<MailboxPasswordRevealDto>.Fail("邮箱不存在", 404));
        if (string.IsNullOrWhiteSpace(entity.PasswordCipher))
            return Ok(ApiResponse<MailboxPasswordRevealDto>.Ok(new MailboxPasswordRevealDto { Password = "" }));

        try
        {
            var plain = _cipher.Decrypt(entity.PasswordCipher, entity.CryptoVersion);
            return Ok(ApiResponse<MailboxPasswordRevealDto>.Ok(new MailboxPasswordRevealDto { Password = plain }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "揭示本人邮箱密码失败 id={Id}", id);
            return StatusCode(500, ApiResponse<MailboxPasswordRevealDto>.Fail("解密失败", 500));
        }
    }

    [HttpPost("{id}/verify")]
    public async Task<ActionResult<ApiResponse<MailboxVerifyResponseDto>>> Verify(string id, CancellationToken ct)
    {
        var uid = UserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<MailboxVerifyResponseDto>.Fail("未登录", 401));

        var entity = await _db.UserMailboxes.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<MailboxVerifyResponseDto>.Fail("邮箱不存在", 404));

        entity.VerifyMessage = null;
        var result = await _verify.VerifyAsync(id, ct);
        entity.VerifyStatus = result.Success ? UserMailboxVerifyStatus.Ok : UserMailboxVerifyStatus.Fail;
        entity.VerifyMessage = result.Message;
        entity.VerifiedAt = result.Success ? DateTime.UtcNow : null;
        if (!result.Success)
            _send.ClearDefaultSend(entity);
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyByUserId = uid;
        await _db.SaveChangesAsync(ct);

        if (result.Success)
            await _send.TryAutoDefaultAfterVerifyOkAsync(entity, ct);

        entity = await _db.UserMailboxes.AsNoTracking()
            .FirstAsync(x => x.Id == id, ct);
        var payload = new MailboxVerifyResponseDto
        {
            Mailbox = ToDto(entity),
            Success = result.Success,
            PopOk = result.PopOk,
            PopMessage = result.PopMessage,
            SmtpOk = result.SmtpOk,
            SmtpMessage = result.SmtpMessage
        };
        return Ok(ApiResponse<MailboxVerifyResponseDto>.Ok(payload, result.Message));
    }

    private async Task EnsurePlatformDraftAsync(string userId, CancellationToken ct)
    {
        var hasPlatform = await _db.UserMailboxes.AnyAsync(
            x => x.UserId == userId && !x.IsDeleted && x.Kind == UserMailboxKind.Platform, ct);
        if (hasPlatform) return;

        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, ct);
        var suffix = MailboxAddressHelper.NormalizeSuffix(tenant?.PlatformEmailSuffix);
        if (suffix == null) return;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var local = MailboxAddressHelper.ExtractLocalPart(user?.Email);
        var address = MailboxAddressHelper.BuildPlatformAddress(local, suffix);

        _db.UserMailboxes.Add(new UserMailbox
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Kind = UserMailboxKind.Platform,
            LocalPart = local,
            Address = address,
            DisplayName = null,
            VerifyStatus = UserMailboxVerifyStatus.None,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = userId
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string?> ApplyWriteAsync(
        UserMailbox? existing,
        string userId,
        UserMailboxWriteRequest body,
        CompanySmtpEmailSettingsDto tenant,
        CancellationToken ct)
    {
        var kind = ParseKind(body.Kind);
        if (kind == null) return "邮箱类型无效（platform / personal）";

        string address;
        string? localPart = null;
        if (kind == UserMailboxKind.Platform)
        {
            var suffix = MailboxAddressHelper.NormalizeSuffix(tenant.PlatformEmailSuffix);
            if (suffix == null) return "管理员尚未配置租户平台邮箱后缀";
            localPart = string.IsNullOrWhiteSpace(body.LocalPart)
                ? MailboxAddressHelper.ExtractLocalPart(body.Address)
                : body.LocalPart.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(localPart)) return "请填写邮箱本地部分";
            address = MailboxAddressHelper.BuildPlatformAddress(localPart, suffix);
        }
        else
        {
            address = (body.Address ?? string.Empty).Trim();
            if (!MailboxAddressHelper.IsValidEmail(address)) return "请填写有效的邮箱地址";
            if (string.IsNullOrWhiteSpace(body.PopHost) && existing == null)
                return "其他邮箱须填写 POP 服务器";
            if (string.IsNullOrWhiteSpace(body.PopHost) && existing != null && string.IsNullOrWhiteSpace(existing.PopHost))
                return "其他邮箱须填写 POP 服务器";
        }

        var dup = await _db.UserMailboxes.AnyAsync(
            x => x.UserId == userId
                 && !x.IsDeleted
                 && x.Address.ToLower() == address.ToLower()
                 && (existing == null || x.Id != existing.Id),
            ct);
        if (dup) return "该邮箱地址已存在";

        if (existing == null && string.IsNullOrWhiteSpace(body.Password))
            return "请填写邮箱密码";

        return null;
    }

    private async Task<UserMailbox> BuildNewEntityAsync(
        string userId,
        UserMailboxWriteRequest body,
        CompanySmtpEmailSettingsDto tenant,
        CancellationToken ct)
    {
        var entity = new UserMailbox
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = userId
        };
        await MapOntoEntityAsync(entity, body, tenant, resetVerifyOnSensitive: false, ct);
        return entity;
    }

    private Task MapOntoEntityAsync(
        UserMailbox entity,
        UserMailboxWriteRequest body,
        CompanySmtpEmailSettingsDto tenant,
        bool resetVerifyOnSensitive,
        CancellationToken ct)
    {
        var kind = ParseKind(body.Kind) ?? entity.Kind;
        var prevAddress = entity.Address;
        var prevPopHost = entity.PopHost;
        var prevPopPort = entity.PopPort;
        var prevPopSsl = entity.PopUseSsl;
        var passwordChanged = !string.IsNullOrWhiteSpace(body.Password);

        entity.Kind = kind;
        if (kind == UserMailboxKind.Platform)
        {
            var suffix = MailboxAddressHelper.NormalizeSuffix(tenant.PlatformEmailSuffix)!;
            var local = string.IsNullOrWhiteSpace(body.LocalPart)
                ? MailboxAddressHelper.ExtractLocalPart(body.Address ?? entity.LocalPart)
                : body.LocalPart!.Trim().TrimStart('@');
            entity.LocalPart = local;
            entity.Address = MailboxAddressHelper.BuildPlatformAddress(local, suffix);
            entity.PopHost = null;
            entity.PopPort = null;
            entity.PopUseSsl = true;
        }
        else
        {
            entity.LocalPart = null;
            entity.Address = (body.Address ?? entity.Address).Trim();
            if (!string.IsNullOrWhiteSpace(body.PopHost))
                entity.PopHost = body.PopHost.Trim();
            if (body.PopPort.HasValue)
                entity.PopPort = body.PopPort;
            else if (entity.PopPort == null)
                entity.PopPort = 995;
            if (body.PopUseSsl.HasValue)
                entity.PopUseSsl = body.PopUseSsl.Value;
            entity.IsDefaultSend = false;
        }

        if (body.DisplayName != null)
            entity.DisplayName = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim();

        if (passwordChanged)
        {
            entity.PasswordCipher = _cipher.Encrypt(body.Password!.Trim());
            entity.CryptoVersion = _cipher.CurrentVersion;
        }

        if (resetVerifyOnSensitive)
        {
            var sensitive =
                passwordChanged
                || !string.Equals(prevAddress, entity.Address, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(prevPopHost ?? "", entity.PopHost ?? "", StringComparison.OrdinalIgnoreCase)
                || prevPopPort != entity.PopPort
                || prevPopSsl != entity.PopUseSsl;
            if (sensitive)
            {
                entity.VerifyStatus = UserMailboxVerifyStatus.None;
                entity.VerifyMessage = null;
                entity.VerifiedAt = null;
                entity.IsDefaultSend = false;
            }
        }

        return Task.CompletedTask;
    }

    private static short? ParseKind(string? kind)
    {
        var k = (kind ?? "").Trim().ToLowerInvariant();
        if (k is "platform" or "0") return UserMailboxKind.Platform;
        if (k is "personal" or "other" or "1") return UserMailboxKind.Personal;
        return null;
    }

    private static UserMailboxDto ToDto(UserMailbox x) => new()
    {
        Id = x.Id,
        Kind = x.Kind == UserMailboxKind.Personal ? "personal" : "platform",
        Address = x.Address,
        LocalPart = x.LocalPart,
        DisplayName = x.DisplayName,
        PasswordSet = !string.IsNullOrWhiteSpace(x.PasswordCipher),
        IsDefaultSend = x.IsDefaultSend,
        PopHost = x.PopHost,
        PopPort = x.PopPort,
        PopUseSsl = x.PopUseSsl,
        VerifyStatus = x.VerifyStatus switch
        {
            UserMailboxVerifyStatus.Ok => "ok",
            UserMailboxVerifyStatus.Fail => "fail",
            _ => "none"
        },
        VerifyMessage = x.VerifyMessage,
        VerifiedAt = x.VerifiedAt
    };
}
