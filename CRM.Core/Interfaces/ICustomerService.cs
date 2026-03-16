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
        Task UpdateCustomerStatusAsync(string id, short status);

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
    }

    /// <summary>
    /// 添加联系历史请求
    /// </summary>
    public class AddContactHistoryRequest
    {
        public string? Type { get; set; } = "call";
        public string? Content { get; set; }
        public DateTime? Time { get; set; }
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
        public short? Status { get; set; }
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
        public short? Gender { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
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
        public short? Gender { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
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
        public string? AccountName { get; set; }
        public string? BankBranch { get; set; }
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
        public string? AccountName { get; set; }
        public string? BankBranch { get; set; }
        public short? Currency { get; set; }
        public bool? IsDefault { get; set; }
        public string? Remark { get; set; }
    }
}
