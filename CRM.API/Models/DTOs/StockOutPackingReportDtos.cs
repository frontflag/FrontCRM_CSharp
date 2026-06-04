using CRM.Core.Interfaces;

namespace CRM.API.Models.DTOs
{
    /// <summary>出库 Packing 打印页：出库详情 + 公司参数 + 是否含出货检验版式。</summary>
    public class StockOutPackingReportBundleDto
    {
        public StockOutDetailViewDto StockOut { get; set; } = null!;
        public CompanyProfileBundleDto CompanyProfile { get; set; } = null!;

        /// <summary>true：含出货检验区块；false：不含出货检验（对应两套 SEMICORE 模版）。</summary>
        public bool WithShipmentInspection { get; set; }

        /// <summary>关联装箱单编号（packing.code，如 PAK…）。</summary>
        public string? PackingCode { get; set; }

        /// <summary>装箱单客户账单/送货地址（来自 packing_extend_ship）。</summary>
        public PackingReportAddressPanelDto PackingAddresses { get; set; } = new();

        /// <summary>出库仓库地址（warehouseinfo.Address）。</summary>
        public string? WarehouseAddress { get; set; }

        /// <summary>出库仓库地域（packing.storage_id 解析 warehouseinfo.RegionType）：10=境内 20=境外。</summary>
        public short? WarehouseRegionType { get; set; }

        /// <summary>装箱单出货方式 packing_extend_ship.shipment_method（LogisticsArrivalMethod ItemCode）。</summary>
        public string? ShipmentMethod { get; set; }

        /// <summary>已废弃：请使用 ShipmentMethod。</summary>
        public short? DeliveryMethod { get; set; }

        /// <summary>装箱单明细行（打印表格 PN / Brand / Qty 等）。</summary>
        public List<PackingReportLineDto> PackingLines { get; set; } = new();
    }

    /// <summary>Packing 报表明细行。</summary>
    public class PackingReportLineDto
    {
        public string? Pn { get; set; }
        public string? CustomerPn { get; set; }
        public string? Brand { get; set; }
        public string? CustomerBrand { get; set; }
        public int Qty { get; set; }
        public string? Carton { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>Packing 报表 Bill To / Ship To 四行：客户名称、地址、联系人、电话。</summary>
    public class PackingReportAddressPanelDto
    {
        public List<string> BillToLines { get; set; } = new();
        public List<string> ShipToLines { get; set; } = new();
    }
}
