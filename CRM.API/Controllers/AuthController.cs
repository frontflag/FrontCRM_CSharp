using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;
        private readonly IRepository<RbacDepartment> _departmentRepo;
        private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
        private readonly IRepository<RbacUserRole> _userRoleRepo;
        private readonly IRepository<RbacRole> _roleRepo;

        public AuthController(
            IAuthService authService,
            IRbacService rbacService,
            ILogger<AuthController> logger,
            IUserService userService,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RbacUserRole> userRoleRepo,
            IRepository<RbacRole> roleRepo)
        {
            _authService = authService;
            _rbacService = rbacService;
            _logger = logger;
            _userService = userService;
            _departmentRepo = departmentRepo;
            _userDepartmentRepo = userDepartmentRepo;
            _userRoleRepo = userRoleRepo;
            _roleRepo = roleRepo;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail("注册失败", 500));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<AuthResponse>.Fail($"登录失败: {ex.Message}", 500));
            }
        }

        /// <summary>系统管理员模拟登录为指定员工账号（无需密码）。Body: { "userId": "..." }</summary>
        [Authorize]
        [HttpPost("impersonate")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Impersonate([FromBody] ImpersonateRequest? request)
        {
            try
            {
                var userId = request?.UserId?.Trim() ?? "";
                var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var result = await _authService.ImpersonateAsync(actorId ?? "", userId);
                if (!result.Success)
                {
                    if (result.ErrorCode == 403 || result.ErrorCode == 401)
                        return StatusCode(result.ErrorCode, result);
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impersonate failed");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail($"模拟登录失败: {ex.Message}", 500));
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("用户不存在", 404));

            var rels = (await _userDepartmentRepo.FindAsync(x => x.UserId == userId)).ToList();
            var deptIdOrder = rels
                .OrderByDescending(x => x.IsPrimary)
                .Select(x => x.DepartmentId?.Trim() ?? string.Empty)
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string? department = null;
            if (deptIdOrder.Count > 0)
            {
                var deptRows = (await _departmentRepo.FindAsync(d => deptIdOrder.Contains(d.Id))).ToList();
                var map = deptRows.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
                var names = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var did in deptIdOrder)
                {
                    if (!map.TryGetValue(did, out var dep)) continue;
                    var n = (dep.DepartmentName ?? string.Empty).Trim();
                    if (n.Length == 0 || seen.Contains(n)) continue;
                    seen.Add(n);
                    names.Add(n);
                }

                department = names.Count == 0 ? null : string.Join("、", names);
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email ?? string.Empty,
                realName = user.RealName,
                mobile = user.Mobile ?? string.Empty,
                department
            }, "获取用户信息成功"));
        }

        [Authorize]
        [HttpGet("permission-summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetPermissionSummary()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                return Ok(ApiResponse<object>.Ok(summary, "获取权限摘要成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPermissionSummary failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取权限摘要失败", 500));
            }
        }

        /// <summary>获取业务员列表（用于下拉选择）</summary>
        [Authorize]
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<object>>> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                var list = users
                    .Where(u => u.Status == 1)
                    .Select(u => new
                    {
                        id = u.Id,
                        label = string.IsNullOrWhiteSpace(u.RealName) ? u.UserName : u.RealName,
                        userName = u.UserName,
                        realName = u.RealName
                    });
                return Ok(ApiResponse<object>.Ok(list, "获取用户列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUsers failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取用户列表失败", 500));
            }
        }

        public class SalesUserTreeNodeDto
        {
            public string Value { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public bool IsUser { get; set; }

            /// <summary>仅人员节点：登录账号，供前端拼接展示</summary>
            public string? UserName { get; set; }

            /// <summary>仅人员节点：真实姓名</summary>
            public string? RealName { get; set; }

            public IReadOnlyList<SalesUserTreeNodeDto> Children { get; set; } = Array.Empty<SalesUserTreeNodeDto>();
        }

        /// <summary>
        /// 获取销售业务员树（仅销售相关部门）。登录用户在销售部门时：仅自己及下属（按组织锚点与职级）；
        /// 不在销售部门时：展示全部销售相关部门下的用户。系统管理员为全部挂部门用户后再按销售树过滤。
        /// </summary>
        [Authorize]
        [HttpGet("sales-users-tree")]
        public async Task<ActionResult<ApiResponse<object>>> GetSalesUsersTree()
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(currentUserId);
                _logger.LogInformation(
                    "sales-users-tree: start userId={UserId} isSysAdmin={IsSysAdmin} primaryDept={PrimaryDeptId} roleCodes={RoleCodes} identityType={IdentityType} saleScope={SaleScope}",
                    currentUserId,
                    summary.IsSysAdmin,
                    summary.PrimaryDepartmentId,
                    string.Join(",", summary.RoleCodes ?? Array.Empty<string>()),
                    summary.IdentityType,
                    summary.SaleDataScope);

                var departments = (await _departmentRepo.GetAllAsync())
                    .Where(d => d.Status == 1)
                    .ToList();
                var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();
                _logger.LogInformation(
                    "sales-users-tree: loaded departments={DeptCount} userDepartments={UserDeptCount}",
                    departments.Count,
                    userDepartments.Count);

                bool IsSalesDepartment(RbacDepartment d)
                {
                    if (d.IdentityType == 1) return true;
                    var name = d.DepartmentName ?? string.Empty;
                    return name.Contains("销售", StringComparison.OrdinalIgnoreCase)
                           || name.Contains("sales", StringComparison.OrdinalIgnoreCase);
                }

                var salesDepartments = departments.Where(IsSalesDepartment).ToList();
                _logger.LogInformation(
                    "sales-users-tree: salesDepartments={SalesDeptCount} (sample={Sample})",
                    salesDepartments.Count,
                    string.Join(" | ", salesDepartments.Take(6).Select(d => $"{d.DepartmentName}({d.IdentityType})#{d.Id}")));
                if (salesDepartments.Count == 0)
                {
                    _logger.LogWarning(
                        "sales-users-tree: no sales departments found. deptNameSamples={DeptNames}",
                        string.Join(" | ", departments.Take(10).Select(d => $"{d.DepartmentName}({d.IdentityType})")));
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取销售业务员树成功"));
                }

                var salesDeptIdSet = salesDepartments.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var salesAnchor = ResolveSalesAnchorDepartmentId(currentUserId, summary, departments, userDepartments);

                HashSet<string> allowedUserIds;
                if (summary.IsSysAdmin)
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, null);
                else if (!string.IsNullOrWhiteSpace(salesAnchor))
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, salesAnchor);
                else
                {
                    // 登录用户不在销售部门：下拉展示全部销售相关部门下的账号（与客户筛选「业务员」一致）
                    allowedUserIds = userDepartments
                        .Where(x => salesDeptIdSet.Contains(x.DepartmentId))
                        .Select(x => x.UserId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

                _logger.LogInformation(
                    "sales-users-tree: allowedUserIds={AllowedCount} (includes self={HasSelf}) salesAnchor={SalesAnchor}",
                    allowedUserIds.Count,
                    allowedUserIds.Contains(currentUserId),
                    salesAnchor ?? "(none)");
                if (allowedUserIds.Count == 0)
                {
                    _logger.LogWarning(
                        "sales-users-tree: empty allowedUserIds. primaryDept={PrimaryDeptId} roleCodes={RoleCodes}",
                        summary.PrimaryDepartmentId,
                        string.Join(",", summary.RoleCodes ?? Array.Empty<string>()));
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取销售业务员树成功"));
                }

                var users = (await _userService.GetAllAsync())
                    .Where(u => u.Status == 1 && allowedUserIds.Contains(u.Id))
                    .ToList();
                _logger.LogInformation("sales-users-tree: scoped active users={UserCount}", users.Count);
                if (users.Count == 0)
                {
                    _logger.LogWarning("sales-users-tree: no active users after allowedUserIds filter");
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取销售业务员树成功"));
                }

                var departmentMap = salesDepartments.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
                var userDepartmentGroups = userDepartments
                    .Where(x => allowedUserIds.Contains(x.UserId))
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
                _logger.LogInformation("sales-users-tree: userDepartmentGroups={GroupCount}", userDepartmentGroups.Count);

                var usersByDepartment = new Dictionary<string, List<User>>(StringComparer.OrdinalIgnoreCase);
                var notInSalesDept = 0;
                var noDeptRel = 0;
                foreach (var user in users)
                {
                    if (!userDepartmentGroups.TryGetValue(user.Id, out var rels) || rels.Count == 0)
                    {
                        noDeptRel++;
                        continue;
                    }

                    // 归属部门优先级：
                    // 1) 主部门且为销售部门
                    // 2) 任意一个销售部门（兼容未设置主部门/主部门非销售的场景）
                    var deptId =
                        rels.FirstOrDefault(x => x.IsPrimary && departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId
                        ?? rels.FirstOrDefault(x => departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId;
                    if (string.IsNullOrWhiteSpace(deptId))
                    {
                        notInSalesDept++;
                        continue;
                    }

                    if (!usersByDepartment.TryGetValue(deptId, out var bucket))
                    {
                        bucket = new List<User>();
                        usersByDepartment[deptId] = bucket;
                    }
                    bucket.Add(user);
                }
                _logger.LogInformation(
                    "sales-users-tree: usersByDepartment deptCount={DeptCount} userCount={UserCount} skipped(noDeptRel={NoDeptRel},notInSalesDept={NotInSalesDept})",
                    usersByDepartment.Count,
                    usersByDepartment.Values.Sum(x => x.Count),
                    noDeptRel,
                    notInSalesDept);

                var salesDepartmentIdsInUse = usersByDepartment.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var candidateDepartments = salesDepartments
                    .Where(d => salesDepartmentIdsInUse.Contains(d.Id))
                    .ToList();
                _logger.LogInformation("sales-users-tree: candidateDepartments={CandidateCount}", candidateDepartments.Count);

                var departmentChildren = candidateDepartments
                    .GroupBy(d => d.ParentId ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartmentName).ToList(), StringComparer.OrdinalIgnoreCase);

                SalesUserTreeNodeDto BuildDepartmentNode(RbacDepartment dept)
                {
                    var childNodes = new List<SalesUserTreeNodeDto>();
                    if (departmentChildren.TryGetValue(dept.Id, out var children))
                    {
                        childNodes.AddRange(children.Select(BuildDepartmentNode));
                    }

                    if (usersByDepartment.TryGetValue(dept.Id, out var departmentUsers))
                    {
                        childNodes.AddRange(departmentUsers
                            .OrderBy(u => u.UserName)
                            .Select(u => new SalesUserTreeNodeDto
                            {
                                Value = u.Id,
                                Label = u.UserName,
                                UserName = u.UserName,
                                RealName = u.RealName,
                                IsUser = true,
                                Children = Array.Empty<SalesUserTreeNodeDto>()
                            }));
                    }

                    return new SalesUserTreeNodeDto
                    {
                        Value = dept.Id,
                        Label = dept.DepartmentName,
                        IsUser = false,
                        Children = childNodes
                    };
                }

                var rootDepartments = candidateDepartments
                    .Where(d => string.IsNullOrWhiteSpace(d.ParentId) || !departmentMap.ContainsKey(d.ParentId!))
                    .OrderBy(d => d.DepartmentName)
                    .ToList();

                var tree = rootDepartments
                    .Select(BuildDepartmentNode)
                    .Where(n => n.Children.Count > 0)
                    .ToList();

                _logger.LogInformation(
                    "sales-users-tree: done roots={RootCount} totalDeptsWithUsers={DeptCount} totalUsers={UserCount}",
                    tree.Count,
                    usersByDepartment.Count,
                    usersByDepartment.Values.Sum(x => x.Count));
                return Ok(ApiResponse<object>.Ok(tree, "获取销售业务员树成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSalesUsersTree failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取销售业务员树失败", 500));
            }
        }

        /// <summary>
        /// 获取采购员树（仅采购相关部门）。登录用户在采购部门时：仅自己及下属（按组织锚点与职级）；
        /// 不在采购部门时：展示全部采购相关部门下的用户。系统管理员为全部挂部门用户后再按采购树过滤。
        /// </summary>
        [Authorize]
        [HttpGet("purchase-users-tree")]
        public async Task<ActionResult<ApiResponse<object>>> GetPurchaseUsersTree()
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(currentUserId);
                var departments = (await _departmentRepo.GetAllAsync())
                    .Where(d => d.Status == 1)
                    .ToList();
                var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();

                var purchaseDepartments = departments.Where(PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer).ToList();
                if (purchaseDepartments.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取采购员树成功"));

                var purchaseDeptIdSet = purchaseDepartments.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var anchor = ResolvePurchaseAnchorDepartmentId(currentUserId, summary, departments, userDepartments);

                HashSet<string> allowedUserIds;
                if (summary.IsSysAdmin)
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, null);
                else if (!string.IsNullOrWhiteSpace(anchor))
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, anchor);
                else
                {
                    // 登录用户不在采购部门：下拉展示全部采购相关部门下的账号
                    allowedUserIds = userDepartments
                        .Where(x => purchaseDeptIdSet.Contains(x.DepartmentId))
                        .Select(x => x.UserId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

                if (allowedUserIds.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取采购员树成功"));

                var users = (await _userService.GetAllAsync())
                    .Where(u => u.Status == 1 && allowedUserIds.Contains(u.Id))
                    .ToList();
                if (users.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取采购员树成功"));

                var departmentMap = purchaseDepartments.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
                var userDepartmentGroups = userDepartments
                    .Where(x => allowedUserIds.Contains(x.UserId))
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var usersByDepartment = new Dictionary<string, List<User>>(StringComparer.OrdinalIgnoreCase);
                foreach (var user in users)
                {
                    if (!userDepartmentGroups.TryGetValue(user.Id, out var rels) || rels.Count == 0)
                        continue;

                    var deptId =
                        rels.FirstOrDefault(x => x.IsPrimary && departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId
                        ?? rels.FirstOrDefault(x => departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId;
                    if (string.IsNullOrWhiteSpace(deptId))
                        continue;

                    if (!usersByDepartment.TryGetValue(deptId, out var bucket))
                    {
                        bucket = new List<User>();
                        usersByDepartment[deptId] = bucket;
                    }
                    bucket.Add(user);
                }

                var purchaseDepartmentIdsInUse = usersByDepartment.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var candidateDepartments = purchaseDepartments
                    .Where(d => purchaseDepartmentIdsInUse.Contains(d.Id))
                    .ToList();

                var departmentChildren = candidateDepartments
                    .GroupBy(d => d.ParentId ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartmentName).ToList(), StringComparer.OrdinalIgnoreCase);

                SalesUserTreeNodeDto BuildDepartmentNode(RbacDepartment dept)
                {
                    var childNodes = new List<SalesUserTreeNodeDto>();
                    if (departmentChildren.TryGetValue(dept.Id, out var children))
                    {
                        childNodes.AddRange(children.Select(BuildDepartmentNode));
                    }

                    if (usersByDepartment.TryGetValue(dept.Id, out var departmentUsers))
                    {
                        childNodes.AddRange(departmentUsers
                            .OrderBy(u => u.UserName)
                            .Select(u => new SalesUserTreeNodeDto
                            {
                                Value = u.Id,
                                Label = u.UserName,
                                IsUser = true,
                                Children = Array.Empty<SalesUserTreeNodeDto>()
                            }));
                    }

                    return new SalesUserTreeNodeDto
                    {
                        Value = dept.Id,
                        Label = dept.DepartmentName,
                        IsUser = false,
                        Children = childNodes
                    };
                }

                var rootDepartments = candidateDepartments
                    .Where(d => string.IsNullOrWhiteSpace(d.ParentId) || !departmentMap.ContainsKey(d.ParentId!))
                    .OrderBy(d => d.DepartmentName)
                    .ToList();

                var tree = rootDepartments
                    .Select(BuildDepartmentNode)
                    .Where(n => n.Children.Count > 0)
                    .ToList();

                return Ok(ApiResponse<object>.Ok(tree, "获取采购员树成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPurchaseUsersTree failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取采购员树失败", 500));
            }
        }

        /// <summary>
        /// 获取物流部门人员树（仅身份为物流或部门名包含物流/仓储等）。规则与采购员树一致：物流部门登录用户按锚点下级可见，否则展示全部物流相关部门用户。
        /// </summary>
        [Authorize]
        [HttpGet("logistics-users-tree")]
        public async Task<ActionResult<ApiResponse<object>>> GetLogisticsUsersTree()
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return Unauthorized(ApiResponse<object>.Fail("未获取到用户信息", 401));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(currentUserId);
                var departments = (await _departmentRepo.GetAllAsync())
                    .Where(d => d.Status == 1)
                    .ToList();
                var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();

                bool IsLogisticsDepartment(RbacDepartment d)
                {
                    if (d.IdentityType == 6) return true;
                    var name = d.DepartmentName ?? string.Empty;
                    return name.Contains("物流", StringComparison.OrdinalIgnoreCase)
                           || name.Contains("仓储", StringComparison.OrdinalIgnoreCase)
                           || name.Contains("logistics", StringComparison.OrdinalIgnoreCase)
                           || name.Contains("warehouse", StringComparison.OrdinalIgnoreCase);
                }

                var logisticsDepartments = departments.Where(IsLogisticsDepartment).ToList();
                if (logisticsDepartments.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取物流部门人员树成功"));

                var logisticsDeptIdSet = logisticsDepartments.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var anchor = ResolveLogisticsAnchorDepartmentId(currentUserId, summary, departments, userDepartments);

                HashSet<string> allowedUserIds;
                if (summary.IsSysAdmin)
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, null);
                else if (!string.IsNullOrWhiteSpace(anchor))
                    allowedUserIds = await GetAllowedUserIdsAsync(summary, departments, userDepartments, anchor);
                else
                {
                    allowedUserIds = userDepartments
                        .Where(x => logisticsDeptIdSet.Contains(x.DepartmentId))
                        .Select(x => x.UserId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

                if (allowedUserIds.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取物流部门人员树成功"));

                var users = (await _userService.GetAllAsync())
                    .Where(u => u.Status == 1 && allowedUserIds.Contains(u.Id))
                    .ToList();
                if (users.Count == 0)
                    return Ok(ApiResponse<object>.Ok(Array.Empty<SalesUserTreeNodeDto>(), "获取物流部门人员树成功"));

                var departmentMap = logisticsDepartments.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
                var userDepartmentGroups = userDepartments
                    .Where(x => allowedUserIds.Contains(x.UserId))
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var usersByDepartment = new Dictionary<string, List<User>>(StringComparer.OrdinalIgnoreCase);
                foreach (var user in users)
                {
                    if (!userDepartmentGroups.TryGetValue(user.Id, out var rels) || rels.Count == 0)
                        continue;

                    var deptId =
                        rels.FirstOrDefault(x => x.IsPrimary && departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId
                        ?? rels.FirstOrDefault(x => departmentMap.ContainsKey(x.DepartmentId))?.DepartmentId;
                    if (string.IsNullOrWhiteSpace(deptId))
                        continue;

                    if (!usersByDepartment.TryGetValue(deptId, out var bucket))
                    {
                        bucket = new List<User>();
                        usersByDepartment[deptId] = bucket;
                    }
                    bucket.Add(user);
                }

                var logisticsDepartmentIdsInUse = usersByDepartment.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var candidateDepartments = logisticsDepartments
                    .Where(d => logisticsDepartmentIdsInUse.Contains(d.Id))
                    .ToList();

                var departmentChildren = candidateDepartments
                    .GroupBy(d => d.ParentId ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartmentName).ToList(), StringComparer.OrdinalIgnoreCase);

                SalesUserTreeNodeDto BuildDepartmentNode(RbacDepartment dept)
                {
                    var childNodes = new List<SalesUserTreeNodeDto>();
                    if (departmentChildren.TryGetValue(dept.Id, out var children))
                    {
                        childNodes.AddRange(children.Select(BuildDepartmentNode));
                    }

                    if (usersByDepartment.TryGetValue(dept.Id, out var departmentUsers))
                    {
                        childNodes.AddRange(departmentUsers
                            .OrderBy(u => u.UserName)
                            .Select(u => new SalesUserTreeNodeDto
                            {
                                Value = u.Id,
                                Label = u.UserName,
                                UserName = u.UserName,
                                RealName = u.RealName,
                                IsUser = true,
                                Children = Array.Empty<SalesUserTreeNodeDto>()
                            }));
                    }

                    return new SalesUserTreeNodeDto
                    {
                        Value = dept.Id,
                        Label = dept.DepartmentName,
                        IsUser = false,
                        Children = childNodes
                    };
                }

                var rootDepartments = candidateDepartments
                    .Where(d => string.IsNullOrWhiteSpace(d.ParentId) || !departmentMap.ContainsKey(d.ParentId!))
                    .OrderBy(d => d.DepartmentName)
                    .ToList();

                var tree = rootDepartments
                    .Select(BuildDepartmentNode)
                    .Where(n => n.Children.Count > 0)
                    .ToList();

                return Ok(ApiResponse<object>.Ok(tree, "获取物流部门人员树成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetLogisticsUsersTree failed");
                return StatusCode(500, ApiResponse<object>.Fail("获取物流部门人员树失败", 500));
            }
        }

        private static string? ResolveSalesAnchorDepartmentId(
            string userId,
            UserPermissionSummaryDto summary,
            IReadOnlyList<RbacDepartment> departments,
            IReadOnlyList<RbacUserDepartment> userDepartments)
        {
            bool IsSales(RbacDepartment d)
            {
                if (d.IdentityType == 1) return true;
                var n = d.DepartmentName ?? string.Empty;
                return n.Contains("销售", StringComparison.OrdinalIgnoreCase)
                       || n.Contains("sales", StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
            {
                var pd = departments.FirstOrDefault(x => x.Id == summary.PrimaryDepartmentId);
                if (pd != null && IsSales(pd))
                    return summary.PrimaryDepartmentId;
            }

            foreach (var r in userDepartments.Where(x => x.UserId == userId))
            {
                var d = departments.FirstOrDefault(x => x.Id == r.DepartmentId);
                if (d != null && IsSales(d))
                    return r.DepartmentId;
            }

            return null;
        }

        private static string? ResolvePurchaseAnchorDepartmentId(
            string userId,
            UserPermissionSummaryDto summary,
            IReadOnlyList<RbacDepartment> departments,
            IReadOnlyList<RbacUserDepartment> userDepartments)
        {
            if (!string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
            {
                var pd = departments.FirstOrDefault(x => x.Id == summary.PrimaryDepartmentId);
                if (pd != null && PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer(pd))
                    return summary.PrimaryDepartmentId;
            }

            foreach (var r in userDepartments.Where(x => x.UserId == userId))
            {
                var d = departments.FirstOrDefault(x => x.Id == r.DepartmentId);
                if (d != null && PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer(d))
                    return r.DepartmentId;
            }

            return null;
        }

        private static string? ResolveLogisticsAnchorDepartmentId(
            string userId,
            UserPermissionSummaryDto summary,
            IReadOnlyList<RbacDepartment> departments,
            IReadOnlyList<RbacUserDepartment> userDepartments)
        {
            bool IsLogistics(RbacDepartment d)
            {
                if (d.IdentityType == 6) return true;
                var n = d.DepartmentName ?? string.Empty;
                return n.Contains("物流", StringComparison.OrdinalIgnoreCase)
                       || n.Contains("仓储", StringComparison.OrdinalIgnoreCase)
                       || n.Contains("logistics", StringComparison.OrdinalIgnoreCase)
                       || n.Contains("warehouse", StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
            {
                var pd = departments.FirstOrDefault(x => x.Id == summary.PrimaryDepartmentId);
                if (pd != null && IsLogistics(pd))
                    return summary.PrimaryDepartmentId;
            }

            foreach (var r in userDepartments.Where(x => x.UserId == userId))
            {
                var d = departments.FirstOrDefault(x => x.Id == r.DepartmentId);
                if (d != null && IsLogistics(d))
                    return r.DepartmentId;
            }

            return null;
        }

        private async Task<HashSet<string>> GetAllowedUserIdsAsync(
            UserPermissionSummaryDto summary,
            IReadOnlyList<RbacDepartment> departments,
            IReadOnlyList<RbacUserDepartment> userDepartments,
            string? anchorDepartmentIdOverride = null)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(summary.UserId))
                return result;

            result.Add(summary.UserId);
            if (summary.IsSysAdmin)
            {
                foreach (var rel in userDepartments)
                    result.Add(rel.UserId);
                return result;
            }

            var anchorDeptId = anchorDepartmentIdOverride ?? summary.PrimaryDepartmentId;
            if (string.IsNullOrWhiteSpace(anchorDeptId))
                return result;

            var currentDepartment = departments.FirstOrDefault(x => x.Id == anchorDeptId);
            if (currentDepartment == null)
                return result;

            var currentOrgLevel = ResolveOrgRoleLevel(summary.RoleCodes, Array.Empty<string>());
            if (currentOrgLevel <= 0)
                return result;
            if (currentOrgLevel == 1)
                return result;

            var scopedUserDepartments = userDepartments
                .Where(x => IsSubordinateDepartment(currentDepartment, departments.FirstOrDefault(d => d.Id == x.DepartmentId)))
                .ToList();
            var scopedUserIds = scopedUserDepartments.Select(x => x.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var userRoleLevels = await BuildUserRoleLevelMapAsync(scopedUserIds);
            var primaryDeptMap = BuildPrimaryDepartmentMap(scopedUserDepartments);

            foreach (var uid in scopedUserIds)
            {
                if (string.Equals(uid, summary.UserId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!primaryDeptMap.TryGetValue(uid, out var targetDeptId))
                    continue;

                var targetDept = departments.FirstOrDefault(x => x.Id == targetDeptId);
                if (!IsSubordinateDepartment(currentDepartment, targetDept))
                    continue;

                var targetLevel = userRoleLevels.TryGetValue(uid, out var lv) ? lv : 0;
                if (targetLevel <= 0)
                    continue;

                var canSee = currentOrgLevel switch
                {
                    3 => targetLevel <= 2,
                    2 => targetLevel <= 1,
                    _ => false
                };
                if (canSee)
                    result.Add(uid);
            }

            return result;
        }

        private async Task<Dictionary<string, int>> BuildUserRoleLevelMapAsync(IReadOnlyList<string> userIds)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (userIds.Count == 0) return map;

            var userRoles = (await _userRoleRepo.FindAsync(x => userIds.Contains(x.UserId))).ToList();
            if (userRoles.Count == 0) return map;

            var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
            var roleMap = (await _roleRepo.FindAsync(x => roleIds.Contains(x.Id)))
                .ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var g in userRoles.GroupBy(x => x.UserId))
            {
                var codes = new List<string>();
                var names = new List<string>();
                foreach (var ur in g)
                {
                    if (!roleMap.TryGetValue(ur.RoleId, out var role)) continue;
                    codes.Add(role.RoleCode);
                    names.Add(role.RoleName);
                }
                map[g.Key] = ResolveOrgRoleLevel(codes, names);
            }

            return map;
        }

        private static Dictionary<string, string> BuildPrimaryDepartmentMap(IReadOnlyList<RbacUserDepartment> scopedUserDepartments)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in scopedUserDepartments.GroupBy(x => x.UserId))
            {
                var primary = g.FirstOrDefault(x => x.IsPrimary) ?? g.First();
                map[g.Key] = primary.DepartmentId;
            }
            return map;
        }

        private static bool IsSubordinateDepartment(RbacDepartment currentDept, RbacDepartment? targetDept)
        {
            if (targetDept == null) return false;
            if (currentDept.Id == targetDept.Id) return true;
            if (string.IsNullOrWhiteSpace(currentDept.Path) || string.IsNullOrWhiteSpace(targetDept.Path)) return false;
            var prefix = currentDept.Path + "/";
            return targetDept.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>3=总监，2=经理，1=员工，0=未知</summary>
        private static int ResolveOrgRoleLevel(IEnumerable<string> roleCodes, IEnumerable<string> roleNames)
        {
            foreach (var code in roleCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var c = code.Trim().ToUpperInvariant();
                if (c == "DEPT_DIRECTOR") return 3;
                if (c == "DEPT_MANAGER") return 2;
                if (c is "DEPT_EMPLOYEE" or "DEPT_STAFF") return 1;
            }

            var normalized = roleCodes
                .Concat(roleNames)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToList();

            if (normalized.Any(x => x.Contains("DIRECTOR") || x.Contains("总监")))
                return 3;
            if (normalized.Any(x => x.Contains("MANAGER") || x.Contains("经理")))
                return 2;
            if (normalized.Any(x => x.Contains("EMPLOYEE") || x.Contains("STAFF") || x.Contains("员工")))
                return 1;
            return 0;
        }
    }
}
