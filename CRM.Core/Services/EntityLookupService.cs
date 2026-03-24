using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Services;

/// <inheritdoc cref="IEntityLookupService" />
public sealed class EntityLookupService : IEntityLookupService
{
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<CustomerContactInfo> _customerContactRepo;
    private readonly IRepository<VendorInfo> _vendorRepo;
    private readonly IRepository<VendorContactInfo> _vendorContactRepo;
    private readonly IUserService _userService;

    public EntityLookupService(
        IRepository<CustomerInfo> customerRepo,
        IRepository<CustomerContactInfo> customerContactRepo,
        IRepository<VendorInfo> vendorRepo,
        IRepository<VendorContactInfo> vendorContactRepo,
        IUserService userService)
    {
        _customerRepo = customerRepo;
        _customerContactRepo = customerContactRepo;
        _vendorRepo = vendorRepo;
        _vendorContactRepo = vendorContactRepo;
        _userService = userService;
    }

    /// <inheritdoc />
    public Task<CustomerInfo?> GetCustomerByIdAsync(string? customerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Task.FromResult<CustomerInfo?>(null);
        return _customerRepo.GetByIdAsync(customerId);
    }

    /// <inheritdoc />
    public Task<CustomerContactInfo?> GetCustomerContactByIdAsync(string? contactId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contactId))
            return Task.FromResult<CustomerContactInfo?>(null);
        return _customerContactRepo.GetByIdAsync(contactId);
    }

    /// <inheritdoc />
    public Task<VendorInfo?> GetVendorByIdAsync(string? vendorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vendorId))
            return Task.FromResult<VendorInfo?>(null);
        return _vendorRepo.GetByIdAsync(vendorId);
    }

    /// <inheritdoc />
    public Task<VendorContactInfo?> GetVendorContactByIdAsync(string? contactId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contactId))
            return Task.FromResult<VendorContactInfo?>(null);
        return _vendorContactRepo.GetByIdAsync(contactId);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;
        return await _userService.GetByIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<string?> GetUserDisplayNameAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var u = await GetUserByIdAsync(userId, cancellationToken);
        return FormatUserDisplayName(u);
    }

    /// <summary>与列表/详情中业务员展示规则一致。</summary>
    public static string? FormatUserDisplayName(User? u)
    {
        if (u == null) return null;
        return string.IsNullOrWhiteSpace(u.RealName) ? u.UserName : u.RealName;
    }
}
