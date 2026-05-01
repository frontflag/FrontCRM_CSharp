using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

public class ForceDeleteGuardService : IForceDeleteGuardService
{
    private readonly IRepository<FinancePaymentItem> _financePaymentItemRepo;
    private readonly IRepository<FinanceReceiptItem> _financeReceiptItemRepo;
    private readonly IRepository<FinancePurchaseInvoice> _financePurchaseInvoiceRepo;
    private readonly IRepository<FinanceSellInvoice> _financeSellInvoiceRepo;
    private readonly IRepository<SellInvoiceItem> _financeSellInvoiceItemRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<PickingTask> _pickingTaskRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepo;
    private readonly IRepository<FinanceReceipt> _financeReceiptRepo;

    public ForceDeleteGuardService(
        IRepository<FinancePaymentItem> financePaymentItemRepo,
        IRepository<FinanceReceiptItem> financeReceiptItemRepo,
        IRepository<FinancePurchaseInvoice> financePurchaseInvoiceRepo,
        IRepository<FinanceSellInvoice> financeSellInvoiceRepo,
        IRepository<SellInvoiceItem> financeSellInvoiceItemRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<PickingTask> pickingTaskRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<PurchaseOrderItem> purchaseOrderItemRepo,
        IRepository<FinanceReceipt> financeReceiptRepo)
    {
        _financePaymentItemRepo = financePaymentItemRepo;
        _financeReceiptItemRepo = financeReceiptItemRepo;
        _financePurchaseInvoiceRepo = financePurchaseInvoiceRepo;
        _financeSellInvoiceRepo = financeSellInvoiceRepo;
        _financeSellInvoiceItemRepo = financeSellInvoiceItemRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _pickingTaskRepo = pickingTaskRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _purchaseOrderItemRepo = purchaseOrderItemRepo;
        _financeReceiptRepo = financeReceiptRepo;
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteFinancePaymentAsync(string financePaymentId)
    {
        if (string.IsNullOrWhiteSpace(financePaymentId))
            return ForceDeleteGuardResult.Deny("付款单ID不能为空");
        var items = (await _financePaymentItemRepo.FindAsync(x => x.FinancePaymentId == financePaymentId)).ToList();
        var blockedItems = items
            .Where(x => x.VerificationStatus > 0 || x.VerificationDone > 0m)
            .ToList();
        if (blockedItems.Count > 0)
        {
            var poItemIds = blockedItems
                .Select(x => x.PurchaseOrderItemId?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
            var poItemCodes = poItemIds.Count == 0
                ? Array.Empty<string>()
                : (await _purchaseOrderItemRepo.FindAsync(x => poItemIds.Contains(x.Id)))
                    .Select(x => string.IsNullOrWhiteSpace(x.PurchaseOrderItemCode) ? x.Id : x.PurchaseOrderItemCode.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();
            return ForceDeleteGuardResult.Deny(poItemCodes.Length == 0
                ? "存在下游业务节点：付款核销明细，不能强制删除付款单"
                : $"存在下游业务节点：付款核销明细；下游数据单号：{string.Join("、", poItemCodes)}");
        }
        return ForceDeleteGuardResult.Allow();
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteFinanceReceiptAsync(string financeReceiptId)
    {
        if (string.IsNullOrWhiteSpace(financeReceiptId))
            return ForceDeleteGuardResult.Deny("收款单ID不能为空");
        var items = (await _financeReceiptItemRepo.FindAsync(x => x.FinanceReceiptId == financeReceiptId)).ToList();
        var blockedItems = items
            .Where(x => x.VerificationStatus > 0 || x.VerifiedAmount > 0m)
            .ToList();
        if (blockedItems.Count > 0)
        {
            var invoiceIds = blockedItems
                .Select(x => x.FinanceSellInvoiceId?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
            var invoiceCodes = invoiceIds.Count == 0
                ? Array.Empty<string>()
                : (await _financeSellInvoiceRepo.FindAsync(x => invoiceIds.Contains(x.Id)))
                    .Select(x => string.IsNullOrWhiteSpace(x.InvoiceCode) ? x.Id : x.InvoiceCode.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();
            return ForceDeleteGuardResult.Deny(invoiceCodes.Length == 0
                ? "存在下游业务节点：收款核销明细，不能强制删除收款单"
                : $"存在下游业务节点：销项发票；下游数据单号：{string.Join("、", invoiceCodes)}");
        }
        return ForceDeleteGuardResult.Allow();
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteFinancePurchaseInvoiceAsync(string financePurchaseInvoiceId)
    {
        if (string.IsNullOrWhiteSpace(financePurchaseInvoiceId))
            return ForceDeleteGuardResult.Deny("进项发票ID不能为空");
        var header = await _financePurchaseInvoiceRepo.GetByIdAsync(financePurchaseInvoiceId);
        if (header == null)
            return ForceDeleteGuardResult.Deny("进项发票不存在");
        var reasons = new List<string>();
        if (header.ConfirmStatus == 1)
            reasons.Add("进项发票已认证，需先人工反处理后再删除");
        if (header.RedInvoiceStatus == 1)
            reasons.Add("进项发票已冲红，需先人工核对后再删除");
        return reasons.Count == 0 ? ForceDeleteGuardResult.Allow() : ForceDeleteGuardResult.Deny(reasons);
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteFinanceSellInvoiceAsync(string financeSellInvoiceId)
    {
        if (string.IsNullOrWhiteSpace(financeSellInvoiceId))
            return ForceDeleteGuardResult.Deny("销项发票ID不能为空");
        var header = await _financeSellInvoiceRepo.GetByIdAsync(financeSellInvoiceId);
        if (header == null)
            return ForceDeleteGuardResult.Deny("销项发票不存在");
        var items = (await _financeSellInvoiceItemRepo.FindAsync(x => x.FinanceSellInvoiceId == financeSellInvoiceId)).ToList();
        if (header.ReceiveStatus > 0 || header.ReceiveDone > 0m || items.Any(x => x.ReceiveStatus > 0))
        {
            var receiptItemRows = (await _financeReceiptItemRepo.FindAsync(x => x.FinanceSellInvoiceId == financeSellInvoiceId))
                .Where(x => x.VerificationStatus > 0 || x.VerifiedAmount > 0m)
                .ToList();
            var receiptIds = receiptItemRows
                .Select(x => x.FinanceReceiptId?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
            var receiptCodes = receiptIds.Count == 0
                ? Array.Empty<string>()
                : (await _financeReceiptRepo.FindAsync(x => receiptIds.Contains(x.Id)))
                    .Select(x => string.IsNullOrWhiteSpace(x.FinanceReceiptCode) ? x.Id : x.FinanceReceiptCode.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();
            return ForceDeleteGuardResult.Deny(receiptCodes.Length == 0
                ? "存在下游业务节点：收款单核销，不能强制删除销项发票"
                : $"存在下游业务节点：收款单；下游数据单号：{string.Join("、", receiptCodes)}");
        }
        return ForceDeleteGuardResult.Allow();
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteStockOutRequestAsync(string stockOutRequestId)
    {
        if (string.IsNullOrWhiteSpace(stockOutRequestId))
            return ForceDeleteGuardResult.Deny("出库通知ID不能为空");
        var request = await _stockOutRequestRepo.GetByIdAsync(stockOutRequestId);
        if (request == null)
            return ForceDeleteGuardResult.Deny("出库通知不存在");
        var key = stockOutRequestId.Trim();
        var keyLower = key.ToLowerInvariant();
        var linkedPickingTasks = (await _pickingTaskRepo.FindAsync(x =>
                x.StockOutRequestId != null &&
                x.StockOutRequestId.ToLower() == keyLower))
            .Where(t => !t.IsDeleted && t.Status != -1)
            .ToList();
        if (linkedPickingTasks.Count > 0)
        {
            var codes = linkedPickingTasks
                .Select(t => string.IsNullOrWhiteSpace(t.TaskCode) ? t.Id : t.TaskCode.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
            return ForceDeleteGuardResult.Deny(
                $"存在下游业务节点：拣货单；下游数据单号：{string.Join("、", codes)}");
        }
        return ForceDeleteGuardResult.Allow();
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteStockOutAsync(string stockOutId)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return ForceDeleteGuardResult.Deny("出库单ID不能为空");
        var stockOut = await _stockOutRepo.GetByIdAsync(stockOutId);
        if (stockOut == null)
            return ForceDeleteGuardResult.Deny("出库单不存在");
        return ForceDeleteGuardResult.Allow();
    }
}
