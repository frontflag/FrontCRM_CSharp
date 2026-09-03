using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>装箱单 Packing 打印页数据：装箱单 + 可选关联出库单 + 公司参数。</summary>
public static class PackingReportBundleLoader
{
    private static async Task<(string? Address, short? RegionType)> ResolveWarehouseMetaAsync(
        ApplicationDbContext db,
        string? warehouseId,
        CancellationToken cancellationToken)
    {
        var wid = warehouseId?.Trim();
        if (string.IsNullOrEmpty(wid))
            return (null, null);

        var wh = await db.Warehouses.AsNoTracking()
            .Where(w => w.Id == wid)
            .Select(w => new { w.Address, w.RegionType })
            .FirstOrDefaultAsync(cancellationToken);
        if (wh == null)
            return (null, null);

        var address = string.IsNullOrWhiteSpace(wh.Address) ? null : wh.Address.Trim();
        return (address, RegionTypeCode.Normalize(wh.RegionType));
    }

    private static PackingReportAddressPanelDto BuildAddressPanel(PackingDetailDto packing)
    {
        static string Line(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v.Trim();
        var customer = Line(packing.CustomerName);
        return new PackingReportAddressPanelDto
        {
            BillToLines = new List<string>
            {
                customer,
                Line(packing.BillAddress),
                Line(packing.BillAttn),
                Line(packing.BillTel)
            },
            ShipToLines = new List<string>
            {
                customer,
                Line(packing.ShipAddress),
                Line(packing.ShipAttn),
                Line(packing.ShipTel)
            }
        };
    }

    private static void ApplyCustomerToAddressPanel(PackingReportAddressPanelDto panel, string? customerName)
    {
        static string Line(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v.Trim();
        var customer = Line(customerName);
        SetCustomerFirstLine(panel.BillToLines, customer);
        SetCustomerFirstLine(panel.ShipToLines, customer);
    }

    private static void SetCustomerFirstLine(List<string> lines, string customer)
    {
        if (lines.Count >= 4)
            lines[0] = customer;
        else if (lines.Count == 3)
            lines.Insert(0, customer);
        else
        {
            lines.Clear();
            lines.Add(customer);
            while (lines.Count < 4) lines.Add("—");
            return;
        }
        while (lines.Count < 4) lines.Add("—");
        if (lines.Count > 4) lines.RemoveRange(4, lines.Count - 4);
    }

    private static StockOutDetailViewDto BuildStockOutFallbackFromPacking(
        PackingDetailDto packing,
        string? warehouseId)
    {
        var qty = packing.Items?.Sum(i => i.Qty) ?? 0;
        var firstLine = packing.Items?.FirstOrDefault();
        return new StockOutDetailViewDto
        {
            Id = packing.Id,
            StockOutCode = packing.Code ?? string.Empty,
            StockOutType = packing.StockOutType,
            SourceCode = firstLine?.SellOrderCode,
            StockOutDate = packing.ScheduleShipDate ?? packing.CreateTime,
            TotalQuantity = qty,
            TotalAmount = packing.Items?.Sum(i => (i.Price ?? 0m) * i.Qty) ?? 0m,
            Status = 2,
            Remark = packing.Comment,
            CreateTime = packing.CreateTime,
            CustomerName = packing.CustomerName,
            SalesUserName = packing.SalesUserName,
            SellOrderItemCode = firstLine?.SellOrderItemCode,
            SellOrderItemId = firstLine?.SellOrderItemId,
            WarehouseId = warehouseId
        };
    }

    public static async Task<StockOutPackingReportBundleDto?> LoadByPackingIdAsync(
        string packingId,
        bool withInspection,
        IPackingService packingService,
        IStockOutService stockOutService,
        ApplicationDbContext db,
        IRbacService rbacService,
        ClaimsPrincipal user,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var pid = packingId?.Trim();
        if (string.IsNullOrEmpty(pid))
            return null;

        var packing = await packingService.GetPackingByIdAsync(pid, cancellationToken);
        if (packing == null)
            return null;

        var packingCode = string.IsNullOrWhiteSpace(packing.Code) ? null : packing.Code.Trim();
        var packingAddresses = BuildAddressPanel(packing);

        var warehouseId = await db.Packings
            .AsNoTracking()
            .Where(p => p.Id == pid && !p.IsDeleted)
            .Select(p => p.StorageId)
            .FirstOrDefaultAsync(cancellationToken);

        StockOutDetailViewDto? stockOut = null;
        var stockOutId = await packingService.ResolveLinkedStockOutIdForPrintAsync(pid, cancellationToken);
        if (!string.IsNullOrWhiteSpace(stockOutId))
        {
            stockOut = await stockOutService.GetDetailViewAsync(stockOutId.Trim());
            if (stockOut != null && await SaleMaskHttp.ShouldMaskSale521Async(rbacService, user))
                SaleSensitiveFieldMask521.ApplyStockOutDetailView(stockOut, true);
        }

        stockOut ??= BuildStockOutFallbackFromPacking(packing, warehouseId);
        ApplyCustomerToAddressPanel(packingAddresses, stockOut.CustomerName);

        var resolvedWarehouseId = !string.IsNullOrWhiteSpace(warehouseId)
            ? warehouseId.Trim()
            : stockOut.WarehouseId?.Trim();
        var (warehouseAddress, warehouseRegionType) = await ResolveWarehouseMetaAsync(
            db,
            resolvedWarehouseId,
            cancellationToken);

        var companyProfile = await CompanyProfileBundleLoader.LoadAsync(db, logger, cancellationToken);
        CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);

        return new StockOutPackingReportBundleDto
        {
            StockOut = stockOut,
            CompanyProfile = companyProfile,
            WithShipmentInspection = withInspection,
            PackingCode = packingCode,
            PackingAddresses = packingAddresses,
            WarehouseAddress = warehouseAddress,
            WarehouseRegionType = warehouseRegionType,
            ShipmentMethod = packing.ShipmentMethod,
#pragma warning disable CS0618
            DeliveryMethod = packing.DeliveryMethod,
#pragma warning restore CS0618
            PackingLines = await MapPackingLinesAsync(packing, db, cancellationToken)
        };
    }

    /// <summary>
    /// 报关装箱单打印：收货人整块覆盖为报关公司。缺资料抛 <see cref="InvalidOperationException"/>。
    /// Packing / 报关装箱 Invoice 均调用；销售装箱 Invoice 因类型不是 20 会直接返回。
    /// </summary>
    public static async Task OverlayCustomsBrokerConsigneeAsync(
        ApplicationDbContext db,
        string? packingId,
        PackingReportAddressPanelDto panel,
        CancellationToken cancellationToken = default)
    {
        var pid = packingId?.Trim();
        if (string.IsNullOrEmpty(pid))
            return;

        var packing = await db.Packings.AsNoTracking()
            .Where(p => p.Id == pid && !p.IsDeleted)
            .Select(p => new { p.StockOutType, p.CustomsBrokerId })
            .FirstOrDefaultAsync(cancellationToken);
        if (packing == null)
            return;
        if (StockOutTypeCode.NormalizeForNotify(packing.StockOutType) != StockOutTypeCode.Customs)
            return;

        var brokerId = packing.CustomsBrokerId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(brokerId))
            throw new InvalidOperationException(CustomsBrokerPrintConsignee.MissingBrokerForPrintMessage);

        var broker = await db.CustomsBrokers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == brokerId, cancellationToken);
        CustomsBrokerPrintConsignee.EnsurePrintReady(broker);

        var lines = CustomsBrokerPrintConsignee.BuildAddressLines(broker!).ToList();
        panel.BillToLines = lines;
        panel.ShipToLines = new List<string>(lines);
        panel.Email = CustomsBrokerPrintConsignee.PrintEmail(broker!);
        panel.CustomsBrokerConsignee = true;
    }

    /// <summary>装箱单 Invoice 打印页：优先关联出库单，否则由装箱单数据合成出库视图。</summary>
    public static async Task<StockOutInvoiceReportBundleDto?> LoadInvoiceByPackingIdAsync(
        string packingId,
        IPackingService packingService,
        IStockOutService stockOutService,
        ApplicationDbContext db,
        IRbacService rbacService,
        ClaimsPrincipal user,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var packingBundle = await LoadByPackingIdAsync(
            packingId,
            withInspection: false,
            packingService,
            stockOutService,
            db,
            rbacService,
            user,
            logger,
            cancellationToken);
        if (packingBundle?.StockOut == null)
            return null;

        await OverlayCustomsBrokerConsigneeAsync(
            db, packingId, packingBundle.PackingAddresses, cancellationToken);

        var packing = await packingService.GetPackingByIdAsync(packingId.Trim(), cancellationToken);
        if (packing != null)
            ApplyCustomsInvoiceUsdPrices(packing, packingBundle.PackingLines);

        return new StockOutInvoiceReportBundleDto
        {
            StockOut = packingBundle.StockOut,
            CompanyProfile = packingBundle.CompanyProfile,
            PackingCode = packingBundle.PackingCode,
            PackingStockOutType = packing?.StockOutType,
            PackingAddresses = packingBundle.PackingAddresses,
            WarehouseAddress = packingBundle.WarehouseAddress,
            WarehouseRegionType = packingBundle.WarehouseRegionType,
            PackingLines = packingBundle.PackingLines
        };
    }

    /// <summary>
    /// 报关装箱 Invoice：美金段，单价用销售折算美金价，币别固定 USD。
    /// 装箱单 Packing List 不改价。
    /// </summary>
    public static void ApplyCustomsInvoiceUsdPrices(
        PackingDetailDto packing,
        List<PackingReportLineDto> lines)
    {
        if (lines.Count == 0 || !CustomsInvoiceReportPriceRules.IsCustomsPacking(packing.StockOutType))
            return;

        var convertByItemId = (packing.ItemExtends ?? new List<PackingDetailItemExtendDto>())
            .Where(e => !string.IsNullOrWhiteSpace(e.PackingItemId))
            .GroupBy(e => e.PackingItemId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().PriceConvertPrice, StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            decimal? convert = null;
            var itemId = line.PackingItemId?.Trim();
            if (!string.IsNullOrEmpty(itemId))
                convertByItemId.TryGetValue(itemId, out convert);

            var resolved = CustomsInvoiceReportPriceRules.ResolveLine(
                packing.StockOutType,
                line.Price,
                convert,
                line.PriceCurrency);
            line.Price = resolved.Price;
            line.PriceCurrency = resolved.Currency;
        }
    }

    public static async Task<List<PackingReportLineDto>> MapPackingLinesAsync(
        PackingDetailDto packing,
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var lines = MapPackingLines(packing);
        await EnrichPackingLineBatchFieldsAsync(db, lines, cancellationToken);
        return lines;
    }

    public static List<PackingReportLineDto> MapPackingLines(PackingDetailDto packing)
    {
        if (packing.Items == null || packing.Items.Count == 0)
            return new List<PackingReportLineDto>();

        return packing.Items.Select(item => new PackingReportLineDto
        {
            PackingItemId = item.Id,
            Pn = item.Pn,
            CustomerPn = item.CustomerPn,
            Brand = item.Brand,
            CustomerBrand = item.CustomerBrand,
            CustomerPo = string.IsNullOrWhiteSpace(item.CustomerSo) ? null : item.CustomerSo.Trim(),
            Qty = item.Qty,
            Carton = null,
            Remark = item.Comment,
            Co = string.IsNullOrWhiteSpace(item.Co) ? null : item.Co.Trim(),
            Price = item.Price,
            PriceCurrency = item.PriceCurrency
        }).ToList();
    }

    /// <summary>
    /// 按装箱行关联拣货层 → 在库明细 → 入库批次，聚合 DC / COD（多值逗号拼接）。
    /// 无拣货时回退 packing_item.stock_item_id。
    /// </summary>
    public static async Task EnrichPackingLineBatchFieldsAsync(
        ApplicationDbContext db,
        List<PackingReportLineDto> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) return;

        var itemIds = lines
            .Select(l => l.PackingItemId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (itemIds.Count == 0) return;

        var picked = await (
            from pti in db.PickingTaskItems.AsNoTracking()
            where pti.PackingItemId != null
                  && itemIds.Contains(pti.PackingItemId)
                  && !pti.IsDeleted
                  && pti.StockItemId != null
                  && pti.StockItemId != ""
            join si in db.StockItems.AsNoTracking() on pti.StockItemId equals si.Id
            where !si.IsDeleted
            join ib in db.StockInBatches.AsNoTracking() on si.StockInItemId equals ib.StockInItemId
            where !ib.IsDeleted
            select new { PackingItemId = pti.PackingItemId!, ib.Dc, ib.WaferOrigin }
        ).ToListAsync(cancellationToken);

        var byItem = new Dictionary<string, (List<string> Dcs, List<string> Cods)>(StringComparer.OrdinalIgnoreCase);
        void Add(string packingItemId, string? dc, string? cod)
        {
            if (!byItem.TryGetValue(packingItemId, out var bag))
            {
                bag = (new List<string>(), new List<string>());
                byItem[packingItemId] = bag;
            }
            AppendDistinct(bag.Dcs, dc);
            AppendDistinct(bag.Cods, cod);
        }

        foreach (var row in picked)
            Add(row.PackingItemId, row.Dc, row.WaferOrigin);

        var missingIds = itemIds.Where(id => !byItem.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var fallback = await (
                from pi in db.PackingItems.AsNoTracking()
                where missingIds.Contains(pi.Id)
                      && !pi.IsDeleted
                      && pi.StockItemId != null
                      && pi.StockItemId != ""
                join si in db.StockItems.AsNoTracking() on pi.StockItemId equals si.Id
                where !si.IsDeleted
                join ib in db.StockInBatches.AsNoTracking() on si.StockInItemId equals ib.StockInItemId
                where !ib.IsDeleted
                select new { PackingItemId = pi.Id, ib.Dc, ib.WaferOrigin }
            ).ToListAsync(cancellationToken);

            foreach (var row in fallback)
                Add(row.PackingItemId, row.Dc, row.WaferOrigin);
        }

        foreach (var line in lines)
        {
            var id = line.PackingItemId?.Trim();
            if (string.IsNullOrEmpty(id) || !byItem.TryGetValue(id, out var bag))
                continue;
            line.Dc = JoinComma(bag.Dcs);
            line.Cod = JoinComma(bag.Cods);
        }
    }

    private static void AppendDistinct(List<string> list, string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return;
        if (list.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
            return;
        list.Add(v);
    }

    private static string? JoinComma(List<string> values)
        => values.Count == 0 ? null : string.Join(", ", values);
}
