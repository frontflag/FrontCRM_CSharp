using CRM.Core.Models.System;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 系统参数服务接口
    /// </summary>
    public interface ISysParamService
    {
        #region 分组管理

        /// <summary>
        /// 获取所有分组（树形结构）
        /// </summary>
        Task<List<SysParamGroup>> GetGroupTreeAsync();

        /// <summary>
        /// 获取分组下的所有参数
        /// </summary>
        Task<List<SysParam>> GetParamsByGroupAsync(Guid groupId);

        /// <summary>
        /// 创建分组
        /// </summary>
        Task<SysParamGroup> CreateGroupAsync(SysParamGroup group);

        /// <summary>
        /// 更新分组
        /// </summary>
        Task<SysParamGroup> UpdateGroupAsync(SysParamGroup group);

        /// <summary>
        /// 删除分组
        /// </summary>
        Task<bool> DeleteGroupAsync(Guid groupId);

        #endregion

        #region 参数管理

        /// <summary>
        /// 根据编码获取参数
        /// </summary>
        Task<SysParam?> GetParamByCodeAsync(string paramCode);

        /// <summary>
        /// 获取所有参数
        /// </summary>
        Task<List<SysParam>> GetAllParamsAsync();

        /// <summary>
        /// 创建参数
        /// </summary>
        Task<SysParam> CreateParamAsync(SysParam param);

        /// <summary>
        /// 更新参数
        /// </summary>
        Task<SysParam> UpdateParamAsync(SysParam param, string changeReason = "");

        /// <summary>
        /// 删除参数
        /// </summary>
        Task<bool> DeleteParamAsync(Guid paramId);

        #endregion

        #region 参数值获取（类型安全）

        /// <summary>
        /// 获取字符串参数值
        /// </summary>
        Task<string> GetStringValueAsync(string paramCode, string defaultValue = "");

        /// <summary>
        /// 获取整数参数值
        /// </summary>
        Task<long> GetIntValueAsync(string paramCode, long defaultValue = 0);

        /// <summary>
        /// 获取实数参数值
        /// </summary>
        Task<decimal> GetDecimalValueAsync(string paramCode, decimal defaultValue = 0m);

        /// <summary>
        /// 获取布尔参数值
        /// </summary>
        Task<bool> GetBoolValueAsync(string paramCode, bool defaultValue = false);

        /// <summary>
        /// 获取字符串数组参数值
        /// </summary>
        Task<List<string>> GetStringArrayAsync(string paramCode);

        /// <summary>
        /// 获取整数数组参数值
        /// </summary>
        Task<List<long>> GetIntArrayAsync(string paramCode);

        /// <summary>
        /// 获取实数数组参数值
        /// </summary>
        Task<List<decimal>> GetDecimalArrayAsync(string paramCode);

        /// <summary>
        /// 获取JSON对象参数值
        /// </summary>
        Task<T?> GetJsonValueAsync<T>(string paramCode) where T : class;

        #endregion

        #region 参数值设置

        /// <summary>
        /// 设置字符串参数值
        /// </summary>
        Task SetStringValueAsync(string paramCode, string value, string changeReason = "");

        /// <summary>
        /// 设置整数参数值
        /// </summary>
        Task SetIntValueAsync(string paramCode, long value, string changeReason = "");

        /// <summary>
        /// 设置实数参数值
        /// </summary>
        Task SetDecimalValueAsync(string paramCode, decimal value, string changeReason = "");

        /// <summary>
        /// 设置布尔参数值
        /// </summary>
        Task SetBoolValueAsync(string paramCode, bool value, string changeReason = "");

        /// <summary>
        /// 设置字符串数组参数值
        /// </summary>
        Task SetStringArrayAsync(string paramCode, List<string> values, string changeReason = "");

        /// <summary>
        /// 设置整数数组参数值
        /// </summary>
        Task SetIntArrayAsync(string paramCode, List<long> values, string changeReason = "");

        /// <summary>
        /// 设置实数数组参数值
        /// </summary>
        Task SetDecimalArrayAsync(string paramCode, List<decimal> values, string changeReason = "");

        #endregion

        #region 历史记录

        /// <summary>
        /// 获取参数修改历史
        /// </summary>
        Task<List<SysParamHistory>> GetParamHistoryAsync(Guid paramId);

        #endregion
    }
}
