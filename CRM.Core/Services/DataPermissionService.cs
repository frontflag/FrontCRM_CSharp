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
        private readonly IRepository<RFQ> _rfqRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IRepository<VendorInfo> _vendorRepo;

        public DataPermissionService(
            IRbacService rbacService,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RFQ> rfqRepo,
            IRepository<CustomerInfo> customerRepo,
            IRepository<VendorInfo> vendorRepo)
        {
            _rbacService = rbacService;
            _departmentRepo = departmentRepo;
            _userDepartmentRepo = userDepartmentRepo;
            _rfqRepo = rfqRepo;
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
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<RFQListItem>();

            var list = source.ToList();
            var ids = list.Select(x => x.Id).Distinct().ToList();
            if (ids.Count == 0) return list;

            var rfqMap = (await _rfqRepo.FindAsync(x => ids.Contains(x.Id)))
                .ToDictionary(x => x.Id, x => x.SalesUserId);

            if (summary.SaleDataScope == 1) // self
                return list.Where(x => rfqMap.TryGetValue(x.Id, out var ownerId) && ownerId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => rfqMap.TryGetValue(x.Id, out var ownerId) && !string.IsNullOrWhiteSpace(ownerId) && allowUserIds.Contains(ownerId!)).ToList();
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
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return source.ToList();
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
            if (summary.IsSysAdmin || summary.PurchaseDataScope == 0) return source.ToList();
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
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return source.ToList();
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
            if (summary.IsSysAdmin || summary.PurchaseDataScope == 0) return source.ToList();
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
            if (summary.IsSysAdmin || summary.SaleDataScope == 0) return true;
            if (summary.SaleDataScope == 4) return false;
            if (summary.SaleDataScope == 1) return rfq.SalesUserId == userId;
            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return !string.IsNullOrWhiteSpace(rfq.SalesUserId) && allowUserIds.Contains(rfq.SalesUserId);
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
            foreach (var rel in userDepartments.Where(x => allowedDepartmentIds.Contains(x.DepartmentId)))
                result.Add(rel.UserId);

            return result;
        }
    }
}
