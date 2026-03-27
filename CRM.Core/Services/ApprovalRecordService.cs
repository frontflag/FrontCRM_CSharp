using CRM.Core.Interfaces;
using CRM.Core.Models.System;

namespace CRM.Core.Services
{
    public class ApprovalRecordService : IApprovalRecordService
    {
        private readonly IRepository<ApprovalRecord> _repo;
        private readonly IUnitOfWork _uow;

        public ApprovalRecordService(IRepository<ApprovalRecord> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task RecordSubmitAsync(string bizType, string businessId, string? documentCode, string? itemDescription, short? fromStatus, short toStatus, string? submitRemark, string? submitterUserId, string? submitterUserName)
        {
            var entity = new ApprovalRecord
            {
                BizType = bizType,
                BusinessId = businessId,
                DocumentCode = documentCode,
                ItemDescription = string.IsNullOrWhiteSpace(itemDescription) ? null : itemDescription.Trim(),
                ActionType = "submit",
                FromStatus = fromStatus,
                ToStatus = toStatus,
                SubmitRemark = string.IsNullOrWhiteSpace(submitRemark) ? null : submitRemark.Trim(),
                SubmitterUserId = submitterUserId,
                SubmitterUserName = submitterUserName,
                ActionTime = DateTime.UtcNow
            };
            await _repo.AddAsync(entity);
            await _uow.SaveChangesAsync();
        }

        public async Task RecordDecisionAsync(string bizType, string businessId, string? documentCode, string? itemDescription, short? fromStatus, short toStatus, string decision, string? auditRemark, string? approverUserId, string? approverUserName)
        {
            var entity = new ApprovalRecord
            {
                BizType = bizType,
                BusinessId = businessId,
                DocumentCode = documentCode,
                ItemDescription = string.IsNullOrWhiteSpace(itemDescription) ? null : itemDescription.Trim(),
                ActionType = decision,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                AuditRemark = string.IsNullOrWhiteSpace(auditRemark) ? null : auditRemark.Trim(),
                ApproverUserId = approverUserId,
                ApproverUserName = approverUserName,
                ActionTime = DateTime.UtcNow
            };
            await _repo.AddAsync(entity);
            await _uow.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ApprovalRecord>> GetHistoryAsync(string bizType, string businessId)
        {
            var list = await _repo.FindAsync(x => x.BizType == bizType && x.BusinessId == businessId);
            return list.OrderByDescending(x => x.ActionTime).ToList();
        }
    }
}

