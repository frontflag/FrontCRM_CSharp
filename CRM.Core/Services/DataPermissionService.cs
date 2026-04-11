using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Services
{
    public class DataPermissionService : IDataPermissionService
    {
        private readonly IRbacService _rbacService;
        private readonly IRepository<RbacDepartment> _departmentRepo;
        private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
        private readonly IRepository<RbacUserRole> _userRoleRepo;
        private readonly IRepository<RbacRole> _roleRepo;
        private readonly IRepository<RFQ> _rfqRepo;
        private readonly IRepository<RFQItem> _rfqItemRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IRepository<VendorInfo> _vendorRepo;

        public DataPermissionService(
            IRbacService rbacService,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RbacUserRole> userRoleRepo,
            IRepository<RbacRole> roleRepo,
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> rfqItemRepo,
            IRepository<CustomerInfo> customerRepo,
            IRepository<VendorInfo> vendorRepo)
        {
            _rbacService = rbacService;
            _departmentRepo = departmentRepo;
            _userDepartmentRepo = userDepartmentRepo;
            _userRoleRepo = userRoleRepo;
            _roleRepo = roleRepo;
            _rfqRepo = rfqRepo;
            _rfqItemRepo = rfqItemRepo;
            _customerRepo = customerRepo;
            _vendorRepo = vendorRepo;
        }

        public async Task<IReadOnlyList<CustomerInfo>> FilterCustomersAsync(string userId, IEnumerable<CustomerInfo> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<CustomerInfo>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1) // self
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        public async Task<IReadOnlyList<VendorInfo>> FilterVendorsAsync(string userId, IEnumerable<VendorInfo> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || summary.PurchaseDataScope == 0) return source.ToList();
            if (summary.PurchaseDataScope == 4) return Array.Empty<VendorInfo>();

            var list = source.ToList();
            if (summary.PurchaseDataScope == 1) // self
                return list.Where(x => x.PurchaseUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.PurchaseUserId) && allowUserIds.Contains(x.PurchaseUserId!)).ToList();
        }

        public async Task<IReadOnlyList<RFQListItem>> FilterRFQsAsync(string userId, IEnumerable<RFQListItem> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin) return source.ToList();

            var list = source.ToList();
            var ids = list.Select(x => x.Id).Distinct().ToList();
            if (ids.Count == 0) return list;

            var rfqEntities = (await _rfqRepo.FindAsync(x => ids.Contains(x.Id))).ToDictionary(x => x.Id);
            var allItems = (await _rfqItemRepo.FindAsync(i => ids.Contains(i.RfqId))).ToList();
            var itemsByRfq = allItems.GroupBy(i => i.RfqId).ToDictionary(g => g.Key, g => g.ToList());

            HashSet<string>? saleAllow = null;
            if (summary.SaleDataScope == 2)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.SaleDataScope == 3)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            HashSet<string>? purchaseAllow = null;
            if (summary.PurchaseDataScope == 2)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.PurchaseDataScope == 3)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            bool SaleOk(string rfqId)
            {
                if (summary.SaleDataScope == 4) return false;
                if (summary.SaleDataScope == 0) return true;
                if (!rfqEntities.TryGetValue(rfqId, out var rfqEntity)) return false;
                var ownerId = rfqEntity.SalesUserId;
                if (summary.SaleDataScope == 1)
                    return string.Equals(ownerId, userId, StringComparison.OrdinalIgnoreCase);
                if ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) && saleAllow != null && !string.IsNullOrWhiteSpace(ownerId))
                    return saleAllow.Contains(ownerId);
                return false;
            }

            bool PurchaseOk(string rfqId)
            {
                if (summary.PurchaseDataScope == 4) return false;
                if (summary.PurchaseDataScope == 0) return true;
                if (!itemsByRfq.TryGetValue(rfqId, out var lines) || lines.Count == 0) return false;
                if (summary.PurchaseDataScope == 1)
                {
                    return lines.Any(i =>
                        string.Equals(i.AssignedPurchaserUserId1, userId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(i.AssignedPurchaserUserId2, userId, StringComparison.OrdinalIgnoreCase));
                }

                if (purchaseAllow == null) return false;
                return lines.Any(i =>
                    (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId1) && purchaseAllow.Contains(i.AssignedPurchaserUserId1!)) ||
                    (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId2) && purchaseAllow.Contains(i.AssignedPurchaserUserId2!)));
            }

            return list.Where(r => SaleOk(r.Id) || PurchaseOk(r.Id)).ToList();
        }

        public async Task<IReadOnlyList<SellOrder>> FilterSalesOrdersAsync(string userId, IEnumerable<SellOrder> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<SellOrder>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        public async Task<IReadOnlyList<PurchaseOrder>> FilterPurchaseOrdersAsync(string userId, IEnumerable<PurchaseOrder> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || summary.PurchaseDataScope == 0) return source.ToList();
            if (summary.PurchaseDataScope == 4) return Array.Empty<PurchaseOrder>();

            var list = source.ToList();
            if (summary.PurchaseDataScope == 1)
                return list.Where(x => x.PurchaseUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.PurchaseUserId) && allowUserIds.Contains(x.PurchaseUserId!)).ToList();
        }

        public async Task<IReadOnlyList<FinanceReceipt>> FilterFinanceReceiptsAsync(string userId, IEnumerable<FinanceReceipt> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            // 财务部（IdentityType=5）：不按客户业务员做销售数据范围过滤，同部门财务可互相查看收款单
            if (summary.IsSysAdmin || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.SaleDataScope == 0)
                return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<FinanceReceipt>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        public async Task<IReadOnlyList<FinancePayment>> FilterFinancePaymentsAsync(string userId, IEnumerable<FinancePayment> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            // 财务部：不按供应商采购员做采购数据范围过滤，同部门财务可互相查看付款单
            if (summary.IsSysAdmin || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
                return source.ToList();
            if (summary.PurchaseDataScope == 4) return Array.Empty<FinancePayment>();

            var list = source.ToList();
            var vendorIds = list.Select(x => x.VendorId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (vendorIds.Count == 0) return Array.Empty<FinancePayment>();

            var vendors = await _vendorRepo.FindAsync(x => vendorIds.Contains(x.Id));
            var vendorOwnerMap = vendors.ToDictionary(x => x.Id, x => x.PurchaseUserId, StringComparer.OrdinalIgnoreCase);

            if (summary.PurchaseDataScope == 1)
                return list.Where(x => vendorOwnerMap.TryGetValue(x.VendorId, out var ownerId) && ownerId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return list.Where(x =>
                    vendorOwnerMap.TryGetValue(x.VendorId, out var ownerId) &&
                    !string.IsNullOrWhiteSpace(ownerId) &&
                    allowUserIds.Contains(ownerId!))
                .ToList();
        }

        public async Task<IReadOnlyList<FinanceSellInvoice>> FilterFinanceSellInvoicesAsync(string userId, IEnumerable<FinanceSellInvoice> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.SaleDataScope == 0)
                return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<FinanceSellInvoice>();

            var list = source.ToList();
            var customerIds = list.Select(x => x.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (customerIds.Count == 0) return Array.Empty<FinanceSellInvoice>();

            var customers = await _customerRepo.FindAsync(x => customerIds.Contains(x.Id));
            var customerOwnerMap = customers.ToDictionary(x => x.Id, x => x.SalesUserId, StringComparer.OrdinalIgnoreCase);

            if (summary.SaleDataScope == 1)
                return list.Where(x => customerOwnerMap.TryGetValue(x.CustomerId, out var ownerId) && ownerId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x =>
                    customerOwnerMap.TryGetValue(x.CustomerId, out var ownerId) &&
                    !string.IsNullOrWhiteSpace(ownerId) &&
                    allowUserIds.Contains(ownerId!))
                .ToList();
        }

        public async Task<IReadOnlyList<FinancePurchaseInvoice>> FilterFinancePurchaseInvoicesAsync(string userId, IEnumerable<FinancePurchaseInvoice> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
                return source.ToList();
            if (summary.PurchaseDataScope == 4) return Array.Empty<FinancePurchaseInvoice>();

            var list = source.ToList();
            var vendorIds = list.Select(x => x.VendorId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (vendorIds.Count == 0) return Array.Empty<FinancePurchaseInvoice>();

            var vendors = await _vendorRepo.FindAsync(x => vendorIds.Contains(x.Id));
            var vendorOwnerMap = vendors.ToDictionary(x => x.Id, x => x.PurchaseUserId, StringComparer.OrdinalIgnoreCase);

            if (summary.PurchaseDataScope == 1)
                return list.Where(x => vendorOwnerMap.TryGetValue(x.VendorId, out var ownerId) && ownerId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return list.Where(x =>
                    vendorOwnerMap.TryGetValue(x.VendorId, out var ownerId) &&
                    !string.IsNullOrWhiteSpace(ownerId) &&
                    allowUserIds.Contains(ownerId!))
                .ToList();
        }

        public async Task<bool> CanAccessCustomerAsync(string userId, CustomerInfo customer)
        {
            var filtered = await FilterCustomersAsync(userId, new[] { customer });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessVendorAsync(string userId, VendorInfo vendor)
        {
            var filtered = await FilterVendorsAsync(userId, new[] { vendor });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessRFQAsync(string userId, RFQ rfq)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin) return true;
            if (await PassesSaleAccessToRfqAsync(userId, rfq, summary)) return true;
            return await PassesPurchaseAccessToRfqAsync(userId, rfq, summary);
        }

        public async Task<Func<RFQ, RFQItem, bool>> GetRfqItemLineVisibilityPredicateAsync(string userId)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.IsSysAdmin)
                return (_, __) => true;

            HashSet<string>? saleAllow = null;
            if (summary.SaleDataScope == 2)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.SaleDataScope == 3)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            HashSet<string>? purchaseAllow = null;
            if (summary.PurchaseDataScope == 2)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.PurchaseDataScope == 3)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            return (rfq, item) =>
            {
                bool saleOk = false;
                if (summary.SaleDataScope != 4)
                {
                    if (summary.SaleDataScope == 0) saleOk = true;
                    else if (summary.SaleDataScope == 1)
                        saleOk = string.Equals(rfq.SalesUserId, userId, StringComparison.OrdinalIgnoreCase);
                    else if ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) && saleAllow != null && !string.IsNullOrWhiteSpace(rfq.SalesUserId))
                        saleOk = saleAllow.Contains(rfq.SalesUserId);
                }

                if (saleOk) return true;

                if (summary.PurchaseDataScope == 4) return false;
                if (summary.PurchaseDataScope == 0) return true;
                if (summary.PurchaseDataScope == 1)
                {
                    return string.Equals(item.AssignedPurchaserUserId1, userId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.AssignedPurchaserUserId2, userId, StringComparison.OrdinalIgnoreCase);
                }

                if (purchaseAllow == null) return false;
                return (!string.IsNullOrWhiteSpace(item.AssignedPurchaserUserId1) && purchaseAllow.Contains(item.AssignedPurchaserUserId1!))
                    || (!string.IsNullOrWhiteSpace(item.AssignedPurchaserUserId2) && purchaseAllow.Contains(item.AssignedPurchaserUserId2!));
            };
        }

        private async Task<bool> PassesSaleAccessToRfqAsync(string userId, RFQ rfq, UserPermissionSummaryDto summary)
        {
            if (summary.SaleDataScope == 4) return false;
            if (summary.SaleDataScope == 0) return true;
            if (summary.SaleDataScope == 1)
                return string.Equals(rfq.SalesUserId, userId, StringComparison.OrdinalIgnoreCase);
            if (summary.SaleDataScope == 2)
            {
                var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: false);
                return !string.IsNullOrWhiteSpace(rfq.SalesUserId) && allowUserIds.Contains(rfq.SalesUserId);
            }

            if (summary.SaleDataScope == 3)
            {
                var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: true);
                return !string.IsNullOrWhiteSpace(rfq.SalesUserId) && allowUserIds.Contains(rfq.SalesUserId);
            }

            return false;
        }

        private async Task<bool> PassesPurchaseAccessToRfqAsync(string userId, RFQ rfq, UserPermissionSummaryDto summary)
        {
            if (summary.PurchaseDataScope == 4) return false;
            if (summary.PurchaseDataScope == 0) return true;

            var items = (await _rfqItemRepo.FindAsync(i => i.RfqId == rfq.Id)).ToList();
            if (items.Count == 0) return false;

            if (summary.PurchaseDataScope == 1)
            {
                return items.Any(i =>
                    string.Equals(i.AssignedPurchaserUserId1, userId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.AssignedPurchaserUserId2, userId, StringComparison.OrdinalIgnoreCase));
            }

            HashSet<string>? purchaseAllow = summary.PurchaseDataScope == 3
                ? await GetAllowedUserIdsAsync(summary, includeChildren: true)
                : await GetAllowedUserIdsAsync(summary, includeChildren: false);

            return items.Any(i =>
                (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId1) && purchaseAllow.Contains(i.AssignedPurchaserUserId1!)) ||
                (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId2) && purchaseAllow.Contains(i.AssignedPurchaserUserId2!)));
        }

        public async Task<bool> CanAccessSalesOrderAsync(string userId, SellOrder salesOrder)
        {
            var filtered = await FilterSalesOrdersAsync(userId, new[] { salesOrder });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessPurchaseOrderAsync(string userId, PurchaseOrder purchaseOrder)
        {
            var filtered = await FilterPurchaseOrdersAsync(userId, new[] { purchaseOrder });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessFinanceReceiptAsync(string userId, FinanceReceipt receipt)
        {
            var filtered = await FilterFinanceReceiptsAsync(userId, new[] { receipt });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessFinancePaymentAsync(string userId, FinancePayment payment)
        {
            var filtered = await FilterFinancePaymentsAsync(userId, new[] { payment });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessFinanceSellInvoiceAsync(string userId, FinanceSellInvoice sellInvoice)
        {
            var filtered = await FilterFinanceSellInvoicesAsync(userId, new[] { sellInvoice });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessFinancePurchaseInvoiceAsync(string userId, FinancePurchaseInvoice purchaseInvoice)
        {
            var filtered = await FilterFinancePurchaseInvoicesAsync(userId, new[] { purchaseInvoice });
            return filtered.Count > 0;
        }

        private async Task<HashSet<string>> GetAllowedUserIdsAsync(UserPermissionSummaryDto summary, bool includeChildren)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(summary.UserId))
                result.Add(summary.UserId);

            if (string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
                return result;

            var departments = await _departmentRepo.GetAllAsync();
            var currentDepartment = departments.FirstOrDefault(x => x.Id == summary.PrimaryDepartmentId);
            if (currentDepartment == null) return result;

            var allowedDepartmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentDepartment.Id };
            if (includeChildren)
            {
                var prefix = string.IsNullOrWhiteSpace(currentDepartment.Path) ? null : currentDepartment.Path + "/";
                foreach (var d in departments)
                {
                    if (prefix != null && !string.IsNullOrWhiteSpace(d.Path) && d.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        allowedDepartmentIds.Add(d.Id);
                }
            }

            var userDepartments = await _userDepartmentRepo.GetAllAsync();
            var scopedUserDepartments = userDepartments
                .Where(x => allowedDepartmentIds.Contains(x.DepartmentId))
                .ToList();

            // 组织层级规则：
            // - 总监：可看下属经理和员工
            // - 经理：可看下属员工
            // - 员工：仅自己
            // 仅当用户角色可识别为总监/经理/员工时生效；否则回退到原数据范围逻辑。
            var currentOrgLevel = ResolveOrgRoleLevel(summary.RoleCodes, Array.Empty<string>());
            if (currentOrgLevel <= 0)
            {
                foreach (var rel in scopedUserDepartments)
                    result.Add(rel.UserId);
                return result;
            }

            if (currentOrgLevel == 1)
                return result;

            var scopedUserIds = scopedUserDepartments.Select(x => x.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (scopedUserIds.Count == 0)
                return result;

            var userRoleLevels = await BuildUserRoleLevelMapAsync(scopedUserIds);
            var primaryDeptMap = BuildPrimaryDepartmentMap(scopedUserDepartments);

            var currentPath = currentDepartment.Path ?? string.Empty;

            foreach (var uid in scopedUserIds)
            {
                if (string.Equals(uid, summary.UserId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!primaryDeptMap.TryGetValue(uid, out var targetDeptId))
                    continue;

                if (!TryGetDepartmentById(departments, targetDeptId, out var targetDept))
                    continue;

                if (!IsSubordinateDepartment(currentDepartment, targetDept))
                    continue;

                var targetLevel = userRoleLevels.TryGetValue(uid, out var lv) ? lv : 0;
                if (targetLevel <= 0)
                    continue;

                var canSee = currentOrgLevel switch
                {
                    3 => targetLevel <= 2, // 总监看经理+员工
                    2 => targetLevel <= 1, // 经理看员工
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

        private static bool TryGetDepartmentById(IEnumerable<RbacDepartment> departments, string id, out RbacDepartment department)
        {
            department = departments.FirstOrDefault(x => x.Id == id)!;
            return department != null;
        }

        private static bool IsSubordinateDepartment(RbacDepartment currentDept, RbacDepartment targetDept)
        {
            if (currentDept.Id == targetDept.Id) return true;
            if (string.IsNullOrWhiteSpace(currentDept.Path) || string.IsNullOrWhiteSpace(targetDept.Path)) return false;
            var prefix = currentDept.Path + "/";
            return targetDept.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>与 <see cref="RbacDepartment.IdentityType"/> 约定一致：5=Finance（财务部）。</summary>
        private static bool IsFinanceDepartmentIdentity(short identityType) => identityType == 5;

        /// <summary>
        /// 3=总监，2=经理，1=员工，0=未知
        /// </summary>
        private static int ResolveOrgRoleLevel(IEnumerable<string> roleCodes, IEnumerable<string> roleNames)
        {
            // 标准编码优先（避免「销售经理」等业务角色名误匹配部门层级）
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
