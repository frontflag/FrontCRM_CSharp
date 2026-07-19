using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class PurchaseOrderVendorChangeService : IPurchaseOrderVendorChangeService
{
    internal const string ManualVendorPlaceholderId = "00000000-0000-0000-0000-000000000002";

    private const short ArrivalNoticeStatusStockedIn = 100;
    private const short FinancePaymentStatusCompleted = 100;
    private const short FinancePaymentStatusCancelled = -2;
    private const byte PurchaseInvoiceConfirmStatusDone = 1;
    private const short PurchaseInvoiceRedStatusDone = 1;

    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<VendorInfo> _vendorRepo;
    private readonly IRepository<VendorContactInfo> _vendorContactRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<FinancePayment> _paymentRepo;
    private readonly IRepository<FinancePaymentItem> _paymentItemRepo;
    private readonly IRepository<FinancePurchaseInvoice> _purchaseInvoiceRepo;
    private readonly IRepository<FinancePurchaseInvoiceItem> _purchaseInvoiceItemRepo;
    private readonly ILogger<PurchaseOrderVendorChangeService> _logger;

    public PurchaseOrderVendorChangeService(
        IRepository<PurchaseOrder> poRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<VendorInfo> vendorRepo,
        IRepository<VendorContactInfo> vendorContactRepo,
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<FinancePayment> paymentRepo,
        IRepository<FinancePaymentItem> paymentItemRepo,
        IRepository<FinancePurchaseInvoice> purchaseInvoiceRepo,
        IRepository<FinancePurchaseInvoiceItem> purchaseInvoiceItemRepo,
        ILogger<PurchaseOrderVendorChangeService> logger)
    {
        _poRepo = poRepo;
        _poItemRepo = poItemRepo;
        _vendorRepo = vendorRepo;
        _vendorContactRepo = vendorContactRepo;
        _notifyRepo = notifyRepo;
        _stockInRepo = stockInRepo;
        _paymentRepo = paymentRepo;
        _paymentItemRepo = paymentItemRepo;
        _purchaseInvoiceRepo = purchaseInvoiceRepo;
        _purchaseInvoiceItemRepo = purchaseInvoiceItemRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderVendorChangePreviewResult> PreviewAsync(
        string purchaseOrderId,
        string newVendorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderId))
            throw new ArgumentException("采购订单ID不能为空", nameof(purchaseOrderId));

        var bundle = await LoadBundleAsync(purchaseOrderId.Trim(), cancellationToken);
        return await BuildPreviewAsync(bundle, newVendorId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderVendorChangeApplyResult> ApplyAsync(
        PurchaseOrder order,
        string newVendorId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        var bundle = await LoadBundleAsync(order.Id, cancellationToken);
        bundle.Order = order;

        var preview = await BuildPreviewAsync(bundle, newVendorId, cancellationToken);
        if (preview.NoOp)
            return new PurchaseOrderVendorChangeApplyResult { Preview = preview, Applied = false };

        if (!preview.CanChange)
            throw new InvalidOperationException(preview.BlockReason ?? "当前采购订单不可更换供应商");

        var vendor = await ResolveTargetVendorAsync(newVendorId, cancellationToken);
        var displayName = FormatVendorDisplayName(vendor)!;
        var vendorCode = string.IsNullOrWhiteSpace(vendor.Code) ? null : vendor.Code.Trim();

        var oldVendorId = order.VendorId?.Trim();
        order.VendorId = vendor.Id.Trim();
        order.VendorName = displayName;
        order.VendorCode = vendorCode;
        await ClearVendorContactIfMismatchAsync(order, vendor.Id, cancellationToken);

        foreach (var item in bundle.Items)
        {
            item.VendorId = vendor.Id.Trim();
            item.ModifyTime = DateTime.UtcNow;
            await _poItemRepo.UpdateAsync(item);
        }

        foreach (var notice in bundle.SyncNotices)
        {
            notice.VendorId = vendor.Id.Trim();
            notice.VendorName = displayName;
            notice.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(notice);
        }

        foreach (var stockIn in bundle.SyncStockIns)
        {
            stockIn.VendorId = vendor.Id.Trim();
            stockIn.ModifyTime = DateTime.UtcNow;
            await _stockInRepo.UpdateAsync(stockIn);
        }

        foreach (var payment in bundle.SyncPayments)
        {
            payment.VendorId = vendor.Id.Trim();
            payment.VendorName = displayName;
            payment.ModifyTime = DateTime.UtcNow;
            payment.VendorBankId = null;
            await _paymentRepo.UpdateAsync(payment);
        }

        foreach (var invoice in bundle.SyncPurchaseInvoices)
        {
            invoice.VendorId = vendor.Id.Trim();
            invoice.VendorName = displayName;
            invoice.ModifyTime = DateTime.UtcNow;
            await _purchaseInvoiceRepo.UpdateAsync(invoice);
        }

        order.ModifyTime = DateTime.UtcNow;

        _logger.LogInformation(
            "PO换供应商: PurchaseOrderId={PurchaseOrderId} Code={Code} OldVendorId={OldVendorId} NewVendorId={NewVendorId} Items={Items} Notices={Notices} StockIns={StockIns} Payments={Payments} Invoices={Invoices} Actor={Actor}",
            order.Id,
            order.PurchaseOrderCode,
            oldVendorId ?? "(null)",
            vendor.Id,
            bundle.Items.Count,
            bundle.SyncNotices.Count,
            bundle.SyncStockIns.Count,
            bundle.SyncPayments.Count,
            bundle.SyncPurchaseInvoices.Count,
            actingUserId ?? "(null)");

        return new PurchaseOrderVendorChangeApplyResult { Preview = preview, Applied = true };
    }

    private async Task<PurchaseOrderVendorChangePreviewResult> BuildPreviewAsync(
        VendorChangeBundle bundle,
        string newVendorIdRaw,
        CancellationToken cancellationToken)
    {
        var order = bundle.Order
            ?? throw new InvalidOperationException("采购订单不存在");

        var newVendorId = newVendorIdRaw?.Trim()
            ?? throw new ArgumentException("新供应商ID不能为空", nameof(newVendorIdRaw));

        ValidateNewVendorIdAllowed(newVendorId);

        var preview = new PurchaseOrderVendorChangePreviewResult
        {
            PurchaseOrderId = order.Id,
            PurchaseOrderCode = order.PurchaseOrderCode,
            OldVendorId = order.VendorId,
            OldVendorName = order.VendorName,
            NewVendorId = newVendorId,
            PoItemsToSync = bundle.Items.Count
        };

        if (string.Equals(order.VendorId?.Trim(), newVendorId, StringComparison.OrdinalIgnoreCase))
        {
            preview.NoOp = true;
            preview.CanChange = true;
            preview.NewVendorName = order.VendorName;
            return preview;
        }

        var vendor = await ResolveTargetVendorAsync(newVendorId, cancellationToken);
        preview.NewVendorName = FormatVendorDisplayName(vendor);

        ClassifyDownstream(bundle);

        preview.ArrivalNoticesToSync = bundle.SyncNotices.Count;
        preview.StockInsToSync = bundle.SyncStockIns.Count;
        preview.PaymentsToSync = bundle.SyncPayments.Count;
        preview.PurchaseInvoicesToSync = bundle.SyncPurchaseInvoices.Count;

        if (bundle.BlockingDocuments.Count > 0)
        {
            preview.CanChange = false;
            preview.BlockReason = "存在已完结下游单据，无法更换供应商：" + string.Join("；", bundle.BlockingDocuments);
            preview.BlockingDocuments = bundle.BlockingDocuments.ToList();
            return preview;
        }

        preview.CanChange = true;
        return preview;
    }

    private static void ClassifyDownstream(VendorChangeBundle bundle)
    {
        bundle.SyncNotices.Clear();
        bundle.SyncStockIns.Clear();
        bundle.SyncPayments.Clear();
        bundle.SyncPurchaseInvoices.Clear();
        bundle.BlockingDocuments.Clear();

        foreach (var notice in bundle.Notices)
        {
            if (notice.Status >= ArrivalNoticeStatusStockedIn)
            {
                bundle.BlockingDocuments.Add($"到货通知 {notice.NoticeCode} 已入库");
                continue;
            }

            bundle.SyncNotices.Add(notice);
        }

        foreach (var stockIn in bundle.StockIns)
        {
            if (stockIn.Status == StockInHeaderStatusCode.Posted)
            {
                bundle.BlockingDocuments.Add($"入库单 {stockIn.StockInCode} 已过账");
                continue;
            }

            if (stockIn.Status == StockInHeaderStatusCode.Cancelled)
                continue;

            bundle.SyncStockIns.Add(stockIn);
        }

        foreach (var payment in bundle.Payments)
        {
            if (payment.Status == FinancePaymentStatusCompleted)
            {
                bundle.BlockingDocuments.Add($"付款单 {payment.FinancePaymentCode} 已付款");
                continue;
            }

            if (payment.Status == FinancePaymentStatusCancelled)
                continue;

            bundle.SyncPayments.Add(payment);
        }

        foreach (var invoice in bundle.PurchaseInvoices)
        {
            if (invoice.ConfirmStatus >= PurchaseInvoiceConfirmStatusDone)
            {
                bundle.BlockingDocuments.Add($"进项发票 {invoice.InvoiceNo ?? invoice.Id} 已认证");
                continue;
            }

            if (invoice.RedInvoiceStatus >= PurchaseInvoiceRedStatusDone)
            {
                bundle.BlockingDocuments.Add($"进项发票 {invoice.InvoiceNo ?? invoice.Id} 已冲红");
                continue;
            }

            bundle.SyncPurchaseInvoices.Add(invoice);
        }
    }

    private async Task<VendorChangeBundle> LoadBundleAsync(string purchaseOrderId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var order = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new InvalidOperationException($"采购订单 {purchaseOrderId} 不存在");

        var items = (await _poItemRepo.FindAsync(i => i.PurchaseOrderId == purchaseOrderId && !i.IsDeleted))
            .ToList();
        var poItemIds = items
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var notices = (await _notifyRepo.FindAsync(n =>
                !n.IsDeleted
                && (n.PurchaseOrderId == purchaseOrderId
                    || (n.PurchaseOrderItemId != null && poItemIds.Contains(n.PurchaseOrderItemId.Trim())))))
            .ToList();

        var noticeIds = notices
            .Select(n => n.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stockIns = noticeIds.Count == 0
            ? new List<StockIn>()
            : (await _stockInRepo.FindAsync(s =>
                !s.IsDeleted && s.SourceId != null && noticeIds.Contains(s.SourceId.Trim()))).ToList();

        var paymentItems = (await _paymentItemRepo.FindAsync(pi =>
                !pi.IsDeleted
                && ((pi.PurchaseOrderId != null && pi.PurchaseOrderId == purchaseOrderId)
                    || (pi.PurchaseOrderItemId != null && poItemIds.Contains(pi.PurchaseOrderItemId.Trim())))))
            .ToList();

        var paymentIds = paymentItems
            .Select(pi => pi.FinancePaymentId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var payments = paymentIds.Count == 0
            ? new List<FinancePayment>()
            : (await _paymentRepo.FindAsync(p => !p.IsDeleted && paymentIds.Contains(p.Id))).ToList();

        var stockInIds = stockIns
            .Select(s => s.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var purchaseInvoices = await LoadPurchaseInvoicesAsync(order.PurchaseOrderCode, stockInIds, cancellationToken);

        return new VendorChangeBundle
        {
            Order = order,
            Items = items,
            Notices = notices,
            StockIns = stockIns,
            Payments = payments,
            PurchaseInvoices = purchaseInvoices
        };
    }

    private async Task<List<FinancePurchaseInvoice>> LoadPurchaseInvoicesAsync(
        string? purchaseOrderCode,
        HashSet<string> stockInIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invoiceIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
        {
            var code = purchaseOrderCode.Trim();
            var byCode = (await _purchaseInvoiceItemRepo.FindAsync(i =>
                    !i.IsDeleted && i.PurchaseOrderCode != null && i.PurchaseOrderCode == code))
                .ToList();
            foreach (var row in byCode)
                invoiceIdSet.Add(row.FinancePurchaseInvoiceId.Trim());
        }

        if (stockInIds.Count > 0)
        {
            var byStockIn = (await _purchaseInvoiceItemRepo.FindAsync(i =>
                    !i.IsDeleted && i.StockInId != null && stockInIds.Contains(i.StockInId.Trim())))
                .ToList();
            foreach (var row in byStockIn)
                invoiceIdSet.Add(row.FinancePurchaseInvoiceId.Trim());
        }

        if (invoiceIdSet.Count == 0)
            return new List<FinancePurchaseInvoice>();

        return (await _purchaseInvoiceRepo.FindAsync(inv =>
            !inv.IsDeleted && invoiceIdSet.Contains(inv.Id))).ToList();
    }

    private async Task<VendorInfo> ResolveTargetVendorAsync(string vendorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateNewVendorIdAllowed(vendorId);

        var vendor = await _vendorRepo.GetByIdAsync(vendorId.Trim())
            ?? throw new InvalidOperationException($"供应商 {vendorId} 不存在");

        if (vendor.IsDeleted)
            throw new InvalidOperationException("不能选择已删除的供应商");
        if (vendor.BlackList)
            throw new InvalidOperationException("不能选择黑名单供应商");
        if (vendor.IsDisenable)
            throw new InvalidOperationException("不能选择已禁用的供应商");

        var name = FormatVendorDisplayName(vendor);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("供应商主数据无可用名称");

        return vendor;
    }

    internal static void ValidateNewVendorIdAllowed(string vendorId)
    {
        if (string.IsNullOrWhiteSpace(vendorId))
            throw new ArgumentException("新供应商ID不能为空");

        var id = vendorId.Trim();
        if (id.Equals("PENDING", StringComparison.OrdinalIgnoreCase)
            || id.Equals(ManualVendorPlaceholderId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("不能选择无效的供应商");
    }

    private static string? FormatVendorDisplayName(VendorInfo vendor)
    {
        if (!string.IsNullOrWhiteSpace(vendor.OfficialName)) return vendor.OfficialName.Trim();
        if (!string.IsNullOrWhiteSpace(vendor.NickName)) return vendor.NickName.Trim();
        return string.IsNullOrWhiteSpace(vendor.Code) ? null : vendor.Code.Trim();
    }

    private async Task ClearVendorContactIfMismatchAsync(
        PurchaseOrder order,
        string newVendorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(order.VendorContactId))
            return;

        cancellationToken.ThrowIfCancellationRequested();
        var contact = await _vendorContactRepo.GetByIdAsync(order.VendorContactId.Trim());
        if (contact == null
            || contact.IsDeleted
            || !string.Equals(contact.VendorId?.Trim(), newVendorId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            order.VendorContactId = null;
        }
    }

    private sealed class VendorChangeBundle
    {
        public PurchaseOrder? Order { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
        public List<StockInNotify> Notices { get; set; } = new();
        public List<StockInNotify> SyncNotices { get; } = new();
        public List<StockIn> StockIns { get; set; } = new();
        public List<StockIn> SyncStockIns { get; } = new();
        public List<FinancePayment> Payments { get; set; } = new();
        public List<FinancePayment> SyncPayments { get; } = new();
        public List<FinancePurchaseInvoice> PurchaseInvoices { get; set; } = new();
        public List<FinancePurchaseInvoice> SyncPurchaseInvoices { get; } = new();
        public List<string> BlockingDocuments { get; } = new();
    }
}
