using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces;

/// <summary>
/// 主数据按 ID 查询（客户 / 客户联系人 / 供应商 / 供应商联系人 / 用户）。
/// 各业务模块可注入此服务，避免重复编写仓储查询与空 ID 判断。
/// </summary>
public interface IEntityLookupService
{
    /// <summary>根据客户主键返回客户主档；ID 为空或不存在时返回 null。</summary>
    Task<CustomerInfo?> GetCustomerByIdAsync(string? customerId, CancellationToken cancellationToken = default);

    /// <summary>根据客户联系人主键返回联系人；ID 为空或不存在时返回 null。</summary>
    Task<CustomerContactInfo?> GetCustomerContactByIdAsync(string? contactId, CancellationToken cancellationToken = default);

    /// <summary>根据供应商主键返回供应商主档；ID 为空或不存在时返回 null。</summary>
    Task<VendorInfo?> GetVendorByIdAsync(string? vendorId, CancellationToken cancellationToken = default);

    /// <summary>根据供应商联系人主键返回联系人；ID 为空或不存在时返回 null。</summary>
    Task<VendorContactInfo?> GetVendorContactByIdAsync(string? contactId, CancellationToken cancellationToken = default);

    /// <summary>根据用户主键返回用户实体；ID 为空或不存在时返回 null。</summary>
    Task<User?> GetUserByIdAsync(string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户标识返回展示名：优先按用户ID查询，失败后按 UserName 查询；
    /// 展示名优先 <see cref="User.RealName"/>，为空则用 <see cref="User.UserName"/>。
    /// 标识为空或用户不存在时返回 null。
    /// </summary>
    Task<string?> GetUserDisplayNameAsync(string? userId, CancellationToken cancellationToken = default);
}
