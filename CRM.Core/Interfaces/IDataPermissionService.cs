using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces
{
    public interface IDataPermissionService
    {
        Task<IReadOnlyList<CustomerInfo>> FilterCustomersAsync(string userId, IEnumerable<CustomerInfo> source);
        Task<IReadOnlyList<VendorInfo>> FilterVendorsAsync(string userId, IEnumerable<VendorInfo> source);
        Task<IReadOnlyList<RFQListItem>> FilterRFQsAsync(string userId, IEnumerable<RFQListItem> source);
        Task<IReadOnlyList<SellOrder>> FilterSalesOrdersAsync(string userId, IEnumerable<SellOrder> source);
        Task<IReadOnlyList<PurchaseOrder>> FilterPurchaseOrdersAsync(string userId, IEnumerable<PurchaseOrder> source);
        Task<IReadOnlyList<FinanceReceipt>> FilterFinanceReceiptsAsync(string userId, IEnumerable<FinanceReceipt> source);
        Task<IReadOnlyList<FinancePayment>> FilterFinancePaymentsAsync(string userId, IEnumerable<FinancePayment> source);
        Task<IReadOnlyList<FinanceSellInvoice>> FilterFinanceSellInvoicesAsync(string userId, IEnumerable<FinanceSellInvoice> source);
        Task<IReadOnlyList<FinancePurchaseInvoice>> FilterFinancePurchaseInvoicesAsync(string userId, IEnumerable<FinancePurchaseInvoice> source);

        Task<bool> CanAccessCustomerAsync(string userId, CustomerInfo customer);
        Task<bool> CanAccessVendorAsync(string userId, VendorInfo vendor);
        Task<bool> CanAccessRFQAsync(string userId, RFQ rfq);
        Task<bool> CanAccessSalesOrderAsync(string userId, SellOrder salesOrder);
        Task<bool> CanAccessPurchaseOrderAsync(string userId, PurchaseOrder purchaseOrder);
        Task<bool> CanAccessFinanceReceiptAsync(string userId, FinanceReceipt receipt);
        Task<bool> CanAccessFinancePaymentAsync(string userId, FinancePayment payment);
        Task<bool> CanAccessFinanceSellInvoiceAsync(string userId, FinanceSellInvoice sellInvoice);
        Task<bool> CanAccessFinancePurchaseInvoiceAsync(string userId, FinancePurchaseInvoice purchaseInvoice);
    }
}
