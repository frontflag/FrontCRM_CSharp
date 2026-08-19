using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/user-levels")]
public class UserLevelsController : ControllerBase
{
    private readonly IUserLevelService _userLevels;
    private readonly IRbacService _rbacService;
    private readonly IRepository<RbacUserRole> _userRoleRepo;
    private readonly IRepository<RbacRole> _roleRepo;
    private readonly ILogger<UserLevelsController> _logger;

    public UserLevelsController(
        IUserLevelService userLevels,
        IRbacService rbacService,
        IRepository<RbacUserRole> userRoleRepo,
        IRepository<RbacRole> roleRepo,
        ILogger<UserLevelsController> logger)
    {
        _userLevels = userLevels;
        _rbacService = rbacService;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _logger = logger;
    }

    public sealed class ChangeUserLevelRequest
    {
        public short Level { get; set; }
        public string? Remark { get; set; }
    }

    public sealed class UserLevelHistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public short OldLevel { get; set; }
        public short NewLevel { get; set; }
        public string? Remark { get; set; }
        public DateTime ChangeTime { get; set; }
        public string? OperatorUserId { get; set; }
        public string? OperatorUserName { get; set; }
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<ApiResponse<object>>> Change(string userId, [FromBody] ChangeUserLevelRequest request)
    {
        try
        {
            var actor = await GetActorAsync();
            if (actor == null) return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            if (!actor.HasPermissionCode(SystemPermissionCodes.OrgUsersWrite)
                && !actor.HasPermissionCode(SystemPermissionCodes.LegacyRbacManage)
                && !actor.IsSysAdmin)
                return StatusCode(403, ApiResponse<object>.Fail($"无权限访问: {SystemPermissionCodes.OrgUsersWrite}", 403));

            var roleCodes = await GetRoleCodesAsync(userId);
            if (!ManagementAccountPolicy.CanMaintainTarget(actor, roleCodes))
                return NotFound(ApiResponse<object>.Fail("用户不存在", 404));

            var operatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _userLevels.ChangeAsync(userId, request.Level, request.Remark, operatorId);
            return Ok(ApiResponse<object>.Ok(result, result.LevelChanged ? "等级已更新" : "备注已保存"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.Fail("用户不存在", 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户等级失败 {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail($"更新用户等级失败: {ex.Message}", 500));
        }
    }

    [HttpGet("{userId}/history")]
    public async Task<ActionResult<ApiResponse<object>>> History(string userId)
    {
        try
        {
            var actor = await GetActorAsync();
            if (actor == null) return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            if (!actor.HasPermissionCode(SystemPermissionCodes.OrgUsersRead)
                && !actor.HasPermissionCode(SystemPermissionCodes.LegacyRbacManage)
                && !actor.IsSysAdmin)
                return StatusCode(403, ApiResponse<object>.Fail($"无权限访问: {SystemPermissionCodes.OrgUsersRead}", 403));

            var roleCodes = await GetRoleCodesAsync(userId);
            if (!ManagementAccountPolicy.CanMaintainTarget(actor, roleCodes))
                return NotFound(ApiResponse<object>.Fail("用户不存在", 404));

            var rows = await _userLevels.GetHistoryAsync(userId);
            var dto = rows.Select(x => new UserLevelHistoryItemDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.UserName,
                OldLevel = x.OldLevel,
                NewLevel = x.NewLevel,
                Remark = x.Remark,
                ChangeTime = x.ChangeTime,
                OperatorUserId = x.OperatorUserId,
                OperatorUserName = x.OperatorUserName
            }).ToList();
            return Ok(ApiResponse<object>.Ok(dto, "获取等级变更记录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户等级履历失败 {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail($"获取等级变更记录失败: {ex.Message}", 500));
        }
    }

    private async Task<UserPermissionSummaryDto?> GetActorAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return await _rbacService.GetUserPermissionSummaryAsync(userId);
    }

    private async Task<List<string>> GetRoleCodesAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return new List<string>();
        var userRoles = (await _userRoleRepo.FindAsync(x => x.UserId == userId)).ToList();
        var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
        if (roleIds.Count == 0) return new List<string>();
        var roles = (await _roleRepo.FindAsync(x => roleIds.Contains(x.Id))).ToList();
        var dict = roles.ToDictionary(r => r.Id, r => r);
        return userRoles
            .Select(ur => dict.TryGetValue(ur.RoleId, out var r) ? r.RoleCode : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct()
            .ToList();
    }
}
