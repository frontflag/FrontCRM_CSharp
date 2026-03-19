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
                PurchaseDataScope = purchaseScope
            };
        }

        public async Task<IReadOnlyList<RbacRole>> GetRolesAsync()
            => (await _roleRepo.GetAllAsync()).OrderBy(x => x.RoleCode).ToList();

        public async Task<IReadOnlyList<RbacPermission>> GetPermissionsAsync()
            => (await _permissionRepo.GetAllAsync()).OrderBy(x => x.PermissionCode).ToList();

        public async Task<IReadOnlyList<RbacDepartment>> GetDepartmentsAsync()
            => (await _departmentRepo.GetAllAsync()).OrderBy(x => x.Path).ThenBy(x => x.DepartmentName).ToList();

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
