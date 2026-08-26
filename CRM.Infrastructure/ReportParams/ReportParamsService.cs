using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.ReportParams;

public sealed class ReportParamsService : IReportParamsService
{
    private readonly ApplicationDbContext _db;

    public ReportParamsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetStyleVersionAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.ReportStyleVersion && p.Status == 1,
                cancellationToken);
        return ReportStyleVersions.NormalizeOrDefault(row?.GetStringValue());
    }

    public async Task<string> SetStyleVersionAsync(string styleVersion, CancellationToken cancellationToken = default)
    {
        var value = ReportStyleVersions.RequireAllowed(styleVersion);

        var row = await _db.SysParams
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.ReportStyleVersion,
                cancellationToken);

        if (row == null)
        {
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.ReportStyleVersion,
                ParamName = "报表样式版本",
                DataType = ParamDataType.String,
                DefaultValue = ReportStyleVersions.Default,
                Description = "全局报表样式版本（V1/V2）。本期仅配置入口，打印页暂不读取。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 30,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            row.SetStringValue(value);
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SetStringValue(value);
            row.Status = 1;
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return value;
    }
}
