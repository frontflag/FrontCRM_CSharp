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

        public async Task<DraftConvertResultDto> ConvertDraftAsync(long userId, string draftId, string? actingRbacUserId = null)
        {
            var draft = await GetOwnedDraftOrThrowAsync(userId, draftId);
            if (draft.Status != 0) throw new InvalidOperationException("仅草稿状态可以转正式");

            var entityType = draft.EntityType.Trim().ToUpperInvariant();
            string entityId;
            if (entityType == "CUSTOMER")
            {
                var createReq = Deserialize<CreateCustomerRequest>(draft.PayloadJson, "客户草稿数据格式错误");
                var customer = await _customerService.CreateCustomerAsync(createReq, actingRbacUserId);
                entityId = customer.Id;

                // Draft payload 中可能包含 contacts，但 CreateCustomerAsync 不负责落库联系人。
                // 因此在草稿转正式时需要把 contacts 同步到正式客户联系人表。
                await SyncCustomerContactsFromDraftPayloadAsync(entityId, draft.PayloadJson);
            }
            else if (entityType == "VENDOR")
            {
                entityId = (await _vendorService.CreateAsync(
                    Deserialize<CreateVendorRequest>(draft.PayloadJson, "供应商草稿数据格式错误"),
                    actingRbacUserId)).Id;

                // 与客户类似：VendorService.CreateAsync 不会自动落库联系人；
                // 因此需要从草稿 payload 中同步 contacts。
                await SyncVendorContactsFromDraftPayloadAsync(entityId, draft.PayloadJson);
            }
            else if (entityType == "RFQ")
            {
                entityId = (await _rfqService.CreateAsync(
                    Deserialize<CreateRFQRequest>(draft.PayloadJson, "RFQ草稿数据格式错误"))).Id;
            }
            else
            {
                throw new NotSupportedException($"不支持的实体类型: {entityType}");
            }

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

        private async Task SyncCustomerContactsFromDraftPayloadAsync(string customerId, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return;
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            if (!doc.RootElement.TryGetProperty("contacts", out var contactsEl)) return;
            if (contactsEl.ValueKind != JsonValueKind.Array) return;

            var firstDefaultAssigned = false;

            foreach (var contactEl in contactsEl.EnumerateArray())
            {
                if (contactEl.ValueKind != JsonValueKind.Object) continue;

                string? contactName = null;
                if (contactEl.TryGetProperty("contactName", out var contactNameEl))
                    contactName = contactNameEl.GetString();
                else if (contactEl.TryGetProperty("name", out var nameEl))
                    contactName = nameEl.GetString();

                // 若联系人内容为空，跳过，避免插入空记录
                if (string.IsNullOrWhiteSpace(contactName)) continue;

                short? gender = null;
                if (contactEl.TryGetProperty("gender", out var genderEl) && genderEl.ValueKind != JsonValueKind.Null)
                {
                    if (genderEl.TryGetInt16(out var g16)) gender = g16;
                    else if (genderEl.TryGetInt32(out var g32)) gender = (short)g32;
                }

                string? department = contactEl.TryGetProperty("department", out var deptEl) ? deptEl.GetString() : null;
                string? position = contactEl.TryGetProperty("position", out var posEl) ? posEl.GetString() : null;
                string? email = contactEl.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
                string? fax = contactEl.TryGetProperty("fax", out var faxEl) ? faxEl.GetString() : null;

                // phone/tel 兼容
                string? phone = null;
                if (contactEl.TryGetProperty("phone", out var phoneEl)) phone = phoneEl.GetString();
                else if (contactEl.TryGetProperty("tel", out var telEl)) phone = telEl.GetString();

                // mobilePhone/mobile 兼容
                string? mobile = null;
                if (contactEl.TryGetProperty("mobilePhone", out var mobilePhoneEl)) mobile = mobilePhoneEl.GetString();
                else if (contactEl.TryGetProperty("mobile", out var mobileEl)) mobile = mobileEl.GetString();

                bool isDefault = false;
                if (contactEl.TryGetProperty("isDefault", out var isDefaultEl))
                {
                    if (isDefaultEl.ValueKind == JsonValueKind.True || isDefaultEl.ValueKind == JsonValueKind.False)
                        isDefault = isDefaultEl.GetBoolean();
                    else if (isDefaultEl.TryGetInt32(out var isDefaultInt))
                        isDefault = isDefaultInt != 0;
                }

                // 保证最多一个默认联系人（与前端同步逻辑一致：只保留第一个 isDefault=true）
                if (isDefault)
                {
                    if (firstDefaultAssigned) isDefault = false;
                    else firstDefaultAssigned = true;
                }

                var req = new AddContactRequest
                {
                    Name = contactName?.Trim(),
                    Gender = gender,
                    Department = department?.Trim(),
                    Position = position?.Trim(),
                    Phone = phone?.Trim(),
                    Mobile = mobile?.Trim(),
                    Email = email?.Trim(),
                    Fax = fax?.Trim(),
                    IsDefault = isDefault
                };

                await _customerService.AddContactAsync(customerId, req);
            }
        }

        private async Task SyncVendorContactsFromDraftPayloadAsync(string vendorId, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(vendorId)) return;
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            if (!doc.RootElement.TryGetProperty("contacts", out var contactsEl)) return;
            if (contactsEl.ValueKind != JsonValueKind.Array) return;

            var firstMainAssigned = false;

            foreach (var contactEl in contactsEl.EnumerateArray())
            {
                if (contactEl.ValueKind != JsonValueKind.Object) continue;

                // UI 字段：cName/title/department/mobile/tel/email/isMain/remark
                string? cName = contactEl.TryGetProperty("cName", out var cNameEl) ? cNameEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(cName)) continue;

                string? title = contactEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
                string? department = contactEl.TryGetProperty("department", out var deptEl) ? deptEl.GetString() : null;
                string? mobile = contactEl.TryGetProperty("mobile", out var mobileEl) ? mobileEl.GetString() : null;
                string? tel = contactEl.TryGetProperty("tel", out var telEl) ? telEl.GetString() : null;
                string? email = contactEl.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
                string? remark = contactEl.TryGetProperty("remark", out var remarkEl) ? remarkEl.GetString() : null;

                bool isMain = false;
                if (contactEl.TryGetProperty("isMain", out var isMainEl))
                {
                    if (isMainEl.ValueKind == JsonValueKind.True || isMainEl.ValueKind == JsonValueKind.False)
                        isMain = isMainEl.GetBoolean();
                    else if (isMainEl.TryGetInt32(out var isMainInt))
                        isMain = isMainInt != 0;
                }

                // 至多一个主联系人
                if (isMain)
                {
                    if (firstMainAssigned) isMain = false;
                    else firstMainAssigned = true;
                }

                var req = new AddVendorContactRequest
                {
                    CName = cName?.Trim(),
                    Title = title?.Trim(),
                    Department = department?.Trim(),
                    Mobile = mobile?.Trim(),
                    Tel = tel?.Trim(),
                    Email = email?.Trim(),
                    IsMain = isMain,
                    Remark = remark?.Trim()
                };

                await _vendorService.AddContactAsync(vendorId, req);
            }
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
