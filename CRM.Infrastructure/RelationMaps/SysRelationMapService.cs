using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RelationMaps;

public class SysRelationMapService : ISysRelationMapService
{
    private readonly ApplicationDbContext _db;

    public SysRelationMapService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetMappedDestIdsAsync(
        short type,
        string objSrc,
        CancellationToken cancellationToken = default)
    {
        ValidateType(type);
        if (string.IsNullOrWhiteSpace(objSrc))
            throw new ArgumentException("objSrc 不能为空", nameof(objSrc));

        return await _db.SysRelationMaps
            .AsNoTracking()
            .Where(m => m.Type == type && m.ObjSrc == objSrc && !m.IsDeleted)
            .Select(m => m.ObjDest)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveMappingsAsync(
        short type,
        string objSrc,
        IReadOnlyList<string> addDestIds,
        IReadOnlyList<string> removeDestIds,
        CancellationToken cancellationToken = default)
    {
        ValidateType(type);
        if (string.IsNullOrWhiteSpace(objSrc))
            throw new ArgumentException("objSrc 不能为空", nameof(objSrc));

        var toAdd = NormalizeIds(addDestIds);
        var toRemove = NormalizeIds(removeDestIds);
        if (toAdd.Count == 0 && toRemove.Count == 0)
            return;

        if (toAdd.Intersect(toRemove, StringComparer.OrdinalIgnoreCase).Any())
            throw new InvalidOperationException("同一目标不能同时新增与移除");

        var existing = await _db.SysRelationMaps
            .IgnoreQueryFilters()
            .Where(m => m.Type == type && m.ObjSrc == objSrc)
            .ToListAsync(cancellationToken);

        var byDest = existing.ToDictionary(m => m.ObjDest, StringComparer.OrdinalIgnoreCase);

        foreach (var dest in toAdd)
        {
            if (byDest.TryGetValue(dest, out var row))
            {
                if (!row.IsDeleted) continue;
                row.IsDeleted = false;
                _db.SysRelationMaps.Update(row);
            }
            else
            {
                await _db.SysRelationMaps.AddAsync(new SysRelationMap
                {
                    Type = type,
                    ObjSrc = objSrc,
                    ObjDest = dest,
                    IsDeleted = false
                }, cancellationToken);
            }
        }

        foreach (var dest in toRemove)
        {
            if (!byDest.TryGetValue(dest, out var row) || row.IsDeleted)
                continue;
            row.IsDeleted = true;
            _db.SysRelationMaps.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateType(short type)
    {
        if (type != SysRelationMapTypeCode.SalesAssistantToSalesperson
            && type != SysRelationMapTypeCode.PurchaseAssistantToPurchaser)
        {
            throw new ArgumentException($"不支持的关系类型: {type}", nameof(type));
        }
    }

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids) =>
        (ids ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
