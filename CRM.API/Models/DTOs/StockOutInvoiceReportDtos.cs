using CRM.Core.Interfaces;

namespace CRM.API.Models.DTOs
{
    /// <summary>出库 Invoice 打印页：出库详情 + 公司参数（与采购订单报表同源数据，避免依赖 purchase-order.read）。</summary>
    public class StockOutInvoiceReportBundleDto
    {
        public StockOutDetailViewDto StockOut { get; set; } = null!;
        public CompanyProfileBundleDto CompanyProfile { get; set; } = null!;

        /// <summary>装箱单编号（从装箱单入口打印 Invoice 时）。</summary>
        public string? PackingCode { get; set; }

        /// <summary>账单/送货地址（packing_extend_ship）。</summary>
        public PackingReportAddressPanelDto? PackingAddresses { get; set; }

        /// <summary>出库仓库地址。</summary>
        public string? WarehouseAddress { get; set; }

        /// <summary>装箱单明细行（PN / Brand / Qty 等，与 Packing 报表一致）。</summary>
        public List<PackingReportLineDto> PackingLines { get; set; } = new();

        /// <summary>
        /// 出库仓库地域（由 packing.storage_id 或出库单 WarehouseId 解析 warehouseinfo.RegionType）：10=境内 20=境外。
        /// Invoice 报表据此选取人民币/外币默认银行资料。
        /// </summary>
        public short? WarehouseRegionType { get; set; }
    }
}
