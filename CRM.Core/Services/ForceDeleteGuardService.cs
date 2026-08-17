using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
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
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<PickingTask> _pickingTaskRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepo;
    private readonly IRepository<FinanceReceipt> _financeReceiptRepo;
    private readonly IRepository<FinanceReceivable> _financeReceivableRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<CustomsDeclaration> _customsDeclarationRepo;
    private readonly IRepository<FinancePurchaseInvoiceWriteOff> _financePurchaseInvoiceWriteOffRepo;

    public ForceDeleteGuardService(
        IRepository<FinancePaymentItem> financePaymentItemRepo,
        IRepository<FinanceReceiptItem> financeReceiptItemRepo,
        IRepository<FinancePurchaseInvoice> financePurchaseInvoiceRepo,
        IRepository<FinanceSellInvoice> financeSellInvoiceRepo,
        IRepository<SellInvoiceItem> financeSellInvoiceItemRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<PickingTask> pickingTaskRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<PurchaseOrderItem> purchaseOrderItemRepo,
        IRepository<FinanceReceipt> financeReceiptRepo,
        IRepository<FinanceReceivable> financeReceivableRepo,
        IRepository<Packing> packingRepo,
        IRepository<CustomsDeclaration> customsDeclarationRepo,
        IRepository<FinancePurchaseInvoiceWriteOff> financePurchaseInvoiceWriteOffRepo)
    {
        _financePaymentItemRepo = financePaymentItemRepo;
        _financeReceiptItemRepo = financeReceiptItemRepo;
        _financePurchaseInvoiceRepo = financePurchaseInvoiceRepo;
        _financeSellInvoiceRepo = financeSellInvoiceRepo;
        _financeSellInvoiceItemRepo = financeSellInvoiceItemRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _packingItemRepo = packingItemRepo;
        _pickingTaskRepo = pickingTaskRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _purchaseOrderItemRepo = purchaseOrderItemRepo;
        _financeReceiptRepo = financeReceiptRepo;
        _financeReceivableRepo = financeReceivableRepo;
        _packingRepo = packingRepo;
        _customsDeclarationRepo = customsDeclarationRepo;
        _financePurchaseInvoiceWriteOffRepo = financePurchaseInvoiceWriteOffRepo;
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
        if (items.Any(x => x.AdvancePoolAmount > 0m))
            return ForceDeleteGuardResult.Deny("存在下游业务节点：客户预收池入账，须先回滚预收池后再删除");
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
        var writeOffs = (await _financePurchaseInvoiceWriteOffRepo.FindAsync(x =>
                x.FinancePurchaseInvoiceId == financePurchaseInvoiceId)).ToList();
        if (writeOffs.Count > 0)
            reasons.Add($"存在下游业务节点：进项发票核销流水 {writeOffs.Count} 笔，须先反核销后再删除");
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
        var packingIds = (await _packingItemRepo.FindAsync(pi =>
                pi.StockOutNotifyId != null && pi.StockOutNotifyId == key))
            .Where(pi => !pi.IsDeleted)
            .Select(pi => pi.PackingId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var linkedPickingTasks = packingIds.Count == 0
            ? new List<PickingTask>()
            : (await _pickingTaskRepo.FindAsync(t =>
                    t.PackingId != null && packingIds.Contains(t.PackingId)))
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
        var receivable = (await _financeReceivableRepo.FindAsync(r =>
            r.StockOutId == stockOutId && !r.IsDeleted)).FirstOrDefault();
        if (receivable != null && receivable.VerifiedDone > 0m)
            return ForceDeleteGuardResult.Deny(
                $"该出库单已有收款核销（已核销 {receivable.VerifiedDone}），不可删除");
        return ForceDeleteGuardResult.Allow();
    }

    public async Task<ForceDeleteGuardResult> CanForceDeleteStockInAsync(string stockInId)
    {
        if (string.IsNullOrWhiteSpace(stockInId))
            return ForceDeleteGuardResult.Deny("入库单ID不能为空");
        var key = stockInId.Trim();
        var writeOffs = (await _financePurchaseInvoiceWriteOffRepo.FindAsync(w => w.StockInId == key)).ToList();
        if (writeOffs.Count == 0)
            return ForceDeleteGuardResult.Allow();

        var invoiceIds = writeOffs
            .Select(w => w.FinancePurchaseInvoiceId?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
        var invoiceCodes = invoiceIds.Count == 0
            ? Array.Empty<string>()
            : (await _financePurchaseInvoiceRepo.FindAsync(x => invoiceIds.Contains(x.Id)))
                .Select(x => string.IsNullOrWhiteSpace(x.InvoiceCode) ? x.InvoiceNo : x.InvoiceCode.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        return ForceDeleteGuardResult.Deny(invoiceCodes.Length == 0
            ? "存在下游业务节点：进项发票核销，不能强制删除入库单"
            : $"存在下游业务节点：进项发票核销；进项发票单号：{string.Join("、", invoiceCodes)}");
    }

    public async Task<ForceDeleteGuardResult> CanForceDeletePackingAsync(string packingId)
    {
        if (string.IsNullOrWhiteSpace(packingId))
            return ForceDeleteGuardResult.Deny("装箱单ID不能为空");

        var packing = await _packingRepo.GetByIdAsync(packingId.Trim());
        if (packing == null || packing.IsDeleted)
            return ForceDeleteGuardResult.Deny("装箱单不存在或已删除");

        var pid = packing.Id.Trim();

        var linkedOutItems = (await _stockOutItemRepo.FindAsync(x =>
                x.PackingId != null && x.PackingId == pid && !x.IsDeleted))
            .ToList();
        if (linkedOutItems.Count > 0)
        {
            var stockOutIds = linkedOutItems
                .Select(x => x.StockOutId?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
            var stockOutCodes = stockOutIds.Count == 0
                ? Array.Empty<string>()
                : (await _stockOutRepo.FindAsync(x => stockOutIds.Contains(x.Id) && !x.IsDeleted))
                    .Select(x => string.IsNullOrWhiteSpace(x.StockOutCode) ? x.Id : x.StockOutCode.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();
            return ForceDeleteGuardResult.Deny(stockOutCodes.Length == 0
                ? "存在下游业务节点：出库单，不能强制删除装箱单"
                : $"存在下游业务节点：出库单；下游数据单号：{string.Join("、", stockOutCodes)}");
        }

        var packingItems = (await _packingItemRepo.FindAsync(i => i.PackingId == pid && !i.IsDeleted)).ToList();
        var notifyIds = packingItems
            .Select(i => i.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
        if (notifyIds.Count > 0)
        {
            var stockedOutCodes = (await _stockOutRequestRepo.FindAsync(r => notifyIds.Contains(r.Id) && !r.IsDeleted))
                .Where(r => r.Status == StockOutRequestStatusCode.StockedOut)
                .Select(r => string.IsNullOrWhiteSpace(r.RequestCode) ? r.Id : r.RequestCode.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
            if (stockedOutCodes.Length > 0)
                return ForceDeleteGuardResult.Deny(
                    $"关联出库通知已出库，不能强制删除装箱单；通知单号：{string.Join("、", stockedOutCodes)}");
        }

        var declarationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headerDecId = packing.CustomsDeclarationId?.Trim();
        if (!string.IsNullOrEmpty(headerDecId))
            declarationIds.Add(headerDecId);

        var linkedDeclarations = (await _customsDeclarationRepo.FindAsync(d =>
                d.PackingId != null && d.PackingId == pid && !d.IsDeleted))
            .ToList();
        foreach (var dec in linkedDeclarations)
            declarationIds.Add(dec.Id.Trim());

        if (declarationIds.Count > 0)
        {
            var declarationCodes = new List<string>();
            foreach (var decId in declarationIds)
            {
                var dec = await _customsDeclarationRepo.GetByIdAsync(decId);
                if (dec == null || dec.IsDeleted)
                    continue;
                var code = string.IsNullOrWhiteSpace(dec.DeclarationCode) ? dec.Id : dec.DeclarationCode.Trim();
                if (!string.IsNullOrWhiteSpace(code))
                    declarationCodes.Add(code);
            }

            if (declarationCodes.Count > 0)
            {
                return ForceDeleteGuardResult.Deny(
                    $"存在下游业务节点：报关单；下游数据单号：{string.Join("、", declarationCodes.Distinct(StringComparer.OrdinalIgnoreCase).Take(5))}");
            }
        }

        return ForceDeleteGuardResult.Allow();
    }
}
