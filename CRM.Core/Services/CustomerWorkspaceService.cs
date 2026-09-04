using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class CustomerWorkspaceService : ICustomerWorkspaceService
{
    private readonly IRepository<RFQ> _rfqRepo;
    private readonly IRepository<RFQItem> _rfqItemRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<FinanceReceipt> _financeReceiptRepo;
    private readonly IRepository<FinanceSellInvoice> _financeSellInvoiceRepo;
    private readonly IRepository<FinanceReceivable> _financeReceivableRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly IRbacService _rbacService;
    private readonly IEntityLookupService _entityLookup;

    public CustomerWorkspaceService(
        IRepository<RFQ> rfqRepo,
        IRepository<RFQItem> rfqItemRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<Packing> packingRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<FinanceReceipt> financeReceiptRepo,
        IRepository<FinanceSellInvoice> financeSellInvoiceRepo,
        IRepository<FinanceReceivable> financeReceivableRepo,
        IRepository<CustomerInfo> customerRepo,
        IDataPermissionService dataPermissionService,
        IRbacService rbacService,
        IEntityLookupService entityLookup)
    {
        _rfqRepo = rfqRepo;
        _rfqItemRepo = rfqItemRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _packingRepo = packingRepo;
        _packingItemRepo = packingItemRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _financeReceiptRepo = financeReceiptRepo;
        _financeSellInvoiceRepo = financeSellInvoiceRepo;
        _financeReceivableRepo = financeReceivableRepo;
        _customerRepo = customerRepo;
        _dataPermissionService = dataPermissionService;
        _rbacService = rbacService;
        _entityLookup = entityLookup;
    }

    public async Task<CustomerWorkspaceDto?> GetAsync(string source, string id, string viewerUserId)
    {
        var src = (source ?? string.Empty).Trim();
        var key = (id ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(src))
            throw new ArgumentException("source 不能为空");
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("id 不能为空");

        var uid = (viewerUserId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(uid))
            throw new UnauthorizedAccessException("未登录");

        var normalized = NormalizeSource(src);
        if (normalized == CustomerWorkspaceSourceCodes.Rfq)
            return await GetFromRfqAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.RfqItem)
            return await GetFromRfqItemAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.SellOrder)
            return await GetFromSellOrderAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.SellOrderItem)
            return await GetFromSellOrderItemAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.StockOutRequest)
            return await GetFromStockOutRequestAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.Packing)
            return await GetFromPackingAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.PackingItem)
            return await GetFromPackingItemAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.StockOut)
            return await GetFromStockOutAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.StockOutItem)
            return await GetFromStockOutItemAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.FinanceReceipt)
            return await GetFromFinanceReceiptAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.FinanceSellInvoice)
            return await GetFromFinanceSellInvoiceAsync(key, uid);
        if (normalized == CustomerWorkspaceSourceCodes.FinanceReceivable)
            return await GetFromFinanceReceivableAsync(key, uid);

        throw new ArgumentException("不支持的来源");
    }

    private static string NormalizeSource(string source)
    {
        if (source.Equals(CustomerWorkspaceSourceCodes.Rfq, StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.Rfq;
        if (source.Equals(CustomerWorkspaceSourceCodes.RfqItem, StringComparison.OrdinalIgnoreCase)
            || source.Equals("rfq-item", StringComparison.OrdinalIgnoreCase)
            || source.Equals("rfq_item", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.RfqItem;
        if (source.Equals(CustomerWorkspaceSourceCodes.SellOrder, StringComparison.OrdinalIgnoreCase)
            || source.Equals("sell-order", StringComparison.OrdinalIgnoreCase)
            || source.Equals("sell_order", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.SellOrder;
        if (source.Equals(CustomerWorkspaceSourceCodes.SellOrderItem, StringComparison.OrdinalIgnoreCase)
            || source.Equals("sell-order-item", StringComparison.OrdinalIgnoreCase)
            || source.Equals("sell_order_item", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.SellOrderItem;
        if (source.Equals(CustomerWorkspaceSourceCodes.StockOutRequest, StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock-out-request", StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock_out_request", StringComparison.OrdinalIgnoreCase)
            || source.Equals("stockOutNotify", StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock-out-notify", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.StockOutRequest;
        if (source.Equals(CustomerWorkspaceSourceCodes.Packing, StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.Packing;
        if (source.Equals(CustomerWorkspaceSourceCodes.PackingItem, StringComparison.OrdinalIgnoreCase)
            || source.Equals("packing-item", StringComparison.OrdinalIgnoreCase)
            || source.Equals("packing_item", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.PackingItem;
        if (source.Equals(CustomerWorkspaceSourceCodes.StockOut, StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock-out", StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock_out", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.StockOut;
        if (source.Equals(CustomerWorkspaceSourceCodes.StockOutItem, StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock-out-item", StringComparison.OrdinalIgnoreCase)
            || source.Equals("stock_out_item", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.StockOutItem;
        if (source.Equals(CustomerWorkspaceSourceCodes.FinanceReceipt, StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance-receipt", StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance_receipt", StringComparison.OrdinalIgnoreCase)
            || source.Equals("receipt", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.FinanceReceipt;
        if (source.Equals(CustomerWorkspaceSourceCodes.FinanceSellInvoice, StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance-sell-invoice", StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance_sell_invoice", StringComparison.OrdinalIgnoreCase)
            || source.Equals("sellInvoice", StringComparison.OrdinalIgnoreCase)
            || source.Equals("sell-invoice", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.FinanceSellInvoice;
        if (source.Equals(CustomerWorkspaceSourceCodes.FinanceReceivable, StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance-receivable", StringComparison.OrdinalIgnoreCase)
            || source.Equals("finance_receivable", StringComparison.OrdinalIgnoreCase)
            || source.Equals("receivable", StringComparison.OrdinalIgnoreCase))
            return CustomerWorkspaceSourceCodes.FinanceReceivable;
        return source;
    }

    private async Task<CustomerWorkspaceDto?> GetFromRfqAsync(string rfqId, string viewerUserId)
    {
        var rfq = await _rfqRepo.GetByIdAsync(rfqId);
        if (rfq == null || rfq.IsDeleted)
            return null;
        await EnsureCanAccessRfqAsync(viewerUserId, rfq);
        return await BuildFromCustomerIdAsync(rfq.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromRfqItemAsync(string itemId, string viewerUserId)
    {
        var item = await _rfqItemRepo.GetByIdAsync(itemId);
        if (item == null || item.IsDeleted)
            return null;

        var rfq = await _rfqRepo.GetByIdAsync(item.RfqId);
        if (rfq == null || rfq.IsDeleted)
            return null;

        await EnsureCanAccessRfqAsync(viewerUserId, rfq);
        return await BuildFromCustomerIdAsync(rfq.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromSellOrderAsync(string sellOrderId, string viewerUserId)
    {
        var so = await _sellOrderRepo.GetByIdAsync(sellOrderId);
        if (so == null || so.IsDeleted)
            return null;
        await EnsureCanAccessSellOrderAsync(viewerUserId, so);
        return await BuildFromCustomerIdAsync(so.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromSellOrderItemAsync(string itemId, string viewerUserId)
    {
        var item = await _sellOrderItemRepo.GetByIdAsync(itemId);
        if (item == null || item.IsDeleted)
            return null;

        var so = await _sellOrderRepo.GetByIdAsync(item.SellOrderId);
        if (so == null || so.IsDeleted)
            return null;

        await EnsureCanAccessSellOrderAsync(viewerUserId, so);
        return await BuildFromCustomerIdAsync(so.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromStockOutRequestAsync(string requestId, string viewerUserId)
    {
        var req = await _stockOutRequestRepo.GetByIdAsync(requestId);
        if (req == null || req.IsDeleted)
            return null;
        await EnsureCanAccessStockOutRequestAsync(viewerUserId, req);
        return await BuildFromCustomerIdAsync(req.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromPackingAsync(string packingId, string viewerUserId)
    {
        var packing = await _packingRepo.GetByIdAsync(packingId);
        if (packing == null || packing.IsDeleted)
            return null;
        await EnsureCanAccessPackingAsync(viewerUserId, packing);
        return await BuildFromCustomerIdAsync(packing.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromPackingItemAsync(string itemId, string viewerUserId)
    {
        var item = await _packingItemRepo.GetByIdAsync(itemId);
        if (item == null || item.IsDeleted)
            return null;

        var packing = await _packingRepo.GetByIdAsync(item.PackingId);
        if (packing == null || packing.IsDeleted)
            return null;

        await EnsureCanAccessPackingAsync(viewerUserId, packing);
        return await BuildFromCustomerIdAsync(packing.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromStockOutAsync(string stockOutId, string viewerUserId)
    {
        var stockOut = await _stockOutRepo.GetByIdAsync(stockOutId);
        if (stockOut == null || stockOut.IsDeleted)
            return null;
        await EnsureCanAccessStockOutDocAsync(viewerUserId, stockOut);
        return await BuildFromCustomerIdAsync(stockOut.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromStockOutItemAsync(string itemId, string viewerUserId)
    {
        var item = await _stockOutItemRepo.GetByIdAsync(itemId);
        if (item == null || item.IsDeleted)
            return null;

        var stockOut = await _stockOutRepo.GetByIdAsync(item.StockOutId);
        if (stockOut == null || stockOut.IsDeleted)
            return null;

        await EnsureCanAccessStockOutDocAsync(viewerUserId, stockOut);
        return await BuildFromCustomerIdAsync(stockOut.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromFinanceReceiptAsync(string receiptId, string viewerUserId)
    {
        var receipt = await _financeReceiptRepo.GetByIdAsync(receiptId);
        if (receipt == null || receipt.IsDeleted)
            return null;
        await EnsureCanAccessFinanceReceiptAsync(viewerUserId, receipt);
        return await BuildFromCustomerIdAsync(receipt.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromFinanceSellInvoiceAsync(string invoiceId, string viewerUserId)
    {
        var invoice = await _financeSellInvoiceRepo.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.IsDeleted)
            return null;
        await EnsureCanAccessFinanceSellInvoiceAsync(viewerUserId, invoice);
        return await BuildFromCustomerIdAsync(invoice.CustomerId, viewerUserId);
    }

    private async Task<CustomerWorkspaceDto?> GetFromFinanceReceivableAsync(string receivableId, string viewerUserId)
    {
        var receivable = await _financeReceivableRepo.GetByIdAsync(receivableId);
        if (receivable == null || receivable.IsDeleted)
            return null;
        await EnsureCanAccessFinanceReceivableAsync(viewerUserId, receivable);
        return await BuildFromCustomerIdAsync(receivable.CustomerId, viewerUserId);
    }

    private async Task EnsureCanAccessRfqAsync(string viewerUserId, RFQ rfq)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (!HasBizCode(summary, "rfq.read"))
            throw new UnauthorizedAccessException("无权限访问该需求");
        if (!await _dataPermissionService.CanAccessRFQAsync(viewerUserId, rfq))
            throw new UnauthorizedAccessException("无权限访问该需求");
    }

    private async Task EnsureCanAccessSellOrderAsync(string viewerUserId, SellOrder salesOrder)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (!HasBizCode(summary, "sales-order.read"))
            throw new UnauthorizedAccessException("无权限访问该销售订单");
        if (!await _dataPermissionService.CanAccessSalesOrderAsync(viewerUserId, salesOrder))
            throw new UnauthorizedAccessException("无权限访问该销售订单");
    }

    private async Task EnsureCanAccessFulfillmentReadAsync(string viewerUserId)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (HasBizCode(summary, "sales-order.read") || HasBizCode(summary, "purchase-order.read"))
            return;
        throw new UnauthorizedAccessException("无权限访问该履约单据");
    }

    private async Task EnsureCanAccessStockOutRequestAsync(string viewerUserId, StockOutRequest request)
    {
        await EnsureCanAccessFulfillmentReadAsync(viewerUserId);
        if (await _dataPermissionService.IsLogisticsModuleUnrestrictedAsync(viewerUserId))
            return;

        var so = await _sellOrderRepo.GetByIdAsync(request.SalesOrderId);
        if (so == null || so.IsDeleted)
            throw new UnauthorizedAccessException("无权限访问该出库通知");
        if (!await _dataPermissionService.CanAccessSalesOrderAsync(viewerUserId, so))
            throw new UnauthorizedAccessException("无权限访问该出库通知");
    }

    private async Task EnsureCanAccessPackingAsync(string viewerUserId, Packing packing)
    {
        await EnsureCanAccessFulfillmentReadAsync(viewerUserId);
        if (await _dataPermissionService.IsLogisticsModuleUnrestrictedAsync(viewerUserId))
            return;

        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (summary.SaleDataScope == 0)
            return;
        if (summary.SaleDataScope == 4
            && !BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            throw new UnauthorizedAccessException("无权限访问该装箱单");

        if (!string.IsNullOrWhiteSpace(packing.SalesId)
            && string.Equals(packing.SalesId.Trim(), viewerUserId, StringComparison.OrdinalIgnoreCase))
            return;

        var cid = packing.CustomerId?.Trim();
        if (!string.IsNullOrWhiteSpace(cid))
        {
            var customer = await _customerRepo.GetByIdAsync(cid);
            if (customer != null
                && !customer.IsDeleted
                && await _dataPermissionService.CanAccessCustomerAsync(viewerUserId, customer))
                return;
        }

        throw new UnauthorizedAccessException("无权限访问该装箱单");
    }

    private async Task EnsureCanAccessStockOutDocAsync(string viewerUserId, StockOut stockOut)
    {
        await EnsureCanAccessFulfillmentReadAsync(viewerUserId);
        if (await _dataPermissionService.IsLogisticsModuleUnrestrictedAsync(viewerUserId))
            return;

        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (summary.SaleDataScope == 0)
            return;
        if (summary.SaleDataScope == 4
            && !BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary))
            throw new UnauthorizedAccessException("无权限访问该出库单");

        var itemId = stockOut.SellOrderItemId?.Trim();
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            var soItem = await _sellOrderItemRepo.GetByIdAsync(itemId);
            if (soItem != null && !soItem.IsDeleted)
            {
                var so = await _sellOrderRepo.GetByIdAsync(soItem.SellOrderId);
                if (so != null
                    && !so.IsDeleted
                    && await _dataPermissionService.CanAccessSalesOrderAsync(viewerUserId, so))
                    return;
            }
        }

        var cid = stockOut.CustomerId?.Trim();
        if (!string.IsNullOrWhiteSpace(cid))
        {
            var customer = await _customerRepo.GetByIdAsync(cid);
            if (customer != null
                && !customer.IsDeleted
                && await _dataPermissionService.CanAccessCustomerAsync(viewerUserId, customer))
                return;
        }

        throw new UnauthorizedAccessException("无权限访问该出库单");
    }

    private async Task EnsureCanAccessFinanceReceiptAsync(string viewerUserId, FinanceReceipt receipt)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (!HasBizCode(summary, "finance-receipt.read"))
            throw new UnauthorizedAccessException("无权限访问该收款单");
        if (!await _dataPermissionService.CanAccessFinanceReceiptAsync(viewerUserId, receipt))
            throw new UnauthorizedAccessException("无权限访问该收款单");
    }

    private async Task EnsureCanAccessFinanceSellInvoiceAsync(string viewerUserId, FinanceSellInvoice invoice)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (!HasBizCode(summary, "finance-sell-invoice.read"))
            throw new UnauthorizedAccessException("无权限访问该销项发票");
        if (!await _dataPermissionService.CanAccessFinanceSellInvoiceAsync(viewerUserId, invoice))
            throw new UnauthorizedAccessException("无权限访问该销项发票");
    }

    private async Task EnsureCanAccessFinanceReceivableAsync(string viewerUserId, FinanceReceivable receivable)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        if (!HasBizCode(summary, "finance-receipt.read"))
            throw new UnauthorizedAccessException("无权限访问该应收款");
        if (!await _dataPermissionService.CanAccessFinanceReceivableAsync(viewerUserId, receivable))
            throw new UnauthorizedAccessException("无权限访问该应收款");
    }

    private async Task<CustomerWorkspaceDto> BuildFromCustomerIdAsync(string? customerId, string viewerUserId)
    {
        var cid = customerId?.Trim();
        if (string.IsNullOrWhiteSpace(cid))
            return new CustomerWorkspaceDto { HasCustomer = false };

        var customer = await _customerRepo.GetByIdAsync(cid);
        if (customer == null || customer.IsDeleted)
            return new CustomerWorkspaceDto { HasCustomer = false };

        var summary = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId);
        var canViewFull =
            !SaleSensitiveFieldMask521.ShouldMask(summary)
            && HasCustomerRead(summary)
            && await _dataPermissionService.CanAccessCustomerAsync(viewerUserId, customer);

        var salesUserName = await _entityLookup.GetUserLoginNameAsync(customer.SalesUserId);
        var dto = new CustomerWorkspaceDto
        {
            HasCustomer = true,
            CanViewFull = canViewFull,
            CustomerCode = NullIfWhite(customer.CustomerCode),
            SalesUserName = NullIfWhite(salesUserName)
        };

        if (!canViewFull)
            return dto;

        dto.CustomerId = customer.Id;
        dto.ChineseName = NullIfWhite(customer.OfficialName) ?? NullIfWhite(customer.StandardOfficialName);
        dto.EnglishName = NullIfWhite(customer.EnglishOfficialName);
        dto.CustomerType = customer.Type is > 0 ? customer.Type : null;
        dto.CustomerLevel = NullIfWhite(customer.CustomerLevel);
        dto.Industry = NullIfWhite(customer.Industry);
        dto.Region = NullIfWhite(customer.Region);
        dto.CreditLimit = customer.CreditLine;
        dto.PaymentTerms = customer.Payment;
        dto.SettlementCurrency = customer.TradeCurrency;
        return dto;
    }

    private static bool HasBizCode(UserPermissionSummaryDto summary, string code)
    {
        if (summary.IsSysAdmin || summary.HasBizDataBypass) return true;
        return summary.PermissionCodes.Any(c =>
            string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCustomerRead(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return true;
        return summary.PermissionCodes.Any(c =>
            string.Equals(c, "customer.read", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NullIfWhite(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }
}
