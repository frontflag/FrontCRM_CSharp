using CRM.Core.Models.System;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 错误日志服务接口
    /// </summary>
    public interface IErrorLogService
    {
        /// <summary>
        /// 记录错误日志
        /// </summary>
        Task LogAsync(
            string moduleName,
            string errorMessage,
            Exception? exception = null,
            string? operationType = null,
            string? documentNo = null,
            string? dataId = null,
            string? userId = null,
            string? userName = null,
            string? requestPath = null,
            string? requestBody = null
        );

        /// <summary>
        /// 分页查询错误日志
        /// </summary>
        Task<(IEnumerable<SysErrorLog> Items, int Total)> GetPagedAsync(
            int page,
            int pageSize,
            string? moduleName = null,
            string? keyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );

        /// <summary>
        /// 标记错误为已处理
        /// </summary>
        Task ResolveAsync(long id, string remark);
    }
}
