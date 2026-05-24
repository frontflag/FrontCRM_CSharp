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
            TotalAmount = 0,
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
            DeliveryMethod = packing.DeliveryMethod,
            PackingLines = MapPackingLines(packing)
        };
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

        return new StockOutInvoiceReportBundleDto
        {
            StockOut = packingBundle.StockOut,
            CompanyProfile = packingBundle.CompanyProfile,
            PackingCode = packingBundle.PackingCode,
            PackingAddresses = packingBundle.PackingAddresses,
            WarehouseAddress = packingBundle.WarehouseAddress,
            WarehouseRegionType = packingBundle.WarehouseRegionType,
            PackingLines = packingBundle.PackingLines
        };
    }

    public static List<PackingReportLineDto> MapPackingLines(PackingDetailDto packing)
    {
        if (packing.Items == null || packing.Items.Count == 0)
            return new List<PackingReportLineDto>();

        return packing.Items.Select(item => new PackingReportLineDto
        {
            Pn = item.Pn,
            CustomerPn = item.CustomerPn,
            Brand = item.Brand,
            CustomerBrand = item.CustomerBrand,
            Qty = item.Qty,
            Carton = null,
            Remark = item.Comment
        }).ToList();
    }
}
