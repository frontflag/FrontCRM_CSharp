using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// SalesInvoice服务接口
    /// </summary>
    public interface ISalesInvoiceService
    {
        /// <summary>
        /// 创建
        /// </summary>
        Task<Invoice> CreateAsync(CreateSalesInvoiceRequest request);

        /// <summary>
        /// 根据ID获取
        /// </summary>
        Task<Invoice?> GetByIdAsync(string id);

        /// <summary>
        /// 获取所有
        /// </summary>
        Task<IEnumerable<Invoice>> GetAllAsync();

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<PagedResult<Invoice>> GetPagedAsync(SalesInvoiceQueryRequest request);

        /// <summary>
        /// 更新
        /// </summary>
        Task<Invoice> UpdateAsync(string id, UpdateSalesInvoiceRequest request);

        /// <summary>
        /// 删除
        /// </summary>
        Task DeleteAsync(string id);

        /// <summary>
        /// 批量删除
        /// </summary>
        Task BatchDeleteAsync(IEnumerable<string> ids);

        /// <summary>
        /// 更新状态
        /// </summary>
        Task UpdateStatusAsync(string id, short status);

        /// <summary>
        /// 搜索
        /// </summary>
        Task<IEnumerable<Invoice>> SearchAsync(string keyword);
    }

    /// <summary>
    /// 创建请求
    /// </summary>
    public class CreateSalesInvoiceRequest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新请求
    /// </summary>
    public class UpdateSalesInvoiceRequest
    {
        public string? Name { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 查询请求
    /// </summary>
    public class SalesInvoiceQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public short? Status { get; set; }
    }
}
