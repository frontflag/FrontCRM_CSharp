using CRM.Core.Models.System;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 业务日志服务接口
    /// </summary>
    public interface IBusinessLogService
    {
        /// <summary>
        /// 记录业务操作日志
        /// </summary>
        /// <param name="businessModule">业务模块</param>
        /// <param name="actionType">动作类型</param>
        /// <param name="documentType">单据类型</param>
        /// <param name="businessDataId">业务数据ID</param>
        /// <param name="documentCode">单据编号</param>
        /// <param name="operatorId">操作人ID</param>
        /// <param name="operatorName">操作人名称</param>
        /// <param name="description">操作描述</param>
        /// <param name="result">操作结果</param>
        /// <returns>日志ID</returns>
        Task<string> LogAsync(
            BusinessModule businessModule,
            ActionType actionType,
            string documentType,
            string businessDataId,
            string? documentCode = null,
            string? operatorId = null,
            string? operatorName = null,
            string? description = null,
            bool result = true);

        /// <summary>
        /// 记录业务操作日志（带字段变更详情）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="businessModule">业务模块</param>
        /// <param name="actionType">动作类型</param>
        /// <param name="oldEntity">旧实体</param>
        /// <param name="newEntity">新实体</param>
        /// <param name="operatorId">操作人ID</param>
        /// <param name="operatorName">操作人名称</param>
        /// <param name="description">操作描述</param>
        /// <returns>日志ID</returns>
        Task<string> LogWithChangesAsync<T>(
            BusinessModule businessModule,
            ActionType actionType,
            T? oldEntity,
            T newEntity,
            string? operatorId = null,
            string? operatorName = null,
            string? description = null) where T : class;

        /// <summary>
        /// 获取业务数据的操作日志
        /// </summary>
        /// <param name="businessDataId">业务数据ID</param>
        /// <param name="documentType">单据类型</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<BusinessLog>> GetLogsByBusinessDataAsync(string businessDataId, string? documentType = null);

        /// <summary>
        /// 获取单据的操作日志
        /// </summary>
        /// <param name="documentCode">单据编号</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<BusinessLog>> GetLogsByDocumentCodeAsync(string documentCode);

        /// <summary>
        /// 获取操作人的操作日志
        /// </summary>
        /// <param name="operatorId">操作人ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<BusinessLog>> GetLogsByOperatorAsync(string operatorId, DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 获取业务模块的操作日志
        /// </summary>
        /// <param name="businessModule">业务模块</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<BusinessLog>> GetLogsByModuleAsync(BusinessModule businessModule, DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 获取日志详情（包含字段变更）
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <returns>日志详情</returns>
        Task<BusinessLog?> GetLogDetailAsync(string logId);

        /// <summary>
        /// 分页查询日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>分页结果</returns>
        Task<(IEnumerable<BusinessLog> Logs, int TotalCount)> QueryLogsAsync(BusinessLogQuery query);

        /// <summary>
        /// 清理历史日志
        /// </summary>
        /// <param name="beforeDate">清理此日期之前的日志</param>
        /// <returns>清理的记录数</returns>
        Task<int> CleanOldLogsAsync(DateTime beforeDate);
    }

    /// <summary>
    /// 业务日志查询条件
    /// </summary>
    public class BusinessLogQuery
    {
        /// <summary>
        /// 业务模块
        /// </summary>
        public BusinessModule? BusinessModule { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public ActionType? ActionType { get; set; }

        /// <summary>
        /// 单据类型
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// 业务数据ID
        /// </summary>
        public string? BusinessDataId { get; set; }

        /// <summary>
        /// 单据编号
        /// </summary>
        public string? DocumentCode { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public string? OperatorId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public bool? OperationResult { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? OrderBy { get; set; } = "OperationTime";

        /// <summary>
        /// 是否降序
        /// </summary>
        public bool IsDescending { get; set; } = true;
    }
}
