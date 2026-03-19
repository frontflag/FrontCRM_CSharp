using CRM.API.Models.DTOs;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    [RequirePermission("rbac.manage")]
    public class RbacController : ControllerBase
    {
        private readonly IRbacService _rbacService;
        private readonly ILogger<RbacController> _logger;

        public RbacController(IRbacService rbacService, ILogger<RbacController> logger)
        {
            _rbacService = rbacService;
            _logger = logger;
        }

        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<object>>> GetRoles()
        {
            var roles = await _rbacService.GetRolesAsync();
            return Ok(ApiResponse<object>.Ok(roles, "获取角色列表成功"));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<ApiResponse<object>>> GetPermissions()
        {
            var permissions = await _rbacService.GetPermissionsAsync();
            return Ok(ApiResponse<object>.Ok(permissions, "获取权限列表成功"));
        }

        [HttpGet("departments")]
        public async Task<ActionResult<ApiResponse<object>>> GetDepartments()
        {
            var departments = await _rbacService.GetDepartmentsAsync();
            return Ok(ApiResponse<object>.Ok(departments, "获取部门列表成功"));
        }

        [HttpPost("users/{userId}/roles")]
        public async Task<ActionResult<ApiResponse<object>>> AssignUserRoles(string userId, [FromBody] AssignIdsRequest request)
        {
            try
            {
                await _rbacService.AssignUserRolesAsync(userId, request.Ids ?? Array.Empty<string>());
                return Ok(ApiResponse<object>.Ok(null, "分配用户角色成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配用户角色失败");
                return StatusCode(500, ApiResponse<object>.Fail($"分配用户角色失败: {ex.Message}", 500));
            }
        }

        [HttpPost("users/{userId}/departments")]
        public async Task<ActionResult<ApiResponse<object>>> AssignUserDepartments(string userId, [FromBody] AssignDepartmentsRequest request)
        {
            try
            {
                await _rbacService.AssignUserDepartmentsAsync(userId, request.DepartmentIds ?? Array.Empty<string>(), request.PrimaryDepartmentId);
                return Ok(ApiResponse<object>.Ok(null, "分配用户部门成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配用户部门失败");
                return StatusCode(500, ApiResponse<object>.Fail($"分配用户部门失败: {ex.Message}", 500));
            }
        }

        [HttpPost("roles/{roleId}/permissions")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRolePermissions(string roleId, [FromBody] AssignIdsRequest request)
        {
            try
            {
                await _rbacService.AssignRolePermissionsAsync(roleId, request.Ids ?? Array.Empty<string>());
                return Ok(ApiResponse<object>.Ok(null, "分配角色权限成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配角色权限失败");
                return StatusCode(500, ApiResponse<object>.Fail($"分配角色权限失败: {ex.Message}", 500));
            }
        }
    }

    public class AssignIdsRequest
    {
        public IReadOnlyList<string>? Ids { get; set; }
    }

    public class AssignDepartmentsRequest
    {
        public IReadOnlyList<string>? DepartmentIds { get; set; }
        public string? PrimaryDepartmentId { get; set; }
    }
}
