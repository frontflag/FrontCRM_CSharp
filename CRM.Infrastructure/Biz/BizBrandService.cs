using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Biz;
using CRM.Core.Models.Dtos;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Biz;

public class BizBrandService : IBizBrandService
{
    private readonly ApplicationDbContext _db;

    public BizBrandService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<BizBrandPagedDto> ListAsync(BizBrandQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.BizBrands.AsNoTracking().AsQueryable();

        var exact = query.ExactMatch;

        var brandCName = TrimOrNull(query.BrandCName);
        if (brandCName != null)
        {
            q = exact
                ? q.Where(b => b.BrandCName != null && EF.Functions.ILike(b.BrandCName, brandCName))
                : q.Where(b => b.BrandCName != null && EF.Functions.ILike(b.BrandCName, $"%{brandCName}%"));
        }

        var brandEName = TrimOrNull(query.BrandEName);
        if (brandEName != null)
        {
            q = exact
                ? q.Where(b => b.BrandEName != null && EF.Functions.ILike(b.BrandEName, brandEName))
                : q.Where(b => b.BrandEName != null && EF.Functions.ILike(b.BrandEName, $"%{brandEName}%"));
        }

        var standardBrand = TrimOrNull(query.StandardBrand);
        if (standardBrand != null)
        {
            q = exact
                ? q.Where(b => b.StandardBrand != null && EF.Functions.ILike(b.StandardBrand, standardBrand))
                : q.Where(b => b.StandardBrand != null && EF.Functions.ILike(b.StandardBrand, $"%{standardBrand}%"));
        }

        var alias = TrimOrNull(query.Alias);
        if (alias != null)
        {
            q = exact
                ? q.Where(b => b.Alias != null && EF.Functions.ILike(b.Alias, alias))
                : q.Where(b => b.Alias != null && EF.Functions.ILike(b.Alias, $"%{alias}%"));
        }

        var country = TrimOrNull(query.Country);
        if (country != null)
        {
            q = exact
                ? q.Where(b =>
                    (b.Country != null && EF.Functions.ILike(b.Country, country)) ||
                    (b.CountryCode != null && EF.Functions.ILike(b.CountryCode, country)))
                : q.Where(b =>
                    (b.Country != null && EF.Functions.ILike(b.Country, $"%{country}%")) ||
                    (b.CountryCode != null && EF.Functions.ILike(b.CountryCode, $"%{country}%")));
        }

        var remark = TrimOrNull(query.Remark);
        if (remark != null)
        {
            q = exact
                ? q.Where(b => b.Remark != null && EF.Functions.ILike(b.Remark, remark))
                : q.Where(b => b.Remark != null && EF.Functions.ILike(b.Remark, $"%{remark}%"));
        }

        if (query.AuditStatus.HasValue)
            q = q.Where(b => b.AuditStatus == query.AuditStatus.Value);

        if (query.CreateTimeFrom.HasValue)
        {
            var from = query.CreateTimeFrom.Value.Date;
            q = q.Where(b => b.CreateTime != null && b.CreateTime >= from);
        }

        if (query.CreateTimeTo.HasValue)
        {
            var toExclusive = query.CreateTimeTo.Value.Date.AddDays(1);
            q = q.Where(b => b.CreateTime != null && b.CreateTime < toExclusive);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var total = await q.CountAsync(cancellationToken);
        var entities = await q
            .OrderByDescending(b => b.CreateTime)
            .ThenBy(b => b.BrandEName)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToDto).ToList();
        await EnrichUserNamesAsync(items, cancellationToken);

        return new BizBrandPagedDto { Items = items, Total = total };
    }

    public async Task<List<BizBrandOptionDto>> ListOptionsAsync(
        BizBrandOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var keyword = TrimOrNull(query.Keyword);

        // 短码优先：在品牌管理「别名」中配置的 token（如 TI、ON）与关键词完全一致时置顶，无需代码硬编码。
        var preferred = keyword != null
            ? await FetchPreferredBrandsByAliasTokenAsync(keyword, cancellationToken)
            : [];

        var q = _db.BizBrands.AsNoTracking().AsQueryable();
        if (keyword != null)
        {
            q = q.Where(b =>
                (b.StandardBrand != null && EF.Functions.ILike(b.StandardBrand, $"%{keyword}%")) ||
                (b.BrandEName != null && EF.Functions.ILike(b.BrandEName, $"%{keyword}%")) ||
                (b.BrandCName != null && EF.Functions.ILike(b.BrandCName, $"%{keyword}%")) ||
                (b.Alias != null && EF.Functions.ILike(b.Alias, $"%{keyword}%")));
        }

        var regular = await q
            .OrderByDescending(b => b.AuditStatus == BizBrandAuditStatus.Approved)
            .ThenBy(b => b.StandardBrand)
            .ThenBy(b => b.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var merged = MergeBrandOptions(preferred, regular, pageSize);
        return merged.Select(ToOptionDto).ToList();
    }

    private async Task<List<BizBrand>> FetchPreferredBrandsByAliasTokenAsync(
        string keyword,
        CancellationToken cancellationToken)
    {
        var candidates = await _db.BizBrands.AsNoTracking()
            .Where(b => b.Alias != null && EF.Functions.ILike(b.Alias, $"%{keyword}%"))
            .OrderByDescending(b => b.AuditStatus == BizBrandAuditStatus.Approved)
            .ThenBy(b => b.StandardBrand)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(b => BizBrandAliasHelper.ContainsExactToken(b.Alias, keyword))
            .ToList();
    }

    private static List<BizBrand> MergeBrandOptions(List<BizBrand> preferred, List<BizBrand> regular, int pageSize)
    {
        var seen = new HashSet<long>();
        var merged = new List<BizBrand>(pageSize);

        foreach (var brand in preferred)
        {
            if (seen.Add(brand.Id))
                merged.Add(brand);
        }

        foreach (var brand in regular)
        {
            if (seen.Add(brand.Id))
                merged.Add(brand);
            if (merged.Count >= pageSize)
                break;
        }

        return merged.Count > pageSize ? merged.Take(pageSize).ToList() : merged;
    }

    private static BizBrandOptionDto ToOptionDto(BizBrand b) => new()
    {
        Id = b.Id,
        StandardBrand = b.StandardBrand,
        AuditStatus = b.AuditStatus,
        BrandEName = b.BrandEName,
        BrandCName = b.BrandCName
    };

    public async Task<BizBrandRowDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return null;
        var row = await _db.BizBrands.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (row == null) return null;
        var dto = ToDto(row);
        await EnrichUserNamesAsync([dto], cancellationToken);
        return dto;
    }

    public async Task<BizBrandRowDto> CreateAsync(
        UpsertBizBrandRequest request,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredTriplet(request);
        await EnsureUniqueTripletAsync(request, excludeId: null, cancellationToken);
        var entity = new BizBrand();
        MapToEntity(request, entity);
        entity.CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        entity.CreateTime = DateTime.UtcNow;
        entity.AuditStatus = BizBrandAuditStatus.Pending;
        entity.AuditByUserId = null;
        entity.AuditTime = null;
        _db.BizBrands.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = ToDto(entity);
        await EnrichUserNamesAsync([dto], cancellationToken);
        return dto;
    }

    public async Task<BizBrandRowDto> UpdateAsync(long id, UpsertBizBrandRequest request, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("品牌 ID 无效", nameof(id));

        var entity = await _db.BizBrands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"找不到 ID 为 {id} 的品牌");

        await EnsureUniqueTripletAsync(request, excludeId: id, cancellationToken);
        MapToEntity(request, entity);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = ToDto(entity);
        await EnrichUserNamesAsync([dto], cancellationToken);
        return dto;
    }

    public async Task<BizBrandRowDto> ApproveAsync(
        long id,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("品牌 ID 无效", nameof(id));

        var entity = await _db.BizBrands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"找不到 ID 为 {id} 的品牌");

        if (entity.AuditStatus != BizBrandAuditStatus.Pending)
            throw new InvalidOperationException("仅「待审核」状态的品牌可审核");

        entity.AuditStatus = BizBrandAuditStatus.Approved;
        entity.AuditByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        entity.AuditTime = DateTime.UtcNow;

        await SyncRfqItemBrandTextByBrandIdAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = ToDto(entity);
        await EnrichUserNamesAsync([dto], cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(long id, string? actingUserId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("品牌 ID 无效", nameof(id));

        var entity = await _db.BizBrands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"找不到 ID 为 {id} 的品牌");

        if (entity.IsDeleted)
            throw new InvalidOperationException("品牌已删除");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncRfqItemBrandTextByBrandIdAsync(BizBrand brand, CancellationToken cancellationToken)
    {
        var brandText = ResolveRfqBrandText(brand);
        if (string.IsNullOrEmpty(brandText))
            return;

        var items = await _db.RFQItems
            .Where(i => i.BrandId == brand.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            if (item.Brand != brandText)
                item.Brand = brandText;
        }
    }

    /// <summary>与 RFQ 保存明细时写入 <c>rfqitem.brand</c> 的规则一致。</summary>
    private static string ResolveRfqBrandText(BizBrand brand)
    {
        var std = TrimOrNull(brand.StandardBrand);
        if (std != null) return std;
        var en = TrimOrNull(brand.BrandEName);
        if (en != null) return en;
        return TrimOrNull(brand.BrandCName) ?? string.Empty;
    }

    private async Task EnrichUserNamesAsync(List<BizBrandRowDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var userIds = items
            .SelectMany(i => new[] { i.CreateByUserId, i.AuditByUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (userIds.Count == 0) return;

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var map = users.ToDictionary(
            u => u.Id,
            u => EntityLookupService.FormatUserLoginName(u) ?? u.UserName,
            StringComparer.Ordinal);

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.CreateByUserId)
                && map.TryGetValue(item.CreateByUserId.Trim(), out var createName))
                item.CreateUserName = createName;
            if (!string.IsNullOrWhiteSpace(item.AuditByUserId)
                && map.TryGetValue(item.AuditByUserId.Trim(), out var auditName))
                item.AuditUserName = auditName;
        }
    }

    private static void ValidateRequiredTriplet(UpsertBizBrandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandEName))
            throw new ArgumentException("品牌英文名为必填项");
        if (string.IsNullOrWhiteSpace(request.BrandCName))
            throw new ArgumentException("品牌中文名为必填项");
        if (string.IsNullOrWhiteSpace(request.StandardBrand))
            throw new ArgumentException("标准品牌名为必填项");
    }

    private async Task EnsureUniqueTripletAsync(
        UpsertBizBrandRequest request,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var brandEName = TrimOrNull(request.BrandEName);
        var brandCName = TrimOrNull(request.BrandCName);
        var standardBrand = TrimOrNull(request.StandardBrand);

        var q = _db.BizBrands.AsNoTracking().AsQueryable();
        if (excludeId.HasValue)
            q = q.Where(b => b.Id != excludeId.Value);

        var duplicate = await q.AnyAsync(b =>
            (b.BrandEName == brandEName || (b.BrandEName == null && brandEName == null)) &&
            (b.BrandCName == brandCName || (b.BrandCName == null && brandCName == null)) &&
            (b.StandardBrand == standardBrand || (b.StandardBrand == null && standardBrand == null)),
            cancellationToken);

        if (duplicate)
            throw new InvalidOperationException(
                "已存在品牌英文名、中文名、标准品牌名完全相同的记录，请修改后保存。");
    }

    private static string? TrimOrNull(string? value)
    {
        var t = (value ?? string.Empty).Trim();
        return t.Length == 0 ? null : t;
    }

    private static void MapToEntity(UpsertBizBrandRequest request, BizBrand entity)
    {
        entity.BrandEName = TrimOrNull(request.BrandEName);
        entity.BrandCName = TrimOrNull(request.BrandCName);
        entity.StandardBrand = TrimOrNull(request.StandardBrand);
        entity.Alias = TrimOrNull(request.Alias);
        entity.CountryCode = TrimOrNull(request.CountryCode);
        entity.Country = TrimOrNull(request.Country);
        entity.Remark = TrimOrNull(request.Remark);
    }

    private static BizBrandRowDto ToDto(BizBrand row) => new()
    {
        Id = row.Id,
        BrandEName = row.BrandEName,
        BrandCName = row.BrandCName,
        StandardBrand = row.StandardBrand,
        Alias = row.Alias,
        CountryCode = row.CountryCode,
        Country = row.Country,
        Remark = row.Remark,
        CreateByUserId = row.CreateByUserId,
        CreateTime = row.CreateTime,
        AuditStatus = row.AuditStatus,
        AuditByUserId = row.AuditByUserId,
        AuditTime = row.AuditTime
    };
}
