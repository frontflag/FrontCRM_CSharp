using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>
/// SuperAdmin 隐蔽运维页 API。非 SA / 未登录一律 404（不暴露能力）。
/// </summary>
[ApiController]
[Route("api/v1/debug/super")]
public class DebugSuperController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IRbacService _rbacService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly IOperationLogQueryService _operationLogQuery;
    private readonly ILogger<DebugSuperController> _logger;

    public DebugSuperController(
        IUserService userService,
        IRbacService rbacService,
        IUnitOfWork unitOfWork,
        ILogOperationAppendService logOperationAppend,
        IOperationLogQueryService operationLogQuery,
        ILogger<DebugSuperController> logger)
    {
        _userService = userService;
        _rbacService = rbacService;
        _unitOfWork = unitOfWork;
        _logOperationAppend = logOperationAppend;
        _operationLogQuery = operationLogQuery;
        _logger = logger;
    }

    private ActionResult<ApiResponse<T>> NotFoundStealth<T>() =>
        NotFound(ApiResponse<T>.Fail("Not Found", 404));

    private ActionResult NotFoundStealth() =>
        NotFound(ApiResponse<object>.Fail("Not Found", 404));

    private async Task<(UserPermissionSummaryDto? summary, string? userId)> TryGetSuperAdminActorAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return (null, null);
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        if (summary?.IsSysAdmin != true)
            return (null, null);
        return (summary, userId);
    }

    /// <summary>分页查询 SuperAdmin 敏感操作记录（仅 SA）。</summary>
    [HttpGet("operation-logs")]
    public async Task<ActionResult<ApiResponse<OperationLogPagedResult>>> ListOperationLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (actor, _) = await TryGetSuperAdminActorAsync();
        if (actor == null) return NotFoundStealth<OperationLogPagedResult>();

        try
        {
            var data = await _operationLogQuery.QueryAsync(new OperationLogQuery
            {
                BizType = SuperAdminOperationLogCodes.BizType,
                AllowSuperAdminBizType = true,
                Page = page,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(ApiResponse<OperationLogPagedResult>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询 SuperAdmin 操作日志失败");
            return StatusCode(500, ApiResponse<OperationLogPagedResult>.Fail("查询失败", 500));
        }
    }

    /// <summary>当前登录 SuperAdmin 修改自己的密码（须验证旧密码）。</summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] DebugSuperChangePasswordRequest? body)
    {
        var (actor, userId) = await TryGetSuperAdminActorAsync();
        if (actor == null || string.IsNullOrWhiteSpace(userId)) return NotFoundStealth();

        if (body == null)
            return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
        if (string.IsNullOrEmpty(body.CurrentPassword))
            return BadRequest(ApiResponse<object>.Fail("请输入当前密码", 400));
        if (string.IsNullOrEmpty(body.NewPassword) || body.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("新密码长度至少 6 位", 400));
        if (string.Equals(body.CurrentPassword, body.NewPassword, StringComparison.Ordinal))
            return BadRequest(ApiResponse<object>.Fail("新密码不能与当前密码相同", 400));

        try
        {
            var user = await _userService.GetByIdForAdminAsync(userId);
            if (user == null) return NotFoundStealth();

            if (!UserPasswordHasher.Verify(body.CurrentPassword, user.PasswordHash))
                return BadRequest(ApiResponse<object>.Fail("当前密码不正确", 400));

            await _userService.ResetPasswordAsync(userId, body.NewPassword);
            await _unitOfWork.SaveChangesAsync();

            await _logOperationAppend.AppendAsync(
                SuperAdminOperationLogCodes.BizType,
                userId,
                user.UserName,
                SuperAdminOperationLogCodes.ChangePassword,
                userId,
                user.UserName,
                $"SuperAdmin「{user.UserName}」更改了自己的登录密码",
                reason: null,
                extraInfo: null);

            return Ok(ApiResponse<object>.Ok(null, "密码已更新"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperAdmin 改密失败 userId={UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("改密失败", 500));
        }
    }

    /// <summary>创建新的 SuperAdmin 账号（仅用户 + SYS_ADMIN；部门可选空）。</summary>
    [HttpPost("create-super-admin")]
    public async Task<ActionResult<ApiResponse<object>>> CreateSuperAdmin(
        [FromBody] DebugSuperCreateRequest? body)
    {
        var (actor, actorId) = await TryGetSuperAdminActorAsync();
        if (actor == null || string.IsNullOrWhiteSpace(actorId)) return NotFoundStealth();

        if (body == null)
            return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
        if (string.IsNullOrWhiteSpace(body.UserName))
            return BadRequest(ApiResponse<object>.Fail("账号不能为空", 400));
        if (string.IsNullOrEmpty(body.Password) || body.Password.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("密码长度至少 6 位", 400));

        try
        {
            var userName = body.UserName.Trim();
            if (await _userService.IsUserNameExistsAsync(userName))
                return BadRequest(ApiResponse<object>.Fail("账号已存在", 400));

            var roles = await _rbacService.GetRolesAsync();
            var saRole = roles.FirstOrDefault(r =>
                string.Equals(r.RoleCode, ManagementRoleCodes.SuperAdmin, StringComparison.OrdinalIgnoreCase));
            if (saRole == null)
                return StatusCode(500, ApiResponse<object>.Fail("系统未配置 SYS_ADMIN 角色", 500));

            var created = await _userService.CreateAsync(new CreateUserRequest
            {
                UserName = userName,
                Password = body.Password,
                RealName = string.IsNullOrWhiteSpace(body.RealName) ? null : body.RealName.Trim(),
                Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim()
            });
            await _unitOfWork.SaveChangesAsync();
            await _rbacService.AssignUserRolesAsync(created.Id, new[] { saRole.Id });

            var actorUser = await _userService.GetByIdForAdminAsync(actorId);
            var actorName = actorUser?.UserName ?? actor.UserId;

            await _logOperationAppend.AppendAsync(
                SuperAdminOperationLogCodes.BizType,
                created.Id,
                created.UserName,
                SuperAdminOperationLogCodes.CreateSuperAdmin,
                actorId,
                actorName,
                $"SuperAdmin「{actorName}」创建了新 SuperAdmin 账号「{created.UserName}」",
                reason: null,
                extraInfo: null);

            return Ok(ApiResponse<object>.Ok(new
            {
                id = created.Id,
                userName = created.UserName,
                realName = created.RealName,
                email = created.Email
            }, "已创建 SuperAdmin"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 SuperAdmin 失败");
            return StatusCode(500, ApiResponse<object>.Fail("创建失败", 500));
        }
    }
}

public sealed class DebugSuperChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class DebugSuperCreateRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? Email { get; set; }
}
