using CRM.Core.Models.Customs;

namespace CRM.Core.Interfaces;

public sealed class CustomsBrokerWriteFields
{
    public string Cname { get; set; } = string.Empty;
    public string? Ename { get; set; }
    public short RegionType { get; set; }
    public decimal AgencyRate { get; set; } = 1m;
    public string? Remark { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
}

public interface ICustomsBrokerService
{
    Task<IReadOnlyList<CustomsBroker>> GetActiveListAsync();
    /// <summary>管理列表：含停用，按编码排序。</summary>
    Task<IReadOnlyList<CustomsBroker>> GetAllOrderedForAdminAsync();
    Task<CustomsBroker?> GetByIdAsync(string id);

    /// <summary>创建报关公司；<c>BrokerCode</c> 由 <see cref="ISerialNumberService"/>（<c>sys_serial_number</c> 模块 <c>CustomsBroker</c>）自动生成。</summary>
    Task<CustomsBroker> CreateAsync(CustomsBrokerWriteFields fields, string? actingUserId);

    Task<CustomsBroker> UpdateAsync(string id, CustomsBrokerWriteFields fields, string? actingUserId);

    Task<CustomsBroker> SetStatusAsync(string id, short status, string? actingUserId);

    Task SoftDeleteAsync(string id, string? actingUserId);
}
