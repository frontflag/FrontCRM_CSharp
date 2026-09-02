using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesParams;

public sealed class SalesParamsService : ISalesParamsService
{
    private static readonly JsonSerializerOptions FacetsJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _db;

    public SalesParamsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> GetAllowRefreshCompletedBizNodesAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.SalesAllowRefreshCompletedBizNodes && p.Status == 1,
                cancellationToken);
        if (row == null)
            return false;

        return row.GetBoolValue();
    }

    public async Task SetAllowRefreshCompletedBizNodesAsync(bool allow, CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.SalesAllowRefreshCompletedBizNodes,
                cancellationToken);

        if (row == null)
        {
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.SalesAllowRefreshCompletedBizNodes,
                ParamName = "刷新客户-允许已完成业务节点",
                DataType = ParamDataType.Boolean,
                DefaultValue = "false",
                Description =
                    "销售订单「刷新客户」时，是否允许同步已出库/已完成的出库通知、装箱单、出库单等下游单据。默认不允许。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 20,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            row.SetBoolValue(allow);
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SetBoolValue(allow);
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await SyncRefreshCompletedFacetsCustomerAsync(allow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SalesRefreshCompletedFacets> GetRefreshCompletedFacetsAsync(
        CancellationToken cancellationToken = default)
    {
        var customer = await GetAllowRefreshCompletedBizNodesAsync(cancellationToken);
        var defaults = new SalesRefreshCompletedFacets { Customer = customer };
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.SalesRefreshCompletedFacets && p.Status == 1,
                cancellationToken);
        if (row == null || string.IsNullOrWhiteSpace(row.ValueString))
            return defaults;

        try
        {
            var parsed = JsonSerializer.Deserialize<SalesRefreshCompletedFacets>(
                row.ValueString,
                FacetsJson);
            if (parsed == null)
                return defaults;
            parsed.Customer = customer;
            return parsed;
        }
        catch (JsonException)
        {
            return defaults;
        }
    }

    /// <inheritdoc />
    public async Task SetRefreshCompletedFacetsAsync(
        SalesRefreshCompletedFacets facets,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facets);
        await SetAllowRefreshCompletedBizNodesAsync(facets.Customer, cancellationToken);
        await UpsertRefreshCompletedFacetsJsonAsync(facets, cancellationToken);
    }

    private async Task SyncRefreshCompletedFacetsCustomerAsync(bool customerAllow, CancellationToken cancellationToken)
    {
        var current = await GetRefreshCompletedFacetsAsync(cancellationToken);
        current.Customer = customerAllow;
        await UpsertRefreshCompletedFacetsJsonAsync(current, cancellationToken);
    }

    private async Task UpsertRefreshCompletedFacetsJsonAsync(
        SalesRefreshCompletedFacets facets,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(facets, FacetsJson);
        var row = await _db.SysParams
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.SalesRefreshCompletedFacets,
                cancellationToken);
        if (row == null)
        {
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.SalesRefreshCompletedFacets,
                ParamName = "分面刷新-允许已完结节点",
                DataType = ParamDataType.String,
                Description = "销售订单分面刷新是否允许覆盖已完结下游（JSON：customer/pn/brand/qty/price）。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 21,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            row.SetStringValue(json);
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SetStringValue(json);
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
