using System.Linq;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

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
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
        private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
        private readonly ISysRelationMapService _relationMapService;
        private readonly Dictionary<string, HashSet<string>> _commerceMappedSalesCache = new(StringComparer.OrdinalIgnoreCase);

        public DataPermissionService(
            IRbacService rbacService,
            IRepository<RbacDepartment> departmentRepo,
            IRepository<RbacUserDepartment> userDepartmentRepo,
            IRepository<RbacUserRole> userRoleRepo,
            IRepository<RbacRole> roleRepo,
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> rfqItemRepo,
            IRepository<CustomerInfo> customerRepo,
            IRepository<VendorInfo> vendorRepo,
            IRepository<SellOrder> sellOrderRepo,
            IRepository<FinanceReceiptItem> receiptItemRepo,
            IPurchaseQuoterPoolService purchaseQuoterPoolService,
            ISysRelationMapService relationMapService)
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
            _sellOrderRepo = sellOrderRepo;
            _receiptItemRepo = receiptItemRepo;
            _purchaseQuoterPoolService = purchaseQuoterPoolService;
            _relationMapService = relationMapService;
        }

        public async Task<IReadOnlyList<CustomerInfo>> FilterCustomersAsync(string userId, IEnumerable<CustomerInfo> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType))
                return source.ToList();
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                if (mapped.Count == 0) return Array.Empty<CustomerInfo>();
                return source
                    .Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && mapped.Contains(x.SalesUserId!))
                    .ToList();
            }
            if (summary.SaleDataScope == 0)
                return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<CustomerInfo>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        /// <inheritdoc />
        public async Task<IQueryable<CustomerInfo>> ApplyCustomerListDataScopeAsync(
            string? userId,
            IQueryable<CustomerInfo> query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType))
                return query;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                if (mapped.Count == 0)
                    return query.Where(_ => false);
                return query.Where(x => x.SalesUserId != null && mapped.Contains(x.SalesUserId!));
            }
            if (summary.SaleDataScope == 0)
                return query;
            if (summary.SaleDataScope == 4)
                return query.Where(_ => false);
            if (summary.SaleDataScope == 1)
                return query.Where(x => x.SalesUserId == userId);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            var ids = allowUserIds.ToList();
            if (ids.Count == 0)
                return query.Where(_ => false);
            return query.Where(x => x.SalesUserId != null && ids.Contains(x.SalesUserId!));
        }

        public async Task<IReadOnlyList<VendorInfo>> FilterVendorsAsync(string userId, IEnumerable<VendorInfo> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            // Finance dept: vendor pick for payments/invoices is not narrowed by purchase scope.
            if (summary.HasBizDataBypass || summary.PurchaseDataScope == 0 || IsFinanceDepartmentIdentity(summary.IdentityType))
                return source.ToList();
            if (summary.PurchaseDataScope == 4)
                return Array.Empty<VendorInfo>();

            return ApplyVendorExclusiveVisibilityFilter(source);
        }

        private static List<VendorInfo> ApplyVendorExclusiveVisibilityFilter(IEnumerable<VendorInfo> source) =>
            source.ToList();

        /// <inheritdoc />
        public async Task<IQueryable<VendorInfo>> ApplyVendorListDataScopeAsync(
            string? userId,
            IQueryable<VendorInfo> query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || summary.PurchaseDataScope == 0 || IsFinanceDepartmentIdentity(summary.IdentityType))
                return query;
            if (summary.PurchaseDataScope == 4)
                return query.Where(_ => false);

            return query;
        }

        public async Task<IReadOnlyList<RFQListItem>> FilterRFQsAsync(string userId, IEnumerable<RFQListItem> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass) return source.ToList();
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return source.ToList();

            var list = source.ToList();
            var ids = list.Select(x => x.Id).Distinct().ToList();
            if (ids.Count == 0) return list;

            var rfqEntities = (await _rfqRepo.FindAsync(x => ids.Contains(x.Id))).ToDictionary(x => x.Id);
            var allItems = (await _rfqItemRepo.FindAsync(i => ids.Contains(i.RfqId))).ToList();
            var itemsByRfq = allItems.GroupBy(i => i.RfqId).ToDictionary(g => g.Key, g => g.ToList());

            HashSet<string>? saleAllow = null;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
                saleAllow = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
            else if (summary.SaleDataScope == 2)
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
                if (!rfqEntities.TryGetValue(rfqId, out var rfqEntity)) return false;
                var ownerId = rfqEntity.SalesUserId;
                if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
                {
                    if (saleAllow == null || saleAllow.Count == 0) return false;
                    return !string.IsNullOrWhiteSpace(ownerId) && saleAllow.Contains(ownerId);
                }
                if (summary.SaleDataScope == 4) return false;
                if (summary.SaleDataScope == 0) return true;
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
            if (summary.HasBizDataBypass)
                return source.ToList();
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                return source.Where(x =>
                    IsSellOrderAssistor(x, userId)
                    || (!string.IsNullOrWhiteSpace(x.SalesUserId) && mapped.Contains(x.SalesUserId!))).ToList();
            }
            if (summary.SaleDataScope == 0)
                return source.ToList();
            if (summary.SaleDataScope == 4) return Array.Empty<SellOrder>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => MatchesSellOrderDataScope(x, userId, null)).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => MatchesSellOrderDataScope(x, userId, allowUserIds)).ToList();
        }

        public async Task<IReadOnlyList<PurchaseOrder>> FilterPurchaseOrdersAsync(string userId, IEnumerable<PurchaseOrder> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || summary.PurchaseDataScope == 0 || summary.LogisticsDataScope == 0)
                return source.ToList();
            if (summary.PurchaseDataScope == 4) return Array.Empty<PurchaseOrder>();

            var list = source.ToList();
            if (summary.PurchaseDataScope == 1)
                return list.Where(x => MatchesPurchaseOrderDataScope(x, userId, null)).ToList();

            if (summary.PurchaseDataScope == 4)
                return list.Where(x => IsPurchaseOrderAssistor(x, userId)).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return list.Where(x => MatchesPurchaseOrderDataScope(x, userId, allowUserIds)).ToList();
        }

        /// <inheritdoc />
        public async Task<IQueryable<PurchaseOrder>> ApplyPurchaseOrderDataScopeAsync(
            string? userId,
            IQueryable<PurchaseOrder> query,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || summary.PurchaseDataScope == 0)
                return query;
            if (summary.LogisticsDataScope == 0)
                return query;
            if (summary.PurchaseDataScope == 4)
                return query.Where(x => x.Assistor == userId);

            if (summary.PurchaseDataScope == 1)
                return query.Where(x => x.PurchaseUserId == userId || x.Assistor == userId);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return query.Where(x =>
                (x.PurchaseUserId != null && allowUserIds.Contains(x.PurchaseUserId))
                || x.Assistor == userId);
        }

        private static bool IsPurchaseOrderAssistor(PurchaseOrder order, string userId) =>
            !string.IsNullOrWhiteSpace(order.Assistor)
            && string.Equals(order.Assistor.Trim(), userId, StringComparison.OrdinalIgnoreCase);

        private static bool MatchesPurchaseOrderDataScope(
            PurchaseOrder order,
            string userId,
            HashSet<string>? allowPurchaseUserIds)
        {
            if (IsPurchaseOrderAssistor(order, userId))
                return true;
            if (allowPurchaseUserIds == null)
                return string.Equals(order.PurchaseUserId, userId, StringComparison.OrdinalIgnoreCase);
            return !string.IsNullOrWhiteSpace(order.PurchaseUserId)
                   && allowPurchaseUserIds.Contains(order.PurchaseUserId!);
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinancePayment>> ApplyFinancePaymentListDataScopeAsync(
            string? userId,
            IQueryable<FinancePayment> payments,
            IQueryable<VendorInfo> vendors,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return payments;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, payments, p => p.CreateByUserId, cancellationToken);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return payments;
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
                return payments;
            if (summary.PurchaseDataScope == 4)
                return payments.Where(_ => false);

            var uid = userId.Trim();
            if (summary.PurchaseDataScope == 1)
            {
                return payments.Where(p =>
                    vendors.Any(v => v.Id == p.VendorId && v.PurchaseUserId == uid));
            }

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return payments.Where(p =>
                vendors.Any(v =>
                    v.Id == p.VendorId &&
                    v.PurchaseUserId != null &&
                    allowUserIds.Contains(v.PurchaseUserId)));
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinancePurchaseInvoice>> ApplyFinancePurchaseInvoiceListDataScopeAsync(
            string? userId,
            IQueryable<FinancePurchaseInvoice> invoices,
            IQueryable<VendorInfo> vendors,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return invoices;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, invoices, inv => inv.CreateByUserId, cancellationToken);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return invoices;
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
                return invoices;
            if (summary.PurchaseDataScope == 4)
                return invoices.Where(_ => false);

            var uid = userId.Trim();
            if (summary.PurchaseDataScope == 1)
            {
                return invoices.Where(inv =>
                    vendors.Any(v => v.Id == inv.VendorId && v.PurchaseUserId == uid));
            }

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);
            return invoices.Where(inv =>
                vendors.Any(v =>
                    v.Id == inv.VendorId &&
                    v.PurchaseUserId != null &&
                    allowUserIds.Contains(v.PurchaseUserId)));
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinanceReceipt>> ApplyFinanceReceiptListDataScopeAsync(
            string? userId,
            IQueryable<FinanceReceipt> receipts,
            IQueryable<SellOrder>? sellOrders = null,
            IQueryable<FinanceReceiptItem>? receiptItems = null,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return receipts;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, receipts, r => r.CreateByUserId, cancellationToken, allowCommerceAssistantReceiptBypass: true);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return receipts;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                var uid = userId.Trim();
                if (sellOrders != null && receiptItems != null)
                {
                    if (mapped.Count == 0)
                    {
                        return receipts.Where(r =>
                            receiptItems.Any(i => i.FinanceReceiptId == r.Id && i.SellOrderId != null &&
                                sellOrders.Any(so => so.Id == i.SellOrderId && so.Assistor == uid)));
                    }

                    return receipts.Where(r =>
                        (r.SalesUserId != null && mapped.Contains(r.SalesUserId))
                        || receiptItems.Any(i => i.FinanceReceiptId == r.Id && i.SellOrderId != null &&
                            sellOrders.Any(so => so.Id == i.SellOrderId &&
                                (so.Assistor == uid || (so.SalesUserId != null && mapped.Contains(so.SalesUserId))))));
                }

                if (mapped.Count == 0)
                    return receipts.Where(_ => false);
                return receipts.Where(r => r.SalesUserId != null && mapped.Contains(r.SalesUserId));
            }
            if (IsSaleDataScopeDenied(summary))
                return receipts.Where(_ => false);

            var uidSale = userId.Trim();
            if (summary.SaleDataScope == 1)
                return receipts.Where(r => r.SalesUserId == uidSale);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return receipts.Where(r => r.SalesUserId != null && allowUserIds.Contains(r.SalesUserId));
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinanceReceivable>> ApplyFinanceReceivableListDataScopeAsync(
            string? userId,
            IQueryable<FinanceReceivable> receivables,
            IQueryable<SellOrder>? sellOrders = null,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return receivables;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, receivables, r => r.CreateByUserId, cancellationToken, allowCommerceAssistantReceiptBypass: true);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return receivables;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                var uid = userId.Trim();
                if (sellOrders != null)
                {
                    if (mapped.Count == 0)
                    {
                        return receivables.Where(r =>
                            sellOrders.Any(so => so.Id == r.SellOrderId && so.Assistor == uid));
                    }

                    return receivables.Where(r =>
                        (r.SalesUserId != null && mapped.Contains(r.SalesUserId))
                        || sellOrders.Any(so => so.Id == r.SellOrderId &&
                            (so.Assistor == uid || (so.SalesUserId != null && mapped.Contains(so.SalesUserId)))));
                }

                if (mapped.Count == 0)
                    return receivables.Where(_ => false);
                return receivables.Where(r => r.SalesUserId != null && mapped.Contains(r.SalesUserId));
            }
            if (IsSaleDataScopeDenied(summary))
                return receivables.Where(_ => false);

            var uidSale = userId.Trim();
            if (summary.SaleDataScope == 1)
                return receivables.Where(r => r.SalesUserId == uidSale);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return receivables.Where(r => r.SalesUserId != null && allowUserIds.Contains(r.SalesUserId));
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinanceCustomerAdvance>> ApplyFinanceCustomerAdvanceListDataScopeAsync(
            string? userId,
            IQueryable<FinanceCustomerAdvance> advances,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return advances;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, advances, a => a.CreateByUserId, cancellationToken, allowCommerceAssistantReceiptBypass: true);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return advances;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                if (mapped.Count == 0)
                    return advances.Where(_ => false);
                return advances.Where(a => a.SalesUserId != null && mapped.Contains(a.SalesUserId));
            }
            if (IsSaleDataScopeDenied(summary))
                return advances.Where(_ => false);

            var uid = userId.Trim();
            if (summary.SaleDataScope == 1)
                return advances.Where(a => a.SalesUserId == uid);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return advances.Where(a => a.SalesUserId != null && allowUserIds.Contains(a.SalesUserId));
        }

        /// <inheritdoc />
        public async Task<IQueryable<FinanceSellInvoice>> ApplyFinanceSellInvoiceListDataScopeAsync(
            string? userId,
            IQueryable<FinanceSellInvoice> invoices,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return invoices;

            var (financeHandled, financeScoped) = await ApplyFinanceDepartmentScopeIfNeededAsync(
                userId, invoices, inv => inv.CreateByUserId, cancellationToken, allowCommerceAssistantReceiptBypass: true);
            if (financeHandled)
                return financeScoped;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return invoices;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                if (mapped.Count == 0)
                    return invoices.Where(_ => false);
                return invoices.Where(inv =>
                    customers.Any(c =>
                        c.Id == inv.CustomerId &&
                        c.SalesUserId != null &&
                        mapped.Contains(c.SalesUserId)));
            }
            if (IsSaleDataScopeDenied(summary))
                return invoices.Where(_ => false);

            var uid = userId.Trim();
            if (summary.SaleDataScope == 1)
            {
                return invoices.Where(inv =>
                    customers.Any(c => c.Id == inv.CustomerId && c.SalesUserId == uid));
            }

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return invoices.Where(inv =>
                customers.Any(c =>
                    c.Id == inv.CustomerId &&
                    c.SalesUserId != null &&
                    allowUserIds.Contains(c.SalesUserId)));
        }

        /// <inheritdoc />
        public async Task<IQueryable<SellOrder>> ApplySellOrderDataScopeAsync(
            string? userId,
            IQueryable<SellOrder> query,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                if (mapped.Count == 0)
                    return query.Where(x => x.Assistor == userId);
                return query.Where(x =>
                    x.Assistor == userId
                    || (x.SalesUserId != null && mapped.Contains(x.SalesUserId)));
            }
            if (summary.SaleDataScope == 0)
                return query;
            if (summary.SaleDataScope == 4)
                return query.Where(_ => false);

            if (summary.SaleDataScope == 1)
                return query.Where(x => x.SalesUserId == userId || x.Assistor == userId);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return query.Where(x =>
                (x.SalesUserId != null && allowUserIds.Contains(x.SalesUserId))
                || x.Assistor == userId);
        }

        /// <inheritdoc />
        public async Task<IQueryable<CustomerQuote>> ApplyCustomerQuoteListDataScopeAsync(
            string? userId,
            IQueryable<CustomerQuote> query,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                if (mapped.Count == 0)
                    return query.Where(_ => false);
                return query.Where(x => x.SalesUserId != null && mapped.Contains(x.SalesUserId));
            }

            if (summary.SaleDataScope == 0)
                return query;
            if (summary.SaleDataScope == 4)
                return query.Where(_ => false);
            if (summary.SaleDataScope == 1)
                return query.Where(x => x.SalesUserId == userId);

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return query.Where(x => x.SalesUserId != null && allowUserIds.Contains(x.SalesUserId));
        }

        private static bool IsSellOrderAssistor(SellOrder order, string userId) =>
            !string.IsNullOrWhiteSpace(order.Assistor)
            && string.Equals(order.Assistor.Trim(), userId, StringComparison.OrdinalIgnoreCase);

        private static bool MatchesSellOrderDataScope(
            SellOrder order,
            string userId,
            HashSet<string>? allowSalesUserIds)
        {
            if (IsSellOrderAssistor(order, userId))
                return true;
            if (allowSalesUserIds == null)
                return string.Equals(order.SalesUserId, userId, StringComparison.OrdinalIgnoreCase);
            return !string.IsNullOrWhiteSpace(order.SalesUserId)
                   && allowSalesUserIds.Contains(order.SalesUserId!);
        }

        /// <inheritdoc />
        public async Task<IQueryable<RFQ>> ApplyRfqMainListDataScopeAsync(
            string? userId,
            IQueryable<RFQ> query,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return query;

            var isCommerce = BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);
            if (!isCommerce && (summary.SaleDataScope == 0 || summary.PurchaseDataScope == 0))
                return query;

            if (!isCommerce && summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
                return query.Where(_ => false);

            HashSet<string>? saleAllow = null;
            if (isCommerce)
                saleAllow = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
            else if (summary.SaleDataScope == 2 || summary.SaleDataScope == 3)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);

            HashSet<string>? purchaseAllow = null;
            if (summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);

            var uid = userId.Trim();
            var commerceMappedIds = isCommerce ? saleAllow?.ToList() ?? new List<string>() : null;

            return query.Where(r =>
                (
                    isCommerce
                        ? commerceMappedIds!.Count > 0
                          && r.SalesUserId != null
                          && commerceMappedIds.Contains(r.SalesUserId)
                        : summary.SaleDataScope != 4
                          && (
                              (summary.SaleDataScope == 1 && r.SalesUserId != null && r.SalesUserId == uid)
                              || ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3)
                                  && saleAllow != null
                                  && r.SalesUserId != null
                                  && saleAllow.Contains(r.SalesUserId))
                          )
                )
                ||
                (
                    summary.PurchaseDataScope != 4 &&
                    (
                        (summary.PurchaseDataScope == 1 &&
                         r.Items.Any(i =>
                             i.AssignedPurchaserUserId1 == uid ||
                             i.AssignedPurchaserUserId2 == uid)) ||
                        ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3) &&
                         purchaseAllow != null &&
                         r.Items.Any(i =>
                             (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId1) &&
                              purchaseAllow.Contains(i.AssignedPurchaserUserId1!)) ||
                             (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId2) &&
                              purchaseAllow.Contains(i.AssignedPurchaserUserId2!))))
                    )
                ));
        }

        /// <inheritdoc />
        public Task<HashSet<string>> GetAllowedUserIdsForDataScopeAsync(
            UserPermissionSummaryDto summary,
            bool includeChildren,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return GetAllowedUserIdsAsync(summary, includeChildren);
        }

        /// <inheritdoc />
        public async Task<HashSet<string>> GetSaleScopeAllowUserIdsAsync(
            UserPermissionSummaryDto summary,
            bool includeChildren,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                if (string.IsNullOrWhiteSpace(summary.UserId))
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return await GetCommerceAssistantMappedSalesUserIdsAsync(summary.UserId);
            }

            if (summary.SaleDataScope == 1)
            {
                var self = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(summary.UserId))
                    self.Add(summary.UserId);
                return self;
            }

            if (summary.SaleDataScope is 2 or 3)
                return await GetAllowedUserIdsAsync(summary, includeChildren);

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<IQueryable<Quote>> ApplyQuoteListDataScopeAsync(
            string? userId,
            IQueryable<Quote> quotes,
            IQueryable<RFQ> rfqs,
            IQueryable<RFQItem> rfqItems,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return quotes;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return quotes;
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return quotes;

            var isCommerce = BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);
            if (!isCommerce && (summary.SaleDataScope == 0 || summary.PurchaseDataScope == 0))
                return quotes;

            if (!isCommerce && summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
                return quotes.Where(_ => false);

            HashSet<string>? saleAllow = null;
            if (isCommerce)
                saleAllow = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
            else if (summary.SaleDataScope == 2 || summary.SaleDataScope == 3)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);

            HashSet<string>? purchaseAllow = null;
            if (summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);

            var uid = userId.Trim();
            var commerceMappedIds = isCommerce ? saleAllow?.ToList() ?? new List<string>() : null;

            return quotes.Where(q =>
                (
                    isCommerce
                        ? commerceMappedIds!.Count > 0
                          && ((q.SalesUserId != null && commerceMappedIds.Contains(q.SalesUserId))
                              || (q.RFQId != null
                                  && rfqs.Any(r =>
                                      r.Id == q.RFQId
                                      && r.SalesUserId != null
                                      && commerceMappedIds.Contains(r.SalesUserId))))
                        : summary.SaleDataScope != 4 &&
                          (
                              (summary.SaleDataScope == 1 &&
                               ((q.SalesUserId != null && q.SalesUserId == uid) ||
                                (q.RFQId != null &&
                                 rfqs.Any(r => r.Id == q.RFQId && r.SalesUserId != null && r.SalesUserId == uid)))) ||
                              ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) &&
                               saleAllow != null &&
                               ((q.SalesUserId != null && saleAllow.Contains(q.SalesUserId)) ||
                                (q.RFQId != null &&
                                 rfqs.Any(r =>
                                     r.Id == q.RFQId &&
                                     r.SalesUserId != null &&
                                     saleAllow.Contains(r.SalesUserId)))))
                          )
                )
                ||
                (
                    summary.PurchaseDataScope != 4 &&
                    (
                        (summary.PurchaseDataScope == 1 &&
                         ((q.PurchaseUserId != null && q.PurchaseUserId == uid) ||
                          (q.RFQItemId != null &&
                           rfqItems.Any(i =>
                               i.Id == q.RFQItemId &&
                               (i.AssignedPurchaserUserId1 == uid || i.AssignedPurchaserUserId2 == uid))))) ||
                        ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3) &&
                         purchaseAllow != null &&
                         ((q.PurchaseUserId != null && purchaseAllow.Contains(q.PurchaseUserId)) ||
                          (q.RFQItemId != null &&
                           rfqItems.Any(i =>
                               i.Id == q.RFQItemId &&
                               ((!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId1) &&
                                 purchaseAllow.Contains(i.AssignedPurchaserUserId1!)) ||
                                (!string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId2) &&
                                 purchaseAllow.Contains(i.AssignedPurchaserUserId2!)))))))
                    )
                ));
        }

        /// <inheritdoc />
        public async Task<IQueryable<PurchaseRequisition>> ApplyPurchaseRequisitionListDataScopeAsync(
            string? userId,
            IQueryable<PurchaseRequisition> query,
            IQueryable<SellOrder> sellOrders,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return query;

            var saleOpen = summary.SaleDataScope == 0;
            var purchaseOpen = summary.PurchaseDataScope == 0;
            if (saleOpen && purchaseOpen)
                return query;

            if (summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
                return query.Where(_ => false);

            IQueryable<SellOrder>? scopedOrders = null;
            if (!saleOpen && summary.SaleDataScope != 4)
                scopedOrders = await ApplySellOrderDataScopeAsync(userId, sellOrders, cancellationToken);

            var uid = userId.Trim();
            HashSet<string>? purchaseAllow = null;
            if (!purchaseOpen && summary.PurchaseDataScope != 4 &&
                (summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3))
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3);

            return query.Where(pr =>
                (saleOpen
                 || (summary.SaleDataScope != 4 && scopedOrders!.Any(so => so.Id == pr.SellOrderId)))
                || (purchaseOpen
                    || (summary.PurchaseDataScope != 4 && (
                        (summary.PurchaseDataScope == 1 && pr.PurchaseUserId == uid)
                        || ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
                            && purchaseAllow != null
                            && pr.PurchaseUserId != null
                            && purchaseAllow.Contains(pr.PurchaseUserId))))));
        }

        /// <inheritdoc />
        public async Task<IQueryable<StockOutRequest>> ApplyStockOutRequestListDataScopeAsync(
            string? userId,
            IQueryable<StockOutRequest> query,
            IQueryable<SellOrder> sellOrders,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                if (summary.HasBizDataBypass || summary.LogisticsDataScope == 0 || IsSaleDataScopeUnrestricted(summary))
                    return query;
            }

            var scopedOrders = await ApplySellOrderDataScopeAsync(userId, sellOrders, cancellationToken);
            return query.Where(r => scopedOrders.Any(so => so.Id == r.SalesOrderId));
        }

        /// <inheritdoc />
        public async Task<IQueryable<StockOut>> ApplyStockOutListDataScopeAsync(
            string? userId,
            IQueryable<StockOut> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsSaleDataScopeUnrestricted(summary))
                return query;
            if (summary.LogisticsDataScope == 0)
                return query;
            if (IsSaleDataScopeDenied(summary))
                return query.Where(_ => false);

            var scopedOrders = await ApplySellOrderDataScopeAsync(userId, sellOrders, cancellationToken);
            var scopedCustomers = await ApplyCustomerListDataScopeAsync(userId, customers, cancellationToken);

            return query.Where(so =>
                (so.SellOrderItemId != null &&
                 sellOrderItems.Any(sol =>
                     sol.Id == so.SellOrderItemId &&
                     scopedOrders.Any(o => o.Id == sol.SellOrderId)))
                ||
                (so.CustomerId != null &&
                 scopedCustomers.Any(c => c.Id == so.CustomerId)));
        }

        /// <inheritdoc />
        public async Task<IQueryable<StockIn>> ApplyStockInListDataScopeAsync(
            string? userId,
            IQueryable<StockIn> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<StockInItemExtend> stockInItemExtends,
            IQueryable<PurchaseOrderItem> purchaseOrderItems,
            IQueryable<PurchaseOrder> purchaseOrders,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;

            if (summary.LogisticsDataScope == 0)
                return query;

            var saleOpen = IsSaleDataScopeUnrestricted(summary);
            var purchaseOpen = summary.PurchaseDataScope == 0;
            var logisticsOpen = summary.LogisticsDataScope == 0;
            if (saleOpen && purchaseOpen && logisticsOpen)
                return query;

            if (IsSaleDataScopeDenied(summary) && summary.PurchaseDataScope == 4 && summary.LogisticsDataScope == 4)
                return query.Where(_ => false);

            IQueryable<SellOrder>? scopedOrders = null;
            if (!saleOpen)
                scopedOrders = await ApplySellOrderDataScopeAsync(userId, sellOrders, cancellationToken);

            IQueryable<PurchaseOrder>? scopedPo = null;
            if (!purchaseOpen)
                scopedPo = await ApplyPurchaseOrderDataScopeAsync(userId, purchaseOrders, cancellationToken);

            IReadOnlyList<string> logisticsUserIds = Array.Empty<string>();
            if (!logisticsOpen && summary.LogisticsDataScope != 4)
            {
                if (summary.LogisticsDataScope == 1)
                    logisticsUserIds = new[] { userId.Trim() };
                else
                    logisticsUserIds = (await GetAllowedUserIdsAsync(summary, includeChildren: summary.LogisticsDataScope == 3))
                        .ToList();
            }

            var scopedOrdersQuery = scopedOrders ?? sellOrders.Where(_ => false);
            var scopedPoQuery = scopedPo ?? purchaseOrders.Where(_ => false);

            return query.Where(si =>
                (stockInItemExtends.Any(ext =>
                    !ext.IsDeleted
                    && ext.StockInId == si.Id
                    && ext.PurchaseOrderItemId != null)
                 && (purchaseOpen
                     || stockInItemExtends.Any(ext =>
                         !ext.IsDeleted
                         && ext.StockInId == si.Id
                         && ext.PurchaseOrderItemId != null
                         && purchaseOrderItems.Any(poi =>
                             poi.Id == ext.PurchaseOrderItemId
                             && scopedPoQuery.Any(po => po.Id == poi.PurchaseOrderId)))))
                || (!stockInItemExtends.Any(ext =>
                        !ext.IsDeleted
                        && ext.StockInId == si.Id
                        && ext.PurchaseOrderItemId != null)
                    && ((saleOpen
                         || stockInItemExtends.Any(ext =>
                             !ext.IsDeleted
                             && ext.StockInId == si.Id
                             && (
                                 (ext.SellOrderItemId != null
                                  && sellOrderItems.Any(sol =>
                                      sol.Id == ext.SellOrderItemId
                                      && scopedOrdersQuery.Any(o => o.Id == sol.SellOrderId)))
                                 || (ext.PurchaseOrderItemId != null
                                     && purchaseOrderItems.Any(poi =>
                                         poi.Id == ext.PurchaseOrderItemId
                                         && poi.SellOrderItemId != null
                                         && sellOrderItems.Any(sol =>
                                             sol.Id == poi.SellOrderItemId
                                             && scopedOrdersQuery.Any(o => o.Id == sol.SellOrderId)))))))
                        || (purchaseOpen
                            || stockInItemExtends.Any(ext =>
                                !ext.IsDeleted
                                && ext.StockInId == si.Id
                                && ext.PurchaseOrderItemId != null
                                && purchaseOrderItems.Any(poi =>
                                    poi.Id == ext.PurchaseOrderItemId
                                    && scopedPoQuery.Any(po => po.Id == poi.PurchaseOrderId))))
                        || (logisticsOpen
                            || (logisticsUserIds.Count > 0
                                && ((si.CreateByUserId != null && logisticsUserIds.Contains(si.CreateByUserId))
                                    || (si.CreatedBy != null && logisticsUserIds.Contains(si.CreatedBy))))))));
        }

        /// <inheritdoc />
        public async Task<IQueryable<Packing>> ApplyPackingListDataScopeAsync(
            string? userId,
            IQueryable<Packing> query,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || IsSaleDataScopeUnrestricted(summary))
                return query;
            if (summary.LogisticsDataScope == 0)
                return query;
            if (IsSaleDataScopeDenied(summary))
                return query.Where(_ => false);

            var uid = userId.Trim();
            var scopedCustomers = await ApplyCustomerListDataScopeAsync(userId, customers, cancellationToken);

            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = (await GetCommerceAssistantMappedSalesUserIdsAsync(userId)).ToList();
                return query.Where(p =>
                    (p.SalesId != null && mapped.Contains(p.SalesId)) ||
                    (p.CustomerId != null && scopedCustomers.Any(c => c.Id == p.CustomerId)));
            }

            if (summary.SaleDataScope == 1)
            {
                return query.Where(p =>
                    (p.SalesId != null && p.SalesId == uid) ||
                    (p.CustomerId != null && scopedCustomers.Any(c => c.Id == p.CustomerId)));
            }

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return query.Where(p =>
                (p.SalesId != null && allowUserIds.Contains(p.SalesId)) ||
                (p.CustomerId != null && scopedCustomers.Any(c => c.Id == p.CustomerId)));
        }

        /// <inheritdoc />
        public async Task<IQueryable<StockItem>> ApplyStockItemListDataScopeAsync(
            string? userId,
            IQueryable<StockItem> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;

            if (summary.LogisticsDataScope == 0)
                return query;

            var isCommerceSale = BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);
            if (IsSaleDataScopeUnrestricted(summary) || summary.PurchaseDataScope == 0)
                return query;

            if (IsSaleDataScopeDenied(summary) && summary.PurchaseDataScope == 4 && !isCommerceSale)
                return query.Where(_ => false);

            var uid = userId.Trim();
            var financeCustomerOpen = IsFinanceDepartmentIdentity(summary.IdentityType);

            if ((!IsSaleDataScopeDenied(summary) || isCommerceSale) && summary.PurchaseDataScope == 4)
                return await ApplyStockItemSaleScopeFilterAsync(
                    query, summary, uid, financeCustomerOpen, sellOrders, sellOrderItems, customers);

            if (IsSaleDataScopeDenied(summary) && !isCommerceSale && summary.PurchaseDataScope != 4)
                return await ApplyStockItemPurchaseScopeFilterAsync(query, summary, uid);

            var saleAllows = summary.SaleDataScope is 2 or 3
                ? (await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3)).ToList()
                : new List<string>();
            var purchaseAllows = summary.PurchaseDataScope is 2 or 3
                ? (await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3)).ToList()
                : new List<string>();

            if (summary.SaleDataScope == 1 && summary.PurchaseDataScope == 1)
            {
                return query.Where(si =>
                    si.SalespersonId == uid
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && (o.SalesUserId == uid || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && (financeCustomerOpen
                            ? customers.Any(c => c.Id == si.CustomerId)
                            : customers.Any(c => c.Id == si.CustomerId && c.SalesUserId == uid)))
                    || si.PurchaserId == uid);
            }

            if (summary.SaleDataScope == 1 && summary.PurchaseDataScope is 2 or 3)
            {
                if (purchaseAllows.Count == 0)
                    return ApplyStockItemSaleScope1Only(query, uid, financeCustomerOpen, sellOrders, sellOrderItems, customers);
                return query.Where(si =>
                    si.SalespersonId == uid
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && (o.SalesUserId == uid || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && (financeCustomerOpen
                            ? customers.Any(c => c.Id == si.CustomerId)
                            : customers.Any(c => c.Id == si.CustomerId && c.SalesUserId == uid)))
                    || (si.PurchaserId != null && purchaseAllows.Contains(si.PurchaserId)));
            }

            if (summary.SaleDataScope is 2 or 3 && summary.PurchaseDataScope == 1)
            {
                if (saleAllows.Count == 0)
                    return query.Where(si => si.PurchaserId == uid);
                return query.Where(si =>
                    (si.SalespersonId != null && saleAllows.Contains(si.SalespersonId))
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && ((o.SalesUserId != null && saleAllows.Contains(o.SalesUserId))
                                    || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && (financeCustomerOpen
                            ? customers.Any(c => c.Id == si.CustomerId)
                            : customers.Any(c =>
                                c.Id == si.CustomerId
                                && c.SalesUserId != null
                                && saleAllows.Contains(c.SalesUserId))))
                    || si.PurchaserId == uid);
            }

            if (summary.SaleDataScope is 2 or 3 && summary.PurchaseDataScope is 2 or 3)
            {
                if (saleAllows.Count == 0 && purchaseAllows.Count == 0)
                    return query.Where(_ => false);
                if (saleAllows.Count == 0)
                    return query.Where(si =>
                        si.PurchaserId != null && purchaseAllows.Contains(si.PurchaserId));
                if (purchaseAllows.Count == 0)
                {
                    return query.Where(si =>
                        (si.SalespersonId != null && saleAllows.Contains(si.SalespersonId))
                        || (si.SellOrderItemId != null
                            && sellOrderItems.Any(sol =>
                                sol.Id == si.SellOrderItemId
                                && sellOrders.Any(o =>
                                    o.Id == sol.SellOrderId
                                    && ((o.SalesUserId != null && saleAllows.Contains(o.SalesUserId))
                                        || o.Assistor == uid))))
                        || (si.CustomerId != null
                            && (financeCustomerOpen
                                ? customers.Any(c => c.Id == si.CustomerId)
                                : customers.Any(c =>
                                    c.Id == si.CustomerId
                                    && c.SalesUserId != null
                                    && saleAllows.Contains(c.SalesUserId)))));
                }

                return query.Where(si =>
                    (si.SalespersonId != null && saleAllows.Contains(si.SalespersonId))
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && ((o.SalesUserId != null && saleAllows.Contains(o.SalesUserId))
                                    || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && (financeCustomerOpen
                            ? customers.Any(c => c.Id == si.CustomerId)
                            : customers.Any(c =>
                                c.Id == si.CustomerId
                                && c.SalesUserId != null
                                && saleAllows.Contains(c.SalesUserId))))
                    || (si.PurchaserId != null && purchaseAllows.Contains(si.PurchaserId)));
            }

            return query.Where(_ => false);
        }

        private static IQueryable<StockItem> ApplyStockItemSaleScope1Only(
            IQueryable<StockItem> query,
            string uid,
            bool financeCustomerOpen,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers) =>
            query.Where(si =>
                si.SalespersonId == uid
                || (si.SellOrderItemId != null
                    && sellOrderItems.Any(sol =>
                        sol.Id == si.SellOrderItemId
                        && sellOrders.Any(o =>
                            o.Id == sol.SellOrderId
                            && (o.SalesUserId == uid || o.Assistor == uid))))
                || (si.CustomerId != null
                    && (financeCustomerOpen
                        ? customers.Any(c => c.Id == si.CustomerId)
                        : customers.Any(c => c.Id == si.CustomerId && c.SalesUserId == uid))));

        private async Task<IQueryable<StockItem>> ApplyStockItemSaleScopeFilterAsync(
            IQueryable<StockItem> query,
            UserPermissionSummaryDto summary,
            string uid,
            bool financeCustomerOpen,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers)
        {
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var saleAllows = (await GetCommerceAssistantMappedSalesUserIdsAsync(summary.UserId!)).ToList();
                if (saleAllows.Count == 0)
                    return query.Where(_ => false);

                return query.Where(si =>
                    (si.SalespersonId != null && saleAllows.Contains(si.SalespersonId))
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && ((o.SalesUserId != null && saleAllows.Contains(o.SalesUserId))
                                    || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && customers.Any(c =>
                            c.Id == si.CustomerId
                            && c.SalesUserId != null
                            && saleAllows.Contains(c.SalesUserId))));
            }

            if (summary.SaleDataScope == 1)
                return ApplyStockItemSaleScope1Only(query, uid, financeCustomerOpen, sellOrders, sellOrderItems, customers);

            if (summary.SaleDataScope is 2 or 3)
            {
                var saleAllows = (await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3)).ToList();
                if (saleAllows.Count == 0)
                    return query.Where(_ => false);

                return query.Where(si =>
                    (si.SalespersonId != null && saleAllows.Contains(si.SalespersonId))
                    || (si.SellOrderItemId != null
                        && sellOrderItems.Any(sol =>
                            sol.Id == si.SellOrderItemId
                            && sellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && ((o.SalesUserId != null && saleAllows.Contains(o.SalesUserId))
                                    || o.Assistor == uid))))
                    || (si.CustomerId != null
                        && (financeCustomerOpen
                            ? customers.Any(c => c.Id == si.CustomerId)
                            : customers.Any(c =>
                                c.Id == si.CustomerId
                                && c.SalesUserId != null
                                && saleAllows.Contains(c.SalesUserId)))));
            }

            return query.Where(_ => false);
        }

        private async Task<IQueryable<StockItem>> ApplyStockItemPurchaseScopeFilterAsync(
            IQueryable<StockItem> query,
            UserPermissionSummaryDto summary,
            string uid)
        {
            if (summary.PurchaseDataScope == 1)
                return query.Where(si => si.PurchaserId == uid);

            if (summary.PurchaseDataScope is 2 or 3)
            {
                var purchaseAllows = (await GetAllowedUserIdsAsync(summary, includeChildren: summary.PurchaseDataScope == 3)).ToList();
                if (purchaseAllows.Count == 0)
                    return query.Where(_ => false);
                return query.Where(si =>
                    si.PurchaserId != null && purchaseAllows.Contains(si.PurchaserId));
            }

            return query.Where(_ => false);
        }

        /// <inheritdoc />
        public async Task<IQueryable<StockInfo>> ApplyStockAggregateListDataScopeAsync(
            string? userId,
            IQueryable<StockInfo> query,
            IQueryable<StockItem> stockItems,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return query;

            if (summary.LogisticsDataScope == 0)
                return query;

            var saleOpen = IsSaleDataScopeUnrestricted(summary);
            var purchaseOpen = summary.PurchaseDataScope == 0;
            if (saleOpen || purchaseOpen)
                return query;

            if (IsSaleDataScopeDenied(summary) && summary.PurchaseDataScope == 4
                && !BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
                return query.Where(_ => false);

            var scopedItems = await ApplyStockItemListDataScopeAsync(
                userId,
                stockItems,
                sellOrders,
                sellOrderItems,
                customers,
                cancellationToken);

            return query.Where(s => scopedItems.Any(si => si.StockAggregateId == s.Id));
        }

        public async Task<IReadOnlyList<FinanceReceipt>> FilterFinanceReceiptsAsync(string userId, IEnumerable<FinanceReceipt> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (!summary.HasBizDataBypass && summary.FinanceDataScope == 4
                && !CommerceAssistantFinanceAccessRules.ShouldBypassFinanceDataScopeDenial(summary))
                return Array.Empty<FinanceReceipt>();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope is >= 1 and <= 3)
                return await FilterByFinanceCreatorAsync(userId, source, r => r.CreateByUserId, summary);

            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return source.ToList();
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                var uid = userId.Trim();
                var result = new List<FinanceReceipt>();
                foreach (var receipt in source)
                {
                    if (await CommerceAssistantCanAccessReceiptAsync(receipt, mapped, uid))
                        result.Add(receipt);
                }

                return result;
            }
            if (IsSaleDataScopeDenied(summary)) return Array.Empty<FinanceReceipt>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        private async Task<IReadOnlyList<FinanceReceivable>> FilterFinanceReceivablesAsync(string userId, IEnumerable<FinanceReceivable> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (!summary.HasBizDataBypass && summary.FinanceDataScope == 4
                && !CommerceAssistantFinanceAccessRules.ShouldBypassFinanceDataScopeDenial(summary))
                return Array.Empty<FinanceReceivable>();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope is >= 1 and <= 3)
                return await FilterByFinanceCreatorAsync(userId, source, r => r.CreateByUserId, summary);

            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return source.ToList();
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                var uid = userId.Trim();
                var result = new List<FinanceReceivable>();
                foreach (var receivable in source)
                {
                    if (await CommerceAssistantCanAccessReceivableAsync(receivable, mapped, uid))
                        result.Add(receivable);
                }

                return result;
            }
            if (IsSaleDataScopeDenied(summary)) return Array.Empty<FinanceReceivable>();

            var list = source.ToList();
            if (summary.SaleDataScope == 1)
                return list.Where(x => x.SalesUserId == userId).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return list.Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId) && allowUserIds.Contains(x.SalesUserId!)).ToList();
        }

        public async Task<IReadOnlyList<FinancePayment>> FilterFinancePaymentsAsync(string userId, IEnumerable<FinancePayment> source)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return source.ToList();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope == 4)
                return Array.Empty<FinancePayment>();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope is >= 1 and <= 3)
                return await FilterByFinanceCreatorAsync(userId, source, p => p.CreateByUserId, summary);

            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
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
            if (!summary.HasBizDataBypass && summary.FinanceDataScope == 4
                && !CommerceAssistantFinanceAccessRules.ShouldBypassFinanceDataScopeDenial(summary))
                return Array.Empty<FinanceSellInvoice>();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope is >= 1 and <= 3)
                return await FilterByFinanceCreatorAsync(userId, source, inv => inv.CreateByUserId, summary);

            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || IsSaleDataScopeUnrestricted(summary))
                return source.ToList();
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                if (mapped.Count == 0) return Array.Empty<FinanceSellInvoice>();

                var listCommerce = source.ToList();
                var customerIdsCommerce = listCommerce.Select(x => x.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (customerIdsCommerce.Count == 0) return Array.Empty<FinanceSellInvoice>();

                var customersCommerce = await _customerRepo.FindAsync(x => customerIdsCommerce.Contains(x.Id));
                var customerOwnerMapCommerce = customersCommerce.ToDictionary(x => x.Id, x => x.SalesUserId, StringComparer.OrdinalIgnoreCase);
                return listCommerce.Where(x =>
                        customerOwnerMapCommerce.TryGetValue(x.CustomerId, out var ownerId) &&
                        !string.IsNullOrWhiteSpace(ownerId) &&
                        mapped.Contains(ownerId!))
                    .ToList();
            }
            if (IsSaleDataScopeDenied(summary)) return Array.Empty<FinanceSellInvoice>();

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
            if (PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary))
                return source.ToList();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope == 4)
                return Array.Empty<FinancePurchaseInvoice>();
            if (!summary.HasBizDataBypass && summary.FinanceDataScope is >= 1 and <= 3)
                return await FilterByFinanceCreatorAsync(userId, source, inv => inv.CreateByUserId, summary);

            if (summary.HasBizDataBypass || IsFinanceDepartmentIdentity(summary.IdentityType) || summary.PurchaseDataScope == 0)
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

        /// <inheritdoc />
        public async Task<IQueryable<T>> ApplyLogisticsCreatorUserScopeAsync<T>(
            string? userId,
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, string?>> createByUserIdSelector,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || summary.LogisticsDataScope == 0)
                return query;
            if (summary.LogisticsDataScope == 4)
                return query.Where(_ => false);

            var uid = userId.Trim();
            if (summary.LogisticsDataScope == 1)
                return query.Where(BuildOwnerInListPredicate(createByUserIdSelector, new List<string> { uid }));

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.LogisticsDataScope == 3);
            var ids = allowUserIds.ToList();
            if (ids.Count == 0)
                return query.Where(_ => false);
            return query.Where(BuildOwnerInListPredicate(createByUserIdSelector, ids));
        }

        /// <inheritdoc />
        public async Task<IQueryable<T>> ApplyFinanceCreatorUserScopeAsync<T>(
            string? userId,
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, string?>> createByUserIdSelector,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return query;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass || summary.FinanceDataScope == 0)
                return query;
            if (summary.FinanceDataScope == 4)
                return query.Where(_ => false);

            var uid = userId.Trim();
            if (summary.FinanceDataScope == 1)
                return query.Where(BuildOwnerInListPredicate(createByUserIdSelector, new List<string> { uid }));

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.FinanceDataScope == 3);
            var ids = allowUserIds.ToList();
            if (ids.Count == 0)
                return query.Where(_ => false);
            return query.Where(BuildOwnerInListPredicate(createByUserIdSelector, ids));
        }

        private async Task<(bool Handled, IQueryable<T> Query)> ApplyFinanceDepartmentScopeIfNeededAsync<T>(
            string userId,
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, string?>> createByUserIdSelector,
            CancellationToken cancellationToken,
            bool allowCommerceAssistantReceiptBypass = false)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return (false, query);
            if (summary.FinanceDataScope == 4)
            {
                if (allowCommerceAssistantReceiptBypass
                    && CommerceAssistantFinanceAccessRules.ShouldBypassFinanceDataScopeDenial(summary))
                    return (false, query);
                return (true, query.Where(_ => false));
            }
            if (summary.FinanceDataScope is >= 1 and <= 3)
            {
                var scoped = await ApplyFinanceCreatorUserScopeAsync(userId, query, createByUserIdSelector, cancellationToken);
                return (true, scoped);
            }

            return (false, query);
        }

        private async Task<List<T>> FilterByFinanceCreatorAsync<T>(
            string userId,
            IEnumerable<T> source,
            System.Func<T, string?> createByUserIdSelector,
            UserPermissionSummaryDto summary)
        {
            var list = source.ToList();
            var uid = userId.Trim();
            if (summary.FinanceDataScope == 1)
                return list.Where(x =>
                {
                    var owner = createByUserIdSelector(x);
                    return !string.IsNullOrWhiteSpace(owner) && string.Equals(owner.Trim(), uid, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.FinanceDataScope == 3);
            return list.Where(x =>
            {
                var owner = createByUserIdSelector(x);
                return !string.IsNullOrWhiteSpace(owner) && allowUserIds.Contains(owner!);
            }).ToList();
        }

        private static System.Linq.Expressions.Expression<Func<T, bool>> BuildOwnerInListPredicate<T>(
            System.Linq.Expressions.Expression<Func<T, string?>> ownerSelector,
            List<string> allowedUserIds)
        {
            var param = ownerSelector.Parameters[0];
            var member = ownerSelector.Body;
            var notNull = System.Linq.Expressions.Expression.NotEqual(member, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
            var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains), new[] { typeof(string) })!;
            var inList = System.Linq.Expressions.Expression.Call(
                System.Linq.Expressions.Expression.Constant(allowedUserIds),
                containsMethod,
                member);
            var body = System.Linq.Expressions.Expression.AndAlso(notNull, inList);
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param);
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
            if (summary.HasBizDataBypass) return true;
            if (await PassesSaleAccessToRfqAsync(userId, rfq, summary)) return true;
            return await PassesPurchaseAccessToRfqAsync(userId, rfq, summary);
        }

        public async Task<Func<RFQ, RFQItem, bool>> GetRfqItemLineVisibilityPredicateAsync(string userId)
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            if (summary.HasBizDataBypass)
                return (_, __) => true;

            var protectionMinutes = await _purchaseQuoterPoolService.GetDemandProtectionMinutesAsync();
            var utcNow = DateTime.UtcNow;

            HashSet<string>? saleAllow = null;
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
                saleAllow = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
            else if (summary.SaleDataScope == 2)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.SaleDataScope == 3)
                saleAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            HashSet<string>? purchaseAllow = null;
            if (summary.PurchaseDataScope == 2)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: false);
            else if (summary.PurchaseDataScope == 3)
                purchaseAllow = await GetAllowedUserIdsAsync(summary, includeChildren: true);

            var uid = userId.Trim();
            var isCommerce = BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);

            return (rfq, item) =>
            {
                bool saleOk = false;
                if (isCommerce)
                {
                    saleOk = saleAllow != null
                             && saleAllow.Count > 0
                             && !string.IsNullOrWhiteSpace(rfq.SalesUserId)
                             && saleAllow.Contains(rfq.SalesUserId);
                }
                else if (!IsSaleDataScopeDenied(summary))
                {
                    if (summary.SaleDataScope == 0) saleOk = true;
                    else if (summary.SaleDataScope == 1)
                        saleOk = string.Equals(rfq.SalesUserId, uid, StringComparison.OrdinalIgnoreCase);
                    else if ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) && saleAllow != null && !string.IsNullOrWhiteSpace(rfq.SalesUserId))
                        saleOk = saleAllow.Contains(rfq.SalesUserId);
                }

                if (saleOk) return true;

                return RfqDemandProtectionRules.IsPurchaseSideVisible(
                    summary, item, uid, purchaseAllow, protectionMinutes, utcNow);
            };
        }

        private async Task<bool> PassesSaleAccessToRfqAsync(string userId, RFQ rfq, UserPermissionSummaryDto summary)
        {
            if (BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            {
                var mapped = await GetCommerceAssistantMappedSalesUserIdsAsync(userId);
                return !string.IsNullOrWhiteSpace(rfq.SalesUserId) && mapped.Contains(rfq.SalesUserId);
            }
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

            var protectionMinutes = await _purchaseQuoterPoolService.GetDemandProtectionMinutesAsync();
            var utcNow = DateTime.UtcNow;

            HashSet<string>? purchaseAllow = summary.PurchaseDataScope == 3
                ? await GetAllowedUserIdsAsync(summary, includeChildren: true)
                : summary.PurchaseDataScope == 2
                    ? await GetAllowedUserIdsAsync(summary, includeChildren: false)
                    : null;

            return items.Any(i =>
                RfqDemandProtectionRules.IsPurchaseSideVisible(
                    summary, i, userId, purchaseAllow, protectionMinutes, utcNow));
        }

        /// <inheritdoc />
        public async Task<bool> CanViewRfqTagsAsync(string userId, string? createByUserId, string? salesUserId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var uid = userId.Trim();
            var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
            if (summary.HasBizDataBypass) return true;
            if (!string.IsNullOrWhiteSpace(createByUserId)
                && string.Equals(createByUserId.Trim(), uid, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(salesUserId)
                && string.Equals(salesUserId.Trim(), uid, StringComparison.OrdinalIgnoreCase))
                return true;
            return await IsSalesSuperiorForRfqTagsAsync(uid, salesUserId, summary);
        }

        /// <inheritdoc />
        public async Task<bool> CanEditRfqTagsAsync(string userId, string? createByUserId, string? salesUserId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var uid = userId.Trim();
            var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
            if (summary.HasBizDataBypass) return false;
            if (!string.IsNullOrWhiteSpace(createByUserId)
                && string.Equals(createByUserId.Trim(), uid, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(salesUserId)
                && string.Equals(salesUserId.Trim(), uid, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private async Task<bool> IsSalesSuperiorForRfqTagsAsync(
            string userId,
            string? salesUserId,
            UserPermissionSummaryDto summary)
        {
            if (string.IsNullOrWhiteSpace(salesUserId)) return false;
            if (string.Equals(salesUserId.Trim(), userId, StringComparison.OrdinalIgnoreCase)) return false;
            if (summary.SaleDataScope != 2 && summary.SaleDataScope != 3) return false;
            var allowUserIds = await GetAllowedUserIdsAsync(summary, includeChildren: summary.SaleDataScope == 3);
            return allowUserIds.Contains(salesUserId.Trim());
        }

        public async Task<bool> CanAccessSalesOrderAsync(string userId, SellOrder salesOrder)
        {
            if (await IsLogisticsModuleUnrestrictedAsync(userId))
                return true;

            var filtered = await FilterSalesOrdersAsync(userId, new[] { salesOrder });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessPurchaseOrderAsync(string userId, PurchaseOrder purchaseOrder)
        {
            if (await IsLogisticsModuleUnrestrictedAsync(userId))
                return true;

            var filtered = await FilterPurchaseOrdersAsync(userId, new[] { purchaseOrder });
            return filtered.Count > 0;
        }

        /// <inheritdoc />
        public async Task<bool> IsLogisticsModuleUnrestrictedAsync(string? userId, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(userId))
                return true;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            return summary.HasBizDataBypass || summary.LogisticsDataScope == 0;
        }

        public async Task<bool> CanAccessFinanceReceiptAsync(string userId, FinanceReceipt receipt)
        {
            var filtered = await FilterFinanceReceiptsAsync(userId, new[] { receipt });
            return filtered.Count > 0;
        }

        public async Task<bool> CanAccessFinanceReceivableAsync(string userId, FinanceReceivable receivable)
        {
            var filtered = await FilterFinanceReceivablesAsync(userId, new[] { receivable });
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

        private async Task<HashSet<string>> GetCommerceAssistantMappedSalesUserIdsAsync(string userId)
        {
            var key = userId.Trim();
            if (_commerceMappedSalesCache.TryGetValue(key, out var cached))
                return cached;

            var destIds = await _relationMapService.GetMappedDestIdsAsync(
                SysRelationMapTypeCode.SalesAssistantToSalesperson,
                key);
            var set = destIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _commerceMappedSalesCache[key] = set;
            return set;
        }

        private async Task<bool> CommerceAssistantCanAccessReceiptAsync(
            FinanceReceipt receipt,
            HashSet<string> mapped,
            string uid)
        {
            if (!string.IsNullOrWhiteSpace(receipt.SalesUserId) && mapped.Contains(receipt.SalesUserId))
                return true;

            var items = await _receiptItemRepo.FindAsync(i =>
                i.FinanceReceiptId == receipt.Id && !i.IsDeleted && i.SellOrderId != null);
            var sellOrderIds = items
                .Select(i => i.SellOrderId!)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sellOrderIds.Count == 0)
                return false;

            var orders = await _sellOrderRepo.FindAsync(o => sellOrderIds.Contains(o.Id));
            if (mapped.Count == 0)
                return orders.Any(so => IsSellOrderAssistor(so, uid));

            return orders.Any(so =>
                IsSellOrderAssistor(so, uid)
                || (!string.IsNullOrWhiteSpace(so.SalesUserId) && mapped.Contains(so.SalesUserId)));
        }

        private async Task<bool> CommerceAssistantCanAccessReceivableAsync(
            FinanceReceivable receivable,
            HashSet<string> mapped,
            string uid)
        {
            if (!string.IsNullOrWhiteSpace(receivable.SalesUserId) && mapped.Contains(receivable.SalesUserId))
                return true;
            if (string.IsNullOrWhiteSpace(receivable.SellOrderId))
                return false;

            var orders = await _sellOrderRepo.FindAsync(o => o.Id == receivable.SellOrderId);
            var order = orders.FirstOrDefault();
            if (order == null)
                return false;
            if (mapped.Count == 0)
                return IsSellOrderAssistor(order, uid);

            return IsSellOrderAssistor(order, uid)
                || (!string.IsNullOrWhiteSpace(order.SalesUserId) && mapped.Contains(order.SalesUserId));
        }

        private static bool IsSaleDataScopeUnrestricted(UserPermissionSummaryDto summary) =>
            summary.SaleDataScope == 0 && !BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);

        private static bool IsSaleDataScopeDenied(UserPermissionSummaryDto summary) =>
            summary.SaleDataScope == 4 && !BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);

        /// <summary>3=director, 2=manager, 1=employee, 0=unknown.</summary>
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

            if (normalized.Any(x => x.Contains("DIRECTOR") || x.Contains("æ»ç")))
                return 3;
            if (normalized.Any(x => x.Contains("MANAGER") || x.Contains("ç»ç")))
                return 2;
            if (normalized.Any(x => x.Contains("EMPLOYEE") || x.Contains("STAFF") || x.Contains("åå·¥")))
                return 1;
            return 0;
        }
    }
}
