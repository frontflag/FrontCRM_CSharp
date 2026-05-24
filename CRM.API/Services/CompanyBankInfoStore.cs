using System.Text.Json;
using CRM.API.Constants;
using CRM.API.Models.DTOs;
using CRM.Core.Models.Company;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>公司银行信息：从 company_bankinfo 表读写（原 sysparam JSON）。</summary>
public static class CompanyBankInfoStore
{
    public static bool IsRmbCurrency(string? currency)
    {
        var t = (currency ?? string.Empty).Trim().ToUpperInvariant();
        return t is "RMB" or "CNY" or "CNH";
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<CompanyBankInfoRowDto>> ReadAllAsync(
        ApplicationDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.CompanyBankInfos.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreateTime)
            .ToListAsync(cancellationToken);
        if (rows.Count > 0)
            return rows.Select(ToDto).ToList();

        return await ReadLegacySysParamAsync(db, logger, cancellationToken);
    }

    public static async Task UpsertAllAsync(
        ApplicationDbContext db,
        List<CompanyBankInfoRowDto>? dtos,
        CancellationToken cancellationToken = default)
    {
        dtos ??= new List<CompanyBankInfoRowDto>();
        var existing = await db.CompanyBankInfos.ToListAsync(cancellationToken);
        var incomingIds = dtos
            .Select(d => (d.Id ?? string.Empty).Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var row in existing.Where(e => !incomingIds.Contains(e.Id)).ToList())
            db.CompanyBankInfos.Remove(row);

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            if (string.IsNullOrWhiteSpace(dto.Id))
                dto.Id = Guid.NewGuid().ToString();

            var entity = existing.FirstOrDefault(e => e.Id == dto.Id);
            if (entity == null)
            {
                entity = new CompanyBankInfo
                {
                    Id = dto.Id.Trim(),
                    CreateTime = DateTime.UtcNow
                };
                db.CompanyBankInfos.Add(entity);
                existing.Add(entity);
            }

            MapToEntity(dto, entity, i);
            entity.ModifyTime = DateTime.UtcNow;
        }
    }

    private static async Task<List<CompanyBankInfoRowDto>> ReadLegacySysParamAsync(
        ApplicationDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var p = await db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParamCode == CompanyProfileParamCodes.BankInfos, cancellationToken);
        if (p == null || string.IsNullOrWhiteSpace(p.ValueJson))
            return new List<CompanyBankInfoRowDto>();
        try
        {
            return JsonSerializer.Deserialize<List<CompanyBankInfoRowDto>>(p.ValueJson, JsonOpts)
                   ?? new List<CompanyBankInfoRowDto>();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "反序列化 legacy 公司银行 sysparam 失败");
            return new List<CompanyBankInfoRowDto>();
        }
    }

    private static CompanyBankInfoRowDto ToDto(CompanyBankInfo row) => new()
    {
        Id = row.Id,
        IsDefault = row.IsDefault,
        Enabled = row.Enabled,
        BankName = row.BankName,
        AccountName = row.AccountName,
        BankAddress = row.BankAddress,
        Swift = row.Swift,
        Iban = row.Iban,
        BankCode = row.BankCode,
        AccountNumber = row.AccountNumber,
        Currency = row.Currency,
        Country = row.Country,
        BankType = row.AccountType,
        PurposeType = row.PurposeType,
        Remark = row.Remark
    };

    private static void MapToEntity(CompanyBankInfoRowDto dto, CompanyBankInfo entity, int sortOrder)
    {
        entity.IsDefault = dto.IsDefault;
        entity.Enabled = dto.Enabled;
        entity.BankName = dto.BankName?.Trim() ?? string.Empty;
        entity.AccountName = dto.AccountName?.Trim() ?? string.Empty;
        entity.BankAddress = dto.BankAddress?.Trim() ?? string.Empty;
        entity.Swift = dto.Swift?.Trim() ?? string.Empty;
        entity.Iban = dto.Iban?.Trim() ?? string.Empty;
        entity.BankCode = dto.BankCode?.Trim() ?? string.Empty;
        entity.AccountNumber = dto.AccountNumber?.Trim() ?? string.Empty;
        entity.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "RMB" : dto.Currency.Trim();
        entity.Country = dto.Country?.Trim() ?? string.Empty;
        entity.AccountType = string.IsNullOrWhiteSpace(dto.BankType) ? "rmb" : dto.BankType.Trim();
        entity.PurposeType = string.IsNullOrWhiteSpace(dto.PurposeType) ? "payment" : dto.PurposeType.Trim();
        entity.Remark = dto.Remark?.Trim() ?? string.Empty;
        entity.SortOrder = sortOrder;
    }
}
