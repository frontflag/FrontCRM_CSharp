using System.Text.Json;
using CRM.API.Constants;
using CRM.API.Models.DTOs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>从 SysParam 读取公司信息多组 JSON（公司信息页与采购报表共用）。</summary>
public static class CompanyProfileBundleLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<CompanyProfileBundleDto> LoadAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        return new CompanyProfileBundleDto
        {
            BasicInfos = await ReadListAsync<CompanyBasicInfoRowDto>(db, logger, CompanyProfileParamCodes.BasicInfos, cancellationToken),
            BankInfos = await CompanyBankInfoStore.ReadAllAsync(db, logger, cancellationToken),
            Logos = await ReadListAsync<CompanyLogoRowDto>(db, logger, CompanyProfileParamCodes.Logos, cancellationToken),
            Seals = await ReadListAsync<CompanySealRowDto>(db, logger, CompanyProfileParamCodes.Seals, cancellationToken),
            Warehouses = await ReadListAsync<CompanyWarehouseRowDto>(db, logger, CompanyProfileParamCodes.Warehouses, cancellationToken),
            SmtpEmail = await ReadSmtpEmailAsync(db, logger, cancellationToken) ?? new CompanySmtpEmailSettingsDto(),
            ReportInfo = await ReadReportInfoAsync(db, cancellationToken)
        };
    }

    public static async Task<CompanyReportInfoDto> ReadReportInfoAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        return new CompanyReportInfoDto
        {
            Invoice = new CompanyReportRemarksDto
            {
                RemarkCn = await ReadStringParamAsync(db, CompanyProfileParamCodes.ReportInvoiceRemarkCn, cancellationToken),
                RemarkEn = await ReadStringParamAsync(db, CompanyProfileParamCodes.ReportInvoiceRemarkEn, cancellationToken)
            },
            PackingList = new CompanyReportRemarksDto
            {
                RemarkCn = await ReadStringParamAsync(db, CompanyProfileParamCodes.ReportPackingListRemarkCn, cancellationToken),
                RemarkEn = await ReadStringParamAsync(db, CompanyProfileParamCodes.ReportPackingListRemarkEn, cancellationToken)
            }
        };
    }

    /// <summary>对外 API（报表、采购订单 report-data 等）不包含 SMTP 与密码。</summary>
    public static void StripSmtpEmail(CompanyProfileBundleDto dto) => dto.SmtpEmail = null;

    /// <summary>发信时读取库内 SMTP（含密码）。无记录时返回 null。</summary>
    public static async Task<CompanySmtpEmailSettingsDto?> LoadSmtpEmailRawAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default) =>
        await ReadSmtpEmailAsync(db, null, cancellationToken);

    private static async Task<CompanySmtpEmailSettingsDto?> ReadSmtpEmailAsync(
        ApplicationDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var p = await db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParamCode == CompanyProfileParamCodes.SmtpEmail, cancellationToken);
        if (p == null || string.IsNullOrWhiteSpace(p.ValueJson))
            return null;
        try
        {
            var dto = JsonSerializer.Deserialize<CompanySmtpEmailSettingsDto>(p.ValueJson, JsonOpts);
            if (dto != null)
                NormalizeImapFromLegacyPop(dto);
            return dto;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "反序列化 SMTP 参数失败");
            return null;
        }
    }

    private static async Task<List<T>> ReadListAsync<T>(
        ApplicationDbContext db,
        ILogger logger,
        string paramCode,
        CancellationToken cancellationToken) where T : class
    {
        var p = await db.SysParams.AsNoTracking().FirstOrDefaultAsync(x => x.ParamCode == paramCode, cancellationToken);
        if (p == null || string.IsNullOrWhiteSpace(p.ValueJson))
            return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(p.ValueJson, JsonOpts) ?? new List<T>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "反序列化参数 {Code} 失败，返回空列表", paramCode);
            return new List<T>();
        }
    }

    /// <summary>旧配置仅有 POP 时，填充 IMAP 字段供校验/同步/公司页使用。</summary>
    private static void NormalizeImapFromLegacyPop(CompanySmtpEmailSettingsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ImapHost) && !string.IsNullOrWhiteSpace(dto.PopHost))
            dto.ImapHost = dto.PopHost;
        if (dto.ImapPort is < 1 or > 65535)
            dto.ImapPort = dto.PopPort is >= 1 and <= 65535 ? dto.PopPort : 993;
        if (string.IsNullOrWhiteSpace(dto.ImapHost))
            return;
        // 若从未写过 ImapUseSsl，与 UseSsl / PopUseSsl 对齐
        if (!dto.ImapUseSsl && dto.UseSsl)
            dto.ImapUseSsl = true;
    }

    /// <summary>读取单值字符串参数（ValueString 或 ValueJson 存长文本）。</summary>
    public static async Task<string> ReadStringParamAsync(
        ApplicationDbContext db,
        string paramCode,
        CancellationToken cancellationToken = default)
    {
        var p = await db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParamCode == paramCode, cancellationToken);
        if (p == null)
            return string.Empty;
        if (!string.IsNullOrEmpty(p.ValueString))
            return p.ValueString;
        if (string.IsNullOrWhiteSpace(p.ValueJson))
            return string.Empty;
        try
        {
            var s = JsonSerializer.Deserialize<string>(p.ValueJson, JsonOpts);
            if (s != null)
                return s;
        }
        catch
        {
            /* 非 JSON 字符串时按原文返回 */
        }
        return p.ValueJson;
    }
}
