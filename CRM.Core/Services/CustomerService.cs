using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using System.Linq.Expressions;

namespace CRM.Core.Services
{
    /// <summary>
    /// 客户服务实现
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IRepository<CustomerAddress> _addressRepository;
        private readonly IRepository<CustomerContactInfo> _contactRepository;
        private readonly IRepository<CustomerBankInfo> _bankRepository;
        private readonly IRepository<CustomerContactHistory> _contactHistoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IErrorLogService _errorLogService;
        private readonly IDataPermissionService _dataPermissionService;

        public CustomerService(
            IRepository<CustomerInfo> customerRepository,
            IRepository<CustomerAddress> addressRepository,
            IRepository<CustomerContactInfo> contactRepository,
            IRepository<CustomerBankInfo> bankRepository,
            IRepository<CustomerContactHistory> contactHistoryRepository,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IErrorLogService errorLogService,
            IDataPermissionService dataPermissionService)
        {
            _customerRepository = customerRepository;
            _addressRepository = addressRepository;
            _contactRepository = contactRepository;
            _bankRepository = bankRepository;
            _contactHistoryRepository = contactHistoryRepository;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _errorLogService = errorLogService;
            _dataPermissionService = dataPermissionService;
        }

        /// <summary>
        /// 创建客户
        /// </summary>
        public async Task<CustomerInfo> CreateCustomerAsync(CreateCustomerRequest request)
        {
            try
            {
            // 始终使用流水号服务自动生成客户编号，忽略前端传入的值
            request.CustomerCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Customer);

            var customer = new CustomerInfo
            {
                Id = Guid.NewGuid().ToString(),
                CustomerCode = request.CustomerCode.Trim(),
                OfficialName = request.OfficialName?.Trim(),
                NickName = request.NickName?.Trim(),
                Level = request.Level,
                Type = request.Type,
                Industry = request.Industry?.Trim(),
                Product = request.Product?.Trim(),
                SalesUserId = request.SalesUserId,
                Remark = request.Remark?.Trim(),
                CreditLine = request.CreditLine,
                Payment = request.Payment,
                TradeCurrency = request.TradeCurrency,
                CreditCode = request.CreditCode?.Trim(),
                Status = 0, // 默认待审核
                CreateTime = DateTime.UtcNow
            };

            await _customerRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return customer;
            }
            catch (Exception ex)
            {
                await _errorLogService.LogAsync(
                    moduleName: "客户管理",
                    errorMessage: ex.Message,
                    exception: ex,
                    operationType: "创建客户",
                    documentNo: request.CustomerCode
                );
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取客户（包含联系人、地址、銀行信息）
        /// </summary>
        public async Task<CustomerInfo?> GetCustomerByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            // 获取客户基本信息
            var customers = await _customerRepository.FindAsync(c => c.Id == id);
            var customer = customers.FirstOrDefault();
            
            if (customer == null)
                return null;

            // 加载联系人
            var contacts = await _contactRepository.FindAsync(c => c.CustomerId == id);
            customer.Contacts = contacts.ToList();

            // 加载地址
            var addresses = await _addressRepository.FindAsync(a => a.CustomerId == id);
            customer.Addresses = addresses.ToList();

            // 加载银行信息
            var banks = await _bankRepository.FindAsync(b => b.CustomerId == id);
            customer.BankAccounts = banks.ToList();

            return customer;
        }

        /// <summary>
        /// 根据客户编码获取客户
        /// </summary>
        public async Task<CustomerInfo?> GetCustomerByCodeAsync(string customerCode)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
                return null;

            var customers = await _customerRepository.FindAsync(
                c => c.CustomerCode == customerCode.Trim());
            return customers.FirstOrDefault();
        }

        /// <summary>
        /// 获取所有客户
        /// </summary>
        public async Task<IEnumerable<CustomerInfo>> GetAllCustomersAsync()
        {
            return await _customerRepository.GetAllAsync();
        }

        /// <summary>
        /// 分页获取客户列表
        /// </summary>
        public async Task<PagedResult<CustomerInfo>> GetCustomersPagedAsync(CustomerQueryRequest request)
        {
            var allCustomers = await _customerRepository.GetAllAsync();
            var query = allCustomers.AsQueryable();

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(c =>
                    (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(keyword)) ||
                    (c.OfficialName != null && c.OfficialName.ToLower().Contains(keyword)) ||
                    (c.NickName != null && c.NickName.ToLower().Contains(keyword)) ||
                    (c.Industry != null && c.Industry.ToLower().Contains(keyword)));
            }

            // 等级筛选
            if (request.Level.HasValue)
                query = query.Where(c => c.Level == request.Level.Value);

            // 类型筛选
            if (request.Type.HasValue)
                query = query.Where(c => c.Type == request.Type.Value);

            // 业务员筛选
            if (!string.IsNullOrWhiteSpace(request.SalesUserId))
                query = query.Where(c => c.SalesUserId == request.SalesUserId);

            // 状态筛选
            if (request.Status.HasValue)
                query = query.Where(c => c.Status == request.Status.Value);

            // 数据权限过滤（在分页前）
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var filtered = await _dataPermissionService.FilterCustomersAsync(request.CurrentUserId, query.ToList());
                query = filtered.AsQueryable();
            }

            var totalCount = query.Count();
            var items = query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // 加载每个客户的联系人信息
            foreach (var customer in items)
            {
                var contacts = await _contactRepository.FindAsync(c => c.CustomerId == customer.Id);
                customer.Contacts = contacts.ToList();
            }

            return new PagedResult<CustomerInfo>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// 更新客户信息
        /// </summary>
        public async Task<CustomerInfo> UpdateCustomerAsync(string id, UpdateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("客户ID不能为空", nameof(id));

            var customer = await GetCustomerByIdAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的客户");

            // 更新字段
            if (request.OfficialName != null)
                customer.OfficialName = request.OfficialName.Trim();
            if (request.StandardOfficialName != null)
                customer.StandardOfficialName = request.StandardOfficialName.Trim();
            if (request.NickName != null)
                customer.NickName = request.NickName.Trim();
            if (request.Level.HasValue)
                customer.Level = request.Level.Value;
            if (request.Type.HasValue)
                customer.Type = request.Type.Value;
            if (request.Industry != null)
                customer.Industry = request.Industry.Trim();
            if (request.Product != null)
                customer.Product = request.Product.Trim();
            if (request.Remark != null)
                customer.Remark = request.Remark.Trim();
            if (request.SalesUserId != null)
                customer.SalesUserId = request.SalesUserId;
            if (request.CreditLine.HasValue)
                customer.CreditLine = request.CreditLine.Value;
            if (request.Payment.HasValue)
                customer.Payment = request.Payment.Value;
            if (request.TradeCurrency.HasValue)
                customer.TradeCurrency = request.TradeCurrency.Value;
            if (request.CreditCode != null)
                customer.CreditCode = request.CreditCode.Trim();

            customer.ModifyTime = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return customer;
        }

        /// <summary>
        /// 删除客户（软删除：设置 IsDeleted=true，不真实删除数据）
        /// </summary>
        public async Task DeleteCustomerAsync(string id, string? operatorUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("客户ID不能为空", nameof(id));

            var customer = await GetCustomerByIdAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的客户");

            // 软删除：设置删除标志和时间
            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;
            customer.DeletedByUserId = operatorUserId;
            customer.ModifyTime = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 批量删除客户（软删除）
        /// </summary>
        public async Task BatchDeleteCustomersAsync(IEnumerable<string> ids, string? operatorUserId = null)
        {
            if (ids == null || !ids.Any())
                return;

            foreach (var id in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                try
                {
                    await DeleteCustomerAsync(id, operatorUserId);
                }
                catch (KeyNotFoundException)
                {
                    // 忽略不存在的客户
                }
            }
        }

        /// <summary>
        /// 添加客户地址
        /// </summary>
        public async Task<CustomerAddress> AddAddressAsync(string customerId, AddAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("客户ID不能为空", nameof(customerId));

            // 验证客户是否存在
            var customer = await GetCustomerByIdAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{customerId}' 的客户");

            // 如果设置为默认地址，先将其他地址设为非默认
            if (request.IsDefault)
            {
                await ResetDefaultAddressAsync(customerId, request.AddressType);
            }

            var address = new CustomerAddress
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
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

        /// <summary>
        /// 更新客户地址
        /// </summary>
        public async Task<CustomerAddress> UpdateAddressAsync(string addressId, UpdateAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var addresses = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = addresses.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            // 如果设置为默认地址，重置其他默认地址
            if (request.IsDefault == true && !address.IsDefault)
            {
                await ResetDefaultAddressAsync(address.CustomerId, request.AddressType ?? address.AddressType);
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

        /// <summary>
        /// 删除客户地址
        /// </summary>
        public async Task DeleteAddressAsync(string addressId)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var addresses = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = addresses.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            await _addressRepository.DeleteAsync(address.Id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 添加客户联系人
        /// </summary>
        public async Task<CustomerContactInfo> AddContactAsync(string customerId, AddContactRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                    throw new ArgumentException("客户ID不能为空", nameof(customerId));

                // 验证客户是否存在
                var customer = await GetCustomerByIdAsync(customerId);
                if (customer == null)
                    throw new KeyNotFoundException($"找不到ID为 '{customerId}' 的客户");

                // 如果设置为默认联系人，先将其他联系人设为非默认
                if (request.IsDefault == true)
                {
                    await ResetDefaultContactAsync(customerId);
                }

                var contact = new CustomerContactInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    Name = request.Name?.Trim(),
                    Gender = request.Gender,
                    Department = request.Department?.Trim(),
                    Position = request.Position?.Trim(),
                    Phone = request.Phone?.Trim(),
                    Mobile = request.Mobile?.Trim(),
                    Email = request.Email?.Trim(),
                    IsDefault = request.IsDefault ?? false,
                    CreateTime = DateTime.UtcNow
                };

                await _contactRepository.AddAsync(contact);
                await _unitOfWork.SaveChangesAsync();
                return contact;
            }
            catch (Exception ex)
            {
                throw new Exception($"添加联系人失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新客户联系人
        /// </summary>
        public async Task<CustomerContactInfo> UpdateContactAsync(string contactId, UpdateContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var contacts = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = contacts.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            // 如果设置为默认联系人，重置其他默认联系人
            if (request.IsDefault == true && !contact.IsDefault)
            {
                await ResetDefaultContactAsync(contact.CustomerId);
            }

            if (request.Name != null)
                contact.Name = request.Name.Trim();
            if (request.Gender.HasValue)
                contact.Gender = request.Gender.Value;
            if (request.Department != null)
                contact.Department = request.Department.Trim();
            if (request.Position != null)
                contact.Position = request.Position.Trim();
            if (request.Phone != null)
                contact.Phone = request.Phone.Trim();
            if (request.Mobile != null)
                contact.Mobile = request.Mobile.Trim();
            if (request.Email != null)
                contact.Email = request.Email.Trim();
            if (request.Fax != null)
                contact.Fax = request.Fax.Trim();
            if (request.IsDefault.HasValue)
                contact.IsDefault = request.IsDefault.Value;

            contact.ModifyTime = DateTime.UtcNow;

            await _contactRepository.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();
            return contact;
        }

        /// <summary>
        /// 删除客户联系人
        /// </summary>
        public async Task DeleteContactAsync(string contactId)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var contacts = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = contacts.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            await _contactRepository.DeleteAsync(contact.Id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 设置默认联系人
        /// </summary>
        public async Task SetDefaultContactAsync(string contactId)
        {
            if (string.IsNullOrWhiteSpace(contactId))
                throw new ArgumentException("联系人ID不能为空", nameof(contactId));

            var contacts = await _contactRepository.FindAsync(c => c.Id == contactId);
            var contact = contacts.FirstOrDefault();
            if (contact == null)
                throw new KeyNotFoundException($"找不到ID为 '{contactId}' 的联系人");

            await ResetDefaultContactAsync(contact.CustomerId);

            contact.IsDefault = true;
            contact.ModifyTime = DateTime.UtcNow;
            await _contactRepository.UpdateAsync(contact);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 设置默认地址（通过地址ID）
        /// </summary>
        public async Task SetDefaultAddressAsync(string addressId)
        {
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var addresses = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = addresses.FirstOrDefault();
            if (address == null)
                throw new KeyNotFoundException($"找不到ID为 '{addressId}' 的地址");

            await ResetDefaultAddressAsync(address.CustomerId, address.AddressType);

            address.IsDefault = true;
            address.ModifyTime = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 设置默认地址（通过客户ID和地址ID）
        /// </summary>
        public async Task SetDefaultAddressAsync(string customerId, string addressId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("客户ID不能为空", nameof(customerId));
            if (string.IsNullOrWhiteSpace(addressId))
                throw new ArgumentException("地址ID不能为空", nameof(addressId));

            var addresses = await _addressRepository.FindAsync(a => a.Id == addressId);
            var address = addresses.FirstOrDefault();
            if (address == null || address.CustomerId != customerId)
                throw new KeyNotFoundException("地址不存在或不属于该客户");

            await ResetDefaultAddressAsync(customerId, address.AddressType);

            address.IsDefault = true;
            address.ModifyTime = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 添加客户银行信息
        /// </summary>
        public async Task<CustomerBankInfo> AddBankAsync(string customerId, AddBankRequest request)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("客户ID不能为空", nameof(customerId));

            // 验证客户是否存在
            var customer = await GetCustomerByIdAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{customerId}' 的客户");

            // 如果设置为默认银行，先将其他银行设为非默认
            if (request.IsDefault)
            {
                await ResetDefaultBankAsync(customerId);
            }

            var bank = new CustomerBankInfo
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
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

        /// <summary>
        /// 获取客户银行信息列表
        /// </summary>
        public async Task<IEnumerable<CustomerBankInfo>> GetBanksByCustomerIdAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return new List<CustomerBankInfo>();

            var banks = await _bankRepository.FindAsync(b => b.CustomerId == customerId);
            return banks.OrderByDescending(b => b.IsDefault).ThenByDescending(b => b.CreateTime);
        }

        /// <summary>
        /// 更新客户银行信息
        /// </summary>
        public async Task<CustomerBankInfo> UpdateBankAsync(string bankId, UpdateBankRequest request)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var banks = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = banks.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行信息");

            // 如果设置为默认银行，重置其他默认银行
            if (request.IsDefault == true && !bank.IsDefault)
            {
                await ResetDefaultBankAsync(bank.CustomerId);
            }

            if (request.BankName != null)
                bank.BankName = request.BankName.Trim();
            if (request.BankAccount != null)
                bank.BankAccount = request.BankAccount.Trim();
            if (request.AccountName != null)
                bank.AccountName = request.AccountName.Trim();
            if (request.BankBranch != null)
                bank.BankBranch = request.BankBranch.Trim();
            if (request.Currency.HasValue)
                bank.Currency = request.Currency.Value;
            if (request.IsDefault.HasValue)
                bank.IsDefault = request.IsDefault.Value;
            if (request.Remark != null)
                bank.Remark = request.Remark.Trim();

            bank.ModifyTime = DateTime.UtcNow;

            await _bankRepository.UpdateAsync(bank);
            await _unitOfWork.SaveChangesAsync();
            return bank;
        }

        /// <summary>
        /// 删除客户银行信息
        /// </summary>
        public async Task DeleteBankAsync(string bankId)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var banks = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = banks.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行信息");

            await _bankRepository.DeleteAsync(bank.Id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 设置默认银行
        /// </summary>
        public async Task SetDefaultBankAsync(string bankId)
        {
            if (string.IsNullOrWhiteSpace(bankId))
                throw new ArgumentException("银行ID不能为空", nameof(bankId));

            var banks = await _bankRepository.FindAsync(b => b.Id == bankId);
            var bank = banks.FirstOrDefault();
            if (bank == null)
                throw new KeyNotFoundException($"找不到ID为 '{bankId}' 的银行信息");

            await ResetDefaultBankAsync(bank.CustomerId);

            bank.IsDefault = true;
            bank.ModifyTime = DateTime.UtcNow;
            await _bankRepository.UpdateAsync(bank);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 重置默认银行
        /// </summary>
        private async Task ResetDefaultBankAsync(string customerId)
        {
            var banks = await _bankRepository.FindAsync(b =>
                b.CustomerId == customerId &&
                b.IsDefault);

            foreach (var bank in banks)
            {
                bank.IsDefault = false;
                bank.ModifyTime = DateTime.UtcNow;
                await _bankRepository.UpdateAsync(bank);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 搜索客户
        /// </summary>
        public async Task<IEnumerable<CustomerInfo>> SearchCustomersAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllCustomersAsync();

            var allCustomers = await _customerRepository.GetAllAsync();
            var searchTerm = keyword.Trim().ToLower();

            return allCustomers.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(searchTerm)) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(searchTerm)) ||
                (c.NickName != null && c.NickName.ToLower().Contains(searchTerm)) ||
                (c.Industry != null && c.Industry.ToLower().Contains(searchTerm)) ||
                (c.Product != null && c.Product.ToLower().Contains(searchTerm)));
        }

        /// <summary>
        /// 更新客户状态
        /// </summary>
        public async Task UpdateCustomerStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("客户ID不能为空", nameof(id));

            var customer = await GetCustomerByIdAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的客户");

            customer.Status = status;
            customer.ModifyTime = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 检查客户编码是否已存在
        /// </summary>
        public async Task<bool> IsCustomerCodeExistsAsync(string customerCode)
        {
            if (string.IsNullOrWhiteSpace(customerCode))
                return false;

            var existing = await GetCustomerByCodeAsync(customerCode);
            return existing != null;
        }

        /// <summary>
        /// 获取客户联系历史
        /// </summary>
        public async Task<IEnumerable<CustomerContactHistory>> GetContactHistoryAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return new List<CustomerContactHistory>();

            var list = await _contactHistoryRepository.FindAsync(h => h.CustomerId == customerId);
            return list.OrderByDescending(h => h.Time).ToList();
        }

        /// <summary>
        /// 添加客户联系记录
        /// </summary>
        public async Task<CustomerContactHistory> AddContactHistoryAsync(string customerId, AddContactHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("客户ID不能为空", nameof(customerId));

            var customer = await GetCustomerByIdAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"找不到ID为 '{customerId}' 的客户");

            var record = new CustomerContactHistory
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                Type = request.Type?.Trim() ?? "call",
                Subject = request.Subject?.Trim(),
                Content = request.Content?.Trim(),
                ContactPerson = request.ContactPerson?.Trim(),
                Time = request.Time.HasValue 
                    ? DateTime.SpecifyKind(request.Time.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                NextFollowUpTime = request.NextFollowUpTime.HasValue
                    ? DateTime.SpecifyKind(request.NextFollowUpTime.Value, DateTimeKind.Utc)
                    : null,
                Result = request.Result?.Trim(),
                CreateTime = DateTime.UtcNow
            };

            await _contactHistoryRepository.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return record;
        }

        /// <summary>
        /// 重置默认地址
        /// </summary>
        private async Task ResetDefaultAddressAsync(string customerId, short addressType)
        {
            var addresses = await _addressRepository.FindAsync(a =>
                a.CustomerId == customerId &&
                a.AddressType == addressType &&
                a.IsDefault);  // CustomerAddress.IsDefault 是实际列，可直接使用

            foreach (var addr in addresses)
            {
                addr.IsDefault = false;
                addr.ModifyTime = DateTime.UtcNow;
                await _addressRepository.UpdateAsync(addr);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 重置默认联系人
        /// </summary>
        private async Task ResetDefaultContactAsync(string customerId)
        {
            var contacts = await _contactRepository.FindAsync(c =>
                c.CustomerId == customerId &&
                c.IsMain);  // IsDefault 是 [NotMapped] 别名，EF 无法翻译，使用实际列 IsMain

            foreach (var contact in contacts)
            {
                contact.IsDefault = false;
                contact.ModifyTime = DateTime.UtcNow;
                await _contactRepository.UpdateAsync(contact);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 更新联系历史
        /// </summary>
        public async Task<CustomerContactHistory> UpdateContactHistoryAsync(string historyId, UpdateContactHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(historyId))
                throw new ArgumentException("联系历史ID不能为空", nameof(historyId));
            var records = await _contactHistoryRepository.FindAsync(h => h.Id == historyId);
            var record = records.FirstOrDefault();
            if (record == null)
                throw new KeyNotFoundException($"找不到ID为 '{historyId}' 的联系历史");
            if (request.Type != null) record.Type = request.Type.Trim();
            if (request.Subject != null) record.Subject = request.Subject.Trim();
            if (request.Content != null) record.Content = request.Content.Trim();
            if (request.ContactPerson != null) record.ContactPerson = request.ContactPerson.Trim();
            if (request.Time.HasValue) record.Time = DateTime.SpecifyKind(request.Time.Value, DateTimeKind.Utc);
            if (request.NextFollowUpTime.HasValue) record.NextFollowUpTime = DateTime.SpecifyKind(request.NextFollowUpTime.Value, DateTimeKind.Utc);
            if (request.Result != null) record.Result = request.Result.Trim();
            await _contactHistoryRepository.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return record;
        }

        /// <summary>
        /// 删除联系历史
        /// </summary>
        public async Task DeleteContactHistoryAsync(string historyId)
        {
            if (string.IsNullOrWhiteSpace(historyId))
                throw new ArgumentException("联系历史ID不能为空", nameof(historyId));
            var records = await _contactHistoryRepository.FindAsync(h => h.Id == historyId);
            var record = records.FirstOrDefault();
            if (record == null)
                throw new KeyNotFoundException($"找不到ID为 '{historyId}' 的联系历史");
            await _contactHistoryRepository.DeleteAsync(record.Id);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>删除客户（带理由）</summary>
        public async Task DeleteCustomerWithReasonAsync(string id, string? reason, string? operatorUserId, string? operatorUserName)
        {
            var customers = await _customerRepository.FindAsync(c => c.Id == id);
            var customer = customers.FirstOrDefault();
            if (customer == null) throw new KeyNotFoundException($"客户 {id} 不存在");
            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;
            customer.DeletedByUserId = operatorUserId;
            customer.DeletedByUserName = operatorUserName;
            customer.DeleteReason = reason;
            customer.ModifyTime = DateTime.UtcNow;
            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(id, "删除", $"删除客户，理由：{reason ?? "无"}", operatorUserId, operatorUserName);
        }

        /// <summary>设置黑名单（带理由）</summary>
        public async Task SetBlackListAsync(string id, string reason, string? operatorUserId, string? operatorUserName)
        {
            var customers = await _customerRepository.FindAsync(c => c.Id == id);
            var customer = customers.FirstOrDefault();
            if (customer == null) throw new KeyNotFoundException($"客户 {id} 不存在");
            customer.BlackList = true;
            customer.BlackListReason = reason;
            customer.BlackListAt = DateTime.UtcNow;
            customer.BlackListByUserId = operatorUserId;
            customer.BlackListByUserName = operatorUserName;
            customer.ModifyTime = DateTime.UtcNow;
            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(id, "加入黑名单", $"加入黑名单，理由：{reason}", operatorUserId, operatorUserName);
        }

        /// <summary>移出黑名单</summary>
        public async Task RemoveFromBlackListAsync(string id, string? operatorUserId, string? operatorUserName)
        {
            var customers = await _customerRepository.FindAsync(c => c.Id == id);
            var customer = customers.FirstOrDefault();
            if (customer == null) throw new KeyNotFoundException($"客户 {id} 不存在");
            customer.BlackList = false;
            customer.BlackListReason = null;
            customer.BlackListAt = null;
            customer.BlackListByUserId = null;
            customer.BlackListByUserName = null;
            customer.ModifyTime = DateTime.UtcNow;
            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(id, "移出黑名单", "客户已从黑名单移出", operatorUserId, operatorUserName);
        }

        /// <summary>恢复已删除的客户</summary>
        public async Task RestoreCustomerAsync(string id, string? operatorUserId, string? operatorUserName)
        {
            var customers = await _customerRepository.FindIgnoreFiltersAsync(c => c.Id == id);
            var customer = customers.FirstOrDefault();
            if (customer == null) throw new KeyNotFoundException($"客户 {id} 不存在");
            customer.IsDeleted = false;
            customer.DeletedAt = null;
            customer.DeletedByUserId = null;
            customer.DeletedByUserName = null;
            customer.DeleteReason = null;
            customer.Status = 0; // 恢复为新建状态
            customer.ModifyTime = DateTime.UtcNow;
            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await AddOperationLogAsync(id, "恢复", "客户已从回收站恢复", operatorUserId, operatorUserName);
        }

        /// <summary>获取已删除的客户列表（回收站）</summary>
        public async Task<PagedResult<CustomerInfo>> GetDeletedCustomersAsync(int pageIndex, int pageSize, string? keyword)
        {
            var all = await _customerRepository.FindIgnoreFiltersAsync(c => c.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
                all = all.Where(c => (c.OfficialName != null && c.OfficialName.Contains(keyword)) || (c.CustomerCode != null && c.CustomerCode.Contains(keyword)));
            var sorted = all.OrderByDescending(c => c.DeletedAt).ToList();
            var total = sorted.Count;
            var items = sorted.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            return new PagedResult<CustomerInfo> { Items = items, TotalCount = total, PageIndex = pageIndex, PageSize = pageSize };
        }

        /// <summary>获取黑名单客户列表</summary>
        public async Task<PagedResult<CustomerInfo>> GetBlackListCustomersAsync(int pageIndex, int pageSize, string? keyword)
        {
            var all = await _customerRepository.FindAsync(c => c.BlackList && !c.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
                all = all.Where(c => (c.OfficialName != null && c.OfficialName.Contains(keyword)) || (c.CustomerCode != null && c.CustomerCode.Contains(keyword)));
            var sorted = all.OrderByDescending(c => c.BlackListAt).ToList();
            var total = sorted.Count;
            var items = sorted.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            return new PagedResult<CustomerInfo> { Items = items, TotalCount = total, PageIndex = pageIndex, PageSize = pageSize };
        }

        /// <summary>获取客户操作日志</summary>
        public async Task<IEnumerable<CustomerOperationLog>> GetOperationLogsAsync(string customerId)
        {
            var sql = $"SELECT \"Id\", \"CustomerId\", \"OperationType\", \"OperationDesc\", \"OperatorUserId\", \"OperatorUserName\", \"OperationTime\", \"Remark\" FROM customer_operation_log WHERE \"CustomerId\" = '{customerId.Replace("'", "''")}' ORDER BY \"OperationTime\" DESC";
            return await _unitOfWork.QueryAsync<CustomerOperationLog>(sql);
        }

        /// <summary>获取客户变更日志</summary>
        public async Task<IEnumerable<CustomerChangeLog>> GetChangeLogsAsync(string customerId)
        {
            var sql = $"SELECT \"Id\", \"CustomerId\", \"FieldName\", \"FieldLabel\", \"OldValue\", \"NewValue\", \"ChangedByUserId\", \"ChangedByUserName\", \"ChangedAt\" FROM customer_change_log WHERE \"CustomerId\" = '{customerId.Replace("'", "''")}' ORDER BY \"ChangedAt\" DESC";
            return await _unitOfWork.QueryAsync<CustomerChangeLog>(sql);
        }

        /// <summary>记录操作日志</summary>
        public async Task AddOperationLogAsync(string customerId, string operationType, string? desc, string? userId, string? userName, string? remark = null)
        {
            var safeDesc = desc?.Replace("'", "''") ?? "";
            var safeUserName = userName?.Replace("'", "''") ?? "";
            var safeRemark = remark?.Replace("'", "''");
            var sql = $"INSERT INTO customer_operation_log (\"Id\", \"CustomerId\", \"OperationType\", \"OperationDesc\", \"OperatorUserId\", \"OperatorUserName\", \"OperationTime\", \"Remark\") VALUES (gen_random_uuid()::text, '{customerId}', '{operationType}', '{safeDesc}', {(userId == null ? "NULL" : $"'{userId}'")}, '{safeUserName}', NOW(), {(safeRemark == null ? "NULL" : $"'{safeRemark}'")})";
            await _unitOfWork.ExecuteAsync(sql);
        }

        /// <summary>记录变更日志</summary>
        public async Task AddChangeLogAsync(string customerId, string fieldName, string? fieldLabel, string? oldValue, string? newValue, string? userId, string? userName)
        {
            var safeOld = oldValue?.Replace("'", "''");
            var safeNew = newValue?.Replace("'", "''");
            var safeLabel = fieldLabel?.Replace("'", "''");
            var safeUserName = userName?.Replace("'", "''") ?? "";
            var sql = $"INSERT INTO customer_change_log (\"Id\", \"CustomerId\", \"FieldName\", \"FieldLabel\", \"OldValue\", \"NewValue\", \"ChangedByUserId\", \"ChangedByUserName\", \"ChangedAt\") VALUES (gen_random_uuid()::text, '{customerId}', '{fieldName}', {(safeLabel == null ? "NULL" : $"'{safeLabel}'")}, {(safeOld == null ? "NULL" : $"'{safeOld}'")}, {(safeNew == null ? "NULL" : $"'{safeNew}'")}, {(userId == null ? "NULL" : $"'{userId}'")}, '{safeUserName}', NOW())";
            await _unitOfWork.ExecuteAsync(sql);
        }
    }
}
