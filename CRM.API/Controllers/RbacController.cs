using CRM.API.Models.DTOs;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Rbac;
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
        private readonly IUserService _userService;
        private readonly ILogger<RbacController> _logger;
        private readonly IRepository<RbacRole> _roleRepo;
        private readonly IRepository<RbacPermission> _permissionRepo;
        private readonly IRepository<RbacDepartment> _departmentRepo;
        private readonly IRepository<RbacUserRole> _userRoleRepo;
        private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
        private readonly IRepository<RbacRolePermission> _rolePermissionRepo;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// 员工/部门账号可分配角色（与前端「员工编辑」一致）。
        /// 含 SYS_ADMIN；含种子中的业务扩展角色（可与部门三角色并存，如采购员 purchase_buyer）。
        /// </summary>
        private static readonly HashSet<string> AssignableUserRoleCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "DEPT_DIRECTOR",
            "DEPT_MANAGER",
            "DEPT_EMPLOYEE",
            "SYS_ADMIN",
            "purchase_buyer",
            "biz_all",
            "sales_operator",
            "purchase_operator",
            "commerce_operator",
            "purchase_ops_operator",
            "logistics_operator",
            "finance_operator"
        };

        public RbacController(
            IRbacService rbacService,
            IUserService userService,
            IRepository<RbacRole> roleRepo,
            IRepository<RbacPermission> permissionRepo,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserRole> userRoleRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RbacRolePermission> rolePermissionRepo,
            IUnitOfWork unitOfWork,
            ILogger<RbacController> logger)
        {
            _rbacService = rbacService;
            _userService = userService;
            _logger = logger;
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
            _departmentRepo = departmentRepo;
            _userRoleRepo = userRoleRepo;
            _userDepartmentRepo = userDepartmentRepo;
            _rolePermissionRepo = rolePermissionRepo;
            _unitOfWork = unitOfWork;
        }

        private async Task<ActionResult<ApiResponse<object>>?> ValidateUserRoleIdsAsync(IReadOnlyList<string>? roleIds)
        {
            if (roleIds == null || roleIds.Count == 0) return null;
            var distinctIds = roleIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinctIds.Count == 0) return null;

            var found = (await _roleRepo.FindAsync(x => distinctIds.Contains(x.Id))).ToList();
            if (found.Count != distinctIds.Count)
                return BadRequest(ApiResponse<object>.Fail("存在无效的角色 ID", 400));

            foreach (var r in found)
            {
                var code = r.RoleCode ?? string.Empty;
                if (!AssignableUserRoleCodes.Contains(code))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        $"不允许分配的角色: {code}。请使用部门角色 DEPT_*、系统管理员 SYS_ADMIN，或业务扩展角色（如 purchase_buyer、finance_operator 等）。",
                        400));
                }
            }

            return null;
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

        public class UpsertDepartmentRequest
        {
            public string DepartmentName { get; set; } = string.Empty;
            public string? ParentId { get; set; }
            public short SaleDataScope { get; set; }
            public short PurchaseDataScope { get; set; }
            public short IdentityType { get; set; }
            public short Status { get; set; } = 1;
        }

        [HttpPost("admin/departments")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdminDepartment([FromBody] UpsertDepartmentRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.DepartmentName))
                    return BadRequest(ApiResponse<object>.Fail("部门名称不能为空", 400));

                var created = await _rbacService.CreateDepartmentAsync(
                    request.DepartmentName.Trim(),
                    request.ParentId,
                    request.SaleDataScope,
                    request.PurchaseDataScope,
                    request.IdentityType,
                    request.Status);

                return Ok(ApiResponse<object>.Ok(created, "创建部门成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建部门失败");
                return StatusCode(500, ApiResponse<object>.Fail($"创建部门失败: {ex.Message}", 500));
            }
        }

        [HttpPut("admin/departments/{departmentId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateAdminDepartment(
            string departmentId,
            [FromBody] UpsertDepartmentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(departmentId))
                    return BadRequest(ApiResponse<object>.Fail("departmentId 不能为空", 400));
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.DepartmentName))
                    return BadRequest(ApiResponse<object>.Fail("部门名称不能为空", 400));

                var updated = await _rbacService.UpdateDepartmentAsync(
                    departmentId.Trim(),
                    request.DepartmentName.Trim(),
                    request.ParentId,
                    request.SaleDataScope,
                    request.PurchaseDataScope,
                    request.IdentityType,
                    request.Status);

                if (updated == null)
                    return NotFound(ApiResponse<object>.Fail("部门不存在", 404));

                return Ok(ApiResponse<object>.Ok(updated, "更新部门成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新部门失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新部门失败: {ex.Message}", 500));
            }
        }

        // ======== 用户管理（列表/新建/编辑/删除）========

        public class AdminUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? RealName { get; set; }
            public string? Email { get; set; }
            public string? Mobile { get; set; }
            public short Status { get; set; }
            public IReadOnlyList<string> RoleIds { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
            public string? PrimaryDepartmentId { get; set; }
            public string? PrimaryDepartmentName { get; set; }
            public string? PrimaryDepartmentPath { get; set; }
        }

        public class CreateAdminUserRequest
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? RealName { get; set; }
            public string? Email { get; set; }
            public string? Mobile { get; set; }
            public short Status { get; set; } = 1;
            public IReadOnlyList<string>? RoleIds { get; set; }
            public IReadOnlyList<string>? DepartmentIds { get; set; }
            public string? PrimaryDepartmentId { get; set; }
        }

        public class UpdateAdminUserRequest
        {
            public string? RealName { get; set; }
            public string? Email { get; set; }
            public string? Mobile { get; set; }
            public short? Status { get; set; }
            public IReadOnlyList<string>? RoleIds { get; set; }
            public IReadOnlyList<string>? DepartmentIds { get; set; }
            public string? PrimaryDepartmentId { get; set; }
        }

        public class ResetAdminUserPasswordRequest
        {
            public string NewPassword { get; set; } = string.Empty;
        }

        private async Task<AdminUserDto?> BuildAdminUserDtoAsync(string userId)
        {
            var user = await _userService.GetByIdForAdminAsync(userId);
            if (user == null) return null;

            var userRoles = (await _userRoleRepo.FindAsync(x => x.UserId == userId)).ToList();
            var userDepartments = (await _userDepartmentRepo.FindAsync(x => x.UserId == userId)).ToList();

            var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
            var deptIds = userDepartments.Select(x => x.DepartmentId).Distinct().ToList();

            var roles = roleIds.Count > 0 ? (await _roleRepo.FindAsync(x => roleIds.Contains(x.Id))).ToList() : new List<RbacRole>();
            var depts = deptIds.Count > 0 ? (await _departmentRepo.FindAsync(x => deptIds.Contains(x.Id))).ToList() : new List<RbacDepartment>();

            var roleDict = roles.ToDictionary(r => r.Id, r => r);
            var deptDict = depts.ToDictionary(d => d.Id, d => d);

            var rolesCodes = userRoles
                .Select(ur => roleDict.TryGetValue(ur.RoleId, out var r) ? r.RoleCode : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToList();

            var departmentIds = userDepartments.Select(x => x.DepartmentId).Distinct().ToList();
            var primaryDept = userDepartments.FirstOrDefault(x => x.IsPrimary) ?? userDepartments.FirstOrDefault();

            deptDict.TryGetValue(primaryDept?.DepartmentId ?? string.Empty, out var primaryDeptEntity);

            return new AdminUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                RealName = user.RealName,
                Email = user.Email,
                Mobile = user.Mobile,
                Status = user.Status,
                RoleIds = userRoles.Select(x => x.RoleId).Distinct().ToList(),
                RoleCodes = rolesCodes,
                DepartmentIds = departmentIds,
                PrimaryDepartmentId = primaryDept?.DepartmentId,
                PrimaryDepartmentName = primaryDeptEntity?.DepartmentName,
                PrimaryDepartmentPath = primaryDeptEntity?.Path
            };
        }

        [HttpGet("admin/users")]
        public async Task<ActionResult<ApiResponse<object>>> GetAdminUsers()
        {
            try
            {
                var users = (await _userService.GetAllForAdminAsync()).ToList();
                var userIds = users.Select(u => u.Id).ToList();

                if (userIds.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<AdminUserDto>(), "获取用户列表成功"));

                var userRoles = (await _userRoleRepo.FindAsync(x => userIds.Contains(x.UserId))).ToList();
                var userDepartments = (await _userDepartmentRepo.FindAsync(x => userIds.Contains(x.UserId))).ToList();

                var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
                var deptIds = userDepartments.Select(x => x.DepartmentId).Distinct().ToList();

                var roles = roleIds.Count > 0 ? (await _roleRepo.FindAsync(x => roleIds.Contains(x.Id))).ToList() : new List<RbacRole>();
                var departments = deptIds.Count > 0 ? (await _departmentRepo.FindAsync(x => deptIds.Contains(x.Id))).ToList() : new List<RbacDepartment>();

                var roleDict = roles.ToDictionary(r => r.Id, r => r);
                var deptDict = departments.ToDictionary(d => d.Id, d => d);

                var result = users.Select(u =>
                {
                    var rolesForUser = userRoles.Where(ur => ur.UserId == u.Id).ToList();
                    var deptsForUser = userDepartments.Where(ud => ud.UserId == u.Id).ToList();

                    var roleCodes = rolesForUser
                        .Select(ur => roleDict.TryGetValue(ur.RoleId, out var r) ? r.RoleCode : null)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .Distinct()
                        .ToList();

                    var departmentIds = deptsForUser.Select(x => x.DepartmentId).Distinct().ToList();
                    var primaryDept = deptsForUser.FirstOrDefault(x => x.IsPrimary) ?? deptsForUser.FirstOrDefault();
                    deptDict.TryGetValue(primaryDept?.DepartmentId ?? string.Empty, out var primaryDeptEntity);

                    return new AdminUserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        RealName = u.RealName,
                        Email = u.Email,
                        Mobile = u.Mobile,
                        Status = u.Status,
                        RoleIds = rolesForUser.Select(x => x.RoleId).Distinct().ToList(),
                        RoleCodes = roleCodes,
                        DepartmentIds = departmentIds,
                        PrimaryDepartmentId = primaryDept?.DepartmentId,
                        PrimaryDepartmentName = primaryDeptEntity?.DepartmentName,
                        PrimaryDepartmentPath = primaryDeptEntity?.Path
                    };
                }).ToList();

                return Ok(ApiResponse<object>.Ok(result, "获取用户列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取用户列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("admin/users/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> GetAdminUserById(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));
                var dto = await BuildAdminUserDtoAsync(userId);
                if (dto == null)
                    return NotFound(ApiResponse<object>.Fail("用户不存在", 404));

                return Ok(ApiResponse<object>.Ok(dto, "获取用户详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取用户详情失败: {ex.Message}", 500));
            }
        }

        [HttpPost("admin/users")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdminUser([FromBody] CreateAdminUserRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return BadRequest(ApiResponse<object>.Fail("UserName 不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(ApiResponse<object>.Fail("Password 不能为空", 400));

                var roleCheck = await ValidateUserRoleIdsAsync(request.RoleIds);
                if (roleCheck != null) return roleCheck;

                var roleId = request.RoleIds?.FirstOrDefault();
                var deptId = request.DepartmentIds?.FirstOrDefault();

                var created = await _userService.CreateAsync(new CreateUserRequest
                {
                    UserName = request.UserName,
                    Password = request.Password,
                    RealName = request.RealName,
                    Email = request.Email,
                    Mobile = request.Mobile,
                    RoleId = roleId,
                    DepartmentId = deptId
                });

                // IUserService 当前简化实现不会主动 SaveChanges
                await _unitOfWork.SaveChangesAsync();

                if (request.RoleIds != null && request.RoleIds.Count > 0)
                    await _rbacService.AssignUserRolesAsync(created.Id, request.RoleIds);

                if (request.DepartmentIds != null && request.DepartmentIds.Count > 0)
                    await _rbacService.AssignUserDepartmentsAsync(created.Id, request.DepartmentIds, request.PrimaryDepartmentId);

                // 角色/部门赋权如果未提供，不做处理

                var userDto = await BuildAdminUserDtoAsync(created.Id);
                return Ok(ApiResponse<object>.Ok(userDto, "创建用户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"创建用户失败: {ex.Message}", 500));
            }
        }

        [HttpPut("admin/users/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateAdminUser(string userId, [FromBody] UpdateAdminUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                await _userService.UpdateAsync(userId, new UpdateUserRequest
                {
                    RealName = request.RealName,
                    Email = request.Email,
                    Mobile = request.Mobile,
                    RoleId = request.RoleIds?.FirstOrDefault(),
                    DepartmentId = request.DepartmentIds?.FirstOrDefault()
                });

                if (request.Status.HasValue)
                    await _userService.UpdateStatusAsync(userId, request.Status.Value);

                // 保存用户基础信息
                await _unitOfWork.SaveChangesAsync();

                if (request.RoleIds != null)
                {
                    var roleCheck = await ValidateUserRoleIdsAsync(request.RoleIds);
                    if (roleCheck != null) return roleCheck;
                    await _rbacService.AssignUserRolesAsync(userId, request.RoleIds);
                }

                if (request.DepartmentIds != null)
                    await _rbacService.AssignUserDepartmentsAsync(userId, request.DepartmentIds, request.PrimaryDepartmentId);

                var userDto = await BuildAdminUserDtoAsync(userId);
                return Ok(ApiResponse<object>.Ok(userDto, "更新用户成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新用户失败: {ex.Message}", 500));
            }
        }

        /// <summary>管理员为员工重置登录密码（需 rbac.manage）。</summary>
        [HttpPost("admin/users/{userId}/reset-password")]
        public async Task<ActionResult<ApiResponse<object>>> ResetAdminUserPassword(string userId, [FromBody] ResetAdminUserPasswordRequest? request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));
                if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest(ApiResponse<object>.Fail("NewPassword 不能为空", 400));
                if (request.NewPassword.Length < 6)
                    return BadRequest(ApiResponse<object>.Fail("新密码长度至少 6 位", 400));

                await _userService.ResetPasswordAsync(userId, request.NewPassword);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "密码已重置"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败 userId={UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail($"重置密码失败: {ex.Message}", 500));
            }
        }

        [HttpPost("admin/users/{userId}/freeze")]
        public async Task<ActionResult<ApiResponse<object>>> FreezeAdminUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));
                var operatorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(operatorUserId) &&
                    string.Equals(operatorUserId.Trim(), userId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return BadRequest(ApiResponse<object>.Fail("不能冻结当前登录账号", 400));

                await _userService.FreezeUserAsync(userId);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "已冻结该员工"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "冻结用户失败 userId={UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail($"冻结失败: {ex.Message}", 500));
            }
        }

        [HttpPost("admin/users/{userId}/unfreeze")]
        public async Task<ActionResult<ApiResponse<object>>> UnfreezeAdminUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));

                await _userService.UnfreezeUserAsync(userId);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "已恢复该员工"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复用户失败 userId={UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail($"恢复失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("admin/users/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAdminUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(ApiResponse<object>.Fail("userId 不能为空", 400));

                var operatorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("DeleteAdminUser start: targetUserId={TargetUserId}, operatorUserId={OperatorUserId}", userId, operatorUserId);

                // 先清理用户与角色/部门的关联，避免出现孤儿关联
                var userRoles = (await _userRoleRepo.FindAsync(x => x.UserId == userId)).ToList();
                _logger.LogInformation("DeleteAdminUser cleaning user roles: targetUserId={TargetUserId}, roleCount={RoleCount}", userId, userRoles.Count);
                foreach (var ur in userRoles)
                    await _userRoleRepo.DeleteAsync(ur.Id);

                var userDepartments = (await _userDepartmentRepo.FindAsync(x => x.UserId == userId)).ToList();
                _logger.LogInformation("DeleteAdminUser cleaning user departments: targetUserId={TargetUserId}, departmentRelationCount={DepartmentRelationCount}", userId, userDepartments.Count);
                foreach (var ud in userDepartments)
                    await _userDepartmentRepo.DeleteAsync(ud.Id);

                _logger.LogInformation("DeleteAdminUser deleting user entity: targetUserId={TargetUserId}", userId);
                await _userService.DeleteAsync(userId);

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("DeleteAdminUser success: targetUserId={TargetUserId}", userId);
                return Ok(ApiResponse<object>.Ok(null, "删除用户成功"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "DeleteAdminUser not found: targetUserId={TargetUserId}", userId);
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAdminUser failed: targetUserId={TargetUserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail($"删除用户失败: {ex.Message}", 500));
            }
        }

        // ======== 角色管理（列表/新建/编辑/删除）========

        public class CreateAdminRoleRequest
        {
            public string RoleCode { get; set; } = string.Empty;
            public string RoleName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public short Status { get; set; } = 1;
        }

        public class UpdateAdminRoleRequest
        {
            public string? RoleCode { get; set; }
            public string? RoleName { get; set; }
            public string? Description { get; set; }
            public short? Status { get; set; }
        }

        [HttpPost("admin/roles")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdminRole([FromBody] CreateAdminRoleRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.RoleCode))
                    return BadRequest(ApiResponse<object>.Fail("RoleCode 不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.RoleName))
                    return BadRequest(ApiResponse<object>.Fail("RoleName 不能为空", 400));

                var role = new RbacRole
                {
                    RoleCode = request.RoleCode.Trim(),
                    RoleName = request.RoleName.Trim(),
                    Description = request.Description,
                    Status = request.Status
                };

                await _roleRepo.AddAsync(role);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(role, "创建角色成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败");
                return StatusCode(500, ApiResponse<object>.Fail($"创建角色失败: {ex.Message}", 500));
            }
        }

        [HttpPut("admin/roles/{roleId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateAdminRole(string roleId, [FromBody] UpdateAdminRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                    return BadRequest(ApiResponse<object>.Fail("roleId 不能为空", 400));
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var role = await _roleRepo.GetByIdAsync(roleId);
                if (role == null)
                    return NotFound(ApiResponse<object>.Fail("角色不存在", 404));

                if (request.RoleCode != null) role.RoleCode = request.RoleCode.Trim();
                if (request.RoleName != null) role.RoleName = request.RoleName.Trim();
                if (request.Description != null) role.Description = request.Description;
                if (request.Status.HasValue) role.Status = request.Status.Value;

                await _roleRepo.UpdateAsync(role);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(role, "更新角色成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新角色失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新角色失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("admin/roles/{roleId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAdminRole(string roleId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                    return BadRequest(ApiResponse<object>.Fail("roleId 不能为空", 400));

                // 清理关联：用户角色、角色权限
                var userRoles = (await _userRoleRepo.FindAsync(x => x.RoleId == roleId)).ToList();
                foreach (var ur in userRoles)
                    await _userRoleRepo.DeleteAsync(ur.Id);

                var rolePermissions = (await _rolePermissionRepo.FindAsync(x => x.RoleId == roleId)).ToList();
                foreach (var rp in rolePermissions)
                    await _rolePermissionRepo.DeleteAsync(rp.Id);

                await _roleRepo.DeleteAsync(roleId);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "删除角色成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除角色失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除角色失败: {ex.Message}", 500));
            }
        }

        [HttpGet("admin/roles/{roleId}/permissions")]
        public async Task<ActionResult<ApiResponse<object>>> GetRolePermissionIds(string roleId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                    return BadRequest(ApiResponse<object>.Fail("roleId 不能为空", 400));

                var mappings = (await _rolePermissionRepo.FindAsync(x => x.RoleId == roleId)).ToList();
                var ids = mappings.Select(x => x.PermissionId).Distinct().ToList();
                return Ok(ApiResponse<object>.Ok(ids, "获取角色权限成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色权限失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取角色权限失败: {ex.Message}", 500));
            }
        }

        // ======== 权限管理（列表/新建/编辑/删除）========

        public class CreateAdminPermissionRequest
        {
            public string PermissionCode { get; set; } = string.Empty;
            public string PermissionName { get; set; } = string.Empty;
            public string PermissionType { get; set; } = "api";
            public string? Resource { get; set; }
            public string? Action { get; set; }
            public short Status { get; set; } = 1;
        }

        public class UpdateAdminPermissionRequest
        {
            public string? PermissionCode { get; set; }
            public string? PermissionName { get; set; }
            public string? PermissionType { get; set; }
            public string? Resource { get; set; }
            public string? Action { get; set; }
            public short? Status { get; set; }
        }

        [HttpPost("admin/permissions")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdminPermission([FromBody] CreateAdminPermissionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.PermissionCode))
                    return BadRequest(ApiResponse<object>.Fail("PermissionCode 不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.PermissionName))
                    return BadRequest(ApiResponse<object>.Fail("PermissionName 不能为空", 400));

                var permission = new RbacPermission
                {
                    PermissionCode = request.PermissionCode.Trim(),
                    PermissionName = request.PermissionName.Trim(),
                    PermissionType = string.IsNullOrWhiteSpace(request.PermissionType) ? "api" : request.PermissionType.Trim(),
                    Resource = request.Resource,
                    Action = request.Action,
                    Status = request.Status
                };

                await _permissionRepo.AddAsync(permission);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(permission, "创建权限成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建权限失败");
                return StatusCode(500, ApiResponse<object>.Fail($"创建权限失败: {ex.Message}", 500));
            }
        }

        [HttpPut("admin/permissions/{permissionId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateAdminPermission(string permissionId, [FromBody] UpdateAdminPermissionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permissionId))
                    return BadRequest(ApiResponse<object>.Fail("permissionId 不能为空", 400));
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var permission = await _permissionRepo.GetByIdAsync(permissionId);
                if (permission == null)
                    return NotFound(ApiResponse<object>.Fail("权限不存在", 404));

                if (request.PermissionCode != null) permission.PermissionCode = request.PermissionCode.Trim();
                if (request.PermissionName != null) permission.PermissionName = request.PermissionName.Trim();
                if (request.PermissionType != null) permission.PermissionType = request.PermissionType.Trim();
                if (request.Resource != null) permission.Resource = request.Resource;
                if (request.Action != null) permission.Action = request.Action;
                if (request.Status.HasValue) permission.Status = request.Status.Value;

                await _permissionRepo.UpdateAsync(permission);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(permission, "更新权限成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新权限失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新权限失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("admin/permissions/{permissionId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAdminPermission(string permissionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permissionId))
                    return BadRequest(ApiResponse<object>.Fail("permissionId 不能为空", 400));

                // 清理关联：角色权限
                var rolePermissions = (await _rolePermissionRepo.FindAsync(x => x.PermissionId == permissionId)).ToList();
                foreach (var rp in rolePermissions)
                    await _rolePermissionRepo.DeleteAsync(rp.Id);

                await _permissionRepo.DeleteAsync(permissionId);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(null, "删除权限成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除权限失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除权限失败: {ex.Message}", 500));
            }
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
