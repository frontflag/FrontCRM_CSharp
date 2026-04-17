using CRM.Core.Models.Customs;

namespace CRM.Core.Interfaces;

public interface ICustomsBrokerService
{
    Task<IReadOnlyList<CustomsBroker>> GetActiveListAsync();
    /// <summary>管理列表：含停用，按编码排序。</summary>
    Task<IReadOnlyList<CustomsBroker>> GetAllOrderedForAdminAsync();
    Task<CustomsBroker?> GetByIdAsync(string id);

    /// <summary>创建报关公司；<c>BrokerCode</c> 由 <see cref="ISerialNumberService"/>（<c>sys_serial_number</c> 模块 <c>CustomsBroker</c>）自动生成。</summary>
    Task<CustomsBroker> CreateAsync(string cname, string? ename, short regionType, string? remark, string? actingUserId);

    Task<CustomsBroker> UpdateAsync(string id, string cname, string? ename, short regionType, string? remark, string? actingUserId);

    Task<CustomsBroker> SetStatusAsync(string id, short status, string? actingUserId);

    Task SoftDeleteAsync(string id, string? actingUserId);
}
