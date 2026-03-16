using CRM.Core.Models.Component;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 物料外部数据获取服务接口
    /// 当前实现：MockComponentDataService（模拟数据）
    /// 待替换：NexarComponentDataService（真实 Nexar/Octopart API）
    /// </summary>
    public interface IComponentDataService
    {
        /// <summary>
        /// 根据物料型号（MPN）从外部数据源获取物料完整信息
        /// </summary>
        /// <param name="mpn">物料型号，如 "LM358N"</param>
        /// <returns>物料详情，若未找到则返回 null</returns>
        Task<ComponentDetailDto?> FetchByMpnAsync(string mpn);

        /// <summary>
        /// 数据源名称，用于标记缓存记录的来源
        /// </summary>
        string SourceName { get; }
    }

    /// <summary>
    /// 物料缓存查询服务接口（带24小时缓存逻辑）
    /// </summary>
    public interface IComponentCacheService
    {
        /// <summary>
        /// 查询物料信息：
        /// - 若数据库中存在且未超过24小时，直接返回缓存数据
        /// - 若不存在或已超过24小时，调用外部 API 并更新数据库
        /// </summary>
        Task<ComponentDetailDto?> GetByMpnAsync(string mpn);

        /// <summary>
        /// 强制刷新缓存（忽略24小时限制，直接调用外部 API）
        /// </summary>
        Task<ComponentDetailDto?> RefreshByMpnAsync(string mpn);

        /// <summary>
        /// 获取缓存记录的元信息（上次获取时间、数据来源、查询次数）
        /// </summary>
        Task<ComponentCacheMetaDto?> GetCacheMetaAsync(string mpn);
    }

    /// <summary>
    /// 缓存元信息 DTO
    /// </summary>
    public class ComponentCacheMetaDto
    {
        public string Mpn { get; set; } = string.Empty;
        public DateTime FetchedAt { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public int QueryCount { get; set; }
        public bool IsExpired { get; set; }
        public double HoursUntilExpiry { get; set; }
    }
}
