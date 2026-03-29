using CRM.Core.Models.Customer;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 客户服务接口
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// 创建客户
        /// </summary>
        Task<CustomerInfo> CreateCustomerAsync(CreateCustomerRequest request);

        /// <summary>
        /// 根据ID获取客户
        /// </summary>
        Task<CustomerInfo?> GetCustomerByIdAsync(string id);

        /// <summary>
        /// 根据客户编码获取客户
        /// </summary>
        Task<CustomerInfo?> GetCustomerByCodeAsync(string customerCode);

        /// <summary>
        /// 获取所有客户
        /// </summary>
        Task<IEnumerable<CustomerInfo>> GetAllCustomersAsync();

        /// <summary>
        /// 分页获取客户列表
        /// </summary>
        Task<PagedResult<CustomerInfo>> GetCustomersPagedAsync(CustomerQueryRequest request);

        /// <summary>
        /// 更新客户信息
        /// </summary>
        Task<CustomerInfo> UpdateCustomerAsync(string id, UpdateCustomerRequest request);

        /// <summary>
        /// 删除客户（软删除）
        /// </summary>
        Task DeleteCustomerAsync(string id, string? operatorUserId = null);

        /// <summary>
        /// 批量删除客户（软删除）
        /// </summary>
        Task BatchDeleteCustomersAsync(IEnumerable<string> ids, string? operatorUserId = null);

        /// <summary>
        /// 添加客户地址
        /// </summary>
        Task<CustomerAddress> AddAddressAsync(string customerId, AddAddressRequest request);

        /// <summary>
        /// 更新客户地址
        /// </summary>
        Task<CustomerAddress> UpdateAddressAsync(string addressId, UpdateAddressRequest request);

        /// <summary>
        /// 删除客户地址
        /// </summary>
        Task DeleteAddressAsync(string addressId);

        /// <summary>
        /// 添加客户联系人
        /// </summary>
        Task<CustomerContactInfo> AddContactAsync(string customerId, AddContactRequest request);

        /// <summary>
        /// 更新客户联系人
        /// </summary>
        Task<CustomerContactInfo> UpdateContactAsync(string contactId, UpdateContactRequest request);

        /// <summary>
        /// 删除客户联系人
        /// </summary>
        Task DeleteContactAsync(string contactId);

        /// <summary>
        /// 设置默认联系人
        /// </summary>
        Task SetDefaultContactAsync(string contactId);

    /// <summary>
    /// 设置默认地址（通过地址ID）
    /// </summary>
    Task SetDefaultAddressAsync(string addressId);

    /// <summary>
    /// 添加客户银行信息
    /// </summary>
    Task<CustomerBankInfo> AddBankAsync(string customerId, AddBankRequest request);

    /// <summary>
    /// 获取客户银行信息列表
    /// </summary>
    Task<IEnumerable<CustomerBankInfo>> GetBanksByCustomerIdAsync(string customerId);

    /// <summary>
    /// 更新客户银行信息
    /// </summary>
    Task<CustomerBankInfo> UpdateBankAsync(string bankId, UpdateBankRequest request);

    /// <summary>
    /// 删除客户银行信息
    /// </summary>
    Task DeleteBankAsync(string bankId);

    /// <summary>
    /// 设置默认银行
    /// </summary>
    Task SetDefaultBankAsync(string bankId);

    /// <summary>
    /// 搜索客户
    /// </summary>
        Task<IEnumerable<CustomerInfo>> SearchCustomersAsync(string keyword);

        /// <summary>
        /// 更新客户状态
        /// </summary>
        Task UpdateCustomerStatusAsync(string id, short status, string? auditRemark = null);

        /// <summary>
        /// 检查客户编码是否已存在
        /// </summary>
        Task<bool> IsCustomerCodeExistsAsync(string customerCode);

        /// <summary>
        /// 获取客户联系历史
        /// </summary>
        Task<IEnumerable<CustomerContactHistory>> GetContactHistoryAsync(string customerId);

        /// <summary>
        /// 添加客户联系记录
        /// </summary>
        Task<CustomerContactHistory> AddContactHistoryAsync(string customerId, AddContactHistoryRequest request);
        Task<CustomerContactHistory> UpdateContactHistoryAsync(string historyId, UpdateContactHistoryRequest request);
        Task DeleteContactHistoryAsync(string historyId);

        /// <summary>删除客户（带理由）</summary>
        Task DeleteCustomerWithReasonAsync(string id, string? reason, string? operatorUserId, string? operatorUserName);

        /// <summary>设置黑名单（带理由）</summary>
        Task SetBlackListAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>移出黑名单（需原因，写入操作日志）</summary>
        Task RemoveFromBlackListAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>冻结客户（设置禁用状态）</summary>
        Task FreezeCustomerAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>启用客户（解除冻结）</summary>
        Task UnfreezeCustomerAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>恢复已删除的客户</summary>
        Task RestoreCustomerAsync(string id, string? operatorUserId, string? operatorUserName);

        /// <summary>获取已删除的客户列表（回收站）</summary>
        Task<PagedResult<CustomerInfo>> GetDeletedCustomersAsync(int pageIndex, int pageSize, string? keyword);

        /// <summary>获取黑名单客户列表</summary>
        Task<PagedResult<CustomerInfo>> GetBlackListCustomersAsync(int pageIndex, int pageSize, string? keyword);

        /// <summary>获取已冻结（禁用）客户列表</summary>
        Task<PagedResult<CustomerInfo>> GetFrozenCustomersAsync(int pageIndex, int pageSize, string? keyword);

        /// <summary>获取客户操作日志</summary>
        Task<IEnumerable<CustomerOperationLog>> GetOperationLogsAsync(string customerId);

        /// <summary>获取客户变更日志</summary>
        Task<IEnumerable<CustomerChangeLog>> GetChangeLogsAsync(string customerId);

        /// <summary>记录操作日志</summary>
        Task AddOperationLogAsync(string customerId, string operationType, string? desc, string? userId, string? userName, string? remark = null);

        /// <summary>记录变更日志</summary>
        Task AddChangeLogAsync(string customerId, string fieldName, string? fieldLabel, string? oldValue, string? newValue, string? userId, string? userName);
    }

    /// <summary>
    /// 客户操作日志
    /// </summary>
    public class CustomerOperationLog
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string? OperationDesc { get; set; }
        public string? OperatorUserId { get; set; }
        public string? OperatorUserName { get; set; }
        public DateTime OperationTime { get; set; }
        public string? Remark { get; set; }
        /// <summary>统一日志业务类型（如 Customer、CustomerContact）</summary>
        public string? BizType { get; set; }
        /// <summary>被操作记录单号（如客户编号）</summary>
        public string? RecordCode { get; set; }
    }

    /// <summary>
    /// 客户变更日志
    /// </summary>
    public class CustomerChangeLog
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? FieldLabel { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? BizType { get; set; }
        public string? RecordCode { get; set; }
    }

    /// <summary>
    /// 设置黑名单请求
    /// </summary>
    public class SetBlackListRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 移出黑名单请求
    /// </summary>
    public class RemoveBlackListRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 删除客户请求（带理由）
    /// </summary>
    public class DeleteCustomerRequest
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 冻结/启用客户请求（需填写原因，写入操作日志）
    /// </summary>
    public class FreezeCustomerRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 更新联系历史请求
    /// </summary>
    public class UpdateContactHistoryRequest
    {
        public string? Type { get; set; }
        public string? ContactType 
        { 
            get => Type; 
            set => Type = value is null ? null : int.TryParse(value, out var n) ? n switch { 1 => "call", 2 => "visit", 3 => "email", 4 => "meeting", _ => "other" } : value;
        }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public string? ContactPerson { get; set; }
        public DateTime? Time { get; set; }
        public DateTime? NextFollowUpTime { get; set; }
        public string? Result { get; set; }
    }

    /// <summary>
    /// 添加联系历史请求
    /// </summary>
    public class AddContactHistoryRequest
    {
        public string? Type { get; set; } = "call";
        /// <summary>ContactType 是 Type 的别名，前端兼容字段（可为数字或字符串）</summary>
        public string? ContactType 
        { 
            get => Type; 
            set => Type = value is null ? null : int.TryParse(value, out var n) ? n switch { 1 => "call", 2 => "visit", 3 => "email", 4 => "meeting", _ => "other" } : value;
        }
        public string? Content { get; set; }
        public DateTime? Time { get; set; }
        /// <summary>ContactTime 是 Time 的别名，前端兼容字段</summary>
        public DateTime? ContactTime { get => Time; set => Time = value; }
        /// <summary>主题（前端字段）</summary>
        public string? Subject { get; set; }
        /// <summary>联系人（前端字段）</summary>
        public string? ContactPerson { get; set; }
        /// <summary>跟进时间（前端字段）</summary>
        public DateTime? NextFollowUpTime { get; set; }
        /// <summary>联系结果（前端字段）</summary>
        public string? Result { get; set; }
    }

    /// <summary>
    /// 创建客户请求
    /// </summary>
    public class CreateCustomerRequest
    {
        public string CustomerCode { get; set; } = string.Empty;
        
        /// <summary>
        /// 公司全称（后端字段名）
        /// </summary>
        public string? OfficialName { get; set; }
        
        /// <summary>
        /// 客户名称（前端字段名，映射到OfficialName）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerName")]
        public string? CustomerName 
        { 
            get => OfficialName; 
            set => OfficialName = value; 
        }
        
        /// <summary>
        /// 公司简称（后端字段名）
        /// </summary>
        public string? NickName { get; set; }
        
        /// <summary>
        /// 客户简称（前端字段名，映射到NickName）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerShortName")]
        public string? CustomerShortName 
        { 
            get => NickName; 
            set => NickName = value; 
        }
        
        /// <summary>
        /// 客户等级数值（后端字段名）
        /// </summary>
        public short Level { get; set; } = 1;
        
        /// <summary>
        /// 客户等级字符串（前端字段名，映射到Level）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerLevel")]
        public string? CustomerLevel
        {
            get => Level switch
            {
                1 => "D",
                2 => "C",
                3 => "B",
                4 => "BPO",
                5 => "VIP",
                6 => "VPO",
                _ => "Normal"
            };
            set => Level = value?.ToUpper() switch
            {
                "D" => (short)1,
                "C" => (short)2,
                "B" => (short)3,
                "BPO" => (short)4,
                "VIP" => (short)5,
                "VPO" => (short)6,
                _ => (short)1
            };
        }
        
        /// <summary>
        /// 客户类型数值（后端字段名）
        /// </summary>
        public short? Type { get; set; }
        
        /// <summary>
        /// 客户类型（前端字段名，映射到Type）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerType")]
        public short? CustomerType 
        { 
            get => Type; 
            set => Type = value; 
        }
        
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        
        /// <summary>
        /// 业务员ID（后端字段名）
        /// </summary>
        public string? SalesUserId { get; set; }
        
        /// <summary>
        /// 业务员ID（前端字段名，映射到SalesUserId）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("salesPersonId")]
        public string? SalesPersonId 
        { 
            get => SalesUserId; 
            set => SalesUserId = value; 
        }
        
        /// <summary>
        /// 备注（后端字段名）
        /// </summary>
        public string? Remark { get; set; }
        
        /// <summary>
        /// 备注（前端字段名，映射到Remark）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("remarks")]
        public string? Remarks 
        { 
            get => Remark; 
            set => Remark = value; 
        }
        
        /// <summary>
        /// 授信额度（后端字段名）
        /// </summary>
        public decimal CreditLine { get; set; } = 0;
        
        /// <summary>
        /// 授信额度（前端字段名，映射到CreditLine）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("creditLimit")]
        public decimal CreditLimit 
        { 
            get => CreditLine; 
            set => CreditLine = value; 
        }
        
        /// <summary>
        /// 账期（后端字段名）
        /// </summary>
        public short? Payment { get; set; } = 30;
        
        /// <summary>
        /// 账期（前端字段名，映射到Payment）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("paymentTerms")]
        public short? PaymentTerms 
        { 
            get => Payment; 
            set => Payment = value; 
        }
        
        /// <summary>
        /// 结算货币（后端字段名）
        /// </summary>
        public short? TradeCurrency { get; set; } = 1;
        
        /// <summary>
        /// 结算货币（前端字段名，映射到TradeCurrency）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public short? Currency 
        { 
            get => TradeCurrency; 
            set => TradeCurrency = value; 
        }
        
        /// <summary>
        /// 统一社会信用代码（后端字段名）
        /// </summary>
        public string? CreditCode { get; set; }
        
        /// <summary>
        /// 统一社会信用代码（前端字段名，映射到CreditCode）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("unifiedSocialCreditCode")]
        public string? UnifiedSocialCreditCode 
        { 
            get => CreditCode; 
            set => CreditCode = value; 
        }

        /// <summary>省/州（地区级联）</summary>
        public string? Province { get; set; }

        /// <summary>市</summary>
        public string? City { get; set; }

        /// <summary>区/县</summary>
        public string? District { get; set; }
    }

    /// <summary>
    /// 更新客户请求
    /// </summary>
    public class UpdateCustomerRequest
    {
        /// <summary>
        /// 公司全称（后端字段名）
        /// </summary>
        public string? OfficialName { get; set; }
        
        /// <summary>
        /// 客户名称（前端字段名，映射到OfficialName）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerName")]
        public string? CustomerName 
        { 
            get => OfficialName; 
            set => OfficialName = value; 
        }
        
        public string? StandardOfficialName { get; set; }
        
        /// <summary>
        /// 公司简称（后端字段名）
        /// </summary>
        public string? NickName { get; set; }
        
        /// <summary>
        /// 客户简称（前端字段名，映射到NickName）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerShortName")]
        public string? CustomerShortName 
        { 
            get => NickName; 
            set => NickName = value; 
        }
        
        /// <summary>
        /// 客户等级数值（后端字段名）
        /// </summary>
        public short? Level { get; set; }
        
        /// <summary>
        /// 客户等级字符串（前端字段名，映射到Level）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerLevel")]
        public string? CustomerLevel
        {
            get => Level.HasValue ? Level.Value switch
            {
                1 => "D",
                2 => "C",
                3 => "B",
                4 => "BPO",
                5 => "VIP",
                6 => "VPO",
                _ => "Normal"
            } : null;
            set => Level = value?.ToUpper() switch
            {
                "D" => (short)1,
                "C" => (short)2,
                "B" => (short)3,
                "BPO" => (short)4,
                "VIP" => (short)5,
                "VPO" => (short)6,
                _ => (short)1
            };
        }
        
        /// <summary>
        /// 客户类型数值（后端字段名）
        /// </summary>
        public short? Type { get; set; }
        
        /// <summary>
        /// 客户类型（前端字段名，映射到Type）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customerType")]
        public short? CustomerType 
        { 
            get => Type; 
            set => Type = value; 
        }
        
        public string? Industry { get; set; }
        public string? Product { get; set; }
        
        /// <summary>
        /// 备注（后端字段名）
        /// </summary>
        public string? Remark { get; set; }
        
        /// <summary>
        /// 备注（前端字段名，映射到Remark）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("remarks")]
        public string? Remarks 
        { 
            get => Remark; 
            set => Remark = value; 
        }
        
        /// <summary>
        /// 业务员ID（后端字段名）
        /// </summary>
        public string? SalesUserId { get; set; }
        
        /// <summary>
        /// 业务员ID（前端字段名，映射到SalesUserId）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("salesPersonId")]
        public string? SalesPersonId 
        { 
            get => SalesUserId; 
            set => SalesUserId = value; 
        }
        
        /// <summary>
        /// 授信额度（后端字段名）
        /// </summary>
        public decimal? CreditLine { get; set; }
        
        /// <summary>
        /// 授信额度（前端字段名，映射到CreditLine）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("creditLimit")]
        public decimal? CreditLimit 
        { 
            get => CreditLine; 
            set => CreditLine = value; 
        }
        
        /// <summary>
        /// 账期（后端字段名）
        /// </summary>
        public short? Payment { get; set; }
        
        /// <summary>
        /// 账期（前端字段名，映射到Payment）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("paymentTerms")]
        public short? PaymentTerms 
        { 
            get => Payment; 
            set => Payment = value; 
        }
        
        /// <summary>
        /// 结算货币（后端字段名）
        /// </summary>
        public short? TradeCurrency { get; set; }
        
        /// <summary>
        /// 结算货币（前端字段名，映射到TradeCurrency）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public short? Currency 
        { 
            get => TradeCurrency; 
            set => TradeCurrency = value; 
        }
        
        /// <summary>
        /// 统一社会信用代码（后端字段名）
        /// </summary>
        public string? CreditCode { get; set; }
        
        /// <summary>
        /// 统一社会信用代码（前端字段名，映射到CreditCode）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("unifiedSocialCreditCode")]
        public string? UnifiedSocialCreditCode 
        { 
            get => CreditCode; 
            set => CreditCode = value; 
        }

        /// <summary>省/州（地区级联）</summary>
        public string? Province { get; set; }

        /// <summary>市</summary>
        public string? City { get; set; }

        /// <summary>区/县</summary>
        public string? District { get; set; }
    }

    /// <summary>
    /// 客户查询请求
    /// </summary>
    public class CustomerQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public short? Level { get; set; }
        public short? Type { get; set; }
        public string? SalesUserId { get; set; }
        /// <summary>行业（与 Industry 字段精确匹配，如 Manufacturing）</summary>
        public string? Industry { get; set; }
        /// <summary>地区关键词（匹配省/市包含）</summary>
        public string? Region { get; set; }
        /// <summary>创建时间起（含当日 00:00:00 UTC 起算，由 API 解析）</summary>
        public DateTime? CreatedFrom { get; set; }
        /// <summary>创建时间止（含当日，按日期边界过滤）</summary>
        public DateTime? CreatedTo { get; set; }
        /// <summary>工作流状态：1 新建、2 待审核、10 已审核、12 待财务审核、20 财务建档、-1 审核失败等</summary>
        public short? Status { get; set; }
        public string? CurrentUserId { get; set; }
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// 添加地址请求
    /// </summary>
    public class AddAddressRequest
    {
        public short AddressType { get; set; } = 1;
        public short? Country { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Area { get; set; }
        public string? Address { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// 更新地址请求
    /// </summary>
    public class UpdateAddressRequest
    {
        public short? AddressType { get; set; }
        public short? Country { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Area { get; set; }
        public string? Address { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public bool? IsDefault { get; set; }
    }

    /// <summary>
    /// 添加联系人请求
    /// </summary>
    public class AddContactRequest
    {
        public string? Name { get; set; }
        /// <summary>ContactName 是 Name 的别名，前端兼容字段</summary>
        public string? ContactName { get => Name; set => Name = value; }
        public short? Gender { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
        /// <summary>Tel 是 Phone 的别名，前端兼容字段</summary>
        public string? Tel { get => Phone; set => Phone = value; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Fax { get; set; }
        public bool? IsDefault { get; set; } = false;
    }

    /// <summary>
    /// 更新联系人请求
    /// </summary>
    public class UpdateContactRequest
    {
        public string? Name { get; set; }
        /// <summary>ContactName 是 Name 的别名，前端兼容字段</summary>
        public string? ContactName { get => Name; set => Name = value; }
        public short? Gender { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
        /// <summary>Tel 是 Phone 的别名，前端兼容字段</summary>
        public string? Tel { get => Phone; set => Phone = value; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Fax { get; set; }
        public bool? IsDefault { get; set; }
    }

    /// <summary>
    /// 添加银行信息请求
    /// </summary>
    public class AddBankRequest
    {
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        /// <summary>AccountNumber 是 BankAccount 的别名，前端兼容字段</summary>
        public string? AccountNumber { get => BankAccount; set => BankAccount = value; }
        public string? AccountName { get; set; }
        public string? BankBranch { get; set; }
        /// <summary>BankCode 是 BankBranch 的别名（SWIFT代码），前端兼容字段</summary>
        public string? BankCode { get => BankBranch; set => BankBranch = value; }
        public short? Currency { get; set; }
        public bool IsDefault { get; set; } = false;
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新银行信息请求
    /// </summary>
    public class UpdateBankRequest
    {
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        /// <summary>AccountNumber 是 BankAccount 的别名，前端兼容字段</summary>
        public string? AccountNumber { get => BankAccount; set => BankAccount = value; }
        public string? AccountName { get; set; }
        public string? BankBranch { get; set; }
        /// <summary>BankCode 是 BankBranch 的别名（SWIFT代码），前端兼容字段</summary>
        public string? BankCode { get => BankBranch; set => BankBranch = value; }
        public short? Currency { get; set; }
        public bool? IsDefault { get; set; }
        public string? Remark { get; set; }
    }
}
