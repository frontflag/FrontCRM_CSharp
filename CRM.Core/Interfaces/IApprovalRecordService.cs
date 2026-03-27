using CRM.Core.Models.System;

namespace CRM.Core.Interfaces
{
    public interface IApprovalRecordService
    {
        Task RecordSubmitAsync(string bizType, string businessId, string? documentCode, string? itemDescription, short? fromStatus, short toStatus, string? submitRemark, string? submitterUserId, string? submitterUserName);
        Task RecordDecisionAsync(string bizType, string businessId, string? documentCode, string? itemDescription, short? fromStatus, short toStatus, string decision, string? auditRemark, string? approverUserId, string? approverUserName);
        Task<IReadOnlyList<ApprovalRecord>> GetHistoryAsync(string bizType, string businessId);
    }
}

