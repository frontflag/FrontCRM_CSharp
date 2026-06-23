using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces
{
    public interface IDataPermissionService
    {
        Task<IReadOnlyList<CustomerInfo>> FilterCustomersAsync(string userId, IEnumerable<CustomerInfo> source);

        /// <summary>
        /// 将客户列表销售数据范围套用到 <see cref="IQueryable{CustomerInfo}"/>（与 <see cref="FilterCustomersAsync"/> 规则一致）。
        /// </summary>
        Task<IQueryable<CustomerInfo>> ApplyCustomerListDataScopeAsync(
            string? userId,
            IQueryable<CustomerInfo> query,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// 供应商列表数据范围：除部门采购范围「禁止」(4) 外，不按 <c>VendorInfo.PurchaseUserId</c> 过滤，
        /// 任意用户均可看到全部供应商（报价选供应商等）；「专属供应商」排除待模型字段落地后在此实现。
        /// </summary>
        Task<IReadOnlyList<VendorInfo>> FilterVendorsAsync(string userId, IEnumerable<VendorInfo> source);

        /// <summary>
        /// 将供应商列表采购数据范围套用到 <see cref="IQueryable{VendorInfo}"/>（与 <see cref="FilterVendorsAsync"/> 规则一致）。
        /// </summary>
        Task<IQueryable<VendorInfo>> ApplyVendorListDataScopeAsync(
            string? userId,
            IQueryable<VendorInfo> query,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RFQListItem>> FilterRFQsAsync(string userId, IEnumerable<RFQListItem> source);
        Task<IReadOnlyList<SellOrder>> FilterSalesOrdersAsync(string userId, IEnumerable<SellOrder> source);
        Task<IReadOnlyList<PurchaseOrder>> FilterPurchaseOrdersAsync(string userId, IEnumerable<PurchaseOrder> source);

        /// <summary>
        /// 将采购订单采购数据范围套用到可翻译的 <see cref="IQueryable{T}"/>（与 <see cref="FilterPurchaseOrdersAsync"/> 规则一致）。
        /// 到货通知、质检、入库批次、批次对账等列表经关联采购单复用本方法。
        /// </summary>
        Task<IQueryable<PurchaseOrder>> ApplyPurchaseOrderDataScopeAsync(
            string? userId,
            IQueryable<PurchaseOrder> query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将销售订单销售数据范围套用到 <see cref="IQueryable{T}"/>（与 <see cref="FilterSalesOrdersAsync"/> 规则一致）。
        /// </summary>
        Task<IQueryable<SellOrder>> ApplySellOrderDataScopeAsync(
            string? userId,
            IQueryable<SellOrder> query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将需求主表列表数据范围套用到 <see cref="IQueryable{RFQ}"/>（与 <see cref="FilterRFQsAsync"/> 规则一致：
        /// 销售数据范围命中主表业务员，或采购数据范围命中该需求下任一条明细的分配采购员）。
        /// </summary>
        Task<IQueryable<RFQ>> ApplyRfqMainListDataScopeAsync(
            string? userId,
            IQueryable<RFQ> query,
            CancellationToken cancellationToken = default);

        /// <summary>报价列表：销售数据范围（报价/关联 RFQ 业务员）∨ 采购数据范围（报价采购员或关联 RFQ 明细分配采购员）。</summary>
        Task<IQueryable<Quote>> ApplyQuoteListDataScopeAsync(
            string? userId,
            IQueryable<Quote> quotes,
            IQueryable<RFQ> rfqs,
            IQueryable<RFQItem> rfqItems,
            CancellationToken cancellationToken = default);

        /// <summary>采购申请列表：关联销售订单在销售范围内，或 <c>PurchaseUserId</c> 在采购范围内。</summary>
        Task<IQueryable<PurchaseRequisition>> ApplyPurchaseRequisitionListDataScopeAsync(
            string? userId,
            IQueryable<PurchaseRequisition> query,
            IQueryable<SellOrder> sellOrders,
            CancellationToken cancellationToken = default);

        /// <summary>装箱单列表：按 <c>SalesId</c> 或客户归属收窄销售数据范围。</summary>
        Task<IQueryable<Packing>> ApplyPackingListDataScopeAsync(
            string? userId,
            IQueryable<Packing> query,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default);

        /// <summary>出库通知列表：按 <c>SalesOrderId</c> 销售数据范围过滤。</summary>
        Task<IQueryable<StockOutRequest>> ApplyStockOutRequestListDataScopeAsync(
            string? userId,
            IQueryable<StockOutRequest> query,
            IQueryable<SellOrder> sellOrders,
            CancellationToken cancellationToken = default);

        /// <summary>出库单列表：经销售明细或客户归属收窄销售数据范围。</summary>
        Task<IQueryable<StockOut>> ApplyStockOutListDataScopeAsync(
            string? userId,
            IQueryable<StockOut> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default);

        /// <summary>入库单列表：无采购明细关联时 销售∨采购∨物流；有关联采购明细时须满足采购数据范围（不可被销售/物流 OR 绕过）。</summary>
        Task<IQueryable<StockIn>> ApplyStockInListDataScopeAsync(
            string? userId,
            IQueryable<StockIn> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<StockInItemExtend> stockInItemExtends,
            IQueryable<PurchaseOrderItem> purchaseOrderItems,
            IQueryable<PurchaseOrder> purchaseOrders,
            CancellationToken cancellationToken = default);

        /// <summary>在库明细列表：销售数据范围 ∨ 采购数据范围（<c>PurchaserId</c>）。</summary>
        Task<IQueryable<StockItem>> ApplyStockItemListDataScopeAsync(
            string? userId,
            IQueryable<StockItem> query,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default);

        /// <summary>库存汇总（<c>stock</c>）列表：至少一条在库明细落在销售或采购数据范围内。</summary>
        Task<IQueryable<StockInfo>> ApplyStockAggregateListDataScopeAsync(
            string? userId,
            IQueryable<StockInfo> query,
            IQueryable<StockItem> stockItems,
            IQueryable<SellOrder> sellOrders,
            IQueryable<SellOrderItem> sellOrderItems,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default);

        /// <summary>当前用户所在部门（及可选子部门）范围内可见的用户 ID，供需求明细等 EF 查询与数据范围表达式复用。</summary>
        Task<HashSet<string>> GetAllowedUserIdsForDataScopeAsync(
            UserPermissionSummaryDto summary,
            bool includeChildren,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FinanceReceipt>> FilterFinanceReceiptsAsync(string userId, IEnumerable<FinanceReceipt> source);
        Task<IReadOnlyList<FinancePayment>> FilterFinancePaymentsAsync(string userId, IEnumerable<FinancePayment> source);
        Task<IReadOnlyList<FinanceSellInvoice>> FilterFinanceSellInvoicesAsync(string userId, IEnumerable<FinanceSellInvoice> source);
        Task<IReadOnlyList<FinancePurchaseInvoice>> FilterFinancePurchaseInvoicesAsync(string userId, IEnumerable<FinancePurchaseInvoice> source);

        /// <summary>
        /// 将付款单列表采购数据范围套用到 <see cref="IQueryable{FinancePayment}"/>（与 <see cref="FilterFinancePaymentsAsync"/> 一致：按供应商 <c>PurchaseUserId</c> 归属）。
        /// </summary>
        Task<IQueryable<FinancePayment>> ApplyFinancePaymentListDataScopeAsync(
            string? userId,
            IQueryable<FinancePayment> payments,
            IQueryable<VendorInfo> vendors,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将进项发票列表采购数据范围套用到 <see cref="IQueryable{FinancePurchaseInvoice}"/>（与 <see cref="FilterFinancePurchaseInvoicesAsync"/> 一致）。
        /// </summary>
        Task<IQueryable<FinancePurchaseInvoice>> ApplyFinancePurchaseInvoiceListDataScopeAsync(
            string? userId,
            IQueryable<FinancePurchaseInvoice> invoices,
            IQueryable<VendorInfo> vendors,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将收款单列表销售数据范围套用到 <see cref="IQueryable{FinanceReceipt}"/>（与 <see cref="FilterFinanceReceiptsAsync"/> 一致：主表 <c>SalesUserId</c>，财务部不按业务员收窄）。
        /// </summary>
        Task<IQueryable<FinanceReceipt>> ApplyFinanceReceiptListDataScopeAsync(
            string? userId,
            IQueryable<FinanceReceipt> receipts,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将应收款列表销售数据范围套用到 <see cref="IQueryable{FinanceReceivable}"/>（主表 <c>SalesUserId</c>）。
        /// </summary>
        Task<IQueryable<FinanceReceivable>> ApplyFinanceReceivableListDataScopeAsync(
            string? userId,
            IQueryable<FinanceReceivable> receivables,
            CancellationToken cancellationToken = default);

        Task<IQueryable<FinanceCustomerAdvance>> ApplyFinanceCustomerAdvanceListDataScopeAsync(
            string? userId,
            IQueryable<FinanceCustomerAdvance> advances,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 将销项发票列表销售数据范围套用到 <see cref="IQueryable{FinanceSellInvoice}"/>（与 <see cref="FilterFinanceSellInvoicesAsync"/> 一致：按客户 <c>SalesUserId</c> 归属）。
        /// </summary>
        Task<IQueryable<FinanceSellInvoice>> ApplyFinanceSellInvoiceListDataScopeAsync(
            string? userId,
            IQueryable<FinanceSellInvoice> invoices,
            IQueryable<CustomerInfo> customers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 物流数据范围：按单据创建人用户 Id 过滤（<c>LogisticsDataScope</c>；0 全部、4 无、1/2/3 与组织数据范围一致）。
        /// 用于入库/出库/报关等带 <c>CreateByUserId</c> 的列表。
        /// </summary>
        Task<IQueryable<T>> ApplyLogisticsCreatorUserScopeAsync<T>(
            string? userId,
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, string?>> createByUserIdSelector,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 财务数据范围：按单据创建人用户 Id 过滤（<c>FinanceDataScope</c>；0 全部、4 无、1/2/3 与组织数据范围一致）。
        /// </summary>
        Task<IQueryable<T>> ApplyFinanceCreatorUserScopeAsync<T>(
            string? userId,
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, string?>> createByUserIdSelector,
            CancellationToken cancellationToken = default);

        Task<bool> CanAccessCustomerAsync(string userId, CustomerInfo customer);
        Task<bool> CanAccessVendorAsync(string userId, VendorInfo vendor);
        Task<bool> CanAccessRFQAsync(string userId, RFQ rfq);

        /// <summary>需求明细行是否可见：销售数据范围 或 采购分配范围（采购员/经理/总监）。</summary>
        Task<Func<RFQ, RFQItem, bool>> GetRfqItemLineVisibilityPredicateAsync(string userId);
        Task<bool> CanAccessSalesOrderAsync(string userId, SellOrder salesOrder);
        Task<bool> CanAccessPurchaseOrderAsync(string userId, PurchaseOrder purchaseOrder);

        /// <summary>物流数据范围「全部」：入库/出库/库存/到货/质检/装箱/拣货等物流板块不按销采范围收窄。</summary>
        Task<bool> IsLogisticsModuleUnrestrictedAsync(string? userId, CancellationToken cancellationToken = default);
        Task<bool> CanAccessFinanceReceiptAsync(string userId, FinanceReceipt receipt);
        Task<bool> CanAccessFinancePaymentAsync(string userId, FinancePayment payment);
        Task<bool> CanAccessFinanceSellInvoiceAsync(string userId, FinanceSellInvoice sellInvoice);
        Task<bool> CanAccessFinancePurchaseInvoiceAsync(string userId, FinancePurchaseInvoice purchaseInvoice);
    }
}
