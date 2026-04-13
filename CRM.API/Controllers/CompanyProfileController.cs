using CRM.API.Authorization;
using CRM.API.Constants;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/company-profile")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CompanyProfileController> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public CompanyProfileController(ApplicationDbContext db, ILogger<CompanyProfileController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<CompanyProfileBundleDto>>> Get(CancellationToken ct)
        {
            try
            {
                var dto = await LoadBundleAsync(ct);
                MaskSmtpForAdminResponse(dto);
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
        [RequireAnyPermission("purchase-order.read", "sales-order.read", "vendor.read")]
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

        [HttpPut]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Put([FromBody] CompanyProfileBundleDto body, CancellationToken ct)
        {
            if (body == null)
                return BadRequest(ApiResponse<object>.Fail("请求体为空", 400));

            body.Logos ??= new List<CompanyLogoRowDto>();

            try
            {
                var err = ValidateDefaults(body.BasicInfos, "公司基础信息")
                    ?? ValidateDefaults(body.BankInfos, "公司银行信息")
                    ?? ValidateDefaults(body.Logos, "公司Logo")
                    ?? ValidateDefaults(body.Seals, "公司印章")
                    ?? ValidateDefaults(body.Warehouses, "公司仓库信息")
                    ?? ValidateSmtp(body.SmtpEmail);
                if (err != null)
                    return BadRequest(ApiResponse<object>.Fail(err, 400));

                await UpsertJsonAsync(CompanyProfileParamCodes.BasicInfos, "公司基础信息（多组）", body.BasicInfos, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.BankInfos, "公司银行信息（多组）", body.BankInfos, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Logos, "公司Logo（多组）", body.Logos, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Seals, "公司印章（多组）", body.Seals, ct);
                await UpsertJsonAsync(CompanyProfileParamCodes.Warehouses, "公司仓库信息（多组）", body.Warehouses, ct);
                await UpsertSmtpEmailAsync(body.SmtpEmail, ct);
                await _db.SaveChangesAsync(ct);
                return Ok(ApiResponse<object>.Ok(null, "保存成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存公司信息参数失败");
                return StatusCode(500, ApiResponse<object>.Fail("保存失败", 500));
            }
        }

        private Task<CompanyProfileBundleDto> LoadBundleAsync(CancellationToken ct) =>
            CompanyProfileBundleLoader.LoadAsync(_db, _logger, ct);

        private static string? ValidateDefaults<T>(List<T> list, string sectionName) where T : ICompanyProfileRow
        {
            var n = list.Count(r => r.IsDefault);
            if (n > 1)
                return $"{sectionName}：「默认」仅能选择一组。";
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

            var hadPwd = !string.IsNullOrWhiteSpace(dto.SmtpEmail.Password);
            dto.SmtpEmail.Password = null;
            dto.SmtpEmail.PasswordSet = hadPwd;
        }

        private static string? ValidateSmtp(CompanySmtpEmailSettingsDto? s)
        {
            if (s == null)
                return null;
            if (!s.Enabled)
                return null;
            if (string.IsNullOrWhiteSpace(s.SmtpHost))
                return "启用系统发信时须填写 SMTP 服务器地址。";
            if (string.IsNullOrWhiteSpace(s.FromAddress))
                return "启用系统发信时须填写发件人邮箱。";
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
            var merged = new CompanySmtpEmailSettingsDto
            {
                Enabled = incoming.Enabled,
                SmtpHost = incoming.SmtpHost?.Trim() ?? string.Empty,
                SmtpPort = port,
                User = string.IsNullOrWhiteSpace(incoming.User) ? null : incoming.User.Trim(),
                Password = string.IsNullOrWhiteSpace(incoming.Password)
                    ? previous?.Password
                    : incoming.Password,
                FromAddress = incoming.FromAddress?.Trim() ?? string.Empty,
                FromName = string.IsNullOrWhiteSpace(incoming.FromName) ? "FrontCRM" : incoming.FromName.Trim(),
                UseSsl = incoming.UseSsl
            };

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
    }
}
