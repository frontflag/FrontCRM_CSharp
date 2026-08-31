using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Auth;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Packings;

public class PackingService : IPackingService
{
    private readonly ApplicationDbContext _db;
    private readonly IPackingListQuery _packingListQuery;
    private readonly IRepository<Packing> _packingRepository;
    private readonly IRepository<PackingItem> _packingItemRepository;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepository;
    private readonly IRepository<SellOrder> _sellOrderRepository;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
    private readonly IRepository<CustomerInfo> _customerRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IPackingItemLineSeqService _packingItemLineSeq;
    private readonly IStockOutService _stockOutService;
    private readonly ICustomsV2FlowService _customsV2FlowService;
    private readonly ICustomsTraceQuery _customsTraceQuery;
    private readonly IForceDeleteGuardService _forceDeleteGuard;
    private readonly IInventoryCenterService _inventoryCenterService;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly IPackingStatusReconcileService _packingStatusReconcile;

    public PackingService(
        ApplicationDbContext db,
        IPackingListQuery packingListQuery,
        IRepository<Packing> packingRepository,
        IRepository<PackingItem> packingItemRepository,
        IRepository<StockOutRequest> stockOutRequestRepository,
        IRepository<SellOrder> sellOrderRepository,
        IRepository<SellOrderItem> sellOrderItemRepository,
        IRepository<CustomerInfo> customerRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        IPackingItemLineSeqService packingItemLineSeq,
        IStockOutService stockOutService,
        ICustomsV2FlowService customsV2FlowService,
        ICustomsTraceQuery customsTraceQuery,
        IForceDeleteGuardService forceDeleteGuard,
        IInventoryCenterService inventoryCenterService,
        ILogOperationAppendService logOperationAppend,
        IPackingStatusReconcileService packingStatusReconcile)
    {
        _db = db;
        _packingListQuery = packingListQuery;
        _packingRepository = packingRepository;
        _packingItemRepository = packingItemRepository;
        _stockOutRequestRepository = stockOutRequestRepository;
        _sellOrderRepository = sellOrderRepository;
        _sellOrderItemRepository = sellOrderItemRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _packingItemLineSeq = packingItemLineSeq;
        _stockOutService = stockOutService;
        _customsV2FlowService = customsV2FlowService;
        _customsTraceQuery = customsTraceQuery;
        _forceDeleteGuard = forceDeleteGuard;
        _inventoryCenterService = inventoryCenterService;
        _logOperationAppend = logOperationAppend;
        _packingStatusReconcile = packingStatusReconcile;
    }

    public async Task<PagedResult<PackingListItemDto>> GetPackingListPagedAsync(
        PackingListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _packingListQuery.GetPagedPackingIdsAsync(filter, page, pageSize, cancellationToken);
        if (paged.TotalCount == 0)
        {
            return new PagedResult<PackingListItemDto>
            {
                Items = Array.Empty<PackingListItemDto>(),
                TotalCount = 0,
                PageIndex = paged.PageIndex,
                PageSize = paged.PageSize
            };
        }

        var items = await ProjectPackingListItemDtosAsync(paged.Items.ToList(), cancellationToken);

        return new PagedResult<PackingListItemDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            PageIndex = paged.PageIndex,
            PageSize = paged.PageSize
        };
    }

    public async Task<List<PackingListItemDto>> GetPackingListItemsByIdsAsync(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return new List<PackingListItemDto>();

        var idList = ids
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new List<PackingListItemDto>();

        var dtos = await ProjectPackingListItemDtosAsync(idList, cancellationToken);
        var dtoById = dtos.ToDictionary(d => d.Id.Trim(), d => d, StringComparer.OrdinalIgnoreCase);

        var result = new List<PackingListItemDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            var key = id.Trim();
            if (!seen.Add(key))
                continue;
            if (dtoById.TryGetValue(key, out var dto))
                result.Add(dto);
        }

        return result;
    }

    private async Task<List<PackingListItemDto>> ProjectPackingListItemDtosAsync(
        IReadOnlyList<string> orderedIds,
        CancellationToken cancellationToken)
    {
        if (orderedIds == null || orderedIds.Count == 0)
            return new List<PackingListItemDto>();

        var idSet = orderedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packings = (await _packingRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
        var byId = packings.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var customerIds = packings.Select(x => x.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var customers = customerIds.Count == 0
            ? new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _customerRepository.FindAsync(c => customerIds.Contains(c.Id)))
                .ToDictionary(c => c.Id.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var warehouseIdsFromPacking = packings
            .Select(x => x.StorageId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var warehouses = warehouseIdsFromPacking.Count == 0
            ? new Dictionary<string, WarehouseInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Warehouses.AsNoTracking().Where(w => warehouseIdsFromPacking.Contains(w.Id)).ToListAsync(cancellationToken))
                .ToDictionary(w => w.Id.Trim(), w => w, StringComparer.OrdinalIgnoreCase);

        // 历史数据：storage_id 曾写入 stock.StockId，列表仍尝试经 stock 解析仓库名
        var legacyStockIds = packings
            .Select(x => x.StorageId?.Trim())
            .Where(sid => !string.IsNullOrEmpty(sid) && !warehouses.ContainsKey(sid!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var legacyStocks = legacyStockIds.Count == 0
            ? new Dictionary<string, StockInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Stocks.AsNoTracking().Where(s => legacyStockIds.Contains(s.Id)).ToListAsync(cancellationToken))
                .ToDictionary(s => s.Id.Trim(), s => s, StringComparer.OrdinalIgnoreCase);
        var legacyWarehouseIds = legacyStocks.Values
            .Select(s => s.WarehouseId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(wid => !warehouses.ContainsKey(wid))
            .ToList();
        if (legacyWarehouseIds.Count > 0)
        {
            var legacyWarehouses = await _db.Warehouses.AsNoTracking()
                .Where(w => legacyWarehouseIds.Contains(w.Id))
                .ToListAsync(cancellationToken);
            foreach (var wh in legacyWarehouses)
                warehouses[wh.Id.Trim()] = wh;
        }

        var userIds = packings
            .SelectMany(x => new[] { x.SalesId, x.CreateByUserId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var users = userIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepository.FindAsync(u => userIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var ships = await _db.PackingExtendShips
            .AsNoTracking()
            .Where(s => idSet.Contains(s.PackingId))
            .ToListAsync(cancellationToken);
        var shipByPackingId = ships
            .GroupBy(s => s.PackingId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var notifySummaryByPackingId = await LoadNotifySummaryByPackingIdsAsync(idSet, cancellationToken);

        // 装箱明细关联销售订单 → 头客户名优先用订单快照（与出库通知一致）
        var itemSoLinks = await _db.PackingItems.AsNoTracking()
            .Where(i =>
                idSet.Contains(i.PackingId)
                && !i.IsDeleted
                && i.SellOrderId != null
                && i.SellOrderId != "")
            .Select(i => new { i.PackingId, SellOrderId = i.SellOrderId! })
            .ToListAsync(cancellationToken);
        var linkedSoIds = itemSoLinks
            .Select(x => x.SellOrderId.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var linkedSellOrders = linkedSoIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => linkedSoIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);
        var soCustomerNameByPackingId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in itemSoLinks.GroupBy(x => x.PackingId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            foreach (var link in g)
            {
                if (!linkedSellOrders.TryGetValue(link.SellOrderId.Trim(), out var so))
                    continue;
                var name = so.CustomerName?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                soCustomerNameByPackingId[g.Key] = name;
                break;
            }
        }

        var decIds = packings
            .Where(p => StockOutTypeCode.NormalizeForNotify(p.StockOutType) == StockOutTypeCode.Customs)
            .Select(p => p.CustomsDeclarationId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var decCodeById = decIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsDeclarations.AsNoTracking()
                    .Where(d => decIds.Contains(d.Id) && !d.IsDeleted)
                    .Select(d => new { d.Id, d.DeclarationCode })
                    .ToListAsync(cancellationToken))
                .ToDictionary(
                    d => d.Id.Trim(),
                    d => d.DeclarationCode.Trim(),
                    StringComparer.OrdinalIgnoreCase);

        static string? FormatUserName(User? user) =>
            user == null
                ? null
                : EntityLookupService.FormatUserLoginName(user) ?? user.RealName ?? user.UserName;

        var items = new List<PackingListItemDto>();
        foreach (var id in orderedIds)
        {
            if (!byId.TryGetValue(id.Trim(), out var pk))
                continue;
            customers.TryGetValue(pk.CustomerId ?? string.Empty, out var cust);
            User? salesUser = null;
            if (!string.IsNullOrWhiteSpace(pk.SalesId))
                users.TryGetValue(pk.SalesId.Trim(), out salesUser);
            User? createUser = null;
            if (!string.IsNullOrWhiteSpace(pk.CreateByUserId))
                users.TryGetValue(pk.CreateByUserId.Trim(), out createUser);

            string? warehouseName = null;
            var storageKey = pk.StorageId?.Trim() ?? "";
            if (storageKey.Length > 0)
            {
                if (warehouses.TryGetValue(storageKey, out var whDirect))
                    warehouseName = whDirect.WarehouseName;
                else if (legacyStocks.TryGetValue(storageKey, out var legacyStock)
                         && warehouses.TryGetValue(legacyStock.WarehouseId.Trim(), out var whLegacy))
                    warehouseName = whLegacy.WarehouseName;
            }

            shipByPackingId.TryGetValue(pk.Id.Trim(), out var ship);
            notifySummaryByPackingId.TryGetValue(pk.Id.Trim(), out var notifySummary);

            var shipmentMethod = ResolvePackingShipmentMethod(ship)
                ?? notifySummary.ShipmentMethod;
            var expressCompany = !string.IsNullOrWhiteSpace(ship?.ExpressCompany)
                ? ship!.ExpressCompany.Trim()
                : notifySummary.ExpressCompany;

            string? customsDeclarationId = null;
            string? customsDeclarationCode = null;
            if (StockOutTypeCode.NormalizeForNotify(pk.StockOutType) == StockOutTypeCode.Customs)
            {
                customsDeclarationId = string.IsNullOrWhiteSpace(pk.CustomsDeclarationId)
                    ? null
                    : pk.CustomsDeclarationId.Trim();
                if (!string.IsNullOrEmpty(customsDeclarationId))
                    decCodeById.TryGetValue(customsDeclarationId, out customsDeclarationCode);
            }

            soCustomerNameByPackingId.TryGetValue(pk.Id.Trim(), out var soCustomerName);
            items.Add(new PackingListItemDto
            {
                Id = pk.Id,
                Code = pk.Code,
                Status = pk.Status,
                StockOutType = pk.StockOutType,
                MaterialType = pk.MaterialType,
                CustomerId = pk.CustomerId,
                CustomerName = !string.IsNullOrEmpty(soCustomerName)
                    ? soCustomerName
                    : FormatCustomerDisplayName(cust),
                SalesId = pk.SalesId,
                SalesUserName = FormatUserName(salesUser),
                StorageId = pk.StorageId,
                WarehouseName = warehouseName,
                ItemRows = pk.ItemRows,
                Comment = pk.Comment,
                ScheduleShipDate = pk.ScheduleShipDate,
                RequestDate = notifySummary.RequestDate,
                ShipmentMethod = shipmentMethod,
                ExpressCompany = expressCompany,
                CreateTime = pk.CreateTime,
                CreateByUserId = pk.CreateByUserId,
                CreateUserName = FormatUserName(createUser),
                ShipCompany = ship?.ShipCompany,
                ShipAddress = ship?.ShipAddress,
                CustomsDeclarationId = customsDeclarationId,
                CustomsDeclarationCode = customsDeclarationCode
            });
        }

        return items;
    }

    public async Task<PagedResult<PackingItemListRowDto>> GetPackingItemListPagedAsync(
        string? keyword,
        string? packingCode,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var paged = await _packingListQuery.GetPagedPackingItemIdsAsync(
            keyword, packingCode, page, pageSize, currentUserId, cancellationToken);
        if (paged.TotalCount == 0)
        {
            return new PagedResult<PackingItemListRowDto>
            {
                Items = Array.Empty<PackingItemListRowDto>(),
                TotalCount = 0,
                PageIndex = paged.PageIndex,
                PageSize = paged.PageSize
            };
        }

        var idSet = paged.Items.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lines = (await _packingItemRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
        var byId = lines.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var packingIds = lines.Select(x => x.PackingId).Distinct().ToList();
        var packings = (await _packingRepository.FindAsync(p => packingIds.Contains(p.Id)))
            .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var soIds = lines.Select(x => x.SellOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var sellOrders = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var soItemIds = lines.Select(x => x.SellOrderItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var sellItems = soItemIds.Count == 0
            ? new Dictionary<string, SellOrderItem>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepository.FindAsync(si => soItemIds.Contains(si.Id)))
                .ToDictionary(si => si.Id.Trim(), si => si, StringComparer.OrdinalIgnoreCase);

        var rows = new List<PackingItemListRowDto>();
        foreach (var id in paged.Items)
        {
            if (!byId.TryGetValue(id.Trim(), out var line))
                continue;
            packings.TryGetValue(line.PackingId.Trim(), out var pk);
            SellOrder? so = null;
            if (!string.IsNullOrWhiteSpace(line.SellOrderId))
                sellOrders.TryGetValue(line.SellOrderId.Trim(), out so);
            SellOrderItem? soItem = null;
            if (!string.IsNullOrWhiteSpace(line.SellOrderItemId))
                sellItems.TryGetValue(line.SellOrderItemId.Trim(), out soItem);

            rows.Add(new PackingItemListRowDto
            {
                Id = line.Id,
                PackingId = line.PackingId,
                PackingCode = pk?.Code ?? string.Empty,
                PackingStatus = pk?.Status ?? PackingStatusCode.New,
                Pn = line.Pn,
                Brand = line.Brand,
                Qty = line.Qty,
                Unit = line.Unit,
                SellOrderId = line.SellOrderId,
                SellOrderItemId = line.SellOrderItemId,
                SellOrderCode = so?.SellOrderCode,
                SellOrderItemCode = soItem?.SellOrderItemCode,
                ItemCode = line.ItemCode,
                CustomerName = so?.CustomerName,
                CreateTime = line.CreateTime
            });
        }

        return new PagedResult<PackingItemListRowDto>
        {
            Items = rows,
            TotalCount = paged.TotalCount,
            PageIndex = paged.PageIndex,
            PageSize = paged.PageSize
        };
    }

    public async Task<PackingDetailDto?> GetPackingByIdAsync(
        string packingId,
        CancellationToken cancellationToken = default)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            return null;

        var pk = await _db.Packings
            .AsNoTracking()
            .Include(p => p.ExtendBox)
            .Include(p => p.ExtendShip)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (pk == null)
            return null;

        var lines = await _db.PackingItems
            .AsNoTracking()
            .Include(i => i.Extend)
            .Where(i => i.PackingId == id && !i.IsDeleted)
            .OrderBy(i => i.CreateTime)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        CustomerInfo? cust = null;
        if (!string.IsNullOrWhiteSpace(pk.CustomerId))
            cust = await _customerRepository.GetByIdAsync(pk.CustomerId.Trim());

        User? salesUser = null;
        if (!string.IsNullOrWhiteSpace(pk.SalesId))
            salesUser = await _userRepository.GetByIdAsync(pk.SalesId.Trim());

        User? createUser = null;
        if (!string.IsNullOrWhiteSpace(pk.CreateByUserId))
            createUser = await _userRepository.GetByIdAsync(pk.CreateByUserId.Trim());

        var lineIds = lines.Select(l => l.Id).ToList();
        var extendRows = lineIds.Count == 0
            ? new List<PackingItemExtend>()
            : await _db.PackingItemExtends
                .AsNoTracking()
                .Where(e => lineIds.Contains(e.PackingItemId))
                .OrderBy(e => e.PackingItemId)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

        var soIds = lines.Select(x => x.SellOrderId)
            .Concat(extendRows.Select(e => e.SellOrderId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellOrders = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var soItemIds = lines.Select(x => x.SellOrderItemId)
            .Concat(extendRows.Select(e => e.SellOrderItemId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellItems = soItemIds.Count == 0
            ? new Dictionary<string, SellOrderItem>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepository.FindAsync(si => soItemIds.Contains(si.Id)))
                .ToDictionary(si => si.Id.Trim(), si => si, StringComparer.OrdinalIgnoreCase);

        var extendCustomerIds = extendRows
            .Select(e => e.CustomerId)
            .Concat(
                extendRows
                    .Select(e => e.SellOrderId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Select(id => sellOrders.TryGetValue(id, out var so) ? so.CustomerId : null))
            .Append(pk.CustomerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var extendCustomers = extendCustomerIds.Count == 0
            ? new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _customerRepository.FindAsync(c => extendCustomerIds.Contains(c.Id)))
                .ToDictionary(c => c.Id.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var extendSalesIds = extendRows
            .Select(e => e.SalesId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var extendSalesUsers = extendSalesIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepository.FindAsync(u => extendSalesIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var detailLines = new List<PackingDetailLineDto>();
        foreach (var line in lines)
        {
            SellOrder? so = null;
            if (!string.IsNullOrWhiteSpace(line.SellOrderId))
                sellOrders.TryGetValue(line.SellOrderId.Trim(), out so);
            SellOrderItem? soItem = null;
            if (!string.IsNullOrWhiteSpace(line.SellOrderItemId))
                sellItems.TryGetValue(line.SellOrderItemId.Trim(), out soItem);

            detailLines.Add(new PackingDetailLineDto
            {
                Id = line.Id,
                Pn = line.Pn,
                Brand = line.Brand,
                Qty = line.Qty,
                Unit = line.Unit,
                SellOrderId = line.SellOrderId,
                SellOrderItemId = line.SellOrderItemId,
                StockOutNotifyId = line.StockOutNotifyId,
                StockItemId = line.StockItemId,
                SellOrderCode = so?.SellOrderCode,
                SellOrderItemCode = soItem?.SellOrderItemCode,
                ItemCode = line.ItemCode,
                CustomerSo = line.Extend?.CustomerSo,
                CustomerPn = FirstNonEmpty(line.Extend?.CustomerPn, soItem?.CustomerPn),
                CustomerBrand = FirstNonEmpty(line.Extend?.CustomerBrand, soItem?.CustomerBrand),
                Co = string.IsNullOrWhiteSpace(line.Co) ? null : line.Co.Trim(),
                Price = line.Extend?.Price,
                PriceCurrency = line.Extend?.PriceCurrency,
                Comment = line.Comment
            });
        }

        var customsDeclarationId = string.IsNullOrWhiteSpace(pk.CustomsDeclarationId)
            ? null
            : pk.CustomsDeclarationId.Trim();
        StockOutCustomsSummaryDto? customsSummary = null;
        string? customsDeclarationCode = null;
        if (!string.IsNullOrEmpty(customsDeclarationId))
        {
            customsSummary = await _customsTraceQuery.ResolveCustomsSummaryByDeclarationIdAsync(
                customsDeclarationId,
                cancellationToken);
            customsDeclarationCode = customsSummary?.DeclarationCode;
        }

        SellOrder? headerSellOrder = null;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.SellOrderId))
                continue;
            if (!sellOrders.TryGetValue(line.SellOrderId.Trim(), out var so))
                continue;
            if (string.IsNullOrWhiteSpace(so.CustomerName))
                continue;
            headerSellOrder = so;
            break;
        }

        return new PackingDetailDto
        {
            Id = pk.Id,
            Code = pk.Code,
            Status = pk.Status,
            StockOutType = pk.StockOutType,
            MaterialType = pk.MaterialType,
            CustomerId = pk.CustomerId,
            CustomerName = ResolvePackingHeaderCustomerName(cust, headerSellOrder),
            SalesId = pk.SalesId,
            SalesUserName = salesUser == null
                ? null
                : EntityLookupService.FormatUserLoginName(salesUser) ?? salesUser.RealName ?? salesUser.UserName,
            ItemRows = pk.ItemRows,
            ScheduleShipDate = pk.ScheduleShipDate,
            Comment = pk.Comment,
            CreateTime = pk.CreateTime,
            CreateByUserId = pk.CreateByUserId,
            CreateUserName = createUser == null
                ? null
                : EntityLookupService.FormatUserLoginName(createUser) ?? createUser.RealName ?? createUser.UserName,
            BoxNw = pk.ExtendBox?.Nw,
            BoxGw = pk.ExtendBox?.Gw,
            BoxDim = pk.ExtendBox?.Dim,
            BoxCtns = pk.ExtendBox?.Ctns,
            ShipCompany = pk.ExtendShip?.ShipCompany,
            ShipAddress = pk.ExtendShip?.ShipAddress,
            ShipAttn = pk.ExtendShip?.ShipAttn,
            ShipTel = pk.ExtendShip?.ShipTel,
            BillCompany = pk.ExtendShip?.BillCompany,
            BillAddress = pk.ExtendShip?.BillAddress,
            BillAttn = pk.ExtendShip?.BillAttn,
            BillTel = pk.ExtendShip?.BillTel,
            DeliveryReq = pk.ExtendShip?.DeliveryReq,
            ShipmentMethod = ResolvePackingShipmentMethod(pk.ExtendShip),
            ExpressCompany = string.IsNullOrWhiteSpace(pk.ExtendShip?.ExpressCompany)
                ? null
                : pk.ExtendShip!.ExpressCompany.Trim(),
#pragma warning disable CS0618
            DeliveryMethod = pk.ExtendShip?.DeliveryMethod,
#pragma warning restore CS0618
            CustomsDeclarationId = customsDeclarationId,
            CustomsDeclarationCode = customsDeclarationCode,
            CustomsSummary = customsSummary,
            Items = detailLines,
            StockOutNotifies = await LoadStockOutNotifiesForPackingAsync(lines, cancellationToken),
            ItemExtends = extendRows.Select(e =>
            {
                SellOrder? so = null;
                if (!string.IsNullOrWhiteSpace(e.SellOrderId))
                    sellOrders.TryGetValue(e.SellOrderId.Trim(), out so);
                SellOrderItem? soItem = null;
                if (!string.IsNullOrWhiteSpace(e.SellOrderItemId))
                    sellItems.TryGetValue(e.SellOrderItemId.Trim(), out soItem);
                var extCust = ResolveItemExtendCustomer(e, so, extendCustomers, cust);
                User? extSales = null;
                if (!string.IsNullOrWhiteSpace(e.SalesId))
                    extendSalesUsers.TryGetValue(e.SalesId.Trim(), out extSales);

                return new PackingDetailItemExtendDto
                {
                    Id = e.Id,
                    PackingItemId = e.PackingItemId,
                    CustomerId = e.CustomerId ?? so?.CustomerId ?? pk.CustomerId,
                    CustomerName = FormatCustomerDisplayName(extCust),
                    SalesId = e.SalesId,
                    SalesUserName = extSales == null
                        ? null
                        : EntityLookupService.FormatUserLoginName(extSales) ?? extSales.RealName ?? extSales.UserName,
                    SellOrderId = e.SellOrderId,
                    SellOrderCode = so?.SellOrderCode,
                    SellOrderItemId = e.SellOrderItemId,
                    SellOrderItemCode = soItem?.SellOrderItemCode,
                    Price = e.Price,
                    PriceCurrency = e.PriceCurrency,
                    PriceConvertPrice = e.PriceConvertPrice,
                    CustomerSo = e.CustomerSo,
                    CustomerPn = e.CustomerPn,
                    CustomerBrand = e.CustomerBrand
                };
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<PackingDetailDto?> GetPackingByStockOutRequestIdAsync(
        string stockOutRequestId,
        CancellationToken cancellationToken = default)
    {
        var rid = stockOutRequestId?.Trim();
        if (string.IsNullOrEmpty(rid))
            return null;

        var packingIdSet = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => !pi.IsDeleted && pi.StockOutNotifyId == rid)
            .Select(pi => pi.PackingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (packingIdSet.Count == 0)
        {
            var sor = await _stockOutRequestRepository.GetByIdAsync(rid);
            if (sor == null || sor.IsDeleted)
                return null;

            var sellLineId = sor.SalesOrderItemId?.Trim() ?? "";
            if (sellLineId.Length == 0)
                return null;

            packingIdSet = await _db.PackingItems
                .AsNoTracking()
                .Where(pi =>
                    !pi.IsDeleted
                    && pi.SellOrderItemId == sellLineId
                    && (pi.StockOutNotifyId == null || pi.StockOutNotifyId == ""))
                .Select(pi => pi.PackingId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        if (packingIdSet.Count == 0)
            return null;
        if (packingIdSet.Count > 1)
            throw new InvalidOperationException("该出库通知关联多张装箱单，无法展示装箱明细");

        return await GetPackingByIdAsync(packingIdSet[0], cancellationToken);
    }

    public async Task<PackingCreateResultDto> CreateFromStockOutRequestsAsync(
        IReadOnlyList<string> stockOutRequestIds,
        PackingCreateExtras? extras,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadValidatedStockOutRequestsAsync(stockOutRequestIds, cancellationToken);
        var orderedRequests = bundle.Requests;
        var soItemMap = bundle.SoItemMap;
        var customerId = bundle.CustomerId;
        ValidatePackingCreateCompleteness(orderedRequests, soItemMap);

        // 批量时各通知 StockOutType 已在 LoadValidated 中校验一致；装箱单头表取入参顺序第一条通知的类型
        var packingStockOutType = StockOutTypeCode.NormalizeForNotify(orderedRequests[0].StockOutType);
        var firstSo = await _sellOrderRepository.GetByIdAsync(orderedRequests[0].SalesOrderId);
        var createBy = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;
        var packingCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Packing);
        var packingStorageId = await ResolvePackingStorageIdFromRequestsAsync(orderedRequests, cancellationToken);

        if (packingStockOutType == StockOutTypeCode.Customs)
        {
            var brokerId = extras?.CustomsBrokerId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(brokerId))
                throw new InvalidOperationException("报关装箱须选择报关公司。");
        }

        var packing = new Packing
        {
            Id = Guid.NewGuid().ToString(),
            Code = packingCode,
            Status = PackingStatusCode.New,
            StockOutType = packingStockOutType,
            MaterialType = PackingMaterialTypeCode.Normal,
            CustomerId = customerId,
            SalesId = firstSo?.SalesUserId,
            StorageId = packingStorageId,
            ItemRows = orderedRequests.Count,
            Comment = extras?.Comment?.Trim(),
            ScheduleShipDate = extras?.ScheduleShipDate,
            CustomsBrokerId = packingStockOutType == StockOutTypeCode.Customs
                ? extras!.CustomsBrokerId!.Trim()
                : null,
            CreateTime = now,
            CreateByUserId = createBy
        };

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _packingRepository.AddAsync(packing);

            if (extras?.Ship != null)
            {
                var s = extras.Ship;
                var resolvedShipment = !string.IsNullOrWhiteSpace(s.ShipmentMethod)
                    ? LogisticsShipmentMethodCode.Normalize(s.ShipmentMethod)
                    : LogisticsShipmentMethodCode.Normalize(orderedRequests[0].ShipmentMethod);
                LogisticsShipmentMethodCode.EnsureRequired(resolvedShipment);
                var resolvedExpress = !string.IsNullOrWhiteSpace(s.ExpressCompany)
                    ? LogisticsShipmentMethodCode.NormalizeExpressCompany(s.ExpressCompany)
                    : LogisticsShipmentMethodCode.NormalizeExpressCompany(orderedRequests[0].ExpressCompany);
                _db.PackingExtendShips.Add(new PackingExtendShip
                {
                    Id = Guid.NewGuid().ToString(),
                    PackingId = packing.Id,
                    ShipCompany = TrimOrNull(s.ShipCompany),
                    ShipAddress = TrimOrNull(s.ShipAddress),
                    ShipAttn = TrimOrNull(s.ShipAttn),
                    ShipTel = TrimOrNull(s.ShipTel),
                    BillCompany = TrimOrNull(s.BillCompany),
                    BillAddress = TrimOrNull(s.BillAddress),
                    BillAttn = TrimOrNull(s.BillAttn),
                    BillTel = TrimOrNull(s.BillTel),
                    DeliveryReq = TrimOrNull(s.DeliveryReq),
                    ShipmentMethod = resolvedShipment,
                    ExpressCompany = resolvedExpress
                });
            }

            if (extras?.Box != null)
            {
                var b = extras.Box;
                _db.PackingExtendBoxes.Add(new PackingExtendBox
                {
                    Id = Guid.NewGuid().ToString(),
                    PackingId = packing.Id,
                    Nw = b.Nw,
                    Gw = b.Gw,
                    Dim = TrimOrNull(b.Dim),
                    Ctns = b.Ctns
                });
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                               && pg.SqlState == PostgresErrorCodes.UndefinedColumn)
            {
                throw new InvalidOperationException(
                    "数据库缺少装箱单表结构（如 packing_extend、packing_extend_ship、packing_item.item_code 等），请在服务器执行最新 EF 迁移（CRM.DbMigrator）后重试。",
                    ex);
            }

            var firstSeq = 0;
            if (orderedRequests.Count > 0)
            {
                try
                {
                    firstSeq = await _packingItemLineSeq.ReserveNextSequenceBlockAsync(
                        packing.Id, orderedRequests.Count, cancellationToken);
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedColumn)
                {
                    throw new InvalidOperationException(
                        "数据库缺少装箱单字段（如 packing_extend.last_item_line_seq 或 packing_item.item_code），请在服务器执行最新 EF 迁移（CRM.DbMigrator）后重试。",
                        ex);
                }
            }

            var lineIndex = 0;
            foreach (var req in orderedRequests)
            {
                var soItem = soItemMap[req.SalesOrderItemId.Trim()];
                var seq = firstSeq + lineIndex++;
                var itemId = Guid.NewGuid().ToString();
                var packingItem = new PackingItem
                {
                    Id = itemId,
                    PackingId = packing.Id,
                    ItemCode = OrderLineItemCodes.PackingItem(packing.Code, seq),
                    SellOrderId = req.SalesOrderId,
                    SellOrderItemId = req.SalesOrderItemId,
                    StockOutNotifyId = req.Id,
                    CustomsPendlistId = packingStockOutType == StockOutTypeCode.Customs
                        ? (req.CustomsPendlistId?.Trim() ?? throw new InvalidOperationException(
                            $"报关出库通知 {req.RequestCode} 缺少待报关关联。"))
                        : null,
                    ProductId = soItem.ProductId,
                    Pn = string.IsNullOrWhiteSpace(req.MaterialCode) ? soItem.PN : req.MaterialCode.Trim(),
                    Brand = string.IsNullOrWhiteSpace(req.MaterialName) ? soItem.Brand : req.MaterialName.Trim(),
                    Qty = req.Quantity,
                    Comment = req.Remark,
                    CreateTime = now,
                    CreateByUserId = createBy
                };
                await _packingItemRepository.AddAsync(packingItem);

                _db.PackingItemExtends.Add(new PackingItemExtend
                {
                    Id = Guid.NewGuid().ToString(),
                    PackingItemId = itemId,
                    CustomerId = !string.IsNullOrWhiteSpace(req.CustomerId) ? req.CustomerId.Trim() : customerId,
                    SalesId = firstSo?.SalesUserId,
                    SellOrderId = req.SalesOrderId,
                    SellOrderItemId = req.SalesOrderItemId,
                    Price = soItem.Price,
                    PriceCurrency = soItem.Currency,
                    PriceConvertPrice = soItem.ConvertPrice,
                    CustomerSo = soItem.CustomerSo,
                    CustomerPn = soItem.CustomerPn,
                    CustomerBrand = soItem.CustomerBrand
                });
            }

            await _unitOfWork.SaveChangesAsync();

            var savedItemCount = await _db.PackingItems.CountAsync(
                pi => pi.PackingId == packing.Id && !pi.IsDeleted,
                cancellationToken);
            if (savedItemCount != orderedRequests.Count)
            {
                throw new InvalidOperationException(
                    $"装箱明细保存失败：期望 {orderedRequests.Count} 行，实际写入 {savedItemCount} 行，请重试或联系管理员。");
            }

            var notifyIdsForSync = orderedRequests
                .Select(r => r.Id.Trim())
                .ToList();
            await SyncStockOutNotifyStatusForPackingAsync(
                packing.Id,
                StockOutRequestStatusCode.Packed,
                createBy,
                notifyIdsForSync,
                cancellationToken);

            await tx.CommitAsync(cancellationToken);

            if (packingStockOutType == StockOutTypeCode.Customs)
                await _customsV2FlowService.OnCustomsPackingCreatedAsync(packing.Id, createBy, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return new PackingCreateResultDto
        {
            PackingId = packing.Id,
            PackingCode = packing.Code,
            ItemCount = orderedRequests.Count
        };
    }

    private sealed class StockOutRequestsForPackingBundle
    {
        public List<StockOutRequest> Requests { get; init; } = new();
        public Dictionary<string, SellOrderItem> SoItemMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public string CustomerId { get; init; } = string.Empty;
        public CustomerInfo? Customer { get; init; }
        public SellOrder? FirstSellOrder { get; init; }
        public User? SalesUser { get; init; }
    }

    /// <summary>生成装箱单前校验：每条出库通知须能落到完整明细行，否则抛错终止。</summary>
    private static void ValidatePackingCreateCompleteness(
        IReadOnlyList<StockOutRequest> requests,
        IReadOnlyDictionary<string, SellOrderItem> soItemMap)
    {
        if (requests.Count == 0)
            throw new InvalidOperationException("请至少选择一条出库通知");

        var lineErrors = new List<string>();
        foreach (var req in requests)
        {
            var code = string.IsNullOrWhiteSpace(req.RequestCode) ? req.Id : req.RequestCode.Trim();
            var lineId = req.SalesOrderItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(lineId))
            {
                lineErrors.Add($"{code}：缺少销售订单明细");
                continue;
            }

            if (!soItemMap.TryGetValue(lineId, out var soItem))
            {
                lineErrors.Add($"{code}：销售明细不存在或已删除");
                continue;
            }

            if (req.Quantity <= 0)
                lineErrors.Add($"{code}：出库数量须大于 0");

            if (string.IsNullOrWhiteSpace(req.SalesOrderId))
                lineErrors.Add($"{code}：缺少销售订单");

            var pn = string.IsNullOrWhiteSpace(req.MaterialCode) ? soItem.PN : req.MaterialCode.Trim();
            if (string.IsNullOrWhiteSpace(pn))
                lineErrors.Add($"{code}：物料型号不能为空");
        }

        if (lineErrors.Count > 0)
        {
            throw new InvalidOperationException(
                "装箱数据不完整，无法生成装箱单：" + string.Join("；", lineErrors));
        }
    }

    private static string? TrimOrNull(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            var trimmed = TrimOrNull(value);
            if (trimmed != null)
                return trimmed;
        }
        return null;
    }

    private static string? FormatCustomerDisplayName(CustomerInfo? customer) =>
        customer == null ? null : customer.OfficialName ?? customer.NickName;

    /// <summary>
    /// 装箱单头客户名：优先关联销售订单快照（与出库通知一致），否则回退客户主数据。
    /// </summary>
    private static string? ResolvePackingHeaderCustomerName(CustomerInfo? customer, SellOrder? sellOrder)
    {
        var fromSo = sellOrder?.CustomerName?.Trim();
        if (!string.IsNullOrEmpty(fromSo))
            return fromSo;
        return FormatCustomerDisplayName(customer);
    }

    /// <summary>装箱单列表：按关联出库通知汇总计划出货日期与出货方式（首条通知，按 RequestCode 排序）。</summary>
    private async Task<Dictionary<string, (DateTime? RequestDate, string? ShipmentMethod, string? ExpressCompany)>> LoadNotifySummaryByPackingIdsAsync(
        IReadOnlyCollection<string> packingIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, (DateTime? RequestDate, string? ShipmentMethod, string? ExpressCompany)>(StringComparer.OrdinalIgnoreCase);
        if (packingIds.Count == 0)
            return result;

        var idSet = packingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packingItems = await _db.PackingItems.AsNoTracking()
            .Where(pi => idSet.Contains(pi.PackingId) && !pi.IsDeleted)
            .Select(pi => new { pi.PackingId, pi.StockOutNotifyId })
            .ToListAsync(cancellationToken);
        if (packingItems.Count == 0)
            return result;

        var notifyIds = packingItems
            .Select(x => x.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (notifyIds.Count == 0)
            return result;

        var notifies = await _db.StockOutRequests.AsNoTracking()
            .Where(r => !r.IsDeleted && notifyIds.Contains(r.Id))
            .OrderBy(r => r.RequestCode)
            .ToListAsync(cancellationToken);
        var notifyById = notifies.ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

        foreach (var group in packingItems.GroupBy(x => x.PackingId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            StockOutRequest? first = null;
            foreach (var pi in group)
            {
                var nid = pi.StockOutNotifyId?.Trim();
                if (string.IsNullOrEmpty(nid) || !notifyById.TryGetValue(nid, out var notify))
                    continue;
                if (first == null || string.Compare(notify.RequestCode, first.RequestCode, StringComparison.OrdinalIgnoreCase) < 0)
                    first = notify;
            }

            if (first == null)
                continue;

            result[group.Key] = (
                first.RequestDate == default ? null : first.RequestDate,
                string.IsNullOrWhiteSpace(first.ShipmentMethod) ? null : first.ShipmentMethod.Trim(),
                string.IsNullOrWhiteSpace(first.ExpressCompany) ? null : first.ExpressCompany.Trim());
        }

        return result;
    }

    private async Task<List<PackingStockOutNotifyRowDto>> LoadStockOutNotifiesForPackingAsync(
        IReadOnlyList<PackingItem> packingItems,
        CancellationToken cancellationToken)
    {
        if (packingItems.Count == 0)
            return new List<PackingStockOutNotifyRowDto>();

        var notifyIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pi in packingItems)
        {
            var nid = pi.StockOutNotifyId?.Trim();
            if (!string.IsNullOrEmpty(nid))
                notifyIdSet.Add(nid);
        }

        foreach (var pi in packingItems.Where(x => string.IsNullOrWhiteSpace(x.StockOutNotifyId)))
        {
            var lineId = pi.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId))
                continue;
            var fallbackId = await TryResolveFallbackStockOutNotifyIdForSellLineAsync(lineId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackId))
                notifyIdSet.Add(fallbackId.Trim());
        }

        if (notifyIdSet.Count == 0)
            return new List<PackingStockOutNotifyRowDto>();

        var notifies = await _db.StockOutRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && notifyIdSet.Contains(r.Id))
            .OrderBy(r => r.RequestCode)
            .ToListAsync(cancellationToken);
        if (notifies.Count == 0)
            return new List<PackingStockOutNotifyRowDto>();

        var soIds = notifies.Select(r => r.SalesOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var sellOrders = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var customerIds = notifies.Select(r => r.CustomerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var customers = customerIds.Count == 0
            ? new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _customerRepository.FindAsync(c => customerIds.Contains(c.Id)))
                .ToDictionary(c => c.Id.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var userIds = notifies.Select(r => r.RequestUserId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepository.FindAsync(u => userIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var rows = new List<PackingStockOutNotifyRowDto>();
        foreach (var r in notifies)
        {
            sellOrders.TryGetValue(r.SalesOrderId.Trim(), out var so);
            customers.TryGetValue(r.CustomerId.Trim(), out var cust);
            users.TryGetValue(r.RequestUserId.Trim(), out var reqUser);
            User? salesUser = null;
            if (so != null && !string.IsNullOrWhiteSpace(so.SalesUserId))
                users.TryGetValue(so.SalesUserId.Trim(), out salesUser);

            rows.Add(new PackingStockOutNotifyRowDto
            {
                Id = r.Id,
                RequestCode = r.RequestCode,
                Status = r.Status,
                SalesOrderId = r.SalesOrderId,
                SalesOrderCode = so?.SellOrderCode,
                SalesOrderItemId = r.SalesOrderItemId,
                MaterialModel = r.MaterialCode,
                Brand = r.MaterialName,
                OutQuantity = r.Quantity,
                RegionType = r.RegionType,
                CustomerName = FormatCustomerDisplayName(cust),
                SalesUserName = salesUser == null
                    ? null
                    : EntityLookupService.FormatUserLoginName(salesUser) ?? salesUser.RealName ?? salesUser.UserName,
                RequestDate = r.RequestDate,
                CreateTime = r.CreateTime,
                Remark = r.Remark,
                StockOutType = r.StockOutType
            });
        }

        var customsRows = rows
            .Where(x => StockOutTypeCode.NormalizeForNotify(x.StockOutType) == StockOutTypeCode.Customs)
            .ToList();
        if (customsRows.Count > 0)
        {
            var traceMap = await _customsTraceQuery.GetByStockOutNotifyIdsAsync(
                customsRows.Select(x => x.Id),
                cancellationToken);
            foreach (var row in customsRows)
            {
                if (!traceMap.TryGetValue(row.Id.Trim(), out var trace))
                    continue;
                row.CustomsDeclarationId = trace.CustomsDeclarationId;
                row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
            }
        }

        return rows;
    }

    /// <summary>扩展行客户：明细 customer_id → 销售订单客户 → 装箱单主表客户。</summary>
    private static CustomerInfo? ResolveItemExtendCustomer(
        PackingItemExtend extend,
        SellOrder? sellOrder,
        IReadOnlyDictionary<string, CustomerInfo> customersById,
        CustomerInfo? packingHeaderCustomer)
    {
        if (!string.IsNullOrWhiteSpace(extend.CustomerId)
            && customersById.TryGetValue(extend.CustomerId.Trim(), out var fromExtend))
            return fromExtend;

        if (sellOrder != null
            && !string.IsNullOrWhiteSpace(sellOrder.CustomerId)
            && customersById.TryGetValue(sellOrder.CustomerId.Trim(), out var fromOrder))
            return fromOrder;

        return packingHeaderCustomer;
    }

    private async Task<StockOutRequestsForPackingBundle> LoadValidatedStockOutRequestsAsync(
        IReadOnlyList<string> stockOutRequestIds,
        CancellationToken cancellationToken)
    {
        if (stockOutRequestIds == null || stockOutRequestIds.Count == 0)
            throw new ArgumentException("请至少选择一条出库通知", nameof(stockOutRequestIds));

        var ids = stockOutRequestIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            throw new ArgumentException("请至少选择一条出库通知", nameof(stockOutRequestIds));

        var requests = (await _stockOutRequestRepository.FindAsync(r => ids.Contains(r.Id) && !r.IsDeleted)).ToList();
        if (requests.Count != ids.Count)
            throw new InvalidOperationException("部分出库通知不存在或已删除，请刷新列表后重试");

        if (requests.Any(r => r.Status == StockOutRequestStatusCode.Cancelled))
            throw new InvalidOperationException("已取消的出库通知不能生成装箱单");
        if (requests.Any(r => r.Status == StockOutRequestStatusCode.StockedOut))
            throw new InvalidOperationException("已出库的出库通知不能生成装箱单");
        if (requests.Any(r => r.Status == StockOutRequestStatusCode.Packed))
            throw new InvalidOperationException("已装箱的出库通知不能重复生成装箱单");
        if (requests.Any(r => r.Status == StockOutRequestStatusCode.PendingCustoms))
            throw new InvalidOperationException("待报关的出库通知不能生成销售装箱单，请先完成报关流程");
        if (requests.Any(r => r.Status != StockOutRequestStatusCode.PendingPacking))
            throw new InvalidOperationException("仅「待装箱」状态的出库通知可生成装箱单");

        var normalizedStockOutTypes = requests
            .Select(r => StockOutTypeCode.NormalizeForNotify(r.StockOutType))
            .Distinct()
            .ToList();
        if (normalizedStockOutTypes.Count != 1)
            throw new InvalidOperationException("所选出库通知的出库类型必须一致");

        var packingStockOutType = normalizedStockOutTypes[0];
        if (packingStockOutType == StockOutTypeCode.Customs)
            await ValidateCustomsStockOutRequestsForPackingAsync(requests, cancellationToken);

        var existingPackingItems = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => !pi.IsDeleted && pi.StockOutNotifyId != null && ids.Contains(pi.StockOutNotifyId))
            .Select(pi => new { pi.StockOutNotifyId, pi.PackingId })
            .ToListAsync(cancellationToken);
        if (existingPackingItems.Count > 0)
        {
            var dupNotifyIdSet = existingPackingItems
                .Select(x => x.StockOutNotifyId!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var notifyCodes = requests
                .Where(r => dupNotifyIdSet.Contains(r.Id))
                .Select(r => r.RequestCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var packingIdSet = existingPackingItems
                .Select(x => x.PackingId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var packingCodes = packingIdSet.Count == 0
                ? new List<string>()
                : await _db.Packings
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && packingIdSet.Contains(p.Id))
                    .Select(p => p.Code)
                    .ToListAsync(cancellationToken);
            var notifyPart = notifyCodes.Count > 0 ? string.Join("、", notifyCodes) : string.Join("、", dupNotifyIdSet);
            var packingPart = packingCodes.Count > 0 ? string.Join("、", packingCodes) : "—";
            throw new InvalidOperationException(
                $"出库通知 {notifyPart} 已存在装箱明细（装箱单：{packingPart}），不能重复生成装箱单");
        }

        var soIdsForCustomer = requests
            .Select(r => r.SalesOrderId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var sellOrdersForCustomer = soIdsForCustomer.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIdsForCustomer.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var customers = (await _customerRepository.FindAsync(c => !c.IsDeleted)).ToList();
        var customerIdByDisplayName = CustomerIdResolveHelper.BuildDisplayNameIndex(customers);

        var resolvedCustomerIds = new List<string>();
        foreach (var r in requests)
        {
            sellOrdersForCustomer.TryGetValue(r.SalesOrderId?.Trim() ?? string.Empty, out var soForCust);
            var cid = CustomerIdResolveHelper.ResolveForStockOutNotify(
                r.CustomerId,
                soForCust,
                customerIdByDisplayName);

            if (string.IsNullOrEmpty(cid))
                throw new InvalidOperationException("出库通知缺少客户信息，无法生成装箱单");

            resolvedCustomerIds.Add(cid);
        }

        var customerIds = resolvedCustomerIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customerIds.Count != 1)
            throw new InvalidOperationException("所选出库通知须属于同一客户");

        if (requests.Any(r => string.IsNullOrWhiteSpace(r.SalesOrderItemId)))
            throw new InvalidOperationException("出库通知缺少销售订单明细，无法生成装箱单");

        var lineIds = requests
            .Select(r => r.SalesOrderItemId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var soItems = (await _sellOrderItemRepository.FindAsync(si => lineIds.Contains(si.Id) && !si.IsDeleted)).ToList();
        if (soItems.Count != lineIds.Count)
            throw new InvalidOperationException("部分销售订单明细不存在，无法生成装箱单");

        var currencies = soItems.Select(si => si.Currency).Distinct().ToList();
        if (currencies.Count != 1)
            throw new InvalidOperationException("所选出库通知对应销售明细的币别必须一致");

        var regionTypes = requests
            .Select(r => RegionTypeCode.Normalize(r.RegionType))
            .Distinct()
            .ToList();
        if (regionTypes.Count != 1)
            throw new InvalidOperationException("所选出库通知的送达地域必须一致");

        LogisticsShipmentMethodCode.EnsureStockOutRequestsConsistentForPacking(requests);

        var soItemMap = soItems.ToDictionary(si => si.Id.Trim(), si => si, StringComparer.OrdinalIgnoreCase);
        var idOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < ids.Count; i++)
            idOrder[ids[i]] = i;
        var orderedRequests = requests
            .OrderBy(r => idOrder.TryGetValue(r.Id, out var i) ? i : int.MaxValue)
            .ThenBy(r => r.RequestCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var firstReq = orderedRequests[0];
        var firstSo = await _sellOrderRepository.GetByIdAsync(firstReq.SalesOrderId);
        CustomerInfo? cust = await _customerRepository.GetByIdAsync(customerIds[0]);

        User? salesUser = null;
        if (!string.IsNullOrWhiteSpace(firstSo?.SalesUserId))
            salesUser = await _userRepository.GetByIdAsync(firstSo.SalesUserId.Trim());

        return new StockOutRequestsForPackingBundle
        {
            Requests = orderedRequests,
            SoItemMap = soItemMap,
            CustomerId = customerIds[0],
            Customer = cust,
            FirstSellOrder = firstSo,
            SalesUser = salesUser
        };
    }

    /// <inheritdoc />
    public async Task ConfirmPackingAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(id);
        if (packing == null || packing.IsDeleted)
            throw new InvalidOperationException("装箱单不存在或已删除");

        if (packing.Status != PackingStatusCode.New)
            throw new InvalidOperationException("仅「新建」状态的装箱单可以确认");

        packing.Status = PackingStatusCode.Confirmed;
        await _packingRepository.UpdateAsync(packing);
        await _unitOfWork.SaveChangesAsync();

        if (StockOutTypeCode.NormalizeForNotify(packing.StockOutType) == StockOutTypeCode.Customs)
            await _customsV2FlowService.GenerateDeclarationOnPackingConfirmAsync(id, actingUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RegenerateCustomsDeclarationAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        await _customsV2FlowService.EnsureCustomsDeclarationForPackingAsync(id, actingUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkPackingReadyAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(id);
        if (packing == null || packing.IsDeleted)
            throw new InvalidOperationException("装箱单不存在或已删除");

        if (packing.Status != PackingStatusCode.Picked)
            throw new InvalidOperationException("仅「已拣货」状态的装箱单可以备货");

        await CompletePickingTasksForPackingOnReadyAsync(id, cancellationToken);

        packing.Status = PackingStatusCode.Ready;
        packing.ModifyTime = DateTime.UtcNow;
        packing.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _packingRepository.UpdateAsync(packing);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>备货时同步将关联拣货任务置为已完成（100），与批量出库前置条件一致。</summary>
    private async Task CompletePickingTasksForPackingOnReadyAsync(
        string packingId,
        CancellationToken cancellationToken)
    {
        var pid = packingId.Trim();
        var now = DateTime.UtcNow;
        var tasks = await _db.PickingTasks
            .Where(t => !t.IsDeleted && t.PackingId == pid && t.Status != -1 && t.Status != 100)
            .ToListAsync(cancellationToken);
        if (tasks.Count == 0)
            return;

        var taskIds = tasks.Select(t => t.Id).ToList();
        var items = await _db.PickingTaskItems
            .Where(i => !i.IsDeleted && taskIds.Contains(i.PickingTaskId))
            .ToListAsync(cancellationToken);
        var itemsByTask = items.GroupBy(i => i.PickingTaskId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var task in tasks)
        {
            task.Status = 100;
            task.ModifyTime = now;
            if (itemsByTask.TryGetValue(task.Id, out var taskItems))
            {
                foreach (var item in taskItems)
                {
                    item.PickedQty = item.PlanQty;
                    item.ModifyTime = now;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task DeletePackingAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        await DeletePackingCoreAsync(packingId, actingUserId, requireNewStatus: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ForceDeletePackingAsync(
        string packingId,
        string confirmBillCode,
        string actingUserId,
        string? actingUserName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmBillCode))
            throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
        if (string.IsNullOrWhiteSpace(actingUserId))
            throw new ArgumentException("操作人不能为空", nameof(actingUserId));

        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(id);
        if (packing == null || packing.IsDeleted)
            throw new InvalidOperationException("装箱单不存在或已删除");

        if (!string.Equals(confirmBillCode.Trim(), packing.Code?.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("确认单号不匹配，已拒绝删除");

        var guard = await _forceDeleteGuard.CanForceDeletePackingAsync(packing.Id);
        if (!guard.CanDelete)
            throw new ArgumentException(guard.Message);

        await _inventoryCenterService.ReleasePickingTasksByPackingIdAsync(packing.Id);

        var deletedStatus = packing.Status;
        var recordCode = string.IsNullOrWhiteSpace(packing.Code) ? null : packing.Code.Trim();
        await DeletePackingCoreAsync(id, actingUserId, requireNewStatus: false, cancellationToken);

        await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
        {
            BizType = BusinessLogTypes.Packing,
            RecordId = packing.Id,
            RecordCode = recordCode,
            EntityDisplayName = DeleteLogEntityNames.Packing,
            IsForceDelete = true,
            ForceConfirmBillCode = confirmBillCode.Trim(),
            OperatorUserId = actingUserId.Trim(),
            OperatorUserName = actingUserName?.Trim(),
            OperationDescOverride =
                $"强制删除装箱单 PackingId={packing.Id}，确认单号={recordCode}，删除时状态={deletedStatus}"
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PackingStatusReconcileResult> RefreshStatusAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(id);
        if (packing == null || packing.IsDeleted)
            throw new InvalidOperationException("装箱单不存在或已删除");

        return await _packingStatusReconcile.ReconcileAsync(
            id,
            actingUserId,
            excludingStockOutId: null,
            saveChanges: true,
            cancellationToken);
    }

    private async Task DeletePackingCoreAsync(
        string packingId,
        string? actingUserId,
        bool requireNewStatus,
        CancellationToken cancellationToken)
    {
        var id = packingId?.Trim();
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("装箱单 ID 无效", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(id);
        if (packing == null || packing.IsDeleted)
            throw new InvalidOperationException("装箱单不存在或已删除");

        if (requireNewStatus && packing.Status != PackingStatusCode.New)
            throw new InvalidOperationException("仅「新建」状态的装箱单可以删除");

        var items = (await _packingItemRepository.FindAsync(i => i.PackingId == id && !i.IsDeleted)).ToList();
        var itemIds = items.Select(i => i.Id).ToList();
        var notifyIds = items
            .Select(i => i.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (itemIds.Count > 0)
        {
            var extends = await _db.PackingItemExtends
                .IgnoreQueryFilters()
                .Where(e => itemIds.Contains(e.PackingItemId) && !e.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var ext in extends)
                ext.IsDeleted = true;
        }

        var boxes = await _db.PackingExtendBoxes
            .IgnoreQueryFilters()
            .Where(b => b.PackingId == id && !b.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var box in boxes)
            box.IsDeleted = true;

        var ships = await _db.PackingExtendShips
            .IgnoreQueryFilters()
            .Where(s => s.PackingId == id && !s.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var ship in ships)
            ship.IsDeleted = true;

        var safePackingId = id.Replace("'", "''", StringComparison.Ordinal);
        await _unitOfWork.ExecuteAsync(
            $@"UPDATE packing_extend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""PackingId"" = '{safePackingId}' AND is_deleted = false");

        foreach (var item in items)
            await _packingItemRepository.DeleteAsync(item.Id);

        var modifyBy = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;

        if (notifyIds.Count > 0)
        {
            var notifies = (await _stockOutRequestRepository.FindAsync(r => notifyIds.Contains(r.Id))).ToList();
            foreach (var req in notifies)
            {
                if (req.Status != StockOutRequestStatusCode.Packed)
                    continue;

                req.Status = StockOutRequestStatusCode.PendingPacking;
                req.ModifyTime = now;
                req.ModifyByUserId = modifyBy;
                await _stockOutRequestRepository.UpdateAsync(req);
            }
        }

        var pendlistIds = items
            .Select(i => i.CustomsPendlistId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pendlistIds.Count > 0)
            await _customsV2FlowService.RevertPendlistOnPackingDeleteAsync(pendlistIds, modifyBy, cancellationToken);

        await _packingRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PackingStockOutRequestsResolveDto> ResolveStockOutRequestIdsFromPackingsAsync(
        IReadOnlyList<string> packingIds,
        bool forPicking = false,
        CancellationToken cancellationToken = default)
    {
        var ids = (packingIds ?? Array.Empty<string>())
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            throw new ArgumentException("请至少选择一张装箱单", nameof(packingIds));

        var packings = await _db.Packings
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (packings.Count != ids.Count)
            throw new InvalidOperationException("部分装箱单不存在或已删除，请刷新列表后重试");

        if (forPicking)
        {
            if (packings.Any(p => p.Status != PackingStatusCode.Confirmed))
                throw new InvalidOperationException("仅「已确认」状态的装箱单可以拣货");
        }
        else if (packings.Any(p => p.Status != PackingStatusCode.Ready))
        {
            throw new InvalidOperationException("仅「已备货」状态的装箱单可以出库");
        }

        var customerIds = packings
            .Select(p => (p.CustomerId ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customerIds.Count != 1)
            throw new InvalidOperationException("所选装箱单须属于同一客户");

        var ships = await _db.PackingExtendShips
            .AsNoTracking()
            .Where(s => ids.Contains(s.PackingId))
            .ToListAsync(cancellationToken);
        EnsureBatchStockOutShipFieldsMatch(ids, ships);

        var packingItems = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => ids.Contains(pi.PackingId) && !pi.IsDeleted)
            .ToListAsync(cancellationToken);
        if (packingItems.Count == 0)
            throw new InvalidOperationException(forPicking
                ? "所选装箱单无有效明细行，无法拣货"
                : "所选装箱单无有效明细行，无法出库");

        var notifyIdsFromItems = packingItems
            .Select(pi => pi.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var sellLineIds = packingItems
            .Where(pi => string.IsNullOrWhiteSpace(pi.StockOutNotifyId))
            .Select(pi => pi.SellOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (notifyIdsFromItems.Count == 0 && sellLineIds.Count == 0)
            throw new InvalidOperationException(forPicking
                ? "所选装箱单无有效明细行，无法拣货"
                : "所选装箱单无有效明细行，无法出库");

        var resolvedNotifyIds = new List<string>(notifyIdsFromItems);
        foreach (var lineId in sellLineIds)
        {
            var fallbackId = await TryResolveFallbackStockOutNotifyIdForSellLineAsync(lineId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackId))
                resolvedNotifyIds.Add(fallbackId.Trim());
        }
        resolvedNotifyIds = resolvedNotifyIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IQueryable<StockOutRequest> requestQuery = _db.StockOutRequests
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted
                && resolvedNotifyIds.Contains(r.Id));
        if (forPicking)
        {
            const short cancelledStatus = StockOutRequestStatusCode.Cancelled;
            requestQuery = requestQuery.Where(r => r.Status != cancelledStatus);
        }

        var requests = await requestQuery
            .OrderBy(r => r.RequestCode)
            .ToListAsync(cancellationToken);

        if (forPicking && requests.Count == 0)
            throw new InvalidOperationException("未找到该装箱单关联的出库通知，无法拣货");

        var links = forPicking
            ? BuildStockOutRequestPackingLinks(ids, packingItems, requests)
            : BuildStockOutLinksPerPacking(ids, packingItems, requests);

        var requestIds = links
            .Select(l => l.StockOutRequestId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        return new PackingStockOutRequestsResolveDto
        {
            StockOutRequestIds = requestIds,
            Links = links,
            CustomerId = customerIds[0],
            PackingCount = packings.Count
        };
    }

    /// <inheritdoc />
    public async Task<PackingBatchStockOutResultDto> BatchExecuteStockOutFromPackingsAsync(
        IReadOnlyList<string> packingIds,
        DateTime expectedStockOutDate,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (expectedStockOutDate == default)
            throw new ArgumentException("请填写预计出库日期", nameof(expectedStockOutDate));

        var expectedUtc = PostgreSqlDateTime.ToUtc(expectedStockOutDate);
        var ids = (packingIds ?? Array.Empty<string>())
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            throw new ArgumentException("请至少选择一张装箱单", nameof(packingIds));

        var packings = await _db.Packings
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (packings.Count != ids.Count)
            throw new InvalidOperationException("部分装箱单不存在或已删除，请刷新列表后重试");

        if (packings.Any(p => p.Status != PackingStatusCode.Ready))
            throw new InvalidOperationException("仅「已备货」状态的装箱单可以出库");

        var customerIds = packings
            .Select(p => (p.CustomerId ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customerIds.Count != 1 || packings.Any(p => string.IsNullOrWhiteSpace(p.CustomerId)))
            throw new InvalidOperationException("所选装箱单须属于同一客户");

        var stockOutTypes = packings
            .Select(p => p.StockOutType)
            .Distinct()
            .ToList();
        if (stockOutTypes.Count != 1)
            throw new InvalidOperationException("所选装箱单出库类型须相同");

        var packingByIdForWh = packings.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var batchWarehouseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pid in ids)
        {
            if (!packingByIdForWh.TryGetValue(pid, out var pkForWh))
                continue;
            var completedTask = await GetCompletedPickingTaskForPackingAsync(pid, cancellationToken);
            var wh = await ResolveWarehouseIdForBatchStockOutAsync(pkForWh, completedTask, cancellationToken);
            batchWarehouseIds.Add(wh);
        }

        if (batchWarehouseIds.Count != 1)
            throw new InvalidOperationException("所选装箱单出库仓库须一致");

        var batchWarehouseId = batchWarehouseIds.First();

        var packingItems = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => ids.Contains(pi.PackingId) && !pi.IsDeleted)
            .ToListAsync(cancellationToken);

        var notifyIdsFromItems = packingItems
            .Select(pi => pi.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var sellLineIds = packingItems
            .Where(pi => string.IsNullOrWhiteSpace(pi.StockOutNotifyId))
            .Select(pi => pi.SellOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var resolvedNotifyIds = new List<string>(notifyIdsFromItems);
        foreach (var lineId in sellLineIds)
        {
            var fallbackId = await TryResolveFallbackStockOutNotifyIdForSellLineAsync(lineId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackId))
                resolvedNotifyIds.Add(fallbackId.Trim());
        }
        resolvedNotifyIds = resolvedNotifyIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var requests = await _db.StockOutRequests
            .AsNoTracking()
            .Where(r =>
                !r.IsDeleted
                && resolvedNotifyIds.Contains(r.Id))
            .OrderBy(r => r.RequestCode)
            .ToListAsync(cancellationToken);

        var packingById = packings.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        // 锚定通知：ExecuteStockOut 仍需一条 StockOutRequestId；一箱一单时销售行以装箱明细为准，不按通知拆单。
        var linkByPackingId = BuildStockOutLinksPerPacking(ids, packingItems, requests)
            .GroupBy(l => l.PackingId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;
        var lines = new List<PackingBatchStockOutLineDto>();

        foreach (var pid in ids)
        {
            if (!packingById.TryGetValue(pid, out var packing))
                throw new InvalidOperationException("部分装箱单不存在或已删除，请刷新列表后重试");

            if (!linkByPackingId.TryGetValue(pid, out var link)
                || string.IsNullOrWhiteSpace(link.StockOutRequestId))
            {
                throw new InvalidOperationException(
                    $"装箱单 {packing.Code} 未关联出库通知，无法出库");
            }

            var requestId = link.StockOutRequestId.Trim();
            var sor = await _stockOutRequestRepository.GetByIdAsync(requestId)
                ?? throw new InvalidOperationException($"出库通知不存在：{requestId}");

            var stockOutCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOut);
            var stockOut = await _stockOutService.ExecuteStockOutAsync(
                new ExecuteStockOutRequest
                {
                    StockOutRequestId = requestId,
                    PackingId = pid,
                    StockOutCode = stockOutCode,
                    WarehouseId = batchWarehouseId,
                    OperatorId = actor ?? string.Empty,
                    ExpectedStockOutDate = expectedUtc,
                    SkipStockOutNotifyStatusChecks = true,
                    PackingListBatchStockOut = true,
                    Items =
                    [
                        new ExecuteStockOutItemRequest
                        {
                            LineNo = 1,
                            MaterialCode = sor.MaterialCode?.Trim() ?? string.Empty,
                            MaterialName = sor.MaterialName?.Trim() ?? string.Empty,
                            Quantity = sor.Quantity
                        }
                    ]
                },
                actingUserId);

            packing.Status = PackingStatusCode.StockOutFinished;
            packing.ModifyTime = now;
            packing.ModifyByUserId = actor;
            await _packingRepository.UpdateAsync(packing);

            // 箱下全部出库通知标已出库（与 ExecuteStockOut 按箱回写一致；勿只传锚定通知）。
            await SyncStockOutNotifyStatusForPackingAsync(
                pid,
                StockOutRequestStatusCode.StockedOut,
                actor,
                stockOutNotifyIds: null,
                cancellationToken);

            lines.Add(new PackingBatchStockOutLineDto
            {
                PackingId = pid,
                PackingCode = packing.Code,
                StockOutId = stockOut.Id,
                StockOutCode = stockOut.StockOutCode
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return new PackingBatchStockOutResultDto { Lines = lines };
    }

    /// <summary>装箱单关联的出库通知 ID（明细 stock_out_notify_id + 无绑定时的销售行回退）。</summary>
    private async Task<List<string>> GetStockOutNotifyIdsForPackingAsync(
        string packingId,
        CancellationToken cancellationToken)
    {
        var pid = packingId.Trim();
        var items = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => pi.PackingId == pid && !pi.IsDeleted)
            .ToListAsync(cancellationToken);

        var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pi in items)
        {
            var nid = pi.StockOutNotifyId?.Trim();
            if (!string.IsNullOrEmpty(nid))
                idSet.Add(nid);
        }

        var sellLineIds = items
            .Where(pi => string.IsNullOrWhiteSpace(pi.StockOutNotifyId))
            .Select(pi => pi.SellOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (sellLineIds.Count > 0)
        {
            foreach (var lineId in sellLineIds)
            {
                var fallbackId = await TryResolveFallbackStockOutNotifyIdForSellLineAsync(lineId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(fallbackId))
                    idSet.Add(fallbackId.Trim());
            }
        }

        return idSet.ToList();
    }

    /// <summary>按装箱单同步关联出库通知状态：生成装箱 → 20 已装箱；执行出库 → 100 已出库。</summary>
    private async Task SyncStockOutNotifyStatusForPackingAsync(
        string packingId,
        short targetStatus,
        string? actingUserId,
        IReadOnlyList<string>? stockOutNotifyIds = null,
        CancellationToken cancellationToken = default)
    {
        var notifyIds = stockOutNotifyIds?
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList()
            ?? await GetStockOutNotifyIdsForPackingAsync(packingId, cancellationToken);
        if (notifyIds.Count == 0)
            return;

        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;
        var notifies = (await _stockOutRequestRepository.FindAsync(r => notifyIds.Contains(r.Id) && !r.IsDeleted))
            .ToList();

        foreach (var notify in notifies)
        {
            if (targetStatus == StockOutRequestStatusCode.Packed)
            {
                if (notify.Status is StockOutRequestStatusCode.Cancelled or StockOutRequestStatusCode.StockedOut)
                    continue;
                notify.Status = StockOutRequestStatusCode.Packed;
            }
            else if (targetStatus == StockOutRequestStatusCode.StockedOut)
            {
                if (notify.Status == StockOutRequestStatusCode.Cancelled)
                    continue;
                notify.Status = StockOutRequestStatusCode.StockedOut;
            }
            else
            {
                notify.Status = targetStatus;
            }

            if (targetStatus == StockOutRequestStatusCode.Packed
                && StockOutTypeCode.NormalizeForNotify(notify.StockOutType) == StockOutTypeCode.Sales
                && notify.CustomsStatus == StockOutNotifyCustomsStatusCode.Unknown)
            {
                notify.CustomsStatus = StockOutNotifyCustomsStatusCode.NotRequired;
            }

            notify.ModifyTime = now;
            notify.ModifyByUserId = actor;
            await _stockOutRequestRepository.UpdateAsync(notify);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<PickingTask?> GetCompletedPickingTaskForPackingAsync(
        string packingId,
        CancellationToken cancellationToken)
    {
        var pid = packingId.Trim();
        if (string.IsNullOrEmpty(pid))
            return null;

        return await _db.PickingTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.PackingId == pid && t.Status == 100)
            .OrderByDescending(t => t.ModifyTime ?? DateTime.MinValue)
            .ThenByDescending(t => t.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>批量出库仓库：优先拣货任务 <see cref="PickingTask.WarehouseId"/>，其次 <see cref="Packing.StorageId"/>（<c>warehouseinfo.Id</c>）。</summary>
    private async Task<string> ResolveWarehouseIdForBatchStockOutAsync(
        Packing packing,
        PickingTask? completedTask,
        CancellationToken cancellationToken)
    {
        var fromTask = completedTask?.WarehouseId?.Trim();
        if (string.IsNullOrEmpty(fromTask))
        {
            var pid = packing.Id.Trim();
            var anyTask = await _db.PickingTasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.PackingId == pid)
                .OrderByDescending(t => t.ModifyTime ?? DateTime.MinValue)
                .ThenByDescending(t => t.CreateTime)
                .FirstOrDefaultAsync(cancellationToken);
            fromTask = anyTask?.WarehouseId?.Trim();
        }

        if (!string.IsNullOrEmpty(fromTask))
            return fromTask;

        var fromStorage = await TryResolveWarehouseIdFromStorageAsync(packing.StorageId, cancellationToken);
        if (!string.IsNullOrEmpty(fromStorage))
            return fromStorage;

        throw new InvalidOperationException(
            $"装箱单 {packing.Code} 缺少出库仓库，请配置拣货仓库或装箱库存储位置");
    }

    private async Task<string?> TryResolveWarehouseIdFromStorageAsync(
        string? storageId,
        CancellationToken cancellationToken)
    {
        var sid = storageId?.Trim();
        if (string.IsNullOrEmpty(sid))
            return null;

        var warehouse = await _db.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == sid, cancellationToken);
        if (warehouse != null)
            return warehouse.Id.Trim();

        // 历史：storage_id 曾为 stock.StockId
        var stock = await _db.Stocks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sid && !s.IsDeleted, cancellationToken);
        if (stock == null || string.IsNullOrWhiteSpace(stock.WarehouseId))
            return null;

        return stock.WarehouseId.Trim();
    }

    private static void EnsureBatchStockOutShipFieldsMatch(
        IReadOnlyList<string> packingIds,
        IReadOnlyList<PackingExtendShip> ships)
    {
        static string Norm(string? value) => (value ?? string.Empty).Trim();

        var shipByPackingId = ships
            .GroupBy(s => s.PackingId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var companies = packingIds
            .Select(pid =>
            {
                shipByPackingId.TryGetValue(pid.Trim(), out var ship);
                return Norm(ship?.ShipCompany);
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (companies.Count != 1)
            throw new InvalidOperationException("所选装箱单送货公司名称须一致");

        var addresses = packingIds
            .Select(pid =>
            {
                shipByPackingId.TryGetValue(pid.Trim(), out var ship);
                return Norm(ship?.ShipAddress);
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (addresses.Count != 1)
            throw new InvalidOperationException("所选装箱单送货地址须一致");
    }

    /// <summary>批量出库：每张装箱单一条队列项（不校验出库通知状态）。</summary>
    private static List<PackingStockOutRequestLinkDto> BuildStockOutLinksPerPacking(
        IReadOnlyList<string> packingIdsInOrder,
        IReadOnlyList<PackingItem> packingItems,
        IReadOnlyList<StockOutRequest> requests)
    {
        var links = new List<PackingStockOutRequestLinkDto>();
        foreach (var pid in packingIdsInOrder)
        {
            var packingId = pid.Trim();
            if (string.IsNullOrEmpty(packingId))
                continue;

            var items = packingItems
                .Where(pi => string.Equals(pi.PackingId?.Trim(), packingId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            string? requestId = null;
            foreach (var pi in items)
            {
                var nid = pi.StockOutNotifyId?.Trim();
                if (string.IsNullOrEmpty(nid))
                    continue;
                var req = requests.FirstOrDefault(r =>
                    string.Equals(r.Id.Trim(), nid, StringComparison.OrdinalIgnoreCase));
                if (req != null)
                {
                    requestId = req.Id.Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(requestId))
            {
                foreach (var pi in items)
                {
                    var sellLineId = pi.SellOrderItemId?.Trim();
                    if (string.IsNullOrEmpty(sellLineId))
                        continue;
                    var req = PickStockOutRequestForSellLine(requests, sellLineId);
                    if (req != null)
                    {
                        requestId = req.Id.Trim();
                        break;
                    }
                }
            }

            links.Add(new PackingStockOutRequestLinkDto
            {
                PackingId = packingId,
                StockOutRequestId = requestId ?? string.Empty
            });
        }

        return links;
    }

    private static List<PackingStockOutRequestLinkDto> BuildStockOutRequestPackingLinks(
        IReadOnlyList<string> packingIdsInOrder,
        IReadOnlyList<PackingItem> packingItems,
        IReadOnlyList<StockOutRequest> requests)
    {
        var links = new List<PackingStockOutRequestLinkDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var req in requests)
        {
            var reqId = req.Id.Trim();
            if (!seen.Add(reqId))
                continue;

            string? packingId = null;
            foreach (var pid in packingIdsInOrder)
            {
                var match = packingItems.FirstOrDefault(pi =>
                    string.Equals(pi.PackingId?.Trim(), pid, StringComparison.OrdinalIgnoreCase)
                    && (
                        (!string.IsNullOrWhiteSpace(pi.StockOutNotifyId)
                         && string.Equals(pi.StockOutNotifyId.Trim(), reqId, StringComparison.OrdinalIgnoreCase))
                        || (string.IsNullOrWhiteSpace(pi.StockOutNotifyId)
                            && !string.IsNullOrWhiteSpace(pi.SellOrderItemId)
                            && string.Equals(
                                pi.SellOrderItemId.Trim(),
                                req.SalesOrderItemId.Trim(),
                                StringComparison.OrdinalIgnoreCase))));
                if (match != null)
                {
                    packingId = match.PackingId.Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(packingId))
                packingId = packingIdsInOrder.FirstOrDefault()?.Trim();

            if (string.IsNullOrEmpty(packingId))
                continue;

            links.Add(new PackingStockOutRequestLinkDto
            {
                StockOutRequestId = reqId,
                PackingId = packingId
            });
        }

        return links;
    }

    /// <summary>
    /// 装箱明细未绑 stockout_notify_id 时，按销售行回退匹配一条出库通知（分批出库时勿取同销售行全部通知）。
    /// </summary>
    private async Task<string?> TryResolveFallbackStockOutNotifyIdForSellLineAsync(
        string sellOrderItemId,
        CancellationToken cancellationToken)
    {
        var lineId = sellOrderItemId?.Trim();
        if (string.IsNullOrEmpty(lineId))
            return null;

        return await _db.StockOutRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.SalesOrderItemId == lineId)
            .OrderBy(r => r.Status == StockOutRequestStatusCode.Packed ? 0 : 1)
            .ThenBy(r => r.Status == StockOutRequestStatusCode.PendingPacking ? 1 : 2)
            .ThenBy(r => r.RequestCode)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static StockOutRequest? PickStockOutRequestForSellLine(
        IReadOnlyList<StockOutRequest> requests,
        string sellOrderItemId)
    {
        var lineId = sellOrderItemId.Trim();
        var candidates = requests
            .Where(r => string.Equals(r.SalesOrderItemId.Trim(), lineId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderBy(r => r.Status == StockOutRequestStatusCode.Packed ? 0 : 1)
            .ThenBy(r => r.Status == StockOutRequestStatusCode.PendingPacking ? 1 : 2)
            .ThenBy(r => r.RequestCode)
            .First();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PackingItemFlowStockOutLineDto>> GetFlowStockOutLinesByPackingItemIdAsync(
        string packingItemId,
        CancellationToken cancellationToken = default)
    {
        var id = packingItemId?.Trim();
        if (string.IsNullOrEmpty(id))
            return Array.Empty<PackingItemFlowStockOutLineDto>();

        var pickItemIds = await _db.PickingTaskItems.AsNoTracking()
            .Where(pti => !pti.IsDeleted && pti.PackingItemId == id)
            .Select(pti => pti.Id)
            .ToListAsync(cancellationToken);
        if (pickItemIds.Count == 0)
            return Array.Empty<PackingItemFlowStockOutLineDto>();

        var rows = await (
            from soi in _db.StockOutItems.AsNoTracking()
            join so in _db.StockOuts.AsNoTracking() on soi.StockOutId equals so.Id
            join cust in _db.Customers.AsNoTracking() on so.CustomerId equals cust.Id into cg
            from cust in cg.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on so.CreateByUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            where !soi.IsDeleted
                  && !so.IsDeleted
                  && soi.PickingTaskItemId != null
                  && pickItemIds.Contains(soi.PickingTaskItemId)
                  && so.StockOutType != StockOutTypeCode.Transfer
            orderby soi.CreateTime, soi.Id
            select new PackingItemFlowStockOutLineDto
            {
                StockOutId = so.Id,
                StockOutCode = so.StockOutCode,
                StockOutItemId = soi.Id,
                StockOutItemCode = soi.StockOutItemCode,
                Qty = soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity,
                Status = so.Status,
                CreateTime = soi.CreateTime,
                CustomerName = cust != null
                    ? (string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.NickName : cust.OfficialName)
                    : null,
                CustomerCode = cust != null ? cust.CustomerCode : null,
                CreateUserName = u != null ? u.UserName : null,
                StockOutType = so.StockOutType
            }).ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return rows;

        var packingMeta = await _db.PackingItems.AsNoTracking()
            .Where(pi => pi.Id == id)
            .Select(pi => new { pi.PackingId, pi.StockOutNotifyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (packingMeta == null)
            return rows;

        string? declarationId = null;
        string? declarationCode = null;
        var packingDecl = await _db.Packings.AsNoTracking()
            .Where(p => p.Id == packingMeta.PackingId)
            .Select(p => p.CustomsDeclarationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(packingDecl))
        {
            declarationId = packingDecl.Trim();
            var summary = await _customsTraceQuery.ResolveCustomsSummaryByDeclarationIdAsync(
                declarationId,
                cancellationToken);
            declarationCode = summary?.DeclarationCode;
        }
        else if (!string.IsNullOrWhiteSpace(packingMeta.StockOutNotifyId))
        {
            var notifyId = packingMeta.StockOutNotifyId.Trim();
            var traceMap = await _customsTraceQuery.GetByStockOutNotifyIdsAsync(
                new[] { notifyId },
                cancellationToken);
            if (traceMap.TryGetValue(notifyId, out var trace))
            {
                declarationId = trace.CustomsDeclarationId;
                declarationCode = trace.CustomsDeclarationCode;
            }
        }

        if (string.IsNullOrWhiteSpace(declarationId))
            return rows;

        foreach (var row in rows)
        {
            if (StockOutTypeCode.NormalizeForNotify(row.StockOutType) != StockOutTypeCode.Customs)
                continue;
            row.CustomsDeclarationId = declarationId;
            row.CustomsDeclarationCode = declarationCode;
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<string?> ResolveLinkedStockOutIdForPrintAsync(
        string packingId,
        CancellationToken cancellationToken = default)
    {
        var pid = packingId?.Trim();
        if (string.IsNullOrEmpty(pid))
            return null;

        var fromItem = await _db.StockOutItems
            .AsNoTracking()
            .Where(soi =>
                !soi.IsDeleted
                && soi.PackingId != null
                && soi.PackingId == pid)
            .OrderByDescending(soi => soi.CreateTime)
            .Select(soi => soi.StockOutId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrEmpty(fromItem))
            return fromItem;

        var sellLineIds = await _db.PackingItems
            .AsNoTracking()
            .Where(pi => pi.PackingId == pid && !pi.IsDeleted && pi.SellOrderItemId != null)
            .Select(pi => pi.SellOrderItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (sellLineIds.Count == 0)
            return null;

        return await _db.StockOuts
            .AsNoTracking()
            .Where(so =>
                !so.IsDeleted
                && so.StockOutType != StockOutTypeCode.Transfer
                && so.SellOrderItemId != null
                && sellLineIds.Contains(so.SellOrderItemId))
            .OrderByDescending(so => so.CreateTime)
            .ThenByDescending(so => so.Id)
            .Select(so => so.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PackingDraftFromStockOutRequestsDto> GetDraftFromStockOutRequestsAsync(
        IReadOnlyList<string> stockOutRequestIds,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadValidatedStockOutRequestsAsync(stockOutRequestIds, cancellationToken);
        ValidatePackingCreateCompleteness(bundle.Requests, bundle.SoItemMap);
        var cust = bundle.Customer;
        var salesUser = bundle.SalesUser;

        var soIds = bundle.Requests.Select(r => r.SalesOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var sellOrders = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var soItemIds = bundle.Requests.Select(r => r.SalesOrderItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var sellItems = soItemIds.Count == 0
            ? new Dictionary<string, SellOrderItem>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepository.FindAsync(si => soItemIds.Contains(si.Id)))
                .ToDictionary(si => si.Id.Trim(), si => si, StringComparer.OrdinalIgnoreCase);

        static string? FormatUserName(User? user) =>
            user == null
                ? null
                : EntityLookupService.FormatUserLoginName(user) ?? user.RealName ?? user.UserName;

        var lines = new List<PackingDraftLineDto>();
        foreach (var req in bundle.Requests)
        {
            bundle.SoItemMap.TryGetValue(req.SalesOrderItemId.Trim(), out var soItem);
            sellOrders.TryGetValue(req.SalesOrderId.Trim(), out var so);
            sellItems.TryGetValue(req.SalesOrderItemId.Trim(), out var soItemRow);

            lines.Add(new PackingDraftLineDto
            {
                StockOutRequestId = req.Id,
                RequestCode = req.RequestCode,
                Pn = string.IsNullOrWhiteSpace(req.MaterialCode) ? soItem?.PN : req.MaterialCode.Trim(),
                Brand = string.IsNullOrWhiteSpace(req.MaterialName) ? soItem?.Brand : req.MaterialName?.Trim(),
                Qty = req.Quantity,
                Unit = null,
                SellOrderId = req.SalesOrderId,
                SellOrderItemId = req.SalesOrderItemId,
                SellOrderCode = so?.SellOrderCode,
                SellOrderItemCode = soItemRow?.SellOrderItemCode,
                Remark = req.Remark
            });
        }

        var storageId = await ResolvePackingStorageIdFromRequestsAsync(bundle.Requests, cancellationToken);
        string? warehouseName = null;
        var wh = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == storageId, cancellationToken);
        if (wh != null)
        {
            var name = (wh.WarehouseName ?? "").Trim();
            var code = (wh.WarehouseCode ?? "").Trim();
            warehouseName = string.IsNullOrEmpty(code)
                ? (string.IsNullOrEmpty(name) ? null : name)
                : (string.IsNullOrEmpty(name) ? code : $"{name}（{code}）");
        }

        return new PackingDraftFromStockOutRequestsDto
        {
            CustomerId = bundle.CustomerId,
            CustomerName = ResolvePackingHeaderCustomerName(cust, bundle.FirstSellOrder),
            SalesId = bundle.FirstSellOrder?.SalesUserId,
            SalesUserName = FormatUserName(salesUser),
            StockOutType = StockOutTypeCode.NormalizeForNotify(bundle.Requests[0].StockOutType),
            WarehouseId = storageId,
            WarehouseName = warehouseName,
            ShipmentMethod = LogisticsShipmentMethodCode.Normalize(bundle.Requests[0].ShipmentMethod),
            ExpressCompany = LogisticsShipmentMethodCode.NormalizeExpressCompany(bundle.Requests[0].ExpressCompany),
            Lines = lines
        };
    }

    private static string? ResolvePackingShipmentMethod(PackingExtendShip? extendShip)
    {
        if (extendShip == null)
            return null;
        var fromField = LogisticsShipmentMethodCode.Normalize(extendShip.ShipmentMethod);
        if (!string.IsNullOrEmpty(fromField))
            return fromField;
#pragma warning disable CS0618
        return LogisticsShipmentMethodCode.MapLegacyDeliveryMethod(extendShip.DeliveryMethod);
#pragma warning restore CS0618
    }

    /// <summary>
    /// 按出库通知 <see cref="StockOutRequest.RegionType"/> 解析装箱单出库仓库（<c>warehouseinfo.Id</c> → <see cref="Packing.StorageId"/>）。
    /// </summary>
    private async Task<string> ResolvePackingStorageIdFromRequestsAsync(
        IReadOnlyList<StockOutRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests == null || requests.Count == 0)
            throw new InvalidOperationException("出库通知不能为空");

        var regionTypes = requests
            .Select(r => RegionTypeCode.Normalize(r.RegionType))
            .Distinct()
            .ToList();
        if (regionTypes.Count != 1)
            throw new InvalidOperationException("所选出库通知的送达地域必须一致");

        var regionType = regionTypes[0];
        var wh = await _db.Warehouses
            .AsNoTracking()
            .Where(w => w.Status == 1 && w.RegionType == regionType)
            .OrderBy(w => w.WarehouseCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (wh == null || string.IsNullOrWhiteSpace(wh.Id))
        {
            var label = regionType == RegionTypeCode.Overseas ? "境外（海外）" : "境内（大陆）";
            throw new InvalidOperationException(
                $"未找到地域为「{label}」的启用仓库，请先在「仓库档案」中配置对应地域的仓库");
        }

        return wh.Id.Trim();
    }

    private async Task ValidateCustomsStockOutRequestsForPackingAsync(
        IReadOnlyList<StockOutRequest> requests,
        CancellationToken cancellationToken)
    {
        foreach (var r in requests)
        {
            if (StockOutTypeCode.NormalizeForNotify(r.StockOutType) != StockOutTypeCode.Customs)
                throw new InvalidOperationException("报关装箱须选择报关出库通知（Type=20）。");
            if (string.IsNullOrWhiteSpace(r.CustomsPendlistId))
            {
                var code = string.IsNullOrWhiteSpace(r.RequestCode) ? r.Id : r.RequestCode.Trim();
                throw new InvalidOperationException($"报关出库通知 {code} 缺少待报关关联。");
            }
        }

        var pendlistIds = requests
            .Select(r => r.CustomsPendlistId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pendlists = await _db.CustomsPendlists
            .AsNoTracking()
            .Where(p => pendlistIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (pendlists.Count != pendlistIds.Count)
            throw new InvalidOperationException("部分待报关记录不存在，请刷新后重试。");

        if (pendlists.Any(p => p.Status != CustomsPendlistStatusCode.CustomsOutNotifyCreated))
            throw new InvalidOperationException("仅「已生成报关出库通知」的待报关记录可组报关装箱。");

        var whIds = pendlists
            .Select(p => p.OverseasWarehouseId?.Trim() ?? string.Empty)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (whIds.Count > 1)
            throw new InvalidOperationException("所选报关出库通知须来自同一境外仓，不能跨仓合并装箱。");
    }
}
