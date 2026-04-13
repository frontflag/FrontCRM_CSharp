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
    }
}
