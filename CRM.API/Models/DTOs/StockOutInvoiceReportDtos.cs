using CRM.Core.Interfaces;

namespace CRM.API.Models.DTOs
{
    /// <summary>出库 Invoice 打印页：出库详情 + 公司参数（与采购订单报表同源数据，避免依赖 purchase-order.read）。</summary>
    public class StockOutInvoiceReportBundleDto
    {
        public StockOutDetailViewDto StockOut { get; set; } = null!;
        public CompanyProfileBundleDto CompanyProfile { get; set; } = null!;
    }
}
