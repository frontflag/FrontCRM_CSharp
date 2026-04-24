using CRM.Core.Interfaces;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Services
{
    public class RbacService : IRbacService
    {
        private readonly IRepository<RbacRole> _roleRepo;
        private readonly IRepository<RbacPermission> _permissionRepo;
        private readonly IRepository<RbacDepartment> _departmentRepo;
        private readonly IRepository<RbacUserRole> _userRoleRepo;
        private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
        private readonly IRepository<RbacRolePermission> _rolePermissionRepo;
        private readonly IUnitOfWork _unitOfWork;

        public RbacService(
            IRepository<RbacRole> roleRepo,
            IRepository<RbacPermission> permissionRepo,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserRole> userRoleRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RbacRolePermission> rolePermissionRepo,
            IUnitOfWork unitOfWork)
        {
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
            _departmentRepo = departmentRepo;
            _userRoleRepo = userRoleRepo;
            _userDepartmentRepo = userDepartmentRepo;
            _rolePermissionRepo = rolePermissionRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserPermissionSummaryDto> GetUserPermissionSummaryAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new UserPermissionSummaryDto();

            var userRoles = (await _userRoleRepo.FindAsync(x => x.UserId == userId)).ToList();
            var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
            var roles = (await _roleRepo.GetAllAsync()).Where(r => roleIds.Contains(r.Id)).ToList();
            var roleCodes = roles.Select(r => r.RoleCode).Distinct().ToList();

            var rolePermissions = (await _rolePermissionRepo.GetAllAsync())
                .Where(x => roleIds.Contains(x.RoleId))
                .Select(x => x.PermissionId)
                .Distinct()
                .ToList();
            var permissionCodes = (await _permissionRepo.GetAllAsync())
                .Where(p => rolePermissions.Contains(p.Id) && p.Status == 1)
                .Select(p => p.PermissionCode)
                .Distinct()
                .ToList();

            var userDepartments = (await _userDepartmentRepo.FindAsync(x => x.UserId == userId)).ToList();
            var departmentIds = userDepartments.Select(x => x.DepartmentId).Distinct().ToList();
            var primaryDepartmentId = userDepartments.FirstOrDefault(x => x.IsPrimary)?.DepartmentId ?? departmentIds.FirstOrDefault();

            short identityType = 0;
            short saleScope = 1;
            short purchaseScope = 1;
            if (!string.IsNullOrWhiteSpace(primaryDepartmentId))
            {
                var department = await _departmentRepo.GetByIdAsync(primaryDepartmentId);
                if (department != null)
                {
                    identityType = department.IdentityType;
                    saleScope = department.SaleDataScope;
                    purchaseScope = department.PurchaseDataScope;
                    // IdentityType 未维护为 5 时按部门名称兜底（与 DataPermissionService 财务单据全员可见一致）
                    if (identityType == 0)
                    {
                        var dn = department.DepartmentName ?? string.Empty;
                        if (dn.Contains("财务", StringComparison.Ordinal)
                            || dn.Contains("Finance", StringComparison.OrdinalIgnoreCase)
                            || dn.Contains("Accounting", StringComparison.OrdinalIgnoreCase))
                            identityType = 5;
                    }
                }
            }

            var primaryIsSales = identityType == 1;
            var belongsToPurchaseDept = identityType is 2 or 3;
            // 主部门未标采购(2/3)时，仍检查兼任部门：仅当主部门不是销售(1)时才认定，避免销售兼采购岗被合并 PO。
            if (!belongsToPurchaseDept && !primaryIsSales && departmentIds.Count > 0)
            {
                foreach (var did in departmentIds)
                {
                    if (string.IsNullOrWhiteSpace(did)) continue;
                    var d = await _departmentRepo.GetByIdAsync(did);
                    if (d?.IdentityType is 2 or 3)
                    {
                        belongsToPurchaseDept = true;
                        break;
                    }
                }
            }

            // 主部门 IdentityType 未维护为 2/3 时，按部门名称兜底（避免「采购部」仍为 0 时员工无 PR/PO 权限）
            if (!belongsToPurchaseDept && !primaryIsSales && !string.IsNullOrWhiteSpace(primaryDepartmentId))
            {
                var pd = await _departmentRepo.GetByIdAsync(primaryDepartmentId);
                if (pd != null)
                {
                    var dn = pd.DepartmentName ?? string.Empty;
                    var looksPurchaseDept =
                        (dn.Contains("采购部", StringComparison.Ordinal)
                         || dn.Contains("采购中心", StringComparison.Ordinal)
                         || string.Equals(dn.Trim(), "采购", StringComparison.Ordinal))
                        && !dn.Contains("销售", StringComparison.Ordinal);
                    var looksPurchaseEn =
                        dn.Contains("Purchasing", StringComparison.OrdinalIgnoreCase)
                        && !dn.Contains("Sales", StringComparison.OrdinalIgnoreCase);
                    if (looksPurchaseDept || looksPurchaseEn)
                        belongsToPurchaseDept = true;
                }
            }

            // 隶属采购侧部门时：不授予销售订单与「销售侧财务」（与采购菜单、数据范围一致）。
            // DEPT_EMPLOYEE 种子可能仍含 finance-receipt / sales-order，此处统一剥离。
            // 采购主部门（含总监/经理绑 biz_all）：不允许「新建需求」——剥离 rfq.create（保留 rfq.write 供分配等维护）。
            if (belongsToPurchaseDept)
            {
                RemovePermissionCodes(permissionCodes,
                    "sales-order.read", "sales-order.write", "sales.amount.read",
                    "finance-receipt.read", "finance-receipt.write",
                    "finance-sell-invoice.read", "finance-sell-invoice.write",
                    "rfq.create");
            }

            // 主部门为销售（1）时：不授予采购订单与采购侧财务（与采购主部门不持有销售订单对称）。
            // 采购申请保留：销售员可提采购申请，与菜单/路由一致。
            if (identityType == 1)
            {
                RemovePermissionCodes(permissionCodes,
                    "finance-payment.read", "finance-payment.write",
                    "finance-purchase-invoice.read", "finance-purchase-invoice.write",
                    "purchase-order.read", "purchase-order.write",
                    "purchase.amount.read");
            }

            // 隶属采购/采购助理部门时合并：仅 DEPT_EMPLOYEE 时种子常无 PR/PO 写权限，与员工页「可选 purchase_buyer」说明一致，
            // 采购部员工仍应能维护采购申请、生成采购订单（与销售员仅 PR、不 PO 的剥离策略独立）。
            // 需求维护（编辑/分配）与「新建需求」拆分：此处补 rfq.read + rfq.write，不补 rfq.create（见上方采购侧剥离）。
            if (belongsToPurchaseDept)
            {
                AddPermissionCodeIfMissing(permissionCodes, "rfq.read");
                AddPermissionCodeIfMissing(permissionCodes, "rfq.write");
                AddPermissionCodeIfMissing(permissionCodes, "vendor.read");
                AddPermissionCodeIfMissing(permissionCodes, "vendor.write");
                AddPermissionCodeIfMissing(permissionCodes, "purchase-requisition.read");
                AddPermissionCodeIfMissing(permissionCodes, "purchase-requisition.write");
                AddPermissionCodeIfMissing(permissionCodes, "purchase-order.read");
                AddPermissionCodeIfMissing(permissionCodes, "purchase-order.write");
                AddPermissionCodeIfMissing(permissionCodes, "purchase.amount.read");
            }

            // 主部门身份为销售（IdentityType=1）时合并客户读写：DEPT_EMPLOYEE 种子常仅有 customer.read，
            // 无 customer.write 时「新建客户」路由与 API 会被拒绝。
            // 同理合并销售订单读写：种子中 DEPT_EMPLOYEE 常仅有 sales-order.read，无 write 则无法进入「新建销售订单」路由与写接口。
            // 销售员业务上需提需求：合并 rfq.read + rfq.write + rfq.create（新建需求 POST 与采购侧仅 write 区分）。
            if (identityType == 1)
            {
                AddPermissionCodeIfMissing(permissionCodes, "customer.read");
                AddPermissionCodeIfMissing(permissionCodes, "customer.write");
                AddPermissionCodeIfMissing(permissionCodes, "sales-order.read");
                AddPermissionCodeIfMissing(permissionCodes, "sales-order.write");
                AddPermissionCodeIfMissing(permissionCodes, "rfq.read");
                AddPermissionCodeIfMissing(permissionCodes, "rfq.write");
                AddPermissionCodeIfMissing(permissionCodes, "rfq.create");
            }

            // 主部门为财务（5，含 IdentityType=0 时按部门名称推断为财务）：DEPT_EMPLOYEE 种子常仅有 finance-*.read，
            // 无 write 时前端会隐藏「付款」且保存/付款完成 API 403；与采购/销售主部门合并写权限策略对称。
            if (identityType == 5)
            {
                AddPermissionCodeIfMissing(permissionCodes, "finance-receipt.read");
                AddPermissionCodeIfMissing(permissionCodes, "finance-receipt.write");
                AddPermissionCodeIfMissing(permissionCodes, "finance-payment.read");
                AddPermissionCodeIfMissing(permissionCodes, "finance-payment.write");
                AddPermissionCodeIfMissing(permissionCodes, "finance-sell-invoice.read");
                AddPermissionCodeIfMissing(permissionCodes, "finance-sell-invoice.write");
                AddPermissionCodeIfMissing(permissionCodes, "finance-purchase-invoice.read");
                AddPermissionCodeIfMissing(permissionCodes, "finance-purchase-invoice.write");
            }

            // 非采购主部门(2/3)且具备销售订单权限时合并采购申请：主部门 IdentityType 未维护为销售(1) 的业务员仍可见菜单与接口。
            if (identityType is not 2 and not 3)
            {
                var hasSalesOrder =
                    permissionCodes.Exists(c => string.Equals(c, "sales-order.read", StringComparison.OrdinalIgnoreCase))
                    || permissionCodes.Exists(c => string.Equals(c, "sales-order.write", StringComparison.OrdinalIgnoreCase));
                if (hasSalesOrder)
                {
                    AddPermissionCodeIfMissing(permissionCodes, "purchase-requisition.read");
                    AddPermissionCodeIfMissing(permissionCodes, "purchase-requisition.write");
                }
            }

            return new UserPermissionSummaryDto
            {
                UserId = userId,
                IsSysAdmin = roleCodes.Contains("SYS_ADMIN"),
                RoleCodes = roleCodes,
                PermissionCodes = permissionCodes,
                DepartmentIds = departmentIds,
                PrimaryDepartmentId = primaryDepartmentId,
                IdentityType = identityType,
                SaleDataScope = saleScope,
                PurchaseDataScope = purchaseScope,
                BelongsToPurchaseDept = belongsToPurchaseDept
            };
        }

        private static void AddPermissionCodeIfMissing(List<string> codes, string code)
        {
            if (codes.Exists(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase))) return;
            codes.Add(code);
        }

        private static void RemovePermissionCodes(List<string> codes, params string[] toRemove)
        {
            foreach (var r in toRemove)
            {
                codes.RemoveAll(c => string.Equals(c, r, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<IReadOnlyList<RbacRole>> GetRolesAsync()
            => (await _roleRepo.GetAllAsync()).OrderBy(x => x.RoleCode).ToList();

        public async Task<IReadOnlyList<RbacPermission>> GetPermissionsAsync()
            => (await _permissionRepo.GetAllAsync()).OrderBy(x => x.PermissionCode).ToList();

        public async Task<IReadOnlyList<RbacDepartment>> GetDepartmentsAsync()
            => (await _departmentRepo.GetAllAsync()).OrderBy(x => x.Path).ThenBy(x => x.DepartmentName).ToList();

        private static string? NormalizeParentId(string? parentId) =>
            string.IsNullOrWhiteSpace(parentId) ? null : parentId.Trim();

        private static bool IsInSubtree(IReadOnlyList<RbacDepartment> all, string rootId, string? candidateId)
        {
            if (string.IsNullOrWhiteSpace(candidateId)) return false;
            if (string.Equals(rootId, candidateId, StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var child in all.Where(d =>
                         d.ParentId != null &&
                         string.Equals(d.ParentId, rootId, StringComparison.OrdinalIgnoreCase)))
            {
                if (IsInSubtree(all, child.Id, candidateId)) return true;
            }

            return false;
        }

        private static string PathSegmentFromDepartmentName(string departmentName)
        {
            var t = (departmentName ?? string.Empty).Trim();
            if (t.Length == 0) return "DEPT";
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var chars = t.Select(c =>
                invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray();
            var s = new string(chars).Trim('_');
            if (string.IsNullOrEmpty(s)) return "DEPT";
            return s.Length > 80 ? s[..80] : s;
        }

        private static void ApplyPathAndLevel(RbacDepartment dept, RbacDepartment? parent)
        {
            if (parent == null)
            {
                dept.Level = 1;
                var seg = PathSegmentFromDepartmentName(dept.DepartmentName);
                dept.Path = "/" + seg;
            }
            else
            {
                dept.Level = parent.Level + 1;
                var basePath = (parent.Path ?? string.Empty).TrimEnd('/');
                var seg = PathSegmentFromDepartmentName(dept.DepartmentName);
                dept.Path = string.IsNullOrEmpty(basePath) ? "/" + seg : basePath + "/" + seg;
            }

            if (dept.Path != null && dept.Path.Length > 500)
                dept.Path = dept.Path[..500];
        }

        public async Task<RbacDepartment> CreateDepartmentAsync(
            string departmentName,
            string? parentId,
            short saleDataScope,
            short purchaseDataScope,
            short identityType,
            short status)
        {
            var name = (departmentName ?? string.Empty).Trim();
            if (name.Length == 0)
                throw new ArgumentException("部门名称不能为空", nameof(departmentName));

            if (saleDataScope is < 0 or > 4 || purchaseDataScope is < 0 or > 4)
                throw new ArgumentOutOfRangeException(nameof(saleDataScope), "数据范围仅支持 0–4");
            if (identityType is < 0 or > 6)
                throw new ArgumentOutOfRangeException(nameof(identityType), "业务身份仅支持 0–6");

            var pid = NormalizeParentId(parentId);
            var all = (await _departmentRepo.GetAllAsync()).ToList();
            RbacDepartment? parent = null;
            if (pid != null)
            {
                parent = all.FirstOrDefault(d => string.Equals(d.Id, pid, StringComparison.OrdinalIgnoreCase));
                if (parent == null)
                    throw new InvalidOperationException("上级部门不存在");
            }

            var dept = new RbacDepartment
            {
                Id = Guid.NewGuid().ToString(),
                DepartmentName = name,
                ParentId = pid,
                SaleDataScope = saleDataScope,
                PurchaseDataScope = purchaseDataScope,
                IdentityType = identityType,
                Status = status
            };
            ApplyPathAndLevel(dept, parent);
            await _departmentRepo.AddAsync(dept);
            await _unitOfWork.SaveChangesAsync();
            return dept;
        }

        public async Task<RbacDepartment?> UpdateDepartmentAsync(
            string departmentId,
            string departmentName,
            string? parentId,
            short saleDataScope,
            short purchaseDataScope,
            short identityType,
            short status)
        {
            if (string.IsNullOrWhiteSpace(departmentId))
                throw new ArgumentException("部门 Id 不能为空", nameof(departmentId));

            var name = (departmentName ?? string.Empty).Trim();
            if (name.Length == 0)
                throw new ArgumentException("部门名称不能为空", nameof(departmentName));

            if (saleDataScope is < 0 or > 4 || purchaseDataScope is < 0 or > 4)
                throw new ArgumentOutOfRangeException(nameof(saleDataScope), "数据范围仅支持 0–4");
            if (identityType is < 0 or > 6)
                throw new ArgumentOutOfRangeException(nameof(identityType), "业务身份仅支持 0–6");

            var dept = await _departmentRepo.GetByIdAsync(departmentId);
            if (dept == null) return null;

            var newPid = NormalizeParentId(parentId);
            if (newPid != null && string.Equals(newPid, departmentId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("上级部门不能为自身");

            var all = (await _departmentRepo.GetAllAsync()).ToList();
            if (newPid != null && IsInSubtree(all, departmentId, newPid))
                throw new InvalidOperationException("上级部门不能为当前部门的下级（避免循环）");

            var oldPid = NormalizeParentId(dept.ParentId);
            var parentChanged = !string.Equals(oldPid ?? string.Empty, newPid ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            dept.DepartmentName = name;
            dept.ParentId = newPid;
            dept.SaleDataScope = saleDataScope;
            dept.PurchaseDataScope = purchaseDataScope;
            dept.IdentityType = identityType;
            dept.Status = status;
            dept.ModifyTime = DateTime.UtcNow;

            if (parentChanged)
            {
                RbacDepartment? parent = null;
                if (newPid != null)
                {
                    parent = all.FirstOrDefault(d => string.Equals(d.Id, newPid, StringComparison.OrdinalIgnoreCase));
                    if (parent == null)
                        throw new InvalidOperationException("上级部门不存在");
                }

                ApplyPathAndLevel(dept, parent);
            }

            await _departmentRepo.UpdateAsync(dept);
            await _unitOfWork.SaveChangesAsync();
            return dept;
        }

        public async Task AssignUserRolesAsync(string userId, IReadOnlyList<string> roleIds)
        {
            var current = (await _userRoleRepo.FindAsync(x => x.UserId == userId)).ToList();
            foreach (var item in current)
                await _userRoleRepo.DeleteAsync(item.Id);

            foreach (var roleId in roleIds.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await _userRoleRepo.AddAsync(new RbacUserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreateTime = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AssignUserDepartmentsAsync(string userId, IReadOnlyList<string> departmentIds, string? primaryDepartmentId)
        {
            var current = (await _userDepartmentRepo.FindAsync(x => x.UserId == userId)).ToList();
            foreach (var item in current)
                await _userDepartmentRepo.DeleteAsync(item.Id);

            var distinctIds = departmentIds.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            foreach (var departmentId in distinctIds)
            {
                await _userDepartmentRepo.AddAsync(new RbacUserDepartment
                {
                    UserId = userId,
                    DepartmentId = departmentId,
                    IsPrimary = departmentId == primaryDepartmentId || (string.IsNullOrWhiteSpace(primaryDepartmentId) && departmentId == distinctIds.First()),
                    CreateTime = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AssignRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionIds)
        {
            var current = (await _rolePermissionRepo.FindAsync(x => x.RoleId == roleId)).ToList();
            foreach (var item in current)
                await _rolePermissionRepo.DeleteAsync(item.Id);

            foreach (var permissionId in permissionIds.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await _rolePermissionRepo.AddAsync(new RbacRolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreateTime = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
