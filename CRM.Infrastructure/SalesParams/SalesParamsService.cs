using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesParams;

public sealed class SalesParamsService : ISalesParamsService
{
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
    }
}
