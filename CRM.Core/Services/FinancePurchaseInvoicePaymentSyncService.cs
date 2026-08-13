using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

public class FinancePurchaseInvoicePaymentSyncService : IFinancePurchaseInvoicePaymentSyncService
{
    private readonly IRepository<FinancePurchaseInvoice> _invoiceRepo;
    private readonly IRepository<FinancePurchaseInvoiceWriteOff> _writeOffRepo;
    private readonly IRepository<PurchaseOrderItemExtend> _poExtendRepo;
    private readonly IUnitOfWork? _unitOfWork;

    public FinancePurchaseInvoicePaymentSyncService(
        IRepository<FinancePurchaseInvoice> invoiceRepo,
        IRepository<FinancePurchaseInvoiceWriteOff> writeOffRepo,
        IRepository<PurchaseOrderItemExtend> poExtendRepo,
        IUnitOfWork? unitOfWork = null)
    {
        _invoiceRepo = invoiceRepo;
        _writeOffRepo = writeOffRepo;
        _poExtendRepo = poExtendRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task RecalculateForInvoiceAsync(string financePurchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(financePurchaseInvoiceId)) return;
        cancellationToken.ThrowIfCancellationRequested();
        var invoice = await _invoiceRepo.GetByIdAsync(financePurchaseInvoiceId.Trim());
        if (invoice == null) return;
        await ApplyPaymentCacheAsync(invoice, cancellationToken);
    }

    public async Task RecalculateForPurchaseOrderItemAsync(string purchaseOrderItemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderItemId)) return;
        cancellationToken.ThrowIfCancellationRequested();
        var pid = purchaseOrderItemId.Trim();
        var links = (await _writeOffRepo.FindAsync(w => w.PurchaseOrderItemId != null && w.PurchaseOrderItemId == pid)).ToList();
        var invoiceIds = links
            .Select(w => w.FinancePurchaseInvoiceId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var id in invoiceIds)
            await RecalculateForInvoiceAsync(id, cancellationToken);
    }

    private async Task ApplyPaymentCacheAsync(FinancePurchaseInvoice invoice, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var links = (await _writeOffRepo.FindAsync(w => w.FinancePurchaseInvoiceId == invoice.Id)).ToList();
        var byPo = links
            .Where(w => !string.IsNullOrWhiteSpace(w.PurchaseOrderItemId))
            .GroupBy(w => w.PurchaseOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount), StringComparer.OrdinalIgnoreCase);

        decimal paymentDone = 0m;
        if (byPo.Count > 0)
        {
            var poIds = byPo.Keys.ToList();
            var extends = (await _poExtendRepo.FindAsync(e => poIds.Contains(e.Id))).ToList();
            var extById = extends.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var (poItemId, linkAmount) in byPo)
            {
                var finish = extById.TryGetValue(poItemId, out var ext) ? ext.PaymentAmountFinish : 0m;
                paymentDone += Math.Min(linkAmount, finish);
            }
        }

        paymentDone = Math.Round(paymentDone, 2, MidpointRounding.AwayFromZero);
        var verifiedDone = invoice.VerifiedDone;
        invoice.PaymentDone = paymentDone;
        invoice.PaymentToBe = Math.Max(0m, Math.Round(verifiedDone - paymentDone, 2, MidpointRounding.AwayFromZero));
        invoice.PaymentStatus = ResolveStatus(paymentDone, verifiedDone);
        invoice.ModifyTime = DateTime.UtcNow;
        await _invoiceRepo.UpdateAsync(invoice);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    private static byte ResolveStatus(decimal done, decimal total)
    {
        if (done <= 0m) return 0;
        if (total > 0m && done + 0.0001m >= total) return 2;
        return 1;
    }
}
