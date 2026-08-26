using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseParams;

public class PurchaseQuoterPoolService : IPurchaseQuoterPoolService
{
    private readonly ApplicationDbContext _db;

    public PurchaseQuoterPoolService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetAssigneeCountAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinAssigneeCount && p.Status == 1, cancellationToken);
        if (row == null)
            return 2;

        if (int.TryParse(row.ValueString?.Trim(), out var parsed))
            return parsed is 1 or 2 ? parsed : 2;

        return 2;
    }

    public async Task SetAssigneeCountAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count is not (1 or 2))
            throw new ArgumentException("报价人数仅支持 1 或 2", nameof(count));

        var row = await _db.SysParams
            .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinAssigneeCount, cancellationToken);

        if (row == null)
        {
            var groupFrom = await _db.SysParams.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinPurchaserRoleCodes, cancellationToken);
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.RfqRoundRobinAssigneeCount,
                ParamName = "需求轮询分配报价员人数",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.Integer,
                ValueString = count.ToString(),
                DefaultValue = "2",
                Description = "每条 RFQItem 从报价员池连续取 N 人（1 或 2），按明细轮询。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 12,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.ValueString = count.ToString();
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PurchaseQuoterPoolListResult> ListMembersAsync(string? filter, CancellationToken cancellationToken = default)
    {
        var normalizedFilter = string.Equals(filter, "selected", StringComparison.OrdinalIgnoreCase) ? "selected" : "all";
        var selectedIds = await LoadPoolUserIdSetAsync(cancellationToken);
        var candidates = await BuildCandidateRowsAsync(selectedIds, cancellationToken);

        var items = normalizedFilter == "selected"
            ? candidates.Where(c => c.IsSelected).ToList()
            : candidates;

        return new PurchaseQuoterPoolListResult
        {
            Items = items,
            SelectedCount = candidates.Count(c => c.IsSelected)
        };
    }

    public async Task<PurchaseQuoterPoolListResult> SavePoolAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default)
    {
        var normalized = (userIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var eligible = await GetEligibleCandidateUserIdsAsync(cancellationToken);
        var invalid = normalized.Where(id => !eligible.Contains(id)).ToList();
        if (invalid.Count > 0)
            throw new ArgumentException($"以下用户不在采购部门职员范围内或不可选：{string.Join(", ", invalid)}");

        var existing = await _db.SysPurchaseQuoterPools.ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _db.SysPurchaseQuoterPools.RemoveRange(existing);

        var now = DateTime.UtcNow;
        for (var i = 0; i < normalized.Count; i++)
        {
            await _db.SysPurchaseQuoterPools.AddAsync(new SysPurchaseQuoterPool
            {
                UserId = normalized[i],
                SortOrder = i,
                CreateTime = now,
                UpdateTime = now
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ListMembersAsync("all", cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetOrderedActivePoolUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var poolRows = await _db.SysPurchaseQuoterPools.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
        if (poolRows.Count == 0)
            return Array.Empty<string>();

        var poolIds = poolRows.Select(p => p.UserId).ToList();
        var activeIds = await _db.Users.AsNoTracking()
            .Where(u => poolIds.Contains(u.Id) && u.IsActive && u.Status == 1)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var activeSet = activeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return poolRows
            .Select(p => p.UserId)
            .Where(id => activeSet.Contains(id))
            .ToList();
    }

    public async Task<int> GetDemandProtectionMinutesAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.RfqDemandProtectionMinutes && p.Status == 1,
                cancellationToken);
        if (row == null)
            return RfqDemandProtectionRules.DefaultProtectionMinutes;

        if (int.TryParse(row.ValueString?.Trim(), out var parsed)
            && parsed >= 0
            && parsed <= RfqDemandProtectionRules.MaxProtectionMinutes)
            return parsed;

        return RfqDemandProtectionRules.DefaultProtectionMinutes;
    }

    public async Task SetDemandProtectionMinutesAsync(int minutes, CancellationToken cancellationToken = default)
    {
        if (minutes < 0 || minutes > RfqDemandProtectionRules.MaxProtectionMinutes)
            throw new ArgumentException(
                $"需求保护时长须在 0～{RfqDemandProtectionRules.MaxProtectionMinutes} 分钟之间",
                nameof(minutes));

        var row = await _db.SysParams
            .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqDemandProtectionMinutes, cancellationToken);

        if (row == null)
        {
            var groupFrom = await _db.SysParams.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinAssigneeCount, cancellationToken);
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.RfqDemandProtectionMinutes,
                ParamName = "需求保护时长",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.Integer,
                ValueString = minutes.ToString(),
                DefaultValue = RfqDemandProtectionRules.DefaultProtectionMinutes.ToString(),
                Description = "需求明细创建后在此分钟数内仅分配采购员可见/可报价；超过后任意采购员可见/可报价。0 表示无保护期。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 13,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.ValueString = minutes.ToString();
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<short> GetDefaultAssignMethodAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.RfqDefaultAssignMethod && p.Status == 1,
                cancellationToken);
        if (row == null)
            return RfqDefaultAssignMethodRules.DefaultAssignMethod;

        if (short.TryParse(row.ValueString?.Trim(), out var parsed))
            return RfqDefaultAssignMethodRules.Normalize(parsed);

        return RfqDefaultAssignMethodRules.DefaultAssignMethod;
    }

    public async Task SetDefaultAssignMethodAsync(short assignMethod, CancellationToken cancellationToken = default)
    {
        if (!RfqDefaultAssignMethodRules.IsAllowed(assignMethod))
            throw new ArgumentException("默认分配方式须为条目轮询、品牌轮询或采报优先", nameof(assignMethod));

        var normalized = RfqDefaultAssignMethodRules.Normalize(assignMethod);
        var row = await _db.SysParams
            .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqDefaultAssignMethod, cancellationToken);

        if (row == null)
        {
            var groupFrom = await _db.SysParams.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinAssigneeCount, cancellationToken);
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.RfqDefaultAssignMethod,
                ParamName = "默认分配方式",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.Integer,
                ValueString = normalized.ToString(),
                DefaultValue = RfqDefaultAssignMethodRules.DefaultAssignMethod.ToString(),
                Description = "新建需求页「分配方式」下拉默认选中项（2 条目轮询 / 3 品牌轮询 / 5 采报优先）。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 14,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            await _db.SysParams.AddAsync(row, cancellationToken);
        }
        else
        {
            row.ValueString = normalized.ToString();
            row.ModifyTime = DateTime.UtcNow;
            _db.SysParams.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> GetAllowDesignatedPurchaserAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.RfqAllowDesignatedPurchaser && p.Status == 1,
                cancellationToken);
        if (row == null)
            return false;

        return row.GetBoolValue();
    }

    public async Task SetAllowDesignatedPurchaserAsync(bool allow, CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.RfqAllowDesignatedPurchaser,
                cancellationToken);

        if (row == null)
        {
            var groupFrom = await _db.SysParams.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqDefaultAssignMethod, cancellationToken);
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.RfqAllowDesignatedPurchaser,
                ParamName = "允许指定采购",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.Boolean,
                DefaultValue = "false",
                Description = "勾选后，新建/编辑需求的「分配方式」下拉才会出现「指定采购」。默认关闭。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 16,
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

    public async Task<bool> GetAllowRefreshCompletedBizNodesAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.PurchaseAllowRefreshCompletedBizNodes && p.Status == 1,
                cancellationToken);
        if (row == null)
            return false;

        return row.GetBoolValue();
    }

    public async Task SetAllowRefreshCompletedBizNodesAsync(bool allow, CancellationToken cancellationToken = default)
    {
        var row = await _db.SysParams
            .FirstOrDefaultAsync(
                p => p.ParamCode == SysParamCodes.PurchaseAllowRefreshCompletedBizNodes,
                cancellationToken);

        if (row == null)
        {
            var groupFrom = await _db.SysParams.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParamCode == SysParamCodes.RfqDefaultAssignMethod, cancellationToken);
            row = new SysParam
            {
                Id = Guid.NewGuid().ToString(),
                ParamCode = SysParamCodes.PurchaseAllowRefreshCompletedBizNodes,
                ParamName = "刷新供应商-允许已完成业务节点",
                GroupId = groupFrom?.GroupId,
                DataType = ParamDataType.Boolean,
                DefaultValue = "false",
                Description =
                    "采购订单「刷新供应商」时，是否允许同步已入库/已过账/已付款/已认证等下游单据。默认不允许。",
                IsSystem = true,
                IsEditable = true,
                IsVisible = true,
                SortOrder = 15,
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

    private async Task<HashSet<string>> LoadPoolUserIdSetAsync(CancellationToken cancellationToken)
    {
        var ids = await _db.SysPurchaseQuoterPools.AsNoTracking()
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<PurchaseQuoterPoolMemberDto>> BuildCandidateRowsAsync(
        HashSet<string> selectedIds,
        CancellationToken cancellationToken)
    {
        var users = await LoadEligibleCandidateUsersAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();

        var userDepts = await _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => userIds.Contains(ud.UserId))
            .ToListAsync(cancellationToken);
        var deptIds = userDepts.Select(ud => ud.DepartmentId).Distinct().ToList();
        var deptNameById = await _db.RbacDepartments.AsNoTracking()
            .Where(d => deptIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? string.Empty, cancellationToken);

        var primaryDeptByUser = userDepts
            .Where(ud => ud.IsPrimary)
            .GroupBy(ud => ud.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().DepartmentId, StringComparer.OrdinalIgnoreCase);

        return users
            .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
            .Select(u =>
            {
                string? deptName = null;
                if (primaryDeptByUser.TryGetValue(u.Id, out var deptId) &&
                    deptNameById.TryGetValue(deptId, out var name))
                {
                    deptName = name;
                }

                return new PurchaseQuoterPoolMemberDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    RealName = u.RealName,
                    DepartmentName = deptName,
                    IsActive = u.IsActive && u.Status == 1,
                    IsSelected = selectedIds.Contains(u.Id)
                };
            })
            .ToList();
    }

    private async Task<HashSet<string>> GetEligibleCandidateUserIdsAsync(CancellationToken cancellationToken)
    {
        var users = await LoadEligibleCandidateUsersAsync(cancellationToken);
        return users.Select(u => u.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<User>> LoadEligibleCandidateUsersAsync(CancellationToken cancellationToken)
    {
        var departments = await _db.RbacDepartments.AsNoTracking()
            .Where(d => d.Status == 1)
            .ToListAsync(cancellationToken);

        var purchaseDeptIds = departments
            .Where(PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer)
            .Select(d => d.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (purchaseDeptIds.Count == 0)
            return new List<User>();

        var opsDeptIds = departments
            .Where(PurchasingDepartmentRules.IsPurchasingOperationsDepartment)
            .Select(d => d.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var userDepartments = await _db.RbacUserDepartments.AsNoTracking().ToListAsync(cancellationToken);
        var candidateIds = userDepartments
            .Where(ud => purchaseDeptIds.Contains(ud.DepartmentId))
            .Select(ud => ud.UserId)
            .Where(uid => !string.IsNullOrWhiteSpace(uid))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var primaryDeptByUser = userDepartments
            .Where(ud => ud.IsPrimary)
            .GroupBy(ud => ud.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().DepartmentId, StringComparer.OrdinalIgnoreCase);

        candidateIds = candidateIds
            .Where(uid =>
            {
                if (!primaryDeptByUser.TryGetValue(uid, out var primaryDeptId))
                    return true;
                return !opsDeptIds.Contains(primaryDeptId);
            })
            .ToList();

        if (candidateIds.Count == 0)
            return new List<User>();

        return await _db.Users.AsNoTracking()
            .Where(u => candidateIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);
    }
}
