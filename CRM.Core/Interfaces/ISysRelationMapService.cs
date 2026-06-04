namespace CRM.Core.Interfaces;

public interface ISysRelationMapService
{
    /// <summary>获取某源对象在指定类型下已映射的目标对象 Id 列表（未删除）。</summary>
    Task<IReadOnlyList<string>> GetMappedDestIdsAsync(short type, string objSrc, CancellationToken cancellationToken = default);

    /// <summary>批量增删映射（软删除去除项；已删除行可恢复）。</summary>
    Task SaveMappingsAsync(
        short type,
        string objSrc,
        IReadOnlyList<string> addDestIds,
        IReadOnlyList<string> removeDestIds,
        CancellationToken cancellationToken = default);
}
