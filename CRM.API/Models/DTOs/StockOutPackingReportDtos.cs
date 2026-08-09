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
        /// <summary>装箱明细主键；用于关联拣货批次取 DC/COD。</summary>
        public string? PackingItemId { get; set; }

        public string? Pn { get; set; }
        public string? CustomerPn { get; set; }
        public string? Brand { get; set; }
        public string? CustomerBrand { get; set; }

        /// <summary>客户订单号（Customer SO / PO）。</summary>
        public string? CustomerPo { get; set; }

        public int Qty { get; set; }
        public string? Carton { get; set; }
        public string? Remark { get; set; }

        /// <summary>Date Code；多批次逗号拼接。</summary>
        public string? Dc { get; set; }

        /// <summary>封装产地（packing_item.CO）。</summary>
        public string? Co { get; set; }

        /// <summary>晶圆产地（StockInBatch.WaferOrigin）；多批次逗号拼接。</summary>
        public string? Cod { get; set; }

        /// <summary>尺寸；本期无行级来源，可空。</summary>
        public string? Size { get; set; }

        /// <summary>净重 kg；本期无行级来源，可空。</summary>
        public decimal? Nw { get; set; }

        /// <summary>毛重 kg；本期无行级来源，可空。</summary>
        public decimal? Gw { get; set; }

        /// <summary>关联 SO 行交易币别（short 编码，与结算币别一致）；用于报表按币别选抬头。</summary>
        public short? PriceCurrency { get; set; }
    }

    /// <summary>Packing 报表 Bill To / Ship To 四行：客户名称、地址、联系人、电话。</summary>
    public class PackingReportAddressPanelDto
    {
        public List<string> BillToLines { get; set; } = new();
        public List<string> ShipToLines { get; set; } = new();
    }
}
