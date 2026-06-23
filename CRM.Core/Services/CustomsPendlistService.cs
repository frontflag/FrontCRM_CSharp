using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class CustomsPendlistService : ICustomsPendlistService
{
    private readonly IRepository<CustomsPendlist> _pendlistRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<WarehouseInfo> _warehouseRepo;
    private readonly IRepository<User> _userRepo;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomsPendlistService(
        IRepository<CustomsPendlist> pendlistRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<WarehouseInfo> warehouseRepo,
        IRepository<User> userRepo,
        ISerialNumberService serialNumberService,
        IUnitOfWork unitOfWork)
    {
        _pendlistRepo = pendlistRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _stockItemRepo = stockItemRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _warehouseRepo = warehouseRepo;
        _userRepo = userRepo;
        _serialNumberService = serialNumberService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomsPendlistListItemDto>> GetListAsync(
        short? status,
        string? keyword,
        int take,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        _ = currentUserId;
        _ = cancellationToken;
        var n = Math.Clamp(take, 1, 1000);
        var all = (await _pendlistRepo.GetAllAsync())
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreateTime)
            .ThenBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (status.HasValue)
            all = all.Where(p => p.Status == status.Value).ToList();

        var kw = (keyword ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(kw))
        {
            var sorIds = all.Select(p => p.SalesStockOutNotifyId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var customsSorIds = all
                .Select(p => p.CustomsStockOutNotifyId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var sorMap = (await _stockOutRequestRepo.FindAsync(r =>
                    sorIds.Contains(r.Id) || customsSorIds.Contains(r.Id)))
                .ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

            all = all.Where(p =>
            {
                if (p.Id.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (sorMap.TryGetValue(p.SalesStockOutNotifyId.Trim(), out var salesSor)
                    && salesSor.RequestCode.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!string.IsNullOrWhiteSpace(p.CustomsStockOutNotifyId)
                    && sorMap.TryGetValue(p.CustomsStockOutNotifyId.Trim(), out var customsSor)
                    && customsSor.RequestCode.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }).ToList();
        }

        all = all.Take(n).ToList();
        if (all.Count == 0)
            return Array.Empty<CustomsPendlistListItemDto>();

        return await ProjectListDtosAsync(all);
    }

    public async Task<CreateCustomsOutNotifyResultDto> CreateCustomsOutNotifyAsync(
        string pendlistId,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var id = pendlistId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("待报关记录 ID 不能为空。");

        var pendlist = await _pendlistRepo.GetByIdAsync(id)
                       ?? throw new InvalidOperationException("待报关记录不存在。");
        if (pendlist.IsDeleted)
            throw new InvalidOperationException("待报关记录已删除。");
        if (pendlist.Status != CustomsPendlistStatusCode.Open)
            throw new InvalidOperationException("仅「待处理」状态的待报关记录可生成报关出库通知。");
        if (!string.IsNullOrWhiteSpace(pendlist.CustomsStockOutNotifyId))
            throw new InvalidOperationException("该待报关记录已生成报关出库通知，请勿重复操作。");

        var salesSor = await _stockOutRequestRepo.GetByIdAsync(pendlist.SalesStockOutNotifyId.Trim())
                       ?? throw new InvalidOperationException("关联的销售出库通知不存在。");
        if (salesSor.IsDeleted)
            throw new InvalidOperationException("关联的销售出库通知已删除。");
        if (StockOutTypeCode.NormalizeForNotify(salesSor.StockOutType) != StockOutTypeCode.Sales)
            throw new InvalidOperationException("待报关记录须关联销售出库通知。");
        if (salesSor.Status != StockOutRequestStatusCode.PendingCustoms)
            throw new InvalidOperationException("销售出库通知须处于「待报关」状态。");

        await EnsureOverseasStockCoversQtyAsync(pendlist.SellOrderItemId.Trim(), pendlist.Qty);

        var requestCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest);
        var customsSorId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        var requestUserId = string.IsNullOrWhiteSpace(salesSor.RequestUserId)
            ? actor
            : salesSor.RequestUserId;
        if (string.IsNullOrWhiteSpace(requestUserId))
            throw new InvalidOperationException("销售出库通知缺少申请人，不能生成报关出库通知。");

        var orphanCustomsSor = (await _stockOutRequestRepo.FindAsync(s =>
                s.CustomsPendlistId == pendlist.Id && !s.IsDeleted))
            .FirstOrDefault();
        if (orphanCustomsSor != null)
        {
            throw new InvalidOperationException(
                $"已存在关联报关出库通知 {orphanCustomsSor.RequestCode}，但待报关记录未正确关联，请联系管理员处理。");
        }

        var customsSor = new StockOutRequest
        {
            Id = customsSorId,
            RequestCode = requestCode,
            SalesOrderId = salesSor.SalesOrderId,
            SalesOrderItemId = salesSor.SalesOrderItemId,
            MaterialCode = salesSor.MaterialCode,
            MaterialName = salesSor.MaterialName,
            Quantity = pendlist.Qty,
            CustomerId = salesSor.CustomerId,
            RequestUserId = requestUserId,
            RequestDate = salesSor.RequestDate,
            Status = StockOutRequestStatusCode.PendingPacking,
            Remark = salesSor.Remark,
            ShipmentMethod = salesSor.ShipmentMethod,
            ExpressCompany = salesSor.ExpressCompany,
            RegionType = RegionTypeCode.Overseas,
            StockOutType = StockOutTypeCode.Customs,
            CustomsPendlistId = pendlist.Id,
            CustomsStatus = StockOutNotifyCustomsStatusCode.InCustoms,
            CreateTime = now,
            CreateByUserId = actor
        };

        // 分步保存，避免 customs_pendlist ↔ stockout_notify 循环外键在同一批次中顺序冲突
        // （与 StockOutService 先落销售出库通知、再落待报关记录一致）
        await _stockOutRequestRepo.AddAsync(customsSor);
        await _unitOfWork.SaveChangesAsync();

        salesSor.CustomsStatus = StockOutNotifyCustomsStatusCode.InCustoms;
        salesSor.ModifyTime = now;
        salesSor.ModifyByUserId = actor;

        pendlist.CustomsStockOutNotifyId = customsSorId;
        pendlist.Status = CustomsPendlistStatusCode.CustomsOutNotifyCreated;
        pendlist.ModifyTime = now;
        pendlist.ModifyByUserId = actor;

        await _stockOutRequestRepo.UpdateAsync(salesSor);
        await _pendlistRepo.UpdateAsync(pendlist);
        await _unitOfWork.SaveChangesAsync();

        return new CreateCustomsOutNotifyResultDto
        {
            PendlistId = pendlist.Id,
            CustomsStockOutNotifyId = customsSorId,
            CustomsStockOutNotifyCode = requestCode,
            PendlistStatus = pendlist.Status
        };
    }

    public async Task EnsureSalesNotifyDeletableAsync(string salesStockOutNotifyId)
    {
        var key = salesStockOutNotifyId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return;

        var rows = (await _pendlistRepo.FindAsync(p =>
            p.SalesStockOutNotifyId == key && !p.IsDeleted)).ToList();
        var blocked = rows.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.CustomsStockOutNotifyId));
        if (blocked != null)
            throw new InvalidOperationException("已生成报关出库通知，不能删除或取消该销售出库通知。");
    }

    public async Task CancelBySalesStockOutNotifyAsync(string salesStockOutNotifyId, string? actingUserId)
    {
        var key = salesStockOutNotifyId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return;

        var rows = (await _pendlistRepo.FindAsync(p =>
            p.SalesStockOutNotifyId == key && !p.IsDeleted)).ToList();
        if (rows.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        foreach (var row in rows)
        {
            if (row.Status == CustomsPendlistStatusCode.Cancelled)
                continue;
            row.Status = CustomsPendlistStatusCode.Cancelled;
            row.ModifyTime = now;
            row.ModifyByUserId = actor;
            await _pendlistRepo.UpdateAsync(row);
        }
    }

    private async Task EnsureOverseasStockCoversQtyAsync(string sellOrderItemId, int qty)
    {
        var lineId = sellOrderItemId.Trim();
        var line = await _sellOrderItemRepo.GetByIdAsync(lineId)
                   ?? throw new InvalidOperationException("关联的销售订单明细不存在。");

        var customerOverseasLayers = (await _stockItemRepo.FindAsync(si =>
            si.SellOrderItemId == lineId
            && si.QtyRepertoryAvailable > 0
            && si.RegionType == RegionTypeCode.Overseas)).ToList();
        var customerOverseasAvail = customerOverseasLayers.Sum(si => si.QtyRepertoryAvailable);
        if (customerOverseasAvail >= qty)
            return;

        var stockingLayers = (await LoadEligibleStockingLayersAsync(line.PN, line.Brand))
            .Where(si => RegionTypeCode.Normalize(si.RegionType) == RegionTypeCode.Overseas)
            .ToList();
        var stockingAvail = stockingLayers.Sum(si => si.QtyRepertoryAvailable);
        if (customerOverseasAvail + stockingAvail < qty)
            throw new InvalidOperationException(
                $"境外可用库存不足（客单 {customerOverseasAvail} + 备货 {stockingAvail}，需要 {qty}），不能生成报关出库通知。");
    }

    /// <summary>与 <see cref="StockOutService.TryBuildCustomsPendlistAsync"/> 备货池口径一致。</summary>
    private async Task<List<StockItem>> LoadEligibleStockingLayersAsync(string? purchasePn, string? purchaseBrand)
    {
        var pnKey = NormInventoryKey(purchasePn);
        var brKey = NormInventoryKey(purchaseBrand);
        if (string.IsNullOrEmpty(pnKey) || string.IsNullOrEmpty(brKey))
            return new List<StockItem>();

        var layers = (await _stockItemRepo.FindAsync(si =>
            si.StockType == StockInventoryTypeCodes.Stocking
            && si.QtyRepertoryAvailable > 0
            && (si.TransferType == null
                || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource))).ToList();

        return layers
            .Where(si =>
                string.Equals(NormInventoryKey(si.PurchasePn), pnKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(NormInventoryKey(si.PurchaseBrand), brKey, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string NormInventoryKey(string? v) => (v ?? string.Empty).Trim();

    private async Task<IReadOnlyList<CustomsPendlistListItemDto>> ProjectListDtosAsync(IReadOnlyList<CustomsPendlist> rows)
    {
        var sorIds = rows.Select(r => r.SalesStockOutNotifyId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var customsSorIds = rows
            .Select(r => r.CustomsStockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var allSorIds = sorIds.Concat(customsSorIds).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sorMap = allSorIds.Count == 0
            ? new Dictionary<string, StockOutRequest>(StringComparer.OrdinalIgnoreCase)
            : (await _stockOutRequestRepo.FindAsync(r => allSorIds.Contains(r.Id)))
                .ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

        var lineIds = rows.Select(r => r.SellOrderItemId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lineMap = lineIds.Count == 0
            ? new Dictionary<string, SellOrderItem>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepo.FindAsync(i => lineIds.Contains(i.Id)))
                .ToDictionary(i => i.Id.Trim(), i => i, StringComparer.OrdinalIgnoreCase);

        var soIds = sorMap.Values
            .Select(r => r.SalesOrderId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var soMap = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepo.FindAsync(s => soIds.Contains(s.Id)))
                .ToDictionary(s => s.Id.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

        var whIds = rows
            .Select(r => r.OverseasWarehouseId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var whMap = whIds.Count == 0
            ? new Dictionary<string, WarehouseInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _warehouseRepo.FindAsync(w => whIds.Contains(w.Id)))
                .ToDictionary(w => w.Id.Trim(), w => w, StringComparer.OrdinalIgnoreCase);

        var userIds = rows
            .Select(r => r.CreateByUserId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var userMap = userIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepo.FindAsync(u => userIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        return rows.Select(p =>
        {
            sorMap.TryGetValue(p.SalesStockOutNotifyId.Trim(), out var salesSor);
            StockOutRequest? customsSor = null;
            if (!string.IsNullOrWhiteSpace(p.CustomsStockOutNotifyId))
                sorMap.TryGetValue(p.CustomsStockOutNotifyId.Trim(), out customsSor);

            lineMap.TryGetValue(p.SellOrderItemId.Trim(), out var line);
            SellOrder? so = null;
            if (salesSor != null && !string.IsNullOrWhiteSpace(salesSor.SalesOrderId))
                soMap.TryGetValue(salesSor.SalesOrderId.Trim(), out so);

            WarehouseInfo? wh = null;
            if (!string.IsNullOrWhiteSpace(p.OverseasWarehouseId))
                whMap.TryGetValue(p.OverseasWarehouseId.Trim(), out wh);

            User? creator = null;
            if (!string.IsNullOrWhiteSpace(p.CreateByUserId))
                userMap.TryGetValue(p.CreateByUserId.Trim(), out creator);

            return new CustomsPendlistListItemDto
            {
                Id = p.Id,
                SalesStockOutNotifyId = p.SalesStockOutNotifyId,
                SalesStockOutNotifyCode = salesSor?.RequestCode,
                SellOrderItemId = p.SellOrderItemId,
                SellOrderItemCode = line?.SellOrderItemCode,
                Qty = p.Qty,
                Status = p.Status,
                CustomsStockOutNotifyId = p.CustomsStockOutNotifyId,
                CustomsStockOutNotifyCode = customsSor?.RequestCode,
                OverseasWarehouseId = p.OverseasWarehouseId,
                OverseasWarehouseName = wh?.WarehouseName,
                SalesOrderId = salesSor?.SalesOrderId,
                SalesOrderCode = so?.SellOrderCode,
                MaterialCode = salesSor?.MaterialCode,
                MaterialName = salesSor?.MaterialName,
                CustomerName = so?.CustomerName,
                CreateTime = p.CreateTime,
                CreateByUserId = p.CreateByUserId,
                CreateUserDisplay = creator?.UserName
            };
        }).ToList();
    }
}
