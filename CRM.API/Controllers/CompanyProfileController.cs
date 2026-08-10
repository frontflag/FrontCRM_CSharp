using CRM.API.Authorization;
using CRM.API.Constants;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/company-profile")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentService _documentService;
        private readonly IFileStorageService _fileStorage;
        private readonly IRbacService _rbacService;
        private readonly IMailboxPasswordCipher _mailboxCipher;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly ILogger<CompanyProfileController> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public CompanyProfileController(
            ApplicationDbContext db,
            IDocumentService documentService,
            IFileStorageService fileStorage,
            IRbacService rbacService,
            IMailboxPasswordCipher mailboxCipher,
            ILogOperationAppendService logOperationAppend,
            ILogger<CompanyProfileController> logger)
        {
            _db = db;
            _documentService = documentService;
            _fileStorage = fileStorage;
            _rbacService = rbacService;
            _mailboxCipher = mailboxCipher;
            _logOperationAppend = logOperationAppend;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private async Task<bool> IsSuperAdminAsync(CancellationToken ct)
        {
            var uid = CurrentUserId;
            if (string.IsNullOrWhiteSpace(uid)) return false;
            var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
            return summary?.IsSysAdmin == true;
        }

        [HttpGet]
        [RequireAnyPermission("system.params.company.read", "system.params.company.write")]
        public async Task<ActionResult<ApiResponse<CompanyProfileBundleDto>>> Get(CancellationToken ct)
        {
            try
            {
                var dto = await LoadBundleAsync(ct);
                if (await IsSuperAdminAsync(ct))
                    MaskSmtpForAdminResponse(dto);
                else
                    CompanyProfileBundleLoader.StripSmtpEmail(dto);
                return Ok(ApiResponse<CompanyProfileBundleDto>.Ok(dto, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取公司信息参数失败");
                return StatusCode(500, ApiResponse<CompanyProfileBundleDto>.Fail("读取失败", 500));
            }
        }

        /// <summary>采购/销售订单报表等场景：仅需读取公司参数，不要求「参数管理」权限。</summary>
        [HttpGet("report-bundle")]
        [RequireAnyPermission("purchase-order.read", "sales-order.read", "vendor.read", "finance-payment.read", "finance-payment.write")]
        public async Task<ActionResult<ApiResponse<CompanyProfileBundleDto>>> GetReportBundle(CancellationToken ct)
        {
            try
            {
                var dto = await LoadBundleAsync(ct);
                CompanyProfileBundleLoader.StripSmtpEmail(dto);
                return Ok(ApiResponse<CompanyProfileBundleDto>.Ok(dto, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取公司信息（报表）失败");
                return StatusCode(500, ApiResponse<CompanyProfileBundleDto>.Fail("读取失败", 500));
            }
        }

        /// <summary>登录页品牌图：与「公司信息」中公司 Logo 的选取规则一致（默认且已上传；否则任一有文件），无需登录。</summary>
        [HttpGet("login-logo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLoginLogo(CancellationToken ct)
        {
            try
            {
                var bundle = await LoadBundleAsync(ct);
                var docId = PickLoginLogoDocumentId(bundle.Logos);
                if (string.IsNullOrWhiteSpace(docId))
                    return NotFound();

                var doc = await _documentService.GetByIdAsync(docId.Trim());
                if (doc == null || doc.IsDeleted)
                    return NotFound();

                try
                {
                    var stream = await _fileStorage.OpenReadAsync(doc.RelativePath);
                    Response.Headers.CacheControl = "public, max-age=600";
                    return File(stream, doc.MimeType ?? "application/octet-stream");
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogWarning(ex, "登录页公司 Logo：物理文件缺失 DocumentId={DocumentId}", docId);
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取登录页公司 Logo 失败");
                return StatusCode(500);
            }
        }

        private static string? PickLoginLogoDocumentId(IReadOnlyList<CompanyLogoRowDto>? logos)
        {
            if (logos == null || logos.Count == 0)
                return null;

            static bool HasDoc(CompanyLogoRowDto r) => !string.IsNullOrWhiteSpace(r.DocumentId);

            var defWithDoc = logos.FirstOrDefault(r => r.IsDefault && HasDoc(r));
            if (defWithDoc != null)
                return defWithDoc.DocumentId!.Trim();

            var anyWithDoc = logos.FirstOrDefault(r => HasDoc(r));
            return string.IsNullOrWhiteSpace(anyWithDoc?.DocumentId) ? null : anyWithDoc.DocumentId.Trim();
        }

        /// <summary>删除公司银行账户前检查：是否已被付款单引用。</summary>
        [HttpGet("bank/{bankId}/can-delete")]
        [RequirePermission("system.params.company.write")]
        public async Task<ActionResult<ApiResponse<CompanyBankDeleteCheckDto>>> CanDeleteBank(string bankId, CancellationToken ct)
        {
            var id = (bankId ?? string.Empty).Trim();
            if (id.Length == 0)
                return BadRequest(ApiResponse<CompanyBankDeleteCheckDto>.Fail("银行ID无效", 400));

            try
            {
                var existsInDb = await _db.CompanyBankInfos.AsNoTracking().AnyAsync(b => b.Id == id, ct);
                if (!existsInDb)
                    return Ok(ApiResponse<CompanyBankDeleteCheckDto>.Ok(new CompanyBankDeleteCheckDto { CanDelete = true }, "ok"));

                var hasPayments = await CompanyBankInfoStore.HasPaymentRecordsAsync(_db, id, ct);
                return Ok(ApiResponse<CompanyBankDeleteCheckDto>.Ok(
                    new CompanyBankDeleteCheckDto { CanDelete = !hasPayments },
                    "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查公司银行账户是否可删除失败 BankId={BankId}", id);
                return StatusCode(500, ApiResponse<CompanyBankDeleteCheckDto>.Fail("检查失败", 500));
            }
        }

        [HttpPut]
        [RequirePermission("system.params.company.write")]
        public async Task<ActionResult<ApiResponse<object>>> Put([FromBody] CompanyProfileBundleDto body, CancellationToken ct)
        {
            if (body == null)
                return BadRequest(ApiResponse<object>.Fail("请求体为空", 400));

            body.Logos ??= new List<CompanyLogoRowDto>();

            try
            {
                var isSa = await IsSuperAdminAsync(ct);
                var err = ValidateDefaults(body.BasicInfos, "公司基础信息")
                    ?? ValidateBasicCurrencyDefaults(body.BasicInfos)
                    ?? ValidateBankDefaults(body.BankInfos)
                    ?? ValidateDefaults(body.Logos, "公司Logo")
                    ?? ValidateDefaults(body.Seals, "公司印章")
                    ?? ValidateSealCurrencyDefaults(body.Seals)
                    ?? ValidateDefaults(body.Warehouses, "公司仓库信息")
                    ?? (isSa ? ValidateSmtp(body.SmtpEmail) : null);
                if (err != null)
                    return BadRequest(ApiResponse<object>.Fail(err, 400));

                var bankDeleteErr = await CompanyBankInfoStore.ValidateBankDeletionsAsync(_db, body.BankInfos, ct);
                if (bankDeleteErr != null)
                    return BadRequest(ApiResponse<object>.Fail(bankDeleteErr, 400));

                await UpsertJsonAsync(CompanyProfileParamCodes.BasicInfos, "公司基础信息（多组）", body.BasicInfos, ct);
                await CompanyBankInfoStore.UpsertAllAsync(_db, body.BankInfos, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Logos, "公司Logo（多组）", body.Logos, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Seals, "公司印章（多组）", body.Seals, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Warehouses, "公司仓库信息（多组）", body.Warehouses, ct);
                if (isSa)
                    await UpsertSmtpEmailAsync(body.SmtpEmail, ct);
                await UpsertReportInfoAsync(body.ReportInfo, ct);
                await _db.SaveChangesAsync(ct);
                return Ok(ApiResponse<object>.Ok(null, "保存成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存公司信息参数失败");
                return StatusCode(500, ApiResponse<object>.Fail("保存失败", 500));
            }
        }

        /// <summary>已验证成功的用户邮箱列表（仅 SuperAdmin）。</summary>
        [HttpGet("verified-user-mailboxes")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<VerifiedUserMailboxRowDto>>>> ListVerifiedMailboxes(
            CancellationToken ct)
        {
            if (!await IsSuperAdminAsync(ct))
                return StatusCode(403, ApiResponse<List<VerifiedUserMailboxRowDto>>.Fail("仅 SuperAdmin 可查看", 403));

            var q =
                from m in _db.UserMailboxes.AsNoTracking()
                join u in _db.Users.AsNoTracking() on m.UserId equals u.Id
                where !m.IsDeleted && m.VerifyStatus == UserMailboxVerifyStatus.Ok
                orderby u.UserName, m.Address
                select new VerifiedUserMailboxRowDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = u.UserName,
                    RealName = u.RealName,
                    Kind = m.Kind == UserMailboxKind.Personal ? "personal" : "platform",
                    Address = m.Address,
                    DisplayName = m.DisplayName,
                    PasswordSet = m.PasswordCipher != null && m.PasswordCipher != "",
                    VerifiedAt = m.VerifiedAt
                };

            var list = await q.ToListAsync(ct);
            return Ok(ApiResponse<List<VerifiedUserMailboxRowDto>>.Ok(list));
        }

        /// <summary>揭示已验证邮箱明文密码（仅 SuperAdmin；记审计）。</summary>
        [HttpGet("verified-user-mailboxes/{id}/password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<MailboxPasswordRevealDto>>> RevealVerifiedMailboxPassword(
            string id,
            CancellationToken ct)
        {
            if (!await IsSuperAdminAsync(ct))
                return StatusCode(403, ApiResponse<MailboxPasswordRevealDto>.Fail("仅 SuperAdmin 可查看", 403));

            var m = await _db.UserMailboxes.AsNoTracking().FirstOrDefaultAsync(
                x => x.Id == id && !x.IsDeleted && x.VerifyStatus == UserMailboxVerifyStatus.Ok, ct);
            if (m == null)
                return NotFound(ApiResponse<MailboxPasswordRevealDto>.Fail("邮箱不存在或未验证成功", 404));

            var actorId = CurrentUserId;
            var actorName = User.Identity?.Name;
            try
            {
                await _logOperationAppend.AppendAsync(
                    "user_mailbox",
                    m.Id,
                    m.Address,
                    "reveal_password",
                    actorId,
                    actorName,
                    "SuperAdmin 查看用户邮箱明文密码",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "写邮箱密码揭示审计失败");
            }

            if (string.IsNullOrWhiteSpace(m.PasswordCipher))
                return Ok(ApiResponse<MailboxPasswordRevealDto>.Ok(new MailboxPasswordRevealDto()));

            try
            {
                var plain = _mailboxCipher.Decrypt(m.PasswordCipher, m.CryptoVersion);
                return Ok(ApiResponse<MailboxPasswordRevealDto>.Ok(new MailboxPasswordRevealDto { Password = plain }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解密已验证邮箱密码失败 id={Id}", id);
                return StatusCode(500, ApiResponse<MailboxPasswordRevealDto>.Fail("解密失败", 500));
            }
        }

        private Task<CompanyProfileBundleDto> LoadBundleAsync(CancellationToken ct) =>
            CompanyProfileBundleLoader.LoadAsync(_db, _logger, ct);

        private async Task CascadePlatformSuffixAsync(string newSuffix, CancellationToken ct)
        {
            var platforms = await _db.UserMailboxes
                .Where(x => !x.IsDeleted && x.Kind == UserMailboxKind.Platform)
                .ToListAsync(ct);
            foreach (var m in platforms)
            {
                var local = string.IsNullOrWhiteSpace(m.LocalPart)
                    ? MailboxAddressHelper.ExtractLocalPart(m.Address)
                    : m.LocalPart.Trim();
                m.LocalPart = local;
                m.Address = MailboxAddressHelper.BuildPlatformAddress(local, newSuffix);
                if (m.VerifyStatus == UserMailboxVerifyStatus.Ok)
                {
                    m.VerifyStatus = UserMailboxVerifyStatus.None;
                    m.VerifyMessage = "后缀已变更，请重新验证";
                    m.VerifiedAt = null;
                    m.IsDefaultSend = false;
                }
                m.ModifyTime = DateTime.UtcNow;
            }
        }

        private static string? ValidateDefaults<T>(List<T> list, string sectionName) where T : ICompanyProfileRow
        {
            var n = list.Count(r => r.IsDefault);
            if (n > 1)
                return $"{sectionName}：「默认」仅能选择一组。";
            return null;
        }

        /// <summary>
        /// 公司基础信息：人民币抬头 / 外币抬头各自最多一组，且同组不可同时勾选；可不勾。
        /// </summary>
        private static string? ValidateBasicCurrencyDefaults(List<CompanyBasicInfoRowDto>? list)
        {
            list ??= new List<CompanyBasicInfoRowDto>();
            if (list.Any(r => r.IsDefaultRmb && r.IsDefaultForeign))
                return "公司基础信息：同一组不可同时勾选「默认含税」与「默认外币」。";
            if (list.Count(r => r.IsDefaultRmb) > 1)
                return "公司基础信息：「默认含税」仅能选择一组。";
            if (list.Count(r => r.IsDefaultForeign) > 1)
                return "公司基础信息：「默认外币」仅能选择一组。";
            return null;
        }

        /// <summary>
        /// 公司印章：人民币印章 / 外币印章各自最多一组，且同组不可同时勾选；可不勾。
        /// </summary>
        private static string? ValidateSealCurrencyDefaults(List<CompanySealRowDto>? list)
        {
            list ??= new List<CompanySealRowDto>();
            if (list.Any(r => r.IsDefaultRmb && r.IsDefaultForeign))
                return "公司印章：同一组不可同时勾选「默认含税」与「默认外币」。";
            if (list.Count(r => r.IsDefaultRmb) > 1)
                return "公司印章：「默认含税」仅能选择一组。";
            if (list.Count(r => r.IsDefaultForeign) > 1)
                return "公司印章：「默认外币」仅能选择一组。";
            return null;
        }

        private static string? ValidateBankDefaults(List<CompanyBankInfoRowDto>? list)
        {
            list ??= new List<CompanyBankInfoRowDto>();
            var rmbDefaults = list.Count(r => r.IsDefault && CompanyBankInfoStore.IsRmbCurrency(r.Currency));
            if (rmbDefaults > 1)
                return "公司银行信息（人民币）：「默认」仅能选择一组。";
            var fxDefaults = list.Count(r => r.IsDefault && !CompanyBankInfoStore.IsRmbCurrency(r.Currency));
            if (fxDefaults > 1)
                return "公司银行信息（外币）：「默认」仅能选择一组。";
            return null;
        }

        private async Task UpsertJsonAsync<T>(string paramCode, string paramName, List<T> data, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(data, JsonOpts);
            var existing = await _db.SysParams.FirstOrDefaultAsync(x => x.ParamCode == paramCode, ct);
            if (existing == null)
            {
                var row = new SysParam
                {
                    Id = Guid.NewGuid().ToString(),
                    ParamCode = paramCode,
                    ParamName = paramName,
                    DataType = ParamDataType.Json,
                    ValueJson = json,
                    IsArray = true,
                    IsSystem = true,
                    IsEditable = true,
                    IsVisible = true,
                    Status = 1,
                    CreateTime = DateTime.UtcNow
                };
                _db.SysParams.Add(row);
            }
            else
            {
                existing.DataType = ParamDataType.Json;
                existing.ValueJson = json;
                existing.IsArray = true;
                existing.ModifyTime = DateTime.UtcNow;
            }
        }

        private static void MaskSmtpForAdminResponse(CompanyProfileBundleDto dto)
        {
            if (dto.SmtpEmail == null)
            {
                dto.SmtpEmail = new CompanySmtpEmailSettingsDto();
                return;
            }

            // 二期：不向客户端暴露已废弃的系统发件账号字段
            dto.SmtpEmail.User = null;
            dto.SmtpEmail.Password = null;
            dto.SmtpEmail.FromAddress = null;
            dto.SmtpEmail.FromName = null;
            dto.SmtpEmail.PasswordSet = false;
        }

        private static string? ValidateSmtp(CompanySmtpEmailSettingsDto? s)
        {
            if (s == null)
                return null;
            if (!s.Enabled)
                return null;
            if (string.IsNullOrWhiteSpace(s.SmtpHost))
                return "启用 SMTP 发信时须填写 SMTP 服务器地址。";
            if (s.SmtpPort is < 1 or > 65535)
                return "SMTP 端口须在 1～65535 之间。";
            return null;
        }

        private async Task UpsertSmtpEmailAsync(CompanySmtpEmailSettingsDto? body, CancellationToken ct)
        {
            var incoming = body ?? new CompanySmtpEmailSettingsDto();
            var existing = await _db.SysParams.FirstOrDefaultAsync(x => x.ParamCode == CompanyProfileParamCodes.SmtpEmail, ct);

            CompanySmtpEmailSettingsDto? previous = null;
            if (existing != null && !string.IsNullOrWhiteSpace(existing.ValueJson))
                previous = JsonSerializer.Deserialize<CompanySmtpEmailSettingsDto>(existing.ValueJson, JsonOpts);

            var port = incoming.SmtpPort is >= 1 and <= 65535 ? incoming.SmtpPort : 587;
            var popPort = incoming.PopPort is >= 1 and <= 65535 ? incoming.PopPort : 995;
            var newSuffix = MailboxAddressHelper.NormalizeSuffix(incoming.PlatformEmailSuffix);
            var oldSuffix = MailboxAddressHelper.NormalizeSuffix(previous?.PlatformEmailSuffix);
            var merged = new CompanySmtpEmailSettingsDto
            {
                Enabled = incoming.Enabled,
                SmtpHost = incoming.SmtpHost?.Trim() ?? string.Empty,
                SmtpPort = port,
                // UI 单一 SSL：SMTP / POP 加密开关保持一致
                UseSsl = incoming.UseSsl,
                PlatformEmailSuffix = newSuffix,
                PopHost = string.IsNullOrWhiteSpace(incoming.PopHost) ? null : incoming.PopHost.Trim(),
                PopPort = popPort,
                PopUseSsl = incoming.UseSsl,
                // 旧系统账号字段：原样保留，业务不再读写
                User = previous?.User,
                Password = previous?.Password,
                FromAddress = previous?.FromAddress,
                FromName = previous?.FromName
            };

            if (!string.Equals(oldSuffix, newSuffix, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(newSuffix))
            {
                await CascadePlatformSuffixAsync(newSuffix, ct);
            }

            var json = JsonSerializer.Serialize(merged, JsonOpts);
            if (existing == null)
            {
                _db.SysParams.Add(new SysParam
                {
                    Id = Guid.NewGuid().ToString(),
                    ParamCode = CompanyProfileParamCodes.SmtpEmail,
                    ParamName = "公司邮箱（SMTP 发信）",
                    DataType = ParamDataType.Json,
                    ValueJson = json,
                    IsArray = false,
                    IsSystem = true,
                    IsEditable = true,
                    IsVisible = true,
                    Status = 1,
                    CreateTime = DateTime.UtcNow
                });
            }
            else
            {
                existing.DataType = ParamDataType.Json;
                existing.ValueJson = json;
                existing.IsArray = false;
                existing.ModifyTime = DateTime.UtcNow;
            }
        }

        private async Task UpsertReportInfoAsync(CompanyReportInfoDto? body, CancellationToken ct)
        {
            var info = body ?? new CompanyReportInfoDto();
            info.Invoice ??= new CompanyReportRemarksDto();
            info.PackingList ??= new CompanyReportRemarksDto();

            await UpsertStringParamAsync(
                CompanyProfileParamCodes.ReportInvoiceRemarkCn,
                "Invoice 报表备注（中文）",
                info.Invoice.RemarkCn,
                ct);
            await UpsertStringParamAsync(
                CompanyProfileParamCodes.ReportInvoiceRemarkEn,
                "Invoice 报表备注（英文）",
                info.Invoice.RemarkEn,
                ct);
            await UpsertStringParamAsync(
                CompanyProfileParamCodes.ReportPackingListRemarkCn,
                "Packing List 报表备注（中文）",
                info.PackingList.RemarkCn,
                ct);
            await UpsertStringParamAsync(
                CompanyProfileParamCodes.ReportPackingListRemarkEn,
                "Packing List 报表备注（英文）",
                info.PackingList.RemarkEn,
                ct);
        }

        private async Task UpsertStringParamAsync(string paramCode, string paramName, string? value, CancellationToken ct)
        {
            var text = value ?? string.Empty;
            var existing = await _db.SysParams.FirstOrDefaultAsync(x => x.ParamCode == paramCode, ct);
            if (existing == null)
            {
                var row = new SysParam
                {
                    Id = Guid.NewGuid().ToString(),
                    ParamCode = paramCode,
                    ParamName = paramName,
                    DataType = ParamDataType.String,
                    ValueString = text.Length <= 500 ? text : null,
                    ValueJson = text.Length > 500 ? text : null,
                    IsArray = false,
                    IsSystem = true,
                    IsEditable = true,
                    IsVisible = true,
                    Status = 1,
                    CreateTime = DateTime.UtcNow
                };
                _db.SysParams.Add(row);
                return;
            }

            existing.DataType = ParamDataType.String;
            existing.IsArray = false;
            if (text.Length <= 500)
            {
                existing.ValueString = text;
                existing.ValueJson = null;
            }
            else
            {
                existing.ValueString = null;
                existing.ValueJson = text;
            }
            existing.ModifyTime = DateTime.UtcNow;
        }
    }
}
