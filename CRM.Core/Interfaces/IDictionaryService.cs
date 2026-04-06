using CRM.Core.Models.Dtos;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 数据字典查询（供下拉等）
    /// </summary>
    public interface IDictionaryService
    {
        Task<IReadOnlyList<DictionaryItemDto>> GetItemsAsync(string category, bool preferEnglish,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, IReadOnlyList<DictionaryItemDto>>> GetBatchAsync(
            IReadOnlyList<string> categories, bool preferEnglish, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按 Category + ItemCode 解析一条字典（含已禁用项），用于历史数据编辑回显
        /// </summary>
        Task<DictionaryItemDto?> LookupByCodeAsync(string category, string code, bool preferEnglish,
            CancellationToken cancellationToken = default);
    }
}
