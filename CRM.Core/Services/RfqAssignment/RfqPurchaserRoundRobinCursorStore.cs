using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models.System;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

public sealed class RfqPurchaserRoundRobinCursorStore : IRfqPurchaserRoundRobinCursorStore
{
    private readonly IRepository<SysParam> _sysParamRepo;
    private readonly ILogger<RfqPurchaserRoundRobinCursorStore> _logger;

    public RfqPurchaserRoundRobinCursorStore(
        IRepository<SysParam> sysParamRepo,
        ILogger<RfqPurchaserRoundRobinCursorStore> logger)
    {
        _sysParamRepo = sysParamRepo;
        _logger = logger;
    }

    public async Task<int> GetCursorAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor);
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            _logger.LogInformation(
                "【需求-采购员轮询】游标参数不存在 {ParamCode}，按 Cursor=0 处理。",
                SysParamCodes.RfqPurchaserRoundRobinCursor);
            return 0;
        }

        var v = int.TryParse(row.ValueString?.Trim(), out var parsed) && parsed >= 0 ? parsed : 0;
        if (row.ValueString?.Trim() is { } s && !int.TryParse(s, out _))
            _logger.LogWarning(
                "【需求-采购员轮询】游标参数 ValueString 非有效非负整数，已按 0 处理：{ParamCode}=\"{Raw}\"",
                SysParamCodes.RfqPurchaserRoundRobinCursor,
                s);

        return v;
    }

    public async Task SaveCursorAsync(int cursor, CancellationToken cancellationToken = default)
    {
        var rows = await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor);
        var row = rows.FirstOrDefault();
        if (row == null)
        {
            var groupFrom = (await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinPurchaserRoleCodes))
                .FirstOrDefault();
            row = new SysParam
            {
                Id = "00000000-0000-4000-8000-000000000013",
                ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
                ParamName = "需求采购员轮询游标",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.String,
                ValueString = cursor.ToString(),
                Status = 1,
                IsSystem = true,
                IsEditable = true,
                IsVisible = false,
                SortOrder = 11,
                CreateTime = DateTime.UtcNow
            };
            await _sysParamRepo.AddAsync(row);
            _logger.LogInformation(
                "【需求-采购员轮询】已新建游标参数 {ParamCode}={Cursor}",
                SysParamCodes.RfqPurchaserRoundRobinCursor,
                cursor);
            return;
        }

        row.ValueString = cursor.ToString();
        row.ModifyTime = DateTime.UtcNow;
        await _sysParamRepo.UpdateAsync(row);
        _logger.LogInformation(
            "【需求-采购员轮询】已更新游标参数 {ParamCode}={Cursor}",
            SysParamCodes.RfqPurchaserRoundRobinCursor,
            cursor);
    }
}
