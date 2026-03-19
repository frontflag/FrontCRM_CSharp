using System.Text.Json;
using CRM.Core.Interfaces;
using CRM.Core.Models.Draft;

namespace CRM.Core.Services
{
    public class DraftService : IDraftService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IRepository<BizDraft> _draftRepository;
        private readonly ICustomerService _customerService;
        private readonly IVendorService _vendorService;
        private readonly IRFQService _rfqService;
        private readonly IUnitOfWork _unitOfWork;

        public DraftService(
            IRepository<BizDraft> draftRepository,
            ICustomerService customerService,
            IVendorService vendorService,
            IRFQService rfqService,
            IUnitOfWork unitOfWork)
        {
            _draftRepository = draftRepository;
            _customerService = customerService;
            _vendorService = vendorService;
            _rfqService = rfqService;
            _unitOfWork = unitOfWork;
        }

        public async Task<DraftDto> SaveDraftAsync(long userId, SaveDraftRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.EntityType)) throw new ArgumentException("EntityType不能为空");

            ValidatePayloadJson(request.PayloadJson);
            var entityType = request.EntityType.Trim().ToUpperInvariant();

            BizDraft draft;
            if (!string.IsNullOrWhiteSpace(request.DraftId))
            {
                draft = await GetOwnedDraftOrThrowAsync(userId, request.DraftId);
                draft.EntityType = entityType;
                draft.DraftName = request.DraftName?.Trim();
                draft.PayloadJson = request.PayloadJson;
                draft.Remark = request.Remark?.Trim();
                draft.ModifyTime = DateTime.UtcNow;
                await _draftRepository.UpdateAsync(draft);
            }
            else
            {
                draft = new BizDraft
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    EntityType = entityType,
                    DraftName = request.DraftName?.Trim(),
                    PayloadJson = request.PayloadJson,
                    Status = 0,
                    Remark = request.Remark?.Trim(),
                    CreateTime = DateTime.UtcNow
                };
                await _draftRepository.AddAsync(draft);
            }

            await _unitOfWork.SaveChangesAsync();
            return ToDto(draft);
        }

        public async Task<IReadOnlyList<DraftDto>> GetDraftsAsync(long userId, GetDraftsRequest request)
        {
            var all = await _draftRepository.FindAsync(d =>
                d.UserId == userId &&
                (string.IsNullOrWhiteSpace(request.EntityType) || d.EntityType == request.EntityType.Trim().ToUpperInvariant()) &&
                (!request.Status.HasValue || d.Status == request.Status.Value));

            var query = all.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLowerInvariant();
                query = query.Where(d =>
                    (!string.IsNullOrWhiteSpace(d.DraftName) && d.DraftName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(d.Remark) && d.Remark.ToLower().Contains(keyword)));
            }

            return query
                .OrderByDescending(d => d.ModifyTime ?? d.CreateTime)
                .Select(ToDto)
                .ToList();
        }

        public async Task<DraftDto?> GetDraftByIdAsync(long userId, string draftId)
        {
            var draft = await _draftRepository.GetByIdAsync(draftId);
            if (draft == null || draft.UserId != userId) return null;
            return ToDto(draft);
        }

        public async Task DeleteDraftAsync(long userId, string draftId)
        {
            var draft = await GetOwnedDraftOrThrowAsync(userId, draftId);
            await _draftRepository.DeleteAsync(draft.Id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<DraftConvertResultDto> ConvertDraftAsync(long userId, string draftId)
        {
            var draft = await GetOwnedDraftOrThrowAsync(userId, draftId);
            if (draft.Status != 0) throw new InvalidOperationException("仅草稿状态可以转正式");

            var entityType = draft.EntityType.Trim().ToUpperInvariant();
            string entityId = entityType switch
            {
                "CUSTOMER" => (await _customerService.CreateCustomerAsync(
                    Deserialize<CreateCustomerRequest>(draft.PayloadJson, "客户草稿数据格式错误"))).Id,
                "VENDOR" => (await _vendorService.CreateAsync(
                    Deserialize<CreateVendorRequest>(draft.PayloadJson, "供应商草稿数据格式错误"))).Id,
                "RFQ" => (await _rfqService.CreateAsync(
                    Deserialize<CreateRFQRequest>(draft.PayloadJson, "RFQ草稿数据格式错误"))).Id,
                _ => throw new NotSupportedException($"不支持的实体类型: {entityType}")
            };

            draft.Status = 1;
            draft.ConvertedEntityId = entityId;
            draft.ConvertedAt = DateTime.UtcNow;
            draft.ModifyTime = DateTime.UtcNow;
            await _draftRepository.UpdateAsync(draft);
            await _unitOfWork.SaveChangesAsync();

            return new DraftConvertResultDto
            {
                DraftId = draft.Id,
                EntityType = entityType,
                EntityId = entityId
            };
        }

        private static void ValidatePayloadJson(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new ArgumentException("PayloadJson不能为空");

            try
            {
                using var _ = JsonDocument.Parse(payloadJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"PayloadJson不是合法JSON: {ex.Message}");
            }
        }

        private static T Deserialize<T>(string json, string errorMessage) where T : class
        {
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (value == null) throw new ArgumentException(errorMessage);
            return value;
        }

        private async Task<BizDraft> GetOwnedDraftOrThrowAsync(long userId, string draftId)
        {
            if (string.IsNullOrWhiteSpace(draftId)) throw new ArgumentException("draftId不能为空");
            var draft = await _draftRepository.GetByIdAsync(draftId);
            if (draft == null || draft.UserId != userId)
                throw new KeyNotFoundException("草稿不存在");
            return draft;
        }

        private static DraftDto ToDto(BizDraft d) => new()
        {
            DraftId = d.Id,
            UserId = d.UserId,
            EntityType = d.EntityType,
            DraftName = d.DraftName,
            PayloadJson = d.PayloadJson,
            Status = d.Status,
            Remark = d.Remark,
            ConvertedEntityId = d.ConvertedEntityId,
            ConvertedAt = d.ConvertedAt,
            CreateTime = d.CreateTime,
            ModifyTime = d.ModifyTime
        };
    }
}
