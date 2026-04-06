using CRM.Core.Models.Dtos;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// sys_dict_item 维护（无删除）
    /// </summary>
    public interface ISysDictItemAdminService
    {
        Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

        Task<SysDictItemAdminPagedDto> ListAsync(SysDictItemAdminQuery query,
            CancellationToken cancellationToken = default);

        Task<SysDictItemAdminRowDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>下一选项编码：同类下 ItemCode 可解析为整数的最大值 +1；无可解析数字时从 1 开始。</summary>
        Task<string> GetNextItemCodeAsync(string category, CancellationToken cancellationToken = default);

        Task<(bool Ok, string? Error)> CreateAsync(CreateSysDictItemDto dto,
            CancellationToken cancellationToken = default);

        Task<(bool Ok, string? Error)> UpdateAsync(string id, UpdateSysDictItemDto dto,
            CancellationToken cancellationToken = default);

        Task<(bool Ok, string? Error)> ReorderAsync(ReorderSysDictItemsDto dto,
            CancellationToken cancellationToken = default);
    }
}
