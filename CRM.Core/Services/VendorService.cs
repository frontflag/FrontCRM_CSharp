using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.System;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// Vendor服务实现
    /// </summary>
    public partial class VendorService : IVendorService
    {
        private readonly IRepository<VendorInfo> _repository;
        private readonly IRepository<VendorContactInfo> _contactRepository;
        private readonly IRepository<VendorAddress> _addressRepository;
        private readonly IRepository<VendorBankInfo> _bankRepository;
        private readonly IRepository<FinancePaymentBank> _financePaymentBankRepository;
        private readonly IRepository<VendorContactHistory> _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUserService _userService;
        private readonly IVendorListQuery _vendorListQuery;
        private readonly ILogOperationAppendService _logOperationAppend;

        public VendorService(
            IRepository<VendorInfo> repository,
            IRepository<VendorContactInfo> contactRepository,
            IRepository<VendorAddress> addressRepository,
            IRepository<VendorBankInfo> bankRepository,
            IRepository<FinancePaymentBank> financePaymentBankRepository,
            IRepository<VendorContactHistory> historyRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IDataPermissionService dataPermissionService,
            IUserService userService,
            IVendorListQuery vendorListQuery,
            ILogOperationAppendService logOperationAppend)
        {
            _repository = repository;
            _contactRepository = contactRepository;
            _addressRepository = addressRepository;
            _bankRepository = bankRepository;
            _financePaymentBankRepository = financePaymentBankRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _dataPermissionService = dataPermissionService;
            _userService = userService;
            _vendorListQuery = vendorListQuery;
            _logOperationAppend = logOperationAppend;
        }

        /// <summary>前端未传采购员姓名时，用归属用户（RBAC Id）的姓名/账号填充展示字段。</summary>
        private async Task TryFillPurchaserNameFromUserAsync(VendorInfo entity, string? preferredRbacUserId)
        {
            if (!string.IsNullOrWhiteSpace(entity.PurchaserName)) return;
            var uid = ActingUserIdNormalizer.Normalize(preferredRbacUserId) ?? entity.PurchaseUserId;
            if (string.IsNullOrWhiteSpace(uid)) return;
            var u = await _userService.GetByIdAsync(uid);
            if (u == null) return;
            if (!string.IsNullOrWhiteSpace(u.RealName))
                entity.PurchaserName = u.RealName.Trim();
            else if (!string.IsNullOrWhiteSpace(u.UserName))
                entity.PurchaserName = u.UserName.Trim();
        }

        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''");

        /// <summary>
        /// 按主键 Id 或业务编号 Code 解析供应商（路由、列表可能传任一种）。
        /// </summary>
        private async Task<VendorInfo?> ResolveVendorByIdOrCodeAsync(string idOrCode)
        {
            if (string.IsNullOrWhiteSpace(idOrCode)) return null;
            var byId = (await _repository.FindAsync(e => e.Id == idOrCode)).FirstOrDefault();
            if (byId != null) return byId;
            return (await _repository.FindAsync(e => e.Code == idOrCode)).FirstOrDefault();
        }

        /// <summary>
        /// 创建
        /// </summary>
        public async Task<VendorInfo> CreateAsync(CreateVendorRequest request, string? actingUserId = null)
        {
            // 若前端未传入编号，则自动生成
            if (string.IsNullOrWhiteSpace(request.Code))
                request.Code = await _serialNumberService.GenerateNextAsync(ModuleCodes.Vendor);

            var official = (request.Name ?? request.OfficialName)?.Trim();
            var tax = request.CreditCode ?? request.TaxNumber;
            var currency = request.TradeCurrency ?? request.Currency;

            var entity = new VendorInfo
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false,
                Code = request.Code.Trim(),
                OfficialName = string.IsNullOrEmpty(official) ? null : official,
                EnglishOfficialName = string.IsNullOrWhiteSpace(request.EnglishOfficialName)
                    ? null
                    : request.EnglishOfficialName.Trim(),
                NickName = string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim(),
                Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim(),
                Level = VendorLevelCodes.NormalizeOrDefault(request.Level),
                Credit = request.Credit,
                Status = request.Status ?? 1,
                OfficeAddress = string.IsNullOrWhiteSpace(request.OfficeAddress) ? null : request.OfficeAddress.Trim(),
                Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
                PurchaserName = string.IsNullOrWhiteSpace(request.PurchaserName) ? null : request.PurchaserName.Trim(),
                TradeCurrency = currency,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim(),
                Payment = request.PaymentDays,
                CreditCode = string.IsNullOrWhiteSpace(tax) ? null : tax.Trim(),
                DUNS = string.IsNullOrWhiteSpace(request.Duns) ? null : request.Duns.Trim(),
                CompanyEmailSuffix = await ResolveUniqueCompanyEmailSuffixAsync(request.CompanyEmailSuffix, null),
                CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim(),
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            // 采购数据权限按 PurchaseUserId 过滤（PurchaseDataScope=1 仅本人）；未写入则列表不可见
            var ownerId = ActingUserIdNormalizer.Normalize(actingUserId);
            if (!string.IsNullOrWhiteSpace(ownerId))
                entity.PurchaseUserId = ownerId;

            await TryFillPurchaserNameFromUserAsync(entity, ownerId);

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        /// <inheritdoc />
        public async Task<VendorImportBatchResult> ImportVendorsBatchAsync(VendorImportBatchRequest request, string? actingUserId = null)
        {
            var result = new VendorImportBatchResult();
            if (request.Items == null || request.Items.Count == 0)
                return result;

            var index = 0;
            foreach (var item in request.Items)
            {
                index++;
                try
                {
                    var vreq = item.Vendor ?? new CreateVendorRequest();
                    var official = (vreq.Name ?? vreq.OfficialName)?.Trim();
                    if (string.IsNullOrWhiteSpace(official))
                        throw new InvalidOperationException("供应商名称不能为空");

                    var vendor = await CreateAsync(vreq, actingUserId);
                    var contacts = item.Contacts ?? new List<AddVendorContactRequest>();
                    var anyMain = contacts.Any(c => c != null && c.IsMain);
                    var added = 0;
                    for (var i = 0; i < contacts.Count; i++)
                    {
                        var cr = contacts[i];
                        if (cr == null) continue;
                        var (resolvedCName, resolvedEName) = ContactNameResolver.ResolveForCreate(
                            cr.CName, cr.EName);
                        var mobile = cr.Mobile?.Trim();
                        var tel = cr.Tel?.Trim();
                        if (string.IsNullOrWhiteSpace(resolvedCName) && string.IsNullOrWhiteSpace(resolvedEName)
                            && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(tel))
                            continue;

                        cr.CName = resolvedCName;
                        cr.EName = resolvedEName;

                        if (!anyMain && added == 0)
                            cr.IsMain = true;

                        await AddContactAsync(vendor.Id, cr);
                        added++;
                    }

                    result.Items.Add(new VendorImportItemResult
                    {
                        Index = index,
                        Success = true,
                        VendorCode = vendor.Code,
                        VendorId = vendor.Id
                    });
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Items.Add(new VendorImportItemResult
                    {
                        Index = index,
                        Success = false,
                        Error = ex.Message
                    });
                    result.FailCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// 根据ID获取（含联系人）
        /// </summary>
        public async Task<VendorInfo?> GetByIdAsync(string id)
        {
            var vendor = await ResolveVendorByIdOrCodeAsync(id);
            if (vendor == null) return null;
            var contacts = await _contactRepository.FindAsync(c => c.VendorId == vendor.Id);
            vendor.Contacts = contacts.ToList();
            var banks = (await _bankRepository.FindAsync(b => b.VendorId == vendor.Id)).ToList();
            await EnrichVendorBankPaymentBankNamesAsync(banks);
            vendor.BankAccounts = banks;
            await TryFillPurchaseUserDisplayNameAsync(vendor);
            return vendor;
        }

        /// <summary>详情/列表展示：采购员登录账号（优先于 PurchaserName 冗余姓名）。</summary>
        private async Task TryFillPurchaseUserDisplayNameAsync(VendorInfo entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PurchaseUserId)) return;
            var u = await _userService.GetByIdAsync(entity.PurchaseUserId.Trim());
            if (u == null) return;
            entity.PurchaseUserName = EntityLookupService.FormatUserLoginName(u)
                ?? EntityLookupService.FormatUserDisplayName(u);
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        public async Task<IEnumerable<VendorInfo>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Where(e => !e.IsDeleted);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public Task<PagedResult<VendorInfo>> GetPagedAsync(VendorQueryRequest request) =>
            _vendorListQuery.GetVendorsPagedAsync(request, default);

        public Task<PagedResult<VendorInfo>> GetBlacklistAsync(VendorQueryRequest request) =>
            _vendorListQuery.GetBlacklistVendorsPagedAsync(
                request.PageIndex,
                request.PageSize,
                request.Keyword,
                request.CurrentUserId,
                default);

        public Task<PagedResult<VendorInfo>> GetFrozenAsync(VendorQueryRequest request) =>
            _vendorListQuery.GetFrozenVendorsPagedAsync(
                request.PageIndex,
                request.PageSize,
                request.Keyword,
                request.CurrentUserId,
                default);

        /// <summary>
        /// 更新
        /// </summary>
        public async Task<VendorInfo> UpdateAsync(string id, UpdateVendorRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            var headerBefore = CaptureVendorHeaderSnapshot(entity);

            if (request.Name != null)
                entity.OfficialName = request.Name.Trim();
            if (request.NickName != null)
                entity.NickName = string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim();
            if (request.Industry != null)
                entity.Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim();
            if (request.Product != null)
                entity.Product = string.IsNullOrWhiteSpace(request.Product) ? null : request.Product.Trim();
            if (request.Credit.HasValue)
                entity.Credit = request.Credit.Value;
            if (request.Status.HasValue)
                entity.Status = request.Status.Value;
            if (request.OfficeAddress != null)
                entity.OfficeAddress = string.IsNullOrWhiteSpace(request.OfficeAddress) ? null : request.OfficeAddress.Trim();
            if (request.Website != null)
                entity.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim();
            if (request.PurchaserName != null)
                entity.PurchaserName = string.IsNullOrWhiteSpace(request.PurchaserName) ? null : request.PurchaserName.Trim();
            if (request.Level.HasValue)
                entity.Level = VendorLevelCodes.NormalizeOrDefault(request.Level);
            if (request.TradeCurrency.HasValue)
                entity.TradeCurrency = request.TradeCurrency.Value;
            if (request.PaymentMethod != null)
                entity.PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim();
            var paymentDays = request.PaymentDays ?? request.Payment;
            if (paymentDays.HasValue)
                entity.Payment = paymentDays.Value;
            if (request.CreditCode != null)
                entity.CreditCode = string.IsNullOrWhiteSpace(request.CreditCode) ? null : request.CreditCode.Trim();
            if (request.Duns != null)
                entity.DUNS = string.IsNullOrWhiteSpace(request.Duns) ? null : request.Duns.Trim();
            if (request.CompanyEmailSuffix != null)
                entity.CompanyEmailSuffix = await ResolveUniqueCompanyEmailSuffixAsync(request.CompanyEmailSuffix, entity.Id);
            if (request.CompanyInfo != null)
                entity.CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim();
            if (request.Remark != null)
                entity.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
            if (request.ExternalNumber != null)
                entity.ExternalNumber = request.ExternalNumber.Trim();
            if (request.EnglishOfficialName != null)
                entity.EnglishOfficialName = string.IsNullOrWhiteSpace(request.EnglishOfficialName)
                    ? null
                    : request.EnglishOfficialName.Trim();

            // 历史数据/草稿首转正式时可能未带归属采购员，补写以便数据权限可见
            var uid = ActingUserIdNormalizer.Normalize(actingUserId);
            if (!string.IsNullOrWhiteSpace(uid) && string.IsNullOrWhiteSpace(entity.PurchaseUserId))
                entity.PurchaseUserId = uid;

            await TryFillPurchaserNameFromUserAsync(entity, uid);

            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorHeaderFieldChangesAsync(entity, headerBefore, actingUserId);
            return entity;
        }

        public async Task ApplyLevelIfChangedAsync(string vendorId, short? level, string? actingUserId)
        {
            if (!level.HasValue) return;
            var id = (vendorId ?? string.Empty).Trim();
            if (id.Length == 0) return;

            var entity = (await _repository.FindAsync(e => e.Id == id)).FirstOrDefault()
                ?? throw new ArgumentException($"供应商不存在：{id}");

            var next = VendorLevelCodes.NormalizeOrDefault(level);
            if (entity.Level == next) return;

            var headerBefore = CaptureVendorHeaderSnapshot(entity);
            entity.Level = next;
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _repository.UpdateAsync(entity);
            await LogVendorHeaderFieldChangesAsync(entity, headerBefore, actingUserId);
        }

        /// <summary>
        /// 删除
        /// </summary>
        public async Task DeleteAsync(string id, string? reason = null, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.IsDeleted = true;
            entity.DeleteTime = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(reason))
                entity.DeleteReason = reason.Trim();
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.Vendor,
                RecordId = entity.Id,
                RecordCode = entity.Code,
                EntityDisplayName = DeleteLogEntityNames.Vendor,
                ActionTypeOverride = OperationLogActionTypes.GenericDelete,
                OperatorUserId = actorId,
                OperatorUserName = actorName,
                Reason = reason,
                OperationDescOverride = $"删除供应商，理由：{reason ?? "无"}"
            });
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        public async Task BatchDeleteAsync(IEnumerable<string> ids, string? reason = null)
        {
            if (ids == null || !ids.Any()) return;

            foreach (var id in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                try { await DeleteAsync(id, reason); }
                catch (KeyNotFoundException) { }
            }
        }

        public Task<PagedResult<VendorInfo>> GetDeletedAsync(int pageIndex, int pageSize, string? keyword, string? currentUserId = null) =>
            _vendorListQuery.GetDeletedVendorsPagedAsync(pageIndex, pageSize, keyword, currentUserId, default);

        public async Task RestoreAsync(string id, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var trimmed = id.Trim();
            var entity =
                (await _repository.FindIgnoreFiltersAsync(e => e.Id == trimmed)).FirstOrDefault()
                ?? (await _repository.FindIgnoreFiltersAsync(e => e.Code == trimmed)).FirstOrDefault();
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.IsDeleted = false;
            entity.DeleteTime = null;
            entity.DeleteReason = null;
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
            await AddOperationLogAsync(entity.Id, "恢复", "供应商已从回收站恢复", actorId, actorName);
        }

        public async Task AddToBlacklistAsync(string id, string? reason, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.BlackList = true;
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            var r = string.IsNullOrWhiteSpace(reason) ? "无" : reason.Trim();
            var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
            await AddOperationLogAsync(entity.Id, "加入黑名单", $"加入黑名单，理由：{r}", actorId, actorName);
        }

        public async Task RemoveFromBlacklistAsync(string id, string reason, string? operatorUserId, string? operatorUserName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("移出黑名单原因不能为空", nameof(reason));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.BlackList = false;
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(operatorUserId);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            var r = reason.Trim();
            await AddOperationLogAsync(entity.Id, "移出黑名单", $"移出黑名单，原因：{r}", operatorUserId, operatorUserName, r);
        }

        private static void ValidateVendorStatusTransition(short current, short target)
        {
            if (current == target) return;
            var ok = current switch
            {
                1 => target is 2,                 // 新建 -> 待审核
                2 => target is 10 or -1,          // 待审核 -> 已审核 / 审核失败
                -1 => target is 1 or 2,           // 审核失败 -> 新建 / 再次提交待审核
                10 => target is 12 or 20,         // 已审核 -> 待财务审核 / 财务建档
                12 => target is 20,               // 待财务审核 -> 财务建档
                _ => false
            };
            if (!ok) throw new InvalidOperationException($"不允许的供应商状态流转: {current} -> {target}");
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public async Task UpdateStatusAsync(string id, short status, string? auditRemark = null, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            var previousStatus = entity.Status;
            ValidateVendorStatusTransition(entity.Status, status);
            entity.Status = status;
            if (status == -1)
            {
                if (string.IsNullOrWhiteSpace(auditRemark))
                    throw new ArgumentException("审核拒绝时必须填写原因", nameof(auditRemark));
                entity.AuditRemark = auditRemark.Trim();
            }
            else if (status == 10)
            {
                entity.AuditRemark = null;
            }
            else if (status == 2 && previousStatus == -1)
            {
                entity.AuditRemark = null;
            }
            entity.ModifyTime = DateTime.UtcNow;
            entity.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorStatusFieldChangeAsync(entity, previousStatus, status, actingUserId);
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public async Task<IEnumerable<VendorInfo>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            var allEntities = await _repository.GetAllAsync();
            var searchTerm = keyword.Trim().ToLower();
            return allEntities.Where(e =>
                (e.Code != null && e.Code.ToLower().Contains(searchTerm)) ||
                (e.OfficialName != null && e.OfficialName.ToLower().Contains(searchTerm)) ||
                (e.NickName != null && e.NickName.ToLower().Contains(searchTerm)) ||
                (e.EnglishOfficialName != null && e.EnglishOfficialName.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// 获取供应商联系人列表
        /// </summary>
        public async Task<IEnumerable<VendorContactInfo>> GetContactsByVendorIdAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId)) return new List<VendorContactInfo>();
            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            if (vendor == null) return new List<VendorContactInfo>();
            var list = await _contactRepository.FindAsync(c => c.VendorId == vendor.Id);
            return list.OrderByDescending(c => c.IsMain).ThenBy(c => c.CName).ToList();
        }

        /// <summary>
        /// 添加供应商联系人
        /// </summary>
        public async Task<VendorContactInfo> AddContactAsync(string vendorId, AddVendorContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(vendorId));

            var vendor = await GetByIdAsync(vendorId);
            if (vendor == null)
                throw new KeyNotFoundException($"找不到ID为 '{vendorId}' 的供应商");

            var (cName, eName) = ContactNameResolver.ResolveForCreate(
                request.CName, request.EName);

            var contact = new VendorContactInfo
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = vendor.Id,
                CName = cName,
                EName = eName,
                Gender = NormalizeContactGender(request.Gender),
                Title = request.Title?.Trim(),
                Department = request.Department?.Trim(),
                Mobile = request.Mobile?.Trim(),
                Tel = request.Tel?.Trim(),
                Email = request.Email?.Trim(),
                IsMain = request.IsMain,
                Remark = request.Remark?.Trim(),
                CreateTime = DateTime.UtcNow
            };

            await _contactRepository.AddAsync(contact);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorContactAddedAsync(contact, null);
            return contact;
        }

        /// <summary>
        /// 更新供应商联系人
        /// </summary>
        public async Task<VendorContactInfo> UpdateContactAsync(string contactId, UpdateVendorContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var list = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = list.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            var contactBefore = CaptureVendorContactSnapshot(contact);

            if (request.CName != null || request.EName != null)
            {
                var (cName, eName) = ContactNameResolver.ResolveForUpdate(
                    contact.CName,
                    contact.EName,
                    request.CName,
                    request.EName);
                contact.CName = cName;
                contact.EName = eName;
            }

            if (request.Title != null) contact.Title = request.Title.Trim();
            if (request.Department != null) contact.Department = request.Department.Trim();
            if (request.Mobile != null) contact.Mobile = request.Mobile.Trim();
            if (request.Tel != null) contact.Tel = request.Tel.Trim();
            if (request.Email != null) contact.Email = request.Email.Trim();
            if (request.Gender.HasValue) contact.Gender = NormalizeContactGender(request.Gender);
            if (request.IsMain.HasValue) contact.IsMain = request.IsMain.Value;
            if (request.Remark != null) contact.Remark = request.Remark.Trim();

            contact.ModifyTime = DateTime.UtcNow;
            await _contactRepository.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorContactFieldChangesAsync(contact, contactBefore, null);
            return contact;
        }

        /// <summary>
        /// 删除供应商联系人
        /// </summary>
        public async Task DeleteContactAsync(string contactId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var list = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = list.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            await _contactRepository.DeleteAsync(contact.Id);
            await _unitOfWork.SaveChangesAsync();
            await AppendSubEntityDeleteLogAsync(
                BusinessLogTypes.VendorContact,
                contact.Id,
                string.IsNullOrWhiteSpace(contact.CName) ? contact.EName : contact.CName,
                DeleteLogEntityNames.VendorContact,
                contact.VendorId,
                actingUserId);
        }

        public async Task SetMainContactAsync(string contactId)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var list = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = list.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            // 取消同一供应商下其他主联系人
            var allContacts = await _contactRepository.FindAsync(c => c.VendorId == contact.VendorId);
            foreach (var c in allContacts)
            {
                c.IsMain = c.Id == contactId;
                await _contactRepository.UpdateAsync(c);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<VendorAddress>> GetAddressesByVendorIdAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId)) return new List<VendorAddress>();
            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            if (vendor == null) return new List<VendorAddress>();
            var list = await _addressRepository.FindAsync(a => a.VendorId == vendor.Id);
            return list.OrderByDescending(a => a.IsDefault).ThenBy(a => a.AddressType).ToList();
        }

        private async Task ResetDefaultAddressAsync(string vendorId, short addressType)
        {
            var addresses = await _addressRepository.FindAsync(a =>
                a.VendorId == vendorId &&
                a.AddressType == addressType &&
                a.IsDefault);

            foreach (var addr in addresses)
            {
                addr.IsDefault = false;
                addr.ModifyTime = DateTime.UtcNow;
                await _addressRepository.UpdateAsync(addr);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<VendorAddress> AddAddressAsync(string vendorId, AddVendorAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(vendorId));

            var vendor = await GetByIdAsync(vendorId);
            if (vendor == null)
                throw new KeyNotFoundException($"找不到ID为 '{vendorId}' 的供应商");

            if (request.IsDefault)
            {
                await ResetDefaultAddressAsync(vendor.Id, request.AddressType);
            }

            var address = new VendorAddress
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = vendor.Id,
                AddressType = request.AddressType,
                Country = request.Country,
                Province = request.Province?.Trim(),
                City = request.City?.Trim(),
                Area = request.Area?.Trim(),
                Address = request.Address?.Trim(),
                ContactName = request.ContactName?.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                IsDefault = request.IsDefault,
                CreateTime = DateTime.UtcNow
            };

            await _addressRepository.AddAsync(address);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorAddressAddedAsync(address, null);
            return address;
        }

        public async Task<VendorAddress> UpdateAddressAsync(string addressId, UpdateVendorAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var list = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = list.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            var addressBefore = CaptureVendorAddressSnapshot(address);

            if (request.IsDefault == true && !address.IsDefault)
            {
                await ResetDefaultAddressAsync(address.VendorId, request.AddressType ?? address.AddressType);
            }

            if (request.AddressType.HasValue)
                address.AddressType = request.AddressType.Value;
            if (request.Country.HasValue)
                address.Country = request.Country.Value;
            if (request.Province != null)
                address.Province = request.Province.Trim();
            if (request.City != null)
                address.City = request.City.Trim();
            if (request.Area != null)
                address.Area = request.Area.Trim();
            if (request.Address != null)
                address.Address = request.Address.Trim();
            if (request.ContactName != null)
                address.ContactName = request.ContactName.Trim();
            if (request.ContactPhone != null)
                address.ContactPhone = request.ContactPhone.Trim();
            if (request.IsDefault.HasValue)
                address.IsDefault = request.IsDefault.Value;

            address.ModifyTime = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorAddressFieldChangesAsync(address, addressBefore, null);
            return address;
        }

        public async Task DeleteAddressAsync(string addressId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var list = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = list.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            await _addressRepository.DeleteAsync(address.Id);
            await _unitOfWork.SaveChangesAsync();
            await AppendSubEntityDeleteLogAsync(
                BusinessLogTypes.VendorAddress,
                address.Id,
                null,
                DeleteLogEntityNames.VendorAddress,
                address.VendorId,
                actingUserId);
        }

        public async Task SetDefaultAddressAsync(string addressId)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var list = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = list.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            await ResetDefaultAddressAsync(address.VendorId, address.AddressType);

            address.IsDefault = true;
            address.ModifyTime = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<VendorBankInfo>> GetBanksByVendorIdAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId)) return new List<VendorBankInfo>();
            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            if (vendor == null) return new List<VendorBankInfo>();
            var list = (await _bankRepository.FindAsync(b => b.VendorId == vendor.Id)).ToList();
            await EnrichVendorBankPaymentBankNamesAsync(list);
            return list.OrderByDescending(b => b.IsDefault).ThenBy(b => b.FinancePaymentBankId).ToList();
        }

        /// <summary>按 <see cref="VendorBankInfo.FinancePaymentBankId"/> 从 financepaymentbank 同步展示用 BankName（只读冗余）。</summary>
        private async Task EnrichVendorBankPaymentBankNamesAsync(IList<VendorBankInfo> banks)
        {
            if (banks.Count == 0) return;
            var ids = banks
                .Select(b => b.FinancePaymentBankId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;

            var paymentBanks = await _financePaymentBankRepository.FindAsync(b => ids.Contains(b.Id));
            var nameById = paymentBanks.ToDictionary(b => b.Id, b => b.BankName.Trim(), StringComparer.OrdinalIgnoreCase);
            foreach (var bank in banks)
            {
                if (string.IsNullOrWhiteSpace(bank.FinancePaymentBankId)) continue;
                var key = bank.FinancePaymentBankId.Trim();
                if (nameById.TryGetValue(key, out var name))
#pragma warning disable CS0618
                    bank.BankName = name;
#pragma warning restore CS0618
            }
        }

        private static string NormalizeVendorBankAccountType(string? accountType, short? currency)
        {
            var normalized = accountType?.Trim().ToLowerInvariant();
            if (normalized is "rmb" or "foreign")
                return normalized;
            return currency == 1 ? "rmb" : "foreign";
        }

        private static string NormalizeVendorBankPurposeType(string? purposeType)
        {
            var normalized = purposeType?.Trim().ToLowerInvariant();
            return normalized is "payment" or "receipt" ? normalized : "payment";
        }

        private async Task ResetDefaultBankAsync(string vendorId)
        {
            var list = await _bankRepository.FindAsync(b => b.VendorId == vendorId && b.IsDefault);
            foreach (var bank in list)
            {
                bank.IsDefault = false;
                bank.ModifyTime = DateTime.UtcNow;
                await _bankRepository.UpdateAsync(bank);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task ApplyVendorBankPaymentBankLinkAsync(VendorBankInfo bank, string? financePaymentBankId)
        {
            if (string.IsNullOrWhiteSpace(financePaymentBankId))
            {
                bank.FinancePaymentBankId = null;
#pragma warning disable CS0618
                bank.BankName = null;
#pragma warning restore CS0618
                return;
            }

            var id = financePaymentBankId.Trim();
            var paymentBank = await _financePaymentBankRepository.GetByIdAsync(id);
            if (paymentBank == null || paymentBank.IsDisabled)
                throw new InvalidOperationException("所选付款银行无效或已禁用，请在「财务参数-付款银行」中维护");
            bank.FinancePaymentBankId = id;
#pragma warning disable CS0618
            bank.BankName = paymentBank.BankName.Trim();
#pragma warning restore CS0618
        }

        public async Task<VendorBankInfo> AddBankAsync(string vendorId, AddVendorBankRequest request)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(vendorId));

            var vendor = await GetByIdAsync(vendorId);
            if (vendor == null)
                throw new KeyNotFoundException($"找不到ID为 '{vendorId}' 的供应商");

            if (request.IsDefault)
                await ResetDefaultBankAsync(vendor.Id);

            var bank = new VendorBankInfo
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = vendor.Id,
                BankAccount = request.BankAccount?.Trim(),
                AccountName = request.AccountName?.Trim(),
                BankBranch = request.BankBranch?.Trim(),
                BankAddress = request.BankAddress?.Trim(),
                Swift = request.Swift?.Trim(),
                Iban = request.Iban?.Trim(),
                BankCode = request.BankCode?.Trim(),
                Country = request.Country?.Trim(),
                AccountType = NormalizeVendorBankAccountType(request.AccountType, request.Currency),
                PurposeType = NormalizeVendorBankPurposeType(request.PurposeType),
                Currency = request.Currency,
                IsDefault = request.IsDefault,
                IsEnabled = request.IsEnabled,
                Remark = request.Remark?.Trim(),
                CreateTime = DateTime.UtcNow
            };
            await ApplyVendorBankPaymentBankLinkAsync(bank, request.FinancePaymentBankId);

            await _bankRepository.AddAsync(bank);
            await _unitOfWork.SaveChangesAsync();
            await EnrichVendorBankPaymentBankNamesAsync(new List<VendorBankInfo> { bank });
            await LogVendorBankAddedAsync(bank, null);
            return bank;
        }

        public async Task<VendorBankInfo> UpdateBankAsync(string bankId, UpdateVendorBankRequest request)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var list = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = list.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行账户");

            var bankBefore = CaptureVendorBankSnapshot(bank);

            if (request.IsDefault == true && !bank.IsDefault)
                await ResetDefaultBankAsync(bank.VendorId);

            if (request.FinancePaymentBankId != null)
                await ApplyVendorBankPaymentBankLinkAsync(bank, request.FinancePaymentBankId);

            if (request.BankAccount != null) bank.BankAccount = request.BankAccount.Trim();
            if (request.AccountName != null) bank.AccountName = request.AccountName.Trim();
            if (request.BankBranch != null) bank.BankBranch = request.BankBranch.Trim();
            if (request.BankAddress != null) bank.BankAddress = request.BankAddress.Trim();
            if (request.Swift != null) bank.Swift = request.Swift.Trim();
            if (request.Iban != null) bank.Iban = request.Iban.Trim();
            if (request.BankCode != null) bank.BankCode = request.BankCode.Trim();
            if (request.Country != null) bank.Country = request.Country.Trim();
            if (request.AccountType != null)
                bank.AccountType = NormalizeVendorBankAccountType(request.AccountType, request.Currency ?? bank.Currency);
            else if (request.Currency.HasValue)
                bank.AccountType = NormalizeVendorBankAccountType(null, request.Currency.Value);
            if (request.PurposeType != null) bank.PurposeType = NormalizeVendorBankPurposeType(request.PurposeType);
            if (request.Currency.HasValue) bank.Currency = request.Currency.Value;
            if (request.IsDefault.HasValue) bank.IsDefault = request.IsDefault.Value;
            if (request.IsEnabled.HasValue) bank.IsEnabled = request.IsEnabled.Value;
            if (request.Remark != null) bank.Remark = request.Remark.Trim();

            bank.ModifyTime = DateTime.UtcNow;
            await _bankRepository.UpdateAsync(bank);
            await _unitOfWork.SaveChangesAsync();
            await EnrichVendorBankPaymentBankNamesAsync(new List<VendorBankInfo> { bank });
            await LogVendorBankFieldChangesAsync(bank, bankBefore, null);
            return bank;
        }

        public async Task DeleteBankAsync(string bankId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var list = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = list.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行账户");

            await _bankRepository.DeleteAsync(bank.Id);
            await _unitOfWork.SaveChangesAsync();
            await AppendSubEntityDeleteLogAsync(
                BusinessLogTypes.VendorBank,
                bank.Id,
                bank.BankName,
                DeleteLogEntityNames.VendorBank,
                bank.VendorId,
                actingUserId);
        }

        public async Task SetDefaultBankAsync(string bankId)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var list = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = list.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行账户");

            await ResetDefaultBankAsync(bank.VendorId);

            bank.IsDefault = true;
            bank.ModifyTime = DateTime.UtcNow;
            await _bankRepository.UpdateAsync(bank);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<VendorContactHistory>> GetContactHistoryAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Enumerable.Empty<VendorContactHistory>();

            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            if (vendor == null) return Enumerable.Empty<VendorContactHistory>();

            var list = await _historyRepository.FindAsync(h => h.VendorId == vendor.Id);
            return list
                .OrderByDescending(h => h.Time)
                .ThenByDescending(h => h.CreateTime)
                .ToList();
        }

        public async Task<VendorContactHistory> AddContactHistoryAsync(string vendorId, AddVendorContactHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(vendorId));

            var vendor = await GetByIdAsync(vendorId);
            if (vendor == null)
                throw new KeyNotFoundException($"找不到ID为 '{vendorId}' 的供应商");

            var record = new VendorContactHistory
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = vendor.Id,
                Type = request.Type ?? "call",
                Subject = request.Subject?.Trim(),
                Content = request.Content?.Trim(),
                ContactPerson = request.ContactPerson?.Trim(),
                Time = request.Time.HasValue ? PostgreSqlDateTime.ToUtc(request.Time.Value) : DateTime.UtcNow,
                NextFollowUpTime = PostgreSqlDateTime.ToUtc(request.NextFollowUpTime),
                Result = request.Result?.Trim(),
                CreateTime = DateTime.UtcNow
            };

            await _historyRepository.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorContactHistoryAddedAsync(record, null);
            return record;
        }

        public async Task<VendorContactHistory> UpdateContactHistoryAsync(string historyId, UpdateVendorContactHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(historyId))
                throw new ArgumentException("记录ID不能为空", nameof(historyId));

            var list = await _historyRepository.FindAsync(h => h.Id == historyId);
            var record = list.FirstOrDefault();
            if (record == null)
                throw new KeyNotFoundException($"找不到ID为 '{historyId}' 的联系记录");

            var historyBefore = CaptureVendorContactHistorySnapshot(record);

            if (request.Type != null) record.Type = request.Type.Trim();
            if (request.Subject != null) record.Subject = request.Subject.Trim();
            if (request.Content != null) record.Content = request.Content.Trim();
            if (request.ContactPerson != null) record.ContactPerson = request.ContactPerson.Trim();
            if (request.Time.HasValue) record.Time = PostgreSqlDateTime.ToUtc(request.Time.Value);
            if (request.NextFollowUpTime.HasValue) record.NextFollowUpTime = PostgreSqlDateTime.ToUtc(request.NextFollowUpTime.Value);
            if (request.Result != null) record.Result = request.Result.Trim();

            record.ModifyTime = DateTime.UtcNow;
            await _historyRepository.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
            await LogVendorContactHistoryFieldChangesAsync(record, historyBefore, null);
            return record;
        }

        public async Task DeleteContactHistoryAsync(string historyId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(historyId))
                throw new ArgumentException("记录ID不能为空", nameof(historyId));

            var list = await _historyRepository.FindAsync(h => h.Id == historyId);
            var record = list.FirstOrDefault();
            if (record == null)
                throw new KeyNotFoundException($"找不到ID为 '{historyId}' 的联系记录");

            await _historyRepository.DeleteAsync(record.Id);
            await _unitOfWork.SaveChangesAsync();
            await AppendSubEntityDeleteLogAsync(
                BusinessLogTypes.VendorContactHistory,
                record.Id,
                null,
                DeleteLogEntityNames.VendorContactHistory,
                record.VendorId,
                actingUserId);
        }

        public async Task<IEnumerable<VendorOperationLog>> GetOperationLogsAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Enumerable.Empty<VendorOperationLog>();

            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            var effectiveId = vendor?.Id ?? vendorId;

            var safeId = effectiveId.Replace("'", "''");
            var sql = $@"
SELECT o.""Id"",
       o.""RecordId"" AS ""VendorId"",
       o.""ActionType"" AS ""OperationType"",
       o.""OperationDesc"",
       o.""OperatorUserId"",
       o.""OperatorUserName"",
       o.""OperationTime"",
       o.""Reason"" AS ""Remark"",
       o.""BizType"",
       o.""RecordCode""
FROM log_operation o
WHERE (o.""BizType"" = '{BusinessLogTypes.Vendor}' AND o.""RecordId"" = '{safeId}')
   OR (o.""BizType"" = '{BusinessLogTypes.VendorContact}' AND o.""RecordId"" IN (
        SELECT ""ContactId"" FROM vendorcontactinfo WHERE ""VendorId"" = '{safeId}'
      ))
ORDER BY o.""OperationTime"" DESC";
            return await _unitOfWork.QueryAsync<VendorOperationLog>(sql);
        }

        public async Task<IEnumerable<VendorChangeLog>> GetChangeLogsAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Enumerable.Empty<VendorChangeLog>();

            var vendor = await ResolveVendorByIdOrCodeAsync(vendorId);
            var effectiveId = vendor?.Id ?? vendorId;

            var safeId = effectiveId.Replace("'", "''");
            var sql = $@"
SELECT c.""Id"",
       c.""RecordId"" AS ""VendorId"",
       c.""FieldName"",
       c.""FieldLabel"",
       c.""OldValue"",
       c.""NewValue"",
       c.""ChangedByUserId"",
       c.""ChangedByUserName"",
       c.""ChangedAt"",
       c.""BizType"",
       c.""RecordCode""
FROM log_change_fldval c
WHERE (c.""BizType"" = '{BusinessLogTypes.Vendor}' AND c.""RecordId"" = '{safeId}')
   OR (c.""BizType"" = '{BusinessLogTypes.VendorContact}' AND c.""RecordId"" IN (
        SELECT ""ContactId"" FROM vendorcontactinfo WHERE ""VendorId"" = '{safeId}'
      ))
   OR (c.""BizType"" = '{BusinessLogTypes.VendorAddress}' AND c.""RecordId"" IN (
        SELECT ""AddressId"" FROM vendoraddress WHERE ""VendorId"" = '{safeId}'
      ))
   OR (c.""BizType"" = '{BusinessLogTypes.VendorBank}' AND c.""RecordId"" IN (
        SELECT ""BankId"" FROM vendorbankinfo WHERE ""VendorId"" = '{safeId}'
      ))
   OR (c.""BizType"" = '{BusinessLogTypes.VendorContactHistory}' AND c.""RecordId"" IN (
        SELECT ""HistoryId"" FROM vendorcontacthistory WHERE ""VendorId"" = '{safeId}'
      ))
ORDER BY c.""ChangedAt"" DESC";
            return await _unitOfWork.QueryAsync<VendorChangeLog>(sql);
        }

        /// <summary>冻结供应商（禁用）</summary>
        public async Task FreezeVendorAsync(string id, string reason, string? operatorUserId, string? operatorUserName)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("冻结原因不能为空", nameof(reason));

            var resolvedId = (await ResolveVendorByIdOrCodeAsync(id))?.Id;
            if (resolvedId == null)
                throw new KeyNotFoundException($"供应商 {id} 不存在");
            var vendors = await _repository.FindAsync(e => e.Id == resolvedId);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                throw new KeyNotFoundException($"供应商 {id} 不存在");
            if (vendor.IsDisenable)
                throw new InvalidOperationException("供应商已处于冻结状态");

            var safeId = SqlQ(resolvedId);
            var modBy = string.IsNullOrWhiteSpace(operatorUserId)
                ? "NULL"
                : $"'{SqlQ(operatorUserId)}'";
            var rows = await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE vendorinfo SET ""IsDisenable"" = TRUE, ""ModifyTime"" = NOW(), modify_by_user_id = {modBy} WHERE ""VendorId"" = '{safeId}'");
            if (rows == 0)
                throw new InvalidOperationException("更新供应商冻结状态失败，请稍后重试");

            var r = reason.Trim();
            await AddOperationLogAsync(resolvedId, "冻结供应商", $"冻结供应商，原因：{r}", operatorUserId, operatorUserName, r);
        }

        /// <summary>启用供应商（解除冻结）</summary>
        public async Task UnfreezeVendorAsync(string id, string reason, string? operatorUserId, string? operatorUserName)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("启用原因不能为空", nameof(reason));

            var resolvedId = (await ResolveVendorByIdOrCodeAsync(id))?.Id;
            if (resolvedId == null)
                throw new KeyNotFoundException($"供应商 {id} 不存在");
            var vendors = await _repository.FindAsync(e => e.Id == resolvedId);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                throw new KeyNotFoundException($"供应商 {id} 不存在");
            if (!vendor.IsDisenable)
                throw new InvalidOperationException("供应商未处于冻结状态，无需启用");

            var safeId = SqlQ(resolvedId);
            var modByU = string.IsNullOrWhiteSpace(operatorUserId)
                ? "NULL"
                : $"'{SqlQ(operatorUserId)}'";
            var rows = await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE vendorinfo SET ""IsDisenable"" = FALSE, ""ModifyTime"" = NOW(), modify_by_user_id = {modByU} WHERE ""VendorId"" = '{safeId}'");
            if (rows == 0)
                throw new InvalidOperationException("更新供应商启用状态失败，请稍后重试");

            var r = reason.Trim();
            await AddOperationLogAsync(resolvedId, "启用供应商", $"启用供应商，原因：{r}", operatorUserId, operatorUserName, r);
        }

        /// <summary>记录供应商主体操作日志（写入 log_operation）</summary>
        public async Task AddOperationLogAsync(string vendorId, string operationType, string? desc, string? userId, string? userName, string? remark = null)
        {
            var canonicalId = (await ResolveVendorByIdOrCodeAsync(vendorId))?.Id ?? vendorId.Trim();
            var venList = await _repository.FindIgnoreFiltersAsync(e => e.Id == canonicalId);
            var ven = venList.FirstOrDefault();
            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.Vendor,
                canonicalId,
                ven?.Code,
                operationType,
                userId,
                userName,
                desc,
                remark);
        }

        private async Task AppendSubEntityDeleteLogAsync(
            string bizType,
            string recordId,
            string? recordCode,
            string entityDisplayName,
            string parentVendorId,
            string? actingUserId = null)
        {
            var vendor = await GetByIdAsync(parentVendorId);
            var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = bizType,
                RecordId = recordId,
                RecordCode = recordCode,
                EntityDisplayName = entityDisplayName,
                ExtraDetail = vendor?.Code != null ? $"所属供应商={vendor.Code}" : $"所属供应商Id={parentVendorId}",
                OperatorUserId = actorId,
                OperatorUserName = actorName
            });
        }

        private static short? NormalizeContactGender(short? gender)
        {
            if (!gender.HasValue) return null;
            return gender.Value switch
            {
                1 or 2 => gender.Value,
                _ => 0
            };
        }

        private async Task<string?> ResolveUniqueCompanyEmailSuffixAsync(string? raw, string? excludeVendorId)
        {
            if (!CompanyEmailSuffix.TryNormalize(raw, out var suffix, out var error))
                throw new InvalidOperationException(error ?? CompanyEmailSuffix.InvalidFormatMessage);
            if (suffix == null)
                return null;

            var hits = await _repository.FindAsNoTrackingAsync(x =>
                x.CompanyEmailSuffix == suffix
                && (excludeVendorId == null || x.Id != excludeVendorId));
            var other = hits.FirstOrDefault();
            if (other != null)
                throw new InvalidOperationException(
                    CompanyEmailSuffix.DuplicateMessage("供应商", suffix, other.OfficialName ?? other.Code));
            return suffix;
        }
    }
}
