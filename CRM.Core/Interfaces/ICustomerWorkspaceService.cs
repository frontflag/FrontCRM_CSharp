using CRM.Core.Models.Customer;

namespace CRM.Core.Interfaces;

/// <summary>按业务单据解析客户并裁剪右栏「客户」页签摘要。不信任前端传入的客户 ID。</summary>
public interface ICustomerWorkspaceService
{
    /// <summary>
    /// 按来源单据返回客户摘要。单据不存在返回 null；无权访问单据抛 <see cref="UnauthorizedAccessException"/>；
    /// 来源不支持或参数无效抛 <see cref="ArgumentException"/>。
    /// </summary>
    Task<CustomerWorkspaceDto?> GetAsync(string source, string id, string viewerUserId);
}
