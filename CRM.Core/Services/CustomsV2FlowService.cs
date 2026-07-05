using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

public class CustomsV2FlowService : ICustomsV2FlowService
{
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<CustomsPendlist> _pendlistRepo;
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<StockInNotify> _stockInNotifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<WarehouseInfo> _warehouseRepo;
    private readonly IRepository<CustomsBroker> _brokerRepo;
    private readonly IRepository<PickingTaskItem> _pickingTaskItemRepo;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IFinanceExchangeRateService _financeExchangeRateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomsV2FlowService> _logger;

    public CustomsV2FlowService(
        IRepository<Packing> packingRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<CustomsPendlist> pendlistRepo,
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<CustomsDeclarationItem> declarationItemRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<StockInNotify> stockInNotifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<WarehouseInfo> warehouseRepo,
        IRepository<CustomsBroker> brokerRepo,
        IRepository<PickingTaskItem> pickingTaskItemRepo,
        ISerialNumberService serialNumberService,
        IFinanceExchangeRateService financeExchangeRateService,
        IUnitOfWork unitOfWork,
        ILogger<CustomsV2FlowService> logger)
    {
        _packingRepo = packingRepo;
        _packingItemRepo = packingItemRepo;
        _pendlistRepo = pendlistRepo;
        _declarationRepo = declarationRepo;
        _declarationItemRepo = declarationItemRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _stockInNotifyRepo = stockInNotifyRepo;
        _stockInRepo = stockInRepo;
        _stockItemRepo = stockItemRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _warehouseRepo = warehouseRepo;
        _brokerRepo = brokerRepo;
        _pickingTaskItemRepo = pickingTaskItemRepo;
        _serialNumberService = serialNumberService;
        _financeExchangeRateService = financeExchangeRateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnCustomsPackingCreatedAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var packing = await RequireCustomsPackingAsync(packingId);
        var items = await LoadPackingItemsAsync(packing.Id);
        var pendlistIds = items
            .Select(i => i.CustomsPendlistId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pendlistIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        foreach (var pid in pendlistIds)
        {
            var row = await _pendlistRepo.GetByIdAsync(pid)
                      ?? throw new InvalidOperationException($"待报关记录不存在：{pid}");
            if (row.IsDeleted)
                throw new InvalidOperationException($"待报关记录已删除：{pid}");
            if (row.Status == CustomsPendlistStatusCode.Cancelled)
                throw new InvalidOperationException("待报关记录已取消，不能生成报关装箱。");
            if (row.Status != CustomsPendlistStatusCode.CustomsOutNotifyCreated
                && row.Status != CustomsPendlistStatusCode.InCustomsProcess)
            {
                throw new InvalidOperationException("待报关记录状态不允许生成报关装箱。");
            }

            row.Status = CustomsPendlistStatusCode.InCustomsProcess;
            row.ModifyTime = now;
            row.ModifyByUserId = actor;
            await _pendlistRepo.UpdateAsync(row);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task GenerateDeclarationOnPackingConfirmAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var packing = await RequireCustomsPackingAsync(packingId);
        if (!string.IsNullOrWhiteSpace(packing.CustomsDeclarationId))
            return;

        if (string.IsNullOrWhiteSpace(packing.CustomsBrokerId))
            throw new InvalidOperationException("报关装箱单缺少报关公司，不能确认。");

        _ = await _brokerRepo.GetByIdAsync(packing.CustomsBrokerId.Trim())
            ?? throw new InvalidOperationException("报关公司不存在。");

        var fromWh = packing.StorageId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(fromWh))
            throw new InvalidOperationException("报关装箱单缺少境外出库仓库。");

        var fromWarehouse = await _warehouseRepo.GetByIdAsync(fromWh)
                            ?? throw new InvalidOperationException("境外出库仓库不存在。");
        if (RegionTypeCode.Normalize(fromWarehouse.RegionType) != RegionTypeCode.Overseas)
            throw new InvalidOperationException("报关装箱单出库仓库须为境外仓。");

        var toWh = await ResolveDefaultDomesticWarehouseIdAsync();
        var items = await LoadPackingItemsAsync(packing.Id);
        if (items.Count == 0)
            throw new InvalidOperationException("报关装箱单无明细，不能确认。");

        var decId = Guid.NewGuid().ToString();
        var decCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.CustomsDeclaration);
        var fx = await _financeExchangeRateService.GetCurrentAsync();
        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        var header = new CustomsDeclaration
        {
            Id = decId,
            DeclarationCode = decCode,
            PackingId = packing.Id,
            CustomsBrokerId = packing.CustomsBrokerId.Trim(),
            DeclarationType = CustomsDeclarationType.Import,
            InternalStatus = CustomsDeclarationInternalStatus.Processing,
            CustomsClearanceStatus = CustomsClearanceStatusCodes.None,
            DeclareDate = now.Date,
            ExchangeRate = fx.UsdToCny > 0m ? fx.UsdToCny : 1m,
            TotalTaxAmount = 0m,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            CreateTime = now,
            CreateByUserId = actor,
            IsDeleted = false
        };
        await _declarationRepo.AddAsync(header);

        var lineNo = 1;
        foreach (var pi in items.OrderBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            var customsSorId = pi.StockOutNotifyId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(customsSorId))
                throw new InvalidOperationException($"装箱明细 {pi.ItemCode} 缺少报关出库通知。");

            var customsSor = await _stockOutRequestRepo.GetByIdAsync(customsSorId)
                             ?? throw new InvalidOperationException("报关出库通知不存在。");
            if (StockOutTypeCode.NormalizeForNotify(customsSor.StockOutType) != StockOutTypeCode.Customs)
                throw new InvalidOperationException("装箱明细须关联报关出库通知。");

            var pendlistId = pi.CustomsPendlistId?.Trim()
                             ?? customsSor.CustomsPendlistId?.Trim()
                             ?? string.Empty;
            if (string.IsNullOrEmpty(pendlistId))
                throw new InvalidOperationException($"装箱明细 {pi.ItemCode} 缺少待报关关联。");

            var pendlist = await _pendlistRepo.GetByIdAsync(pendlistId)
                           ?? throw new InvalidOperationException("待报关记录不存在。");
            var salesSorId = pendlist.SalesStockOutNotifyId.Trim();
            var sellLineId = pi.SellOrderItemId?.Trim() ?? pendlist.SellOrderItemId.Trim();

            SellOrderItem? sellLine = null;
            if (!string.IsNullOrEmpty(sellLineId))
                sellLine = await _sellOrderItemRepo.GetByIdAsync(sellLineId);

            SellOrder? sellOrder = null;
            if (!string.IsNullOrWhiteSpace(pi.SellOrderId))
                sellOrder = await _sellOrderRepo.GetByIdAsync(pi.SellOrderId.Trim());
            else if (sellLine != null && !string.IsNullOrWhiteSpace(sellLine.SellOrderId))
                sellOrder = await _sellOrderRepo.GetByIdAsync(sellLine.SellOrderId.Trim());

            var materialId = !string.IsNullOrWhiteSpace(sellLine?.ProductId)
                ? sellLine!.ProductId!.Trim()
                : (!string.IsNullOrWhiteSpace(pi.ProductId) ? pi.ProductId!.Trim() : sellLineId);

            var decItem = new CustomsDeclarationItem
            {
                Id = Guid.NewGuid().ToString(),
                DeclarationId = decId,
                LineNo = lineNo++,
                StockOutRequestId = salesSorId,
                CustomsPendlistId = pendlistId,
                CustomsStockOutNotifyId = customsSorId,
                PackingItemId = pi.Id,
                MaterialId = materialId,
                PurchasePn = pi.Pn,
                PurchaseBrand = pi.Brand,
                CustomerId = sellOrder?.CustomerId,
                SalesUserId = sellOrder?.SalesUserId,
                SellOrderItemId = sellLineId,
                SellOrderItemCode = sellLine?.SellOrderItemCode,
                DeclareQty = pi.Qty,
                DeclareUnitPrice = 0m,
                DutyAmount = 0m,
                VatAmount = 0m,
                CustomsPaymentGoods = 0m,
                CustomsAgencyFee = 0m,
                OtherFee = 0m,
                InspectionFee = 0m,
                TotalValueTax = 0m,
                TaxIncludedUnitPrice = 0m,
                OriginalPurchasePrice = 0m,
                CreateTime = now,
                IsDeleted = false
            };
            await _declarationItemRepo.AddAsync(decItem);

            pendlist.Status = CustomsPendlistStatusCode.InCustomsProcess;
            pendlist.ModifyTime = now;
            pendlist.ModifyByUserId = actor;
            await _pendlistRepo.UpdateAsync(pendlist);
        }

        packing.CustomsDeclarationId = decId;
        packing.ModifyTime = now;
        packing.ModifyByUserId = actor;
        await _packingRepo.UpdateAsync(packing);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "CustomsV2 declaration generated PackingId={PackingId} DeclarationId={DeclarationId} Code={Code} Lines={Lines}",
            packing.Id, decId, decCode, items.Count);
    }

    public async Task WritebackDeclarationItemsAfterPickingAsync(
        string packingId,
        string pickingTaskId,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var packing = await RequireCustomsPackingAsync(packingId);
        var decId = packing.CustomsDeclarationId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(decId))
            return;

        var decItems = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == decId && !i.IsDeleted)).ToList();
        if (decItems.Count == 0)
            return;

        var pickItems = (await _pickingTaskItemRepo.FindAsync(i =>
                i.PickingTaskId == pickingTaskId.Trim() && !i.IsDeleted))
            .Where(i => !string.IsNullOrWhiteSpace(i.PackingItemId) && i.PlanQty > 0)
            .ToList();

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        foreach (var decItem in decItems)
        {
            var piKey = decItem.PackingItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(piKey))
                continue;

            var related = pickItems
                .Where(x => string.Equals(x.PackingItemId?.Trim(), piKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (related.Count == 0)
                continue;

            var pickedQty = related.Sum(x => x.PlanQty);
            var primary = related[0];
            var layerId = primary.StockItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(layerId))
                throw new InvalidOperationException($"拣货明细缺少在库行，无法回写报关明细行 {decItem.LineNo}。");

            var layer = await _stockItemRepo.GetByIdAsync(layerId)
                        ?? throw new InvalidOperationException("拣货引用的在库明细不存在。");

            decItem.SourceStockItemId = layerId;
            decItem.DeclareQty = pickedQty;
            decItem.OriginalPurchasePrice = layer.PurchasePrice;
            decItem.VendorId = string.IsNullOrWhiteSpace(layer.VendorId) ? null : layer.VendorId.Trim();
            decItem.ModifyTime = now;
            await _declarationItemRepo.UpdateAsync(decItem);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task EnsureCustomsOutReadyAsync(string customsStockOutRequestId, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var sor = await LoadCustomsStockOutRequestAsync(customsStockOutRequestId);
        var decItem = await FindDeclarationItemByCustomsStockOutNotifyAsync(sor.Id)
                      ?? throw new InvalidOperationException("未找到报关明细，请先完成装箱确认与拣货。");
        var dec = await _declarationRepo.GetByIdAsync(decItem.DeclarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在。");

        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
            throw new InvalidOperationException("报关单已作废。");

        if (string.IsNullOrWhiteSpace(dec.ToWarehouseId))
            throw new InvalidOperationException("请先在报关单上维护目标境内仓库（ToWarehouse）。");

        var toWh = await _warehouseRepo.GetByIdAsync(dec.ToWarehouseId.Trim())
                   ?? throw new InvalidOperationException("目标仓库不存在。");
        if (RegionTypeCode.Normalize(toWh.RegionType) != RegionTypeCode.Domestic)
            throw new InvalidOperationException("报关单目标仓库须为境内仓。");
        if (toWh.Status != 1)
            throw new InvalidOperationException("目标仓库已停用。");

        var allItems = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted)).ToList();
        foreach (var line in allItems)
        {
            if (line.DeclareQty <= 0)
                throw new InvalidOperationException($"报关明细行 {line.LineNo} 申报数量无效。");
            if (string.IsNullOrWhiteSpace(line.SourceStockItemId))
                throw new InvalidOperationException($"报关明细行 {line.LineNo} 尚未拣货回写源在库行。");
        }
    }

    public async Task<IReadOnlyDictionary<string, CustomsDeclarationItem>> GetDeclarationItemsMapForCustomsStockOutAsync(
        string customsStockOutRequestId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var anchor = await FindDeclarationItemByCustomsStockOutNotifyAsync(customsStockOutRequestId.Trim());
        if (anchor == null)
            return new Dictionary<string, CustomsDeclarationItem>(StringComparer.OrdinalIgnoreCase);

        var all = (await _declarationItemRepo.FindAsync(i =>
            i.DeclarationId == anchor.DeclarationId && !i.IsDeleted)).ToList();
        return all
            .Where(i => !string.IsNullOrWhiteSpace(i.PackingItemId))
            .GroupBy(i => i.PackingItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public void ApplyCustomsStockOutExtend(
        StockOutItemExtend ext,
        StockItem layer,
        string? packingItemId,
        IReadOnlyDictionary<string, CustomsDeclarationItem> decItemByPackingItemId)
    {
        if (string.IsNullOrWhiteSpace(packingItemId))
            return;
        if (!decItemByPackingItemId.TryGetValue(packingItemId.Trim(), out var decItem))
            return;

        var p0 = decItem.OriginalPurchasePrice > 0m ? decItem.OriginalPurchasePrice : layer.PurchasePrice;
        ext.OriginalPurchasePrice = p0;
        ext.PurchasePrice = p0;
        ext.VendorId = string.IsNullOrWhiteSpace(decItem.VendorId) ? layer.VendorId : decItem.VendorId;
        ext.CustomsDeclarationItemId = decItem.Id;
    }

    public async Task<CreateCustomsArrivalNotifiesResultDto> CreateCustomsArrivalNotifiesAsync(
        string declarationId,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var readiness = await EvaluateArrivalNotifyReadinessAsync(declarationId);
        if (!readiness.CanCreate)
            throw new InvalidOperationException(readiness.BlockReason ?? "当前不能生成报关到货通知。");

        var dec = await _declarationRepo.GetByIdAsync(declarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在。");
        var items = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted))
            .OrderBy(i => i.LineNo)
            .ToList();

        var now = DateTime.UtcNow;
        var created = new List<CreatedCustomsArrivalNotifyLineDto>();

        foreach (var decItem in items)
        {
            var existing = (await _stockInNotifyRepo.FindAsync(n =>
                n.CustomsDeclarationItemId == decItem.Id && !n.IsDeleted)).FirstOrDefault();
            if (existing != null)
                continue;

            var customsSorId = decItem.CustomsStockOutNotifyId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(customsSorId))
                continue;

            var sor = await _stockOutRequestRepo.GetByIdAsync(customsSorId);
            if (sor == null || sor.IsDeleted || sor.Status != StockOutRequestStatusCode.StockedOut)
                continue;

            var notice = await BuildCustomsArrivalNotifyAsync(dec, decItem, now);
            await _stockInNotifyRepo.AddAsync(notice);
            created.Add(new CreatedCustomsArrivalNotifyLineDto
            {
                NoticeId = notice.Id,
                NoticeCode = notice.NoticeCode,
                LineNo = decItem.LineNo,
                CustomsDeclarationItemId = decItem.Id
            });

            _logger.LogInformation(
                "CustomsV2 arrival notify created manually DeclarationId={DeclarationId} NoticeId={NoticeId} CdiId={CdiId}",
                dec.Id, notice.Id, decItem.Id);
        }

        if (created.Count == 0)
            throw new InvalidOperationException("没有可生成的报关到货通知（可能已全部生成）。");

        await _unitOfWork.SaveChangesAsync();

        return new CreateCustomsArrivalNotifiesResultDto
        {
            DeclarationId = dec.Id,
            CreatedCount = created.Count,
            Created = created
        };
    }

    public Task<CustomsDeclarationArrivalNotifyReadinessDto> GetArrivalNotifyReadinessAsync(
        string declarationId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return EvaluateArrivalNotifyReadinessAsync(declarationId);
    }

    public async Task OnCustomsStockInCompletedAsync(string stockInId, string? actingUserId, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var stockIn = await _stockInRepo.GetByIdAsync(stockInId.Trim())
                      ?? throw new InvalidOperationException("入库单不存在。");
        if (stockIn.StockInType != StockInTypeCode.Customs)
            return;

        StockInNotify? notify = null;
        if (!string.IsNullOrWhiteSpace(stockIn.SourceId))
            notify = await _stockInNotifyRepo.GetByIdAsync(stockIn.SourceId.Trim());

        var cdiId = notify?.CustomsDeclarationItemId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(cdiId))
            return;

        var decItem = await _declarationItemRepo.GetByIdAsync(cdiId)
                      ?? throw new InvalidOperationException("报关明细不存在。");
        var dec = await _declarationRepo.GetByIdAsync(decItem.DeclarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在。");

        var p1 = decItem.TaxIncludedUnitPrice;
        if (p1 > 0m)
        {
            var layers = (await _stockItemRepo.FindAsync(si => si.StockInId == stockIn.Id && !si.IsDeleted)).ToList();
            foreach (var layer in layers)
            {
                layer.PurchasePrice = p1;
                layer.ModifyTime = DateTime.UtcNow;
                await _stockItemRepo.UpdateAsync(layer);
            }
        }

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        if (!string.IsNullOrWhiteSpace(decItem.CustomsPendlistId))
        {
            var pendlist = await _pendlistRepo.GetByIdAsync(decItem.CustomsPendlistId.Trim());
            if (pendlist != null && !pendlist.IsDeleted && pendlist.Status != CustomsPendlistStatusCode.Closed)
            {
                pendlist.Status = CustomsPendlistStatusCode.Closed;
                pendlist.ModifyTime = now;
                pendlist.ModifyByUserId = actor;
                await _pendlistRepo.UpdateAsync(pendlist);

                var salesSor = await _stockOutRequestRepo.GetByIdAsync(pendlist.SalesStockOutNotifyId.Trim());
                if (salesSor != null
                    && !salesSor.IsDeleted
                    && salesSor.Status == StockOutRequestStatusCode.PendingCustoms)
                {
                    salesSor.Status = StockOutRequestStatusCode.PendingPacking;
                    salesSor.CustomsStatus = StockOutNotifyCustomsStatusCode.Completed;
                    salesSor.ModifyTime = now;
                    salesSor.ModifyByUserId = actor;
                    await _stockOutRequestRepo.UpdateAsync(salesSor);
                }

                if (!string.IsNullOrWhiteSpace(pendlist.CustomsStockOutNotifyId))
                {
                    var customsSor = await _stockOutRequestRepo.GetByIdAsync(pendlist.CustomsStockOutNotifyId.Trim());
                    if (customsSor != null && !customsSor.IsDeleted)
                    {
                        customsSor.CustomsStatus = StockOutNotifyCustomsStatusCode.Completed;
                        customsSor.ModifyTime = now;
                        customsSor.ModifyByUserId = actor;
                        await _stockOutRequestRepo.UpdateAsync(customsSor);
                    }
                }
            }
        }

        var openItems = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted)).ToList();
        var pendlistIds = openItems
            .Select(i => i.CustomsPendlistId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pendlists = pendlistIds.Count == 0
            ? new List<CustomsPendlist>()
            : (await _pendlistRepo.FindAsync(p => pendlistIds.Contains(p.Id))).ToList();
        if (pendlists.Count > 0 && pendlists.All(p => p.Status == CustomsPendlistStatusCode.Closed))
        {
            dec.InternalStatus = CustomsDeclarationInternalStatus.Completed;
            dec.ModifyTime = now;
            dec.ModifyByUserId = actor;
            await _declarationRepo.UpdateAsync(dec);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RevertPendlistOnPackingDeleteAsync(
        IReadOnlyList<string> customsPendlistIds,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (customsPendlistIds == null || customsPendlistIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        foreach (var id in customsPendlistIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var key = id?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
                continue;

            var row = await _pendlistRepo.GetByIdAsync(key);
            if (row == null || row.IsDeleted)
                continue;
            if (row.Status == CustomsPendlistStatusCode.Cancelled || row.Status == CustomsPendlistStatusCode.Closed)
                continue;

            row.Status = CustomsPendlistStatusCode.Open;
            row.ModifyTime = now;
            row.ModifyByUserId = actor;
            await _pendlistRepo.UpdateAsync(row);

            var salesSor = await _stockOutRequestRepo.GetByIdAsync(row.SalesStockOutNotifyId.Trim());
            if (salesSor != null
                && !salesSor.IsDeleted
                && salesSor.CustomsStatus == StockOutNotifyCustomsStatusCode.InCustoms)
            {
                salesSor.CustomsStatus = StockOutNotifyCustomsStatusCode.PendingCustoms;
                salesSor.ModifyTime = now;
                salesSor.ModifyByUserId = actor;
                await _stockOutRequestRepo.UpdateAsync(salesSor);
            }

            if (!string.IsNullOrWhiteSpace(row.CustomsStockOutNotifyId))
            {
                var customsSor = await _stockOutRequestRepo.GetByIdAsync(row.CustomsStockOutNotifyId.Trim());
                if (customsSor != null
                    && !customsSor.IsDeleted
                    && customsSor.CustomsStatus == StockOutNotifyCustomsStatusCode.InCustoms)
                {
                    customsSor.CustomsStatus = StockOutNotifyCustomsStatusCode.PendingCustoms;
                    customsSor.ModifyTime = now;
                    customsSor.ModifyByUserId = actor;
                    await _stockOutRequestRepo.UpdateAsync(customsSor);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateDeclarationHeaderAsync(
        string declarationId,
        string? toWarehouseId,
        string? remark,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var dec = await _declarationRepo.GetByIdAsync(declarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在。");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Completed)
            throw new InvalidOperationException("已完成报关单不能修改头信息。");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
            throw new InvalidOperationException("报关单已作废。");

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        if (toWarehouseId != null)
        {
            var whKey = toWarehouseId.Trim();
            if (string.IsNullOrEmpty(whKey))
                throw new InvalidOperationException("目标仓库不能为空。");
            var wh = await _warehouseRepo.GetByIdAsync(whKey)
                     ?? throw new InvalidOperationException("目标仓库不存在。");
            if (RegionTypeCode.Normalize(wh.RegionType) != RegionTypeCode.Domestic)
                throw new InvalidOperationException("目标仓库须为境内仓。");
            dec.ToWarehouseId = whKey;
        }

        if (remark != null)
            dec.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();

        dec.ModifyTime = now;
        dec.ModifyByUserId = actor;
        await _declarationRepo.UpdateAsync(dec);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateDeclarationItemAsync(
        string itemId,
        CustomsDeclarationItemPatch patch,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (patch == null)
            throw new ArgumentNullException(nameof(patch));

        var row = await _declarationItemRepo.GetByIdAsync(itemId.Trim())
                  ?? throw new InvalidOperationException("报关明细不存在。");
        var dec = await _declarationRepo.GetByIdAsync(row.DeclarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在。");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Completed)
            throw new InvalidOperationException("已完成报关单不能修改明细。");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
            throw new InvalidOperationException("报关单已作废。");

        if (patch.HsCode != null)
            row.HsCode = string.IsNullOrWhiteSpace(patch.HsCode) ? null : patch.HsCode.Trim();
        if (patch.DeclareQty.HasValue)
        {
            if (patch.DeclareQty.Value <= 0)
                throw new InvalidOperationException("申报数量须大于 0。");
            row.DeclareQty = patch.DeclareQty.Value;
        }

        if (patch.DeclareUnitPrice.HasValue) row.DeclareUnitPrice = patch.DeclareUnitPrice.Value;
        if (patch.DutyAmount.HasValue) row.DutyAmount = patch.DutyAmount.Value;
        if (patch.VatAmount.HasValue) row.VatAmount = patch.VatAmount.Value;
        if (patch.CustomsPaymentGoods.HasValue) row.CustomsPaymentGoods = patch.CustomsPaymentGoods.Value;
        if (patch.CustomsAgencyFee.HasValue) row.CustomsAgencyFee = patch.CustomsAgencyFee.Value;
        if (patch.OtherFee.HasValue) row.OtherFee = patch.OtherFee.Value;
        if (patch.InspectionFee.HasValue) row.InspectionFee = patch.InspectionFee.Value;
        if (patch.TotalValueTax.HasValue) row.TotalValueTax = patch.TotalValueTax.Value;
        if (patch.TaxIncludedUnitPrice.HasValue) row.TaxIncludedUnitPrice = patch.TaxIncludedUnitPrice.Value;

        row.ModifyTime = DateTime.UtcNow;
        await _declarationItemRepo.UpdateAsync(row);

        var allItems = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted)).ToList();
        dec.TotalTaxAmount = allItems.Sum(i => i.TotalValueTax);
        dec.ModifyTime = DateTime.UtcNow;
        dec.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _declarationRepo.UpdateAsync(dec);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Packing> RequireCustomsPackingAsync(string packingId)
    {
        var id = packingId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepo.GetByIdAsync(id)
                      ?? throw new InvalidOperationException("装箱单不存在。");
        if (packing.IsDeleted)
            throw new InvalidOperationException("装箱单已删除。");
        if (StockOutTypeCode.NormalizeForNotify(packing.StockOutType) != StockOutTypeCode.Customs)
            throw new InvalidOperationException("非报关装箱单。");
        return packing;
    }

    private async Task<List<PackingItem>> LoadPackingItemsAsync(string packingId) =>
        (await _packingItemRepo.FindAsync(i => i.PackingId == packingId && !i.IsDeleted))
        .OrderBy(i => i.ItemCode, StringComparer.OrdinalIgnoreCase)
        .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private async Task<StockOutRequest> LoadCustomsStockOutRequestAsync(string customsStockOutRequestId)
    {
        var key = customsStockOutRequestId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("报关出库通知 ID 无效", nameof(customsStockOutRequestId));

        var sor = await _stockOutRequestRepo.GetByIdAsync(key)
                  ?? throw new InvalidOperationException("报关出库通知不存在。");
        if (sor.IsDeleted)
            throw new InvalidOperationException("报关出库通知已删除。");
        if (StockOutTypeCode.NormalizeForNotify(sor.StockOutType) != StockOutTypeCode.Customs)
            throw new InvalidOperationException("须为报关出库通知。");
        return sor;
    }

    private async Task<StockInNotify> BuildCustomsArrivalNotifyAsync(
        CustomsDeclaration dec,
        CustomsDeclarationItem decItem,
        DateTime now)
    {
        var toWh = dec.ToWarehouseId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(toWh))
            throw new InvalidOperationException("报关单未维护目标境内仓库，无法生成报关到货通知。");

        var noticeCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice);
        var cost = decItem.TaxIncludedUnitPrice > 0m ? decItem.TaxIncludedUnitPrice : decItem.DeclareUnitPrice;
        var expectQty = decItem.DeclareQty;
        var expectTotal = Math.Round(expectQty * cost, 2, MidpointRounding.AwayFromZero);

        return new StockInNotify
        {
            Id = Guid.NewGuid().ToString(),
            NoticeCode = noticeCode,
            PurchaseOrderId = string.Empty,
            PurchaseOrderCode = string.Empty,
            PurchaseOrderItemId = string.Empty,
            SellOrderItemId = decItem.SellOrderItemId,
            VendorId = decItem.VendorId,
            Status = 10,
            ExpectedArrivalDate = now.Date,
            RegionType = RegionTypeCode.Domestic,
            StockInType = StockInTypeCode.Customs,
            Pn = decItem.PurchasePn,
            Brand = decItem.PurchaseBrand,
            ExpectQty = expectQty,
            ReceiveQty = 0,
            PassedQty = 0,
            Cost = cost,
            ExpectTotal = expectTotal,
            ReceiveTotal = 0,
            CustomsDeclarationItemId = decItem.Id,
            CreateTime = now,
            IsDeleted = false
        };
    }

    private async Task<CustomsDeclarationArrivalNotifyReadinessDto> EvaluateArrivalNotifyReadinessAsync(string declarationId)
    {
        var key = declarationId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "报关单 ID 无效。"
            };
        }

        var dec = await _declarationRepo.GetByIdAsync(key);
        if (dec == null || dec.IsDeleted)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "报关单不存在。"
            };
        }

        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "报关单已作废。"
            };
        }

        if (dec.CustomsClearanceStatus != CustomsClearanceStatusCodes.Cleared)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "海关状态须为「已结关」后才能生成报关到货通知。"
            };
        }

        if (string.IsNullOrWhiteSpace(dec.ToWarehouseId))
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "请先在报关单上维护目标境内仓库。"
            };
        }

        var items = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == dec.Id && !i.IsDeleted))
            .OrderBy(i => i.LineNo)
            .ToList();
        if (items.Count == 0)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                BlockReason = "报关单无明细。"
            };
        }

        var existingCodes = new List<string>();
        var pendingCount = 0;
        var existingCount = 0;
        var blockedByStockOut = false;

        foreach (var decItem in items)
        {
            var existing = (await _stockInNotifyRepo.FindAsync(n =>
                n.CustomsDeclarationItemId == decItem.Id && !n.IsDeleted)).FirstOrDefault();
            if (existing != null)
            {
                existingCount++;
                if (!string.IsNullOrWhiteSpace(existing.NoticeCode))
                    existingCodes.Add(existing.NoticeCode.Trim());
                continue;
            }

            var customsSorId = decItem.CustomsStockOutNotifyId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(customsSorId))
            {
                blockedByStockOut = true;
                continue;
            }

            var sor = await _stockOutRequestRepo.GetByIdAsync(customsSorId);
            if (sor == null || sor.IsDeleted || sor.Status != StockOutRequestStatusCode.StockedOut)
            {
                blockedByStockOut = true;
                continue;
            }

            pendingCount++;
        }

        if (pendingCount == 0 && existingCount == items.Count)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                CanCreate = false,
                PendingCount = 0,
                ExistingCount = existingCount,
                ExistingNoticeCodes = existingCodes,
                BlockReason = "全部明细已生成报关到货通知。"
            };
        }

        if (pendingCount == 0 && blockedByStockOut)
        {
            return new CustomsDeclarationArrivalNotifyReadinessDto
            {
                CanCreate = false,
                PendingCount = 0,
                ExistingCount = existingCount,
                ExistingNoticeCodes = existingCodes,
                BlockReason = "存在未完成报关出库的明细，请执行报关出库后再生成。"
            };
        }

        return new CustomsDeclarationArrivalNotifyReadinessDto
        {
            CanCreate = pendingCount > 0,
            PendingCount = pendingCount,
            ExistingCount = existingCount,
            ExistingNoticeCodes = existingCodes,
            BlockReason = pendingCount > 0 ? null : "当前没有可生成的报关到货通知。"
        };
    }

    private async Task<CustomsDeclarationItem?> FindDeclarationItemByCustomsStockOutNotifyAsync(string customsStockOutNotifyId)
    {
        var key = customsStockOutNotifyId.Trim();
        var items = (await _declarationItemRepo.FindAsync(i =>
            i.CustomsStockOutNotifyId == key && !i.IsDeleted)).ToList();
        return items.OrderBy(i => i.LineNo).FirstOrDefault();
    }

    private async Task<string> ResolveDefaultDomesticWarehouseIdAsync()
    {
        var warehouses = (await _warehouseRepo.FindAsync(w => w.Status == 1)).ToList();
        var domestic = warehouses
            .Where(w => RegionTypeCode.Normalize(w.RegionType) == RegionTypeCode.Domestic)
            .OrderBy(w => w.WarehouseCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (domestic == null || string.IsNullOrWhiteSpace(domestic.Id))
            throw new InvalidOperationException("未找到启用的境内仓库，请先在仓库档案中配置后再确认报关装箱。");
        return domestic.Id.Trim();
    }
}
