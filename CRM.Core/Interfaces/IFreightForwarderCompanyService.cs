using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFreightForwarderCompanyService
{
    Task<IReadOnlyList<FreightForwarderCompany>> GetActiveListAsync();
    Task<IReadOnlyList<FreightForwarderCompany>> GetAllOrderedForAdminAsync();
    Task<FreightForwarderCompany?> GetByIdAsync(string id, bool includeBanks = false);
    Task<FreightForwarderCompany> CreateAsync(string cname, string? ename, string? remark, string? actingUserId);
    Task<FreightForwarderCompany> UpdateAsync(string id, string cname, string? ename, string? remark, string? actingUserId);
    Task<FreightForwarderCompany> SetStatusAsync(string id, short status, string? actingUserId);
    Task SoftDeleteAsync(string id, string? actingUserId);

    Task<IReadOnlyList<FreightForwarderCompanyBank>> GetBanksAsync(string companyId);
    Task<FreightForwarderCompanyBank> CreateBankAsync(
        string companyId, string bankName, string? accountName, string? accountNo, byte currency,
        bool isDefault, string? actingUserId);
    Task<FreightForwarderCompanyBank> UpdateBankAsync(
        string bankId, string bankName, string? accountName, string? accountNo, byte currency,
        bool isDefault, bool isDisabled, string? actingUserId);
    Task DeleteBankAsync(string bankId, string? actingUserId);
}
