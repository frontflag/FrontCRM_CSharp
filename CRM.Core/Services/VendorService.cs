using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// Vendor服务实现
    /// </summary>
    public class VendorService : IVendorService
    {
        private readonly IRepository<VendorInfo> _repository;
        private readonly IRepository<VendorContactInfo> _contactRepository;
        private readonly IRepository<VendorAddress> _addressRepository;
        private readonly IRepository<VendorBankInfo> _bankRepository;
        private readonly IRepository<VendorContactHistory> _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IDataPermissionService _dataPermissionService;

        public VendorService(
            IRepository<VendorInfo> repository,
            IRepository<VendorContactInfo> contactRepository,
            IRepository<VendorAddress> addressRepository,
            IRepository<VendorBankInfo> bankRepository,
            IRepository<VendorContactHistory> historyRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IDataPermissionService dataPermissionService)
        {
            _repository = repository;
            _contactRepository = contactRepository;
            _addressRepository = addressRepository;
            _bankRepository = bankRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _dataPermissionService = dataPermissionService;
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
        public async Task<VendorInfo> CreateAsync(CreateVendorRequest request)
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
                Code = request.Code.Trim(),
                OfficialName = string.IsNullOrEmpty(official) ? null : official,
                NickName = string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim(),
                Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim(),
                Level = request.Level,
                Credit = request.Credit,
                Status = request.Status ?? 1,
                OfficeAddress = string.IsNullOrWhiteSpace(request.OfficeAddress) ? null : request.OfficeAddress.Trim(),
                Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
                PurchaserName = string.IsNullOrWhiteSpace(request.PurchaserName) ? null : request.PurchaserName.Trim(),
                TradeCurrency = currency,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim(),
                Payment = request.PaymentDays,
                CreditCode = string.IsNullOrWhiteSpace(tax) ? null : tax.Trim(),
                CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim(),
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
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
            var banks = await _bankRepository.FindAsync(b => b.VendorId == vendor.Id);
            vendor.BankAccounts = banks.ToList();
            return vendor;
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
        public async Task<PagedResult<VendorInfo>> GetPagedAsync(VendorQueryRequest request)
        {
            var allEntities = await _repository.GetAllAsync();
            var query = allEntities.Where(e => !e.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(e =>
                    (e.Code != null && e.Code.ToLower().Contains(keyword)) ||
                    (e.OfficialName != null && e.OfficialName.ToLower().Contains(keyword)) ||
                    (e.NickName != null && e.NickName.ToLower().Contains(keyword)));
            }

            if (request.Status.HasValue)
                query = query.Where(e => e.Status == request.Status.Value);

            if (request.Level.HasValue)
                query = query.Where(e => e.Level == request.Level.Value);

            if (!string.IsNullOrWhiteSpace(request.Industry))
            {
                var ind = request.Industry.Trim();
                query = query.Where(e => e.Industry != null && e.Industry.Contains(ind));
            }

            if (request.Credit.HasValue)
                query = query.Where(e => e.Credit == request.Credit.Value);

            if (request.AscriptionType.HasValue)
                query = query.Where(e => e.AscriptionType == request.AscriptionType.Value);

            if (!string.IsNullOrWhiteSpace(request.PurchaseUserId))
            {
                var pid = request.PurchaseUserId.Trim();
                query = query.Where(e => e.PurchaseUserId == pid);
            }

            if (request.CreatedFrom.HasValue)
            {
                var from = request.CreatedFrom.Value.Date;
                query = query.Where(e => e.CreateTime >= from);
            }

            if (request.CreatedTo.HasValue)
            {
                var toExclusive = request.CreatedTo.Value.Date.AddDays(1);
                query = query.Where(e => e.CreateTime < toExclusive);
            }

            // 数据权限过滤（在分页前）
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var filtered = await _dataPermissionService.FilterVendorsAsync(request.CurrentUserId, query.ToList());
                query = filtered.AsQueryable();
            }

            var totalCount = query.Count();
            var items = query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // 加载联系人列表并按供应商分组，便于列表页展示主联系人信息
            var allContacts = await _contactRepository.GetAllAsync();
            var contactGroups = allContacts
                .GroupBy(c => c.VendorId)
                .ToDictionary(g => g.Key, g => g
                    .OrderByDescending(c => c.IsMain)
                    .ThenBy(c => c.CName)
                    .ToList());

            foreach (var vendor in items)
            {
                if (contactGroups.TryGetValue(vendor.Id, out var contacts))
                    vendor.Contacts = contacts;
                else
                    vendor.Contacts = new List<VendorContactInfo>();
            }

            return new PagedResult<VendorInfo>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<VendorInfo>> GetBlacklistAsync(VendorQueryRequest request)
        {
            var allEntities = await _repository.GetAllAsync();
            var query = allEntities.Where(e => e.BlackList && !e.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(e =>
                    (e.Code != null && e.Code.ToLower().Contains(keyword)) ||
                    (e.OfficialName != null && e.OfficialName.ToLower().Contains(keyword)) ||
                    (e.NickName != null && e.NickName.ToLower().Contains(keyword)));
            }

            var totalCount = query.Count();
            var items = query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<VendorInfo>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<VendorInfo>> GetFrozenAsync(VendorQueryRequest request)
        {
            var allEntities = await _repository.GetAllAsync();
            var query = allEntities.Where(e => e.IsDisenable && !e.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(e =>
                    (e.Code != null && e.Code.ToLower().Contains(keyword)) ||
                    (e.OfficialName != null && e.OfficialName.ToLower().Contains(keyword)) ||
                    (e.NickName != null && e.NickName.ToLower().Contains(keyword)));
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(e => e.ModifyTime ?? e.CreateTime)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<VendorInfo>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// 更新
        /// </summary>
        public async Task<VendorInfo> UpdateAsync(string id, UpdateVendorRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

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
                entity.Level = request.Level.Value;
            if (request.TradeCurrency.HasValue)
                entity.TradeCurrency = request.TradeCurrency.Value;
            if (request.PaymentMethod != null)
                entity.PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim();
            var paymentDays = request.PaymentDays ?? request.Payment;
            if (paymentDays.HasValue)
                entity.Payment = paymentDays.Value;
            if (request.CreditCode != null)
                entity.CreditCode = string.IsNullOrWhiteSpace(request.CreditCode) ? null : request.CreditCode.Trim();
            if (request.CompanyInfo != null)
                entity.CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim();
            if (request.Remark != null)
                entity.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
            if (request.ExternalNumber != null)
                entity.ExternalNumber = request.ExternalNumber.Trim();

            entity.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 删除
        /// </summary>
        public async Task DeleteAsync(string id, string? reason = null)
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
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(entity.Id, "删除", $"删除供应商，理由：{reason ?? "无"}", null, "系统用户");
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

        public async Task<PagedResult<VendorInfo>> GetDeletedAsync(int pageIndex, int pageSize, string? keyword)
        {
            var allEntities = await _repository.GetAllAsync();
            var query = allEntities.Where(e => e.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(e =>
                    (e.Code != null && e.Code.ToLower().Contains(k)) ||
                    (e.OfficialName != null && e.OfficialName.ToLower().Contains(k)) ||
                    (e.NickName != null && e.NickName.ToLower().Contains(k)));
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(e => e.DeleteTime ?? e.ModifyTime ?? e.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<VendorInfo>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task RestoreAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.IsDeleted = false;
            entity.DeleteTime = null;
            entity.DeleteReason = null;
            entity.ModifyTime = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(entity.Id, "恢复", "供应商已从回收站恢复", null, "系统用户");
        }

        public async Task AddToBlacklistAsync(string id, string? reason)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

            entity.BlackList = true;
            entity.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            var r = string.IsNullOrWhiteSpace(reason) ? "无" : reason.Trim();
            await AddOperationLogAsync(entity.Id, "加入黑名单", $"加入黑名单，理由：{r}", null, "系统用户");
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
                -1 => target is 1,                // 审核失败 -> 回到新建（可重新提交）
                10 => target is 12 or 20,         // 已审核 -> 待财务审核 / 财务建档
                12 => target is 20,               // 待财务审核 -> 财务建档
                _ => false
            };
            if (!ok) throw new InvalidOperationException($"不允许的供应商状态流转: {current} -> {target}");
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public async Task UpdateStatusAsync(string id, short status, string? auditRemark = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var entity = await GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的记录");

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
            entity.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
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
                (e.NickName != null && e.NickName.ToLower().Contains(searchTerm)));
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

            var contact = new VendorContactInfo
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = vendor.Id,
                CName = request.CName?.Trim(),
                EName = request.EName?.Trim(),
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

            if (request.CName != null) contact.CName = request.CName.Trim();
            if (request.EName != null) contact.EName = request.EName.Trim();
            if (request.Title != null) contact.Title = request.Title.Trim();
            if (request.Department != null) contact.Department = request.Department.Trim();
            if (request.Mobile != null) contact.Mobile = request.Mobile.Trim();
            if (request.Tel != null) contact.Tel = request.Tel.Trim();
            if (request.Email != null) contact.Email = request.Email.Trim();
            if (request.IsMain.HasValue) contact.IsMain = request.IsMain.Value;
            if (request.Remark != null) contact.Remark = request.Remark.Trim();

            contact.ModifyTime = DateTime.UtcNow;
            await _contactRepository.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();
            return contact;
        }

        /// <summary>
        /// 删除供应商联系人
        /// </summary>
        public async Task DeleteContactAsync(string contactId)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var list = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = list.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            await _contactRepository.DeleteAsync(contact.Id);
            await _unitOfWork.SaveChangesAsync();
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
            return address;
        }

        public async Task DeleteAddressAsync(string addressId)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var list = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = list.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            await _addressRepository.DeleteAsync(address.Id);
            await _unitOfWork.SaveChangesAsync();
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
            var list = await _bankRepository.FindAsync(b => b.VendorId == vendor.Id);
            return list.OrderByDescending(b => b.IsDefault).ThenBy(b => b.BankName).ToList();
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
                BankName = request.BankName?.Trim(),
                BankAccount = request.BankAccount?.Trim(),
                AccountName = request.AccountName?.Trim(),
                BankBranch = request.BankBranch?.Trim(),
                Currency = request.Currency,
                IsDefault = request.IsDefault,
                Remark = request.Remark?.Trim(),
                CreateTime = DateTime.UtcNow
            };

            await _bankRepository.AddAsync(bank);
            await _unitOfWork.SaveChangesAsync();
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

            if (request.IsDefault == true && !bank.IsDefault)
                await ResetDefaultBankAsync(bank.VendorId);

            if (request.BankName != null) bank.BankName = request.BankName.Trim();
            if (request.BankAccount != null) bank.BankAccount = request.BankAccount.Trim();
            if (request.AccountName != null) bank.AccountName = request.AccountName.Trim();
            if (request.BankBranch != null) bank.BankBranch = request.BankBranch.Trim();
            if (request.Currency.HasValue) bank.Currency = request.Currency.Value;
            if (request.IsDefault.HasValue) bank.IsDefault = request.IsDefault.Value;
            if (request.Remark != null) bank.Remark = request.Remark.Trim();

            bank.ModifyTime = DateTime.UtcNow;
            await _bankRepository.UpdateAsync(bank);
            await _unitOfWork.SaveChangesAsync();
            return bank;
        }

        public async Task DeleteBankAsync(string bankId)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var list = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = list.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行账户");

            await _bankRepository.DeleteAsync(bank.Id);
            await _unitOfWork.SaveChangesAsync();
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
            return record;
        }

        public async Task DeleteContactHistoryAsync(string historyId)
        {
            if (string.IsNullOrWhiteSpace(historyId))
                throw new ArgumentException("记录ID不能为空", nameof(historyId));

            var list = await _historyRepository.FindAsync(h => h.Id == historyId);
            var record = list.FirstOrDefault();
            if (record == null)
                throw new KeyNotFoundException($"找不到ID为 '{historyId}' 的联系记录");

            await _historyRepository.DeleteAsync(record.Id);
            await _unitOfWork.SaveChangesAsync();
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
            var rows = await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE vendorinfo SET ""IsDisenable"" = TRUE, ""ModifyTime"" = NOW() WHERE ""VendorId"" = '{safeId}'");
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
            var rows = await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE vendorinfo SET ""IsDisenable"" = FALSE, ""ModifyTime"" = NOW() WHERE ""VendorId"" = '{safeId}'");
            if (rows == 0)
                throw new InvalidOperationException("更新供应商启用状态失败，请稍后重试");

            var r = reason.Trim();
            await AddOperationLogAsync(resolvedId, "启用供应商", $"启用供应商，原因：{r}", operatorUserId, operatorUserName, r);
        }

        /// <summary>记录供应商主体操作日志（写入 log_operation）</summary>
        public async Task AddOperationLogAsync(string vendorId, string operationType, string? desc, string? userId, string? userName, string? remark = null)
        {
            var canonicalId = (await ResolveVendorByIdOrCodeAsync(vendorId))?.Id ?? vendorId.Trim();
            var venList = await _repository.FindAsync(e => e.Id == canonicalId);
            var ven = venList.FirstOrDefault();
            var recordCodeSql = ven?.Code != null ? $"'{SqlQ(ven.Code)}'" : "NULL";
            var safeRecordId = SqlQ(canonicalId);
            var safeOpType = SqlQ(operationType);
            var safeDesc = SqlQ(desc);
            var safeUserName = SqlQ(userName);
            var safeUserId = userId != null ? SqlQ(userId) : null;
            var safeRemark = remark != null ? SqlQ(remark) : null;
            var sql = $@"
INSERT INTO log_operation (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""ActionType"", ""OperationTime"", ""OperatorUserId"", ""OperatorUserName"", ""Reason"", ""ExtraInfo"", ""SysRemark"", ""OperationDesc"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.Vendor}', '{safeRecordId}', {recordCodeSql}, '{safeOpType}', NOW(), {(safeUserId == null ? "NULL" : $"'{safeUserId}'")}, '{safeUserName}', {(safeRemark == null ? "NULL" : $"'{safeRemark}'")}, NULL, NULL, '{safeDesc}')";
            await _unitOfWork.ExecuteAsync(sql);
        }
    }
}
