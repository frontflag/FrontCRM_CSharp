using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// Vendor服务接口
    /// </summary>
    public interface IVendorService
    {
        /// <summary>
        /// 创建
        /// </summary>
        Task<VendorInfo> CreateAsync(CreateVendorRequest request);

        /// <summary>
        /// 根据ID获取
        /// </summary>
        Task<VendorInfo?> GetByIdAsync(string id);

        /// <summary>
        /// 获取所有
        /// </summary>
        Task<IEnumerable<VendorInfo>> GetAllAsync();

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<PagedResult<VendorInfo>> GetPagedAsync(VendorQueryRequest request);

        /// <summary>
        /// 更新
        /// </summary>
        Task<VendorInfo> UpdateAsync(string id, UpdateVendorRequest request);

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
        Task<IEnumerable<VendorInfo>> SearchAsync(string keyword);
    }

    /// <summary>
    /// 创建请求
    /// </summary>
    public class CreateVendorRequest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新请求
    /// </summary>
    public class UpdateVendorRequest
    {
        public string? Name { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 查询请求
    /// </summary>
    public class VendorQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public short? Status { get; set; }
    }
}
