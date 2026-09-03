using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class CustomsAgencyRateInboundCostRefreshService : ICustomsAgencyRateInboundCostRefreshService
{
    private const int MaxCodes = 50;
    private const int MaxFailMessages = 20;

    private readonly IRepository<CustomsBroker> _brokerRepo;
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly ICustomsFeeCalculator _feeCalculator;
    private readonly IFinanceExchangeRateService _financeFx;
    private readonly IUnitOfWork _unitOfWork;

    public CustomsAgencyRateInboundCostRefreshService(
        IRepository<CustomsBroker> brokerRepo,
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<CustomsDeclarationItem> declarationItemRepo,
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockItem> stockItemRepo,
        ICustomsFeeCalculator feeCalculator,
        IFinanceExchangeRateService financeFx,
        IUnitOfWork unitOfWork)
    {
        _brokerRepo = brokerRepo;
        _declarationRepo = declarationRepo;
        _declarationItemRepo = declarationItemRepo;
        _notifyRepo = notifyRepo;
        _stockInRepo = stockInRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockItemRepo = stockItemRepo;
        _feeCalculator = feeCalculator;
        _financeFx = financeFx;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomsAgencyRateInboundCostRefreshResult> RefreshAsync(
        string customsBrokerId,
        decimal agencyRate,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        var brokerKey = customsBrokerId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(brokerKey))
            throw new ArgumentException("请选择报关公司。", nameof(customsBrokerId));

        CustomsAgencyRateRules.EnsureValid(agencyRate);

        var broker = await _brokerRepo.GetByIdAsync(brokerKey)
                     ?? throw new InvalidOperationException("报关公司不存在。");
        if (broker.IsDeleted)
            throw new InvalidOperationException("报关公司不存在。");

        var result = new CustomsAgencyRateInboundCostRefreshResult();
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        FinanceExchangeRateDto? fx = null;
        try
        {
            fx = await _financeFx.GetCurrentAsync(cancellationToken);
        }
        catch
        {
            fx = null;
        }

        var declarations = (await _declarationRepo.FindAsync(d =>
                !d.IsDeleted && d.CustomsBrokerId == brokerKey))
            .ToList();
        result.TotalDeclarations = declarations.Count;

        foreach (var dec in declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var code = string.IsNullOrWhiteSpace(dec.DeclarationCode) ? dec.Id : dec.DeclarationCode.Trim();
            try
            {
                if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
                {
                    result.SkippedVoided++;
                    continue;
                }

                if (dec.AgencyRateManual)
                {
                    result.SkippedManual++;
                    continue;
                }

                var applied = await RefreshDeclarationAsync(dec, agencyRate, actor, fx, result);
                if (!applied)
                {
                    result.SkippedNoFees++;
                    continue;
                }

                result.RefreshedDeclarations++;
                if (result.RefreshedDeclarationCodes.Count < MaxCodes)
                    result.RefreshedDeclarationCodes.Add(code);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                if (result.FailedMessages.Count < MaxFailMessages)
                    result.FailedMessages.Add($"{code}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<bool> RefreshDeclarationAsync(
        CustomsDeclaration dec,
        decimal agencyRate,
        string? actor,
        FinanceExchangeRateDto? fx,
        CustomsAgencyRateInboundCostRefreshResult result)
    {
        var items = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted))
            .OrderBy(i => i.LineNo)
            .ToList();
        if (items.Count == 0)
            return false;

        var eligible = items.Where(i => i.DeclareQty > 0 && i.CustomsPaymentGoods > 0m).ToList();
        if (eligible.Count == 0)
            return false;

        var now = DateTime.UtcNow;
        var feesChanged = false;
        var arrivalUpdated = 0;
        var stockInItemsUpdated = 0;
        var layersUpdated = 0;

        foreach (var item in eligible)
        {
            var calc = _feeCalculator.RecalculateAgencyFeeFromSnapshots(
                item.CustomsPaymentGoods,
                item.DutyAmount,
                item.VatAmount,
                item.OtherFee,
                item.DeclareQty,
                agencyRate);

            if (item.CustomsAgencyFee != calc.CustomsAgencyFee
                || item.TotalValueTax != calc.TotalValueTax
                || item.TaxIncludedUnitPrice != calc.TaxIncludedUnitPrice)
            {
                feesChanged = true;
            }

            item.CustomsAgencyFee = calc.CustomsAgencyFee;
            item.TotalValueTax = calc.TotalValueTax;
            item.TaxIncludedUnitPrice = calc.TaxIncludedUnitPrice;
            item.ModifyTime = now;
            await _declarationItemRepo.UpdateAsync(item);

            var p1 = calc.TaxIncludedUnitPrice;
            if (p1 <= 0m)
                continue;

            var inbound = await CascadeInboundCostAsync(item.Id, p1, fx, now);
            arrivalUpdated += inbound.Notices;
            stockInItemsUpdated += inbound.StockInItems;
            layersUpdated += inbound.Layers;
        }

        dec.BrokerAgencyRate = agencyRate;
        dec.AgencyRateManual = false;
        dec.TotalTaxAmount = items.Sum(i => i.TotalValueTax);
        dec.FeesCalculatedAt = now;
        dec.ModifyTime = now;
        dec.ModifyByUserId = actor;
        await _declarationRepo.UpdateAsync(dec);
        await _unitOfWork.SaveChangesAsync();

        if (feesChanged)
            result.FeesChangedDeclarations++;
        result.ArrivalNoticesUpdated += arrivalUpdated;
        result.StockInItemsUpdated += stockInItemsUpdated;
        result.StockItemLayersUpdated += layersUpdated;

        return true;
    }

    private async Task<(int Notices, int StockInItems, int Layers)> CascadeInboundCostAsync(
        string declarationItemId,
        decimal p1,
        FinanceExchangeRateDto? fx,
        DateTime now)
    {
        var notices = 0;
        var stockInItems = 0;
        var layers = 0;
        var usd = fx == null
            ? (decimal?)null
            : ExchangeRateToUsdConverter.UnitLocalToUsd(
                p1,
                (short)CurrencyCode.RMB,
                fx.UsdToCny,
                fx.UsdToHkd,
                fx.UsdToEur);

        var notifyRows = (await _notifyRepo.FindAsync(n =>
                !n.IsDeleted
                && n.StockInType == StockInTypeCode.Customs
                && n.CustomsDeclarationItemId == declarationItemId))
            .ToList();

        foreach (var notify in notifyRows)
        {
            var expectTotal = Math.Round(notify.ExpectQty * p1, 2, MidpointRounding.AwayFromZero);
            if (notify.Cost != p1 || notify.ExpectTotal != expectTotal)
            {
                notify.Cost = p1;
                notify.ExpectTotal = expectTotal;
                notify.ModifyTime = now;
                await _notifyRepo.UpdateAsync(notify);
                notices++;
            }

            var stockIns = (await _stockInRepo.FindAsync(s =>
                    !s.IsDeleted
                    && s.StockInType == StockInTypeCode.Customs
                    && s.SourceId == notify.Id))
                .ToList();

            foreach (var stockIn in stockIns)
            {
                var lines = (await _stockInItemRepo.FindAsync(i =>
                        !i.IsDeleted && i.StockInId == stockIn.Id))
                    .ToList();
                foreach (var line in lines)
                {
                    var amount = Math.Round(line.Quantity * p1, 2, MidpointRounding.AwayFromZero);
                    if (line.Price == p1 && line.Amount == amount)
                        continue;
                    line.Price = p1;
                    line.Amount = amount;
                    line.ModifyTime = now;
                    await _stockInItemRepo.UpdateAsync(line);
                    stockInItems++;
                }

                var layerRows = (await _stockItemRepo.FindAsync(si =>
                        !si.IsDeleted && si.StockInId == stockIn.Id))
                    .ToList();
                foreach (var layer in layerRows)
                {
                    var amount = Math.Round(layer.QtyInbound * p1, 2, MidpointRounding.AwayFromZero);
                    var usdPrice = usd ?? layer.PurchasePriceUsd;
                    if (layer.PurchasePrice == p1
                        && layer.PurchaseAmount == amount
                        && layer.PurchasePriceUsd == usdPrice)
                        continue;
                    layer.PurchasePrice = p1;
                    layer.PurchaseAmount = amount;
                    layer.PurchasePriceUsd = usdPrice;
                    layer.ModifyTime = now;
                    await _stockItemRepo.UpdateAsync(layer);
                    layers++;
                }
            }
        }

        return (notices, stockInItems, layers);
    }
}
