using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Services;

public sealed class StockInFlowService : IStockInFlowService
{
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<QCInfo> _qcRepo;
    private readonly IRepository<VendorInfo> _vendorRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IInventoryStockItemListQuery _stockItemListQuery;
    private readonly IStockItemFlowService _stockItemFlowService;
    private readonly ICustomsTraceQuery _customsTraceQuery;

    public StockInFlowService(
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrder> poRepo,
        IRepository<QCInfo> qcRepo,
        IRepository<VendorInfo> vendorRepo,
        IRepository<User> userRepo,
        IInventoryStockItemListQuery stockItemListQuery,
        IStockItemFlowService stockItemFlowService,
        ICustomsTraceQuery customsTraceQuery)
    {
        _stockInRepo = stockInRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _stockItemRepo = stockItemRepo;
        _poItemRepo = poItemRepo;
        _poRepo = poRepo;
        _qcRepo = qcRepo;
        _vendorRepo = vendorRepo;
        _userRepo = userRepo;
        _stockItemListQuery = stockItemListQuery;
        _stockItemFlowService = stockItemFlowService;
        _customsTraceQuery = customsTraceQuery;
    }

    /// <inheritdoc />
    public async Task<StockInFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockInId,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var id = stockInId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("入库单ID不能为空", nameof(stockInId));

        var stockIn = await _stockInRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("入库单不存在");

        var dto = new StockInFlowAggregatesDto { StockInId = id };

        var headerVendor = await LoadVendorAsync(stockIn.VendorId);
        await FillPurchaseOrderItemsAsync(dto, id, headerVendor, cancellationToken);
        await FillQcAsync(dto, stockIn, cancellationToken);
        dto.StockIn = await MapStockInStationAsync(stockIn, headerVendor, cancellationToken);

        var layers = (await _stockItemRepo.FindIgnoreFiltersAsync(x => x.StockInId == id))
            .OrderBy(x => x.CreateTime)
            .ThenBy(x => x.Id)
            .ToList();
        var layerIds = layers.Select(x => x.Id).ToList();
        var rowMap = new Dictionary<string, InventoryStockItemListRowDto>(StringComparer.OrdinalIgnoreCase);
        if (layerIds.Count > 0)
        {
            var rows = await _stockItemListQuery.GetByIdsAsync(
                layerIds,
                currentUserId,
                applyDataScope: false,
                cancellationToken);
            foreach (var row in rows)
                rowMap[row.StockItemId] = row;
        }

        foreach (var layer in layers)
        {
            if (!rowMap.TryGetValue(layer.Id, out var row))
                row = MapStockItemRowFallback(layer, stockIn);
            dto.StockItems.Add(MapStockItemStation(row));
        }

        foreach (var layer in layers)
        {
            if (!rowMap.TryGetValue(layer.Id, out var row))
                row = MapStockItemRowFallback(layer, stockIn);
            var slice = await _stockItemFlowService.GetDownstreamSliceAsync(layer.Id, row, cancellationToken);
            MergeDownstreamDocs(dto.StockOutNotifies, slice.StockOutNotifies);
            MergeDownstreamDocs(dto.Packings, slice.Packings);
            MergeDownstreamDocs(dto.StockOuts, slice.StockOuts);
        }

        dto.StockOutNotifies = SortDocs(dto.StockOutNotifies);
        dto.Packings = SortDocs(dto.Packings);
        dto.StockOuts = SortDocs(dto.StockOuts);
        return dto;
    }

    private async Task FillPurchaseOrderItemsAsync(
        StockInFlowAggregatesDto dto,
        string stockInId,
        VendorSnapshot? headerVendor,
        CancellationToken cancellationToken)
    {
        var lines = (await _stockInItemRepo.FindAsync(x => x.StockInId == stockInId && !x.IsDeleted)).ToList();
        if (lines.Count == 0)
            return;

        var lineIds = lines.Select(x => x.Id).ToList();
        var extends = (await _stockInItemExtendRepo.FindAsync(x => lineIds.Contains(x.Id) && !x.IsDeleted)).ToList();
        var extByLine = extends.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var poItemIds = extends
            .Select(x => x.PurchaseOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (poItemIds.Count == 0)
            return;

        var poItems = (await _poItemRepo.FindAsync(x => poItemIds.Contains(x.Id))).ToList();
        var poItemMap = poItems.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var poIds = poItems
            .Select(x => x.PurchaseOrderId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var pos = poIds.Count == 0
            ? new List<PurchaseOrder>()
            : (await _poRepo.FindAsync(x => poIds.Contains(x.Id))).ToList();
        var poMap = pos.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var poItemId in poItemIds)
        {
            if (!poItemMap.TryGetValue(poItemId, out var poItem) || poItem.IsDeleted)
                continue;

            PurchaseOrder? po = null;
            var poId = poItem.PurchaseOrderId?.Trim();
            if (!string.IsNullOrEmpty(poId))
                poMap.TryGetValue(poId, out po);

            var ext = extends.FirstOrDefault(x =>
                string.Equals(x.PurchaseOrderItemId?.Trim(), poItemId, StringComparison.OrdinalIgnoreCase));

            VendorSnapshot? vendor = headerVendor;
            if (po != null && !string.IsNullOrWhiteSpace(po.VendorId))
                vendor = await LoadVendorAsync(po.VendorId) ?? vendor;

            dto.PurchaseOrderItems.Add(new StockItemFlowDocDto
            {
                Id = poItem.Id,
                DocCode = FirstNonEmpty(
                    poItem.PurchaseOrderItemCode,
                    ext?.PurchaseOrderItemCode),
                Status = poItem.Status,
                CreateTime = poItem.CreateTime,
                VendorName = vendor?.Name,
                VendorCode = vendor?.Code,
                PersonName = FirstNonEmpty(po?.PurchaseUserName),
                UnitPrice = poItem.Cost,
                Currency = poItem.Currency,
                Qty = poItem.Qty,
                PurchaseOrderId = poItem.PurchaseOrderId,
                PurchaseOrderItemId = poItem.Id
            });
        }

        dto.PurchaseOrderItems = dto.PurchaseOrderItems
            .OrderBy(x => x.CreateTime)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private async Task FillQcAsync(
        StockInFlowAggregatesDto dto,
        StockIn stockIn,
        CancellationToken cancellationToken)
    {
        var qcId = stockIn.QcId?.Trim();
        if (string.IsNullOrEmpty(qcId))
            return;

        var qc = await _qcRepo.GetByIdAsync(qcId);
        if (qc == null || qc.IsDeleted)
            return;

        string? personName = null;
        if (!string.IsNullOrWhiteSpace(qc.CreateByUserId))
        {
            var user = await _userRepo.GetByIdAsync(qc.CreateByUserId.Trim());
            personName = FormatUser(user);
        }

        dto.Qcs.Add(new StockItemFlowDocDto
        {
            Id = qc.Id,
            DocCode = qc.QcCode,
            Status = qc.Status,
            CreateTime = qc.CreateTime,
            PersonName = personName,
            PassQty = qc.PassQty,
            RejectQty = qc.RejectQty,
            Qty = qc.PassQty,
            StockInNotifyId = qc.StockInNotifyId
        });
    }

    private async Task<StockItemFlowDocDto> MapStockInStationAsync(
        StockIn stockIn,
        VendorSnapshot? headerVendor,
        CancellationToken cancellationToken)
    {
        string? personName = null;
        var userIds = new List<string>();
        AddId(userIds, stockIn.CreateByUserId);
        AddId(userIds, stockIn.CreatedBy);
        if (userIds.Count > 0)
        {
            var users = (await _userRepo.FindAsync(u => userIds.Contains(u.Id))).ToList();
            personName = UserDisplay(users, stockIn.CreateByUserId) ?? UserDisplay(users, stockIn.CreatedBy);
        }

        string? customsDeclarationId = null;
        string? customsDeclarationCode = null;
        if (stockIn.StockInType == StockInTypeCode.Customs && !string.IsNullOrWhiteSpace(stockIn.SourceId))
        {
            var traceMap = await _customsTraceQuery.GetByStockInNotifyIdsAsync(
                new[] { stockIn.SourceId.Trim() },
                cancellationToken);
            if (traceMap.TryGetValue(stockIn.SourceId.Trim(), out var trace))
            {
                customsDeclarationId = EmptyToNull(trace.CustomsDeclarationId);
                customsDeclarationCode = EmptyToNull(trace.CustomsDeclarationCode);
            }
        }

        return new StockItemFlowDocDto
        {
            Id = stockIn.Id,
            DocCode = stockIn.StockInCode,
            Status = stockIn.Status,
            CreateTime = stockIn.CreateTime,
            BizDate = stockIn.StockInDate,
            VendorName = headerVendor?.Name,
            VendorCode = headerVendor?.Code,
            PersonName = personName,
            Qty = stockIn.TotalQuantity,
            StockInType = stockIn.StockInType,
            CustomsDeclarationId = customsDeclarationId,
            CustomsDeclarationCode = customsDeclarationCode
        };
    }

    private static StockItemFlowDocDto MapStockItemStation(InventoryStockItemListRowDto row) =>
        new()
        {
            Id = row.StockItemId,
            DocCode = row.StockItemCode,
            Status = row.OutboundStatus,
            CreateTime = row.CreateTime,
            BizDate = row.StockInDate,
            VendorName = row.VendorName,
            VendorCode = row.VendorCode,
            CustomerName = row.CustomerName,
            PersonName = row.SalespersonName,
            UnitPrice = row.PurchasePrice,
            Currency = row.PurchaseCurrency,
            SalesUnitPrice = row.SalesPrice,
            SalesCurrency = row.SalesCurrency,
            Qty = row.QtyInbound,
            Qty2 = row.QtyStockOut,
            StockInType = row.StockInType,
            StockAggregateId = row.StockAggregateId,
            IsDeleted = false
        };

    private static InventoryStockItemListRowDto MapStockItemRowFallback(StockItem layer, StockIn stockIn) =>
        new()
        {
            StockItemId = layer.Id,
            StockItemCode = layer.StockItemCode,
            StockInId = layer.StockInId,
            StockInCode = stockIn.StockInCode,
            StockInDate = stockIn.StockInDate,
            MaterialId = layer.MaterialId,
            PurchasePn = layer.PurchasePn,
            PurchaseBrand = layer.PurchaseBrand,
            PurchaseOrderItemCode = layer.PurchaseOrderItemCode,
            SellOrderItemCode = layer.SellOrderItemCode,
            QtyInbound = layer.QtyInbound,
            QtyStockOut = layer.QtyStockOut,
            QtyRepertory = layer.QtyRepertory,
            PurchasePrice = layer.PurchasePrice,
            PurchaseCurrency = layer.PurchaseCurrency,
            SalesPrice = layer.SalesPrice,
            SalesCurrency = layer.SalesCurrency,
            VendorId = layer.VendorId,
            VendorName = layer.VendorName,
            CustomerId = layer.CustomerId,
            CustomerName = layer.CustomerName,
            SalespersonName = layer.SalespersonName,
            PurchaserName = layer.PurchaserName,
            CreateTime = layer.CreateTime,
            StockAggregateId = layer.StockAggregateId,
            WarehouseId = layer.WarehouseId,
            OutboundStatus = layer.StockOutStatus,
            StockInType = stockIn.StockInType
        };

    private async Task<VendorSnapshot?> LoadVendorAsync(string? vendorId)
    {
        var id = vendorId?.Trim();
        if (string.IsNullOrEmpty(id))
            return null;
        var vendor = await _vendorRepo.GetByIdAsync(id);
        if (vendor == null)
            return null;
        return new VendorSnapshot
        {
            Name = FirstNonEmpty(vendor.OfficialName, vendor.NickName),
            Code = vendor.Code?.Trim()
        };
    }

    private static void MergeDownstreamDocs(List<StockItemFlowDocDto> target, IReadOnlyList<StockItemFlowDocDto> incoming)
    {
        foreach (var doc in incoming)
        {
            var key = doc.Id?.Trim();
            if (string.IsNullOrEmpty(key))
                continue;
            var existing = target.FirstOrDefault(x =>
                string.Equals(x.Id?.Trim(), key, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                target.Add(CloneDoc(doc));
                continue;
            }

            existing.Qty = SumQty(existing.Qty, doc.Qty);
        }
    }

    private static StockItemFlowDocDto CloneDoc(StockItemFlowDocDto doc) =>
        new()
        {
            Id = doc.Id,
            DocCode = doc.DocCode,
            Status = doc.Status,
            CreateTime = doc.CreateTime,
            BizDate = doc.BizDate,
            VendorName = doc.VendorName,
            VendorCode = doc.VendorCode,
            CustomerName = doc.CustomerName,
            CustomerCode = doc.CustomerCode,
            PersonName = doc.PersonName,
            UnitPrice = doc.UnitPrice,
            Currency = doc.Currency,
            SalesUnitPrice = doc.SalesUnitPrice,
            SalesCurrency = doc.SalesCurrency,
            Qty = doc.Qty,
            Qty2 = doc.Qty2,
            PassQty = doc.PassQty,
            RejectQty = doc.RejectQty,
            StockInType = doc.StockInType,
            StockOutType = doc.StockOutType,
            CustomsDeclarationId = doc.CustomsDeclarationId,
            CustomsDeclarationCode = doc.CustomsDeclarationCode,
            StockInNotifyId = doc.StockInNotifyId,
            PurchaseOrderId = doc.PurchaseOrderId,
            PurchaseOrderItemId = doc.PurchaseOrderItemId,
            StockAggregateId = doc.StockAggregateId,
            SellOrderId = doc.SellOrderId,
            LineDocCode = doc.LineDocCode,
            IsDeleted = doc.IsDeleted
        };

    private static List<StockItemFlowDocDto> SortDocs(List<StockItemFlowDocDto> docs) =>
        docs.OrderBy(x => x.CreateTime).ThenBy(x => x.Id).ToList();

    private static decimal? SumQty(decimal? a, decimal? b)
    {
        if (a == null && b == null) return null;
        return (a ?? 0m) + (b ?? 0m);
    }

    private static void AddId(List<string> ids, string? id)
    {
        var v = id?.Trim();
        if (string.IsNullOrEmpty(v) || ids.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
            return;
        ids.Add(v);
    }

    private static string? UserDisplay(IReadOnlyList<User> users, string? id)
    {
        var key = id?.Trim();
        if (string.IsNullOrEmpty(key))
            return null;
        var u = users.FirstOrDefault(x => string.Equals(x.Id, key, StringComparison.OrdinalIgnoreCase));
        return FormatUser(u);
    }

    private static string? FormatUser(User? u)
    {
        if (u == null) return null;
        var real = u.RealName?.Trim();
        if (!string.IsNullOrEmpty(real)) return real;
        var login = u.UserName?.Trim();
        return string.IsNullOrEmpty(login) ? null : login;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            var s = v?.Trim();
            if (!string.IsNullOrEmpty(s))
                return s;
        }
        return null;
    }

    private static string? EmptyToNull(string? v)
    {
        var s = v?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private sealed class VendorSnapshot
    {
        public string? Name { get; init; }
        public string? Code { get; init; }
    }
}
