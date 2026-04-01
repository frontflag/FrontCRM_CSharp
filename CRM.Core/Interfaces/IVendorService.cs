using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// Vendor服务接口
    /// </summary>
    public interface IVendorService
    {
        /// <summary>
        /// 创建
        /// </summary>
        Task<VendorInfo> CreateAsync(CreateVendorRequest request);

        /// <summary>
        /// 批量导入供应商（Excel 解析后的结构化数据）
        /// </summary>
        Task<VendorImportBatchResult> ImportVendorsBatchAsync(VendorImportBatchRequest request);

        /// <summary>
        /// 根据ID获取
        /// </summary>
        Task<VendorInfo?> GetByIdAsync(string id);

        /// <summary>
        /// 获取所有
        /// </summary>
        Task<IEnumerable<VendorInfo>> GetAllAsync();

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<PagedResult<VendorInfo>> GetPagedAsync(VendorQueryRequest request);

        /// <summary>
        /// 更新
        /// </summary>
        Task<VendorInfo> UpdateAsync(string id, UpdateVendorRequest request);

        /// <summary>
        /// 删除
        /// </summary>
        Task DeleteAsync(string id, string? reason = null);

        /// <summary>
        /// 批量删除
        /// </summary>
        Task BatchDeleteAsync(IEnumerable<string> ids, string? reason = null);

        /// <summary>
        /// 更新状态
        /// </summary>
        Task UpdateStatusAsync(string id, short status, string? auditRemark = null);

        /// <summary>
        /// 搜索
        /// </summary>
        Task<IEnumerable<VendorInfo>> SearchAsync(string keyword);

        /// <summary>
        /// 获取供应商联系人列表
        /// </summary>
        Task<IEnumerable<VendorContactInfo>> GetContactsByVendorIdAsync(string vendorId);

        /// <summary>
        /// 添加供应商联系人
        /// </summary>
        Task<VendorContactInfo> AddContactAsync(string vendorId, AddVendorContactRequest request);

        /// <summary>
        /// 更新供应商联系人
        /// </summary>
        Task<VendorContactInfo> UpdateContactAsync(string contactId, UpdateVendorContactRequest request);

        /// <summary>
        /// 删除供应商联系人
        /// </summary>
        Task DeleteContactAsync(string contactId);

        /// <summary>
        /// 设置主联系人
        /// </summary>
        Task SetMainContactAsync(string contactId);

        /// <summary>
        /// 获取供应商地址列表
        /// </summary>
        Task<IEnumerable<VendorAddress>> GetAddressesByVendorIdAsync(string vendorId);

        /// <summary>
        /// 添加供应商地址
        /// </summary>
        Task<VendorAddress> AddAddressAsync(string vendorId, AddVendorAddressRequest request);

        /// <summary>
        /// 更新供应商地址
        /// </summary>
        Task<VendorAddress> UpdateAddressAsync(string addressId, UpdateVendorAddressRequest request);

        /// <summary>
        /// 删除供应商地址
        /// </summary>
        Task DeleteAddressAsync(string addressId);

        /// <summary>
        /// 设置默认地址
        /// </summary>
        Task SetDefaultAddressAsync(string addressId);

        /// <summary>
        /// 获取供应商银行账户列表
        /// </summary>
        Task<IEnumerable<VendorBankInfo>> GetBanksByVendorIdAsync(string vendorId);

        /// <summary>
        /// 添加供应商银行账户
        /// </summary>
        Task<VendorBankInfo> AddBankAsync(string vendorId, AddVendorBankRequest request);

        /// <summary>
        /// 更新供应商银行账户
        /// </summary>
        Task<VendorBankInfo> UpdateBankAsync(string bankId, UpdateVendorBankRequest request);

        /// <summary>
        /// 删除供应商银行账户
        /// </summary>
        Task DeleteBankAsync(string bankId);

        /// <summary>
        /// 设置默认银行账户
        /// </summary>
        Task SetDefaultBankAsync(string bankId);

        /// <summary>
        /// 加入黑名单
        /// </summary>
        Task AddToBlacklistAsync(string id, string? reason);

        /// <summary>
        /// 移出黑名单（需原因，写入操作日志）
        /// </summary>
        Task RemoveFromBlacklistAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>
        /// 获取黑名单供应商分页列表
        /// </summary>
        Task<PagedResult<VendorInfo>> GetBlacklistAsync(VendorQueryRequest request);

        /// <summary>
        /// 获取已冻结（禁用）供应商分页列表
        /// </summary>
        Task<PagedResult<VendorInfo>> GetFrozenAsync(VendorQueryRequest request);

        /// <summary>
        /// 获取已删除供应商（回收站）分页列表
        /// </summary>
        Task<PagedResult<VendorInfo>> GetDeletedAsync(int pageIndex, int pageSize, string? keyword);

        /// <summary>
        /// 恢复已删除供应商
        /// </summary>
        Task RestoreAsync(string id);

        /// <summary>
        /// 获取供应商联系历史
        /// </summary>
        Task<IEnumerable<VendorContactHistory>> GetContactHistoryAsync(string vendorId);

        /// <summary>
        /// 添加供应商联系记录
        /// </summary>
        Task<VendorContactHistory> AddContactHistoryAsync(string vendorId, AddVendorContactHistoryRequest request);

        /// <summary>
        /// 更新供应商联系记录
        /// </summary>
        Task<VendorContactHistory> UpdateContactHistoryAsync(string historyId, UpdateVendorContactHistoryRequest request);

        /// <summary>
        /// 删除供应商联系记录
        /// </summary>
        Task DeleteContactHistoryAsync(string historyId);

        /// <summary>
        /// 获取供应商操作日志
        /// </summary>
        Task<IEnumerable<VendorOperationLog>> GetOperationLogsAsync(string vendorId);

        /// <summary>
        /// 获取供应商字段变更日志
        /// </summary>
        Task<IEnumerable<VendorChangeLog>> GetChangeLogsAsync(string vendorId);

        /// <summary>冻结供应商（禁用）</summary>
        Task FreezeVendorAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>启用供应商（解除冻结）</summary>
        Task UnfreezeVendorAsync(string id, string reason, string? operatorUserId, string? operatorUserName);

        /// <summary>记录供应商主体操作日志（写入 log_operation）</summary>
        Task AddOperationLogAsync(string vendorId, string operationType, string? desc, string? userId, string? userName, string? remark = null);
    }

    /// <summary>
    /// 冻结/启用供应商请求（原因写入操作日志）
    /// </summary>
    public class FreezeVendorRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 添加供应商联系人请求
    /// </summary>
    public class AddVendorContactRequest
    {
        public string? CName { get; set; }
        public string? EName { get; set; }
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Mobile { get; set; }
        public string? Tel { get; set; }
        public string? Email { get; set; }
        public bool IsMain { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新供应商联系人请求
    /// </summary>
    public class UpdateVendorContactRequest
    {
        public string? CName { get; set; }
        public string? EName { get; set; }
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Mobile { get; set; }
        public string? Tel { get; set; }
        public string? Email { get; set; }
        public bool? IsMain { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 添加供应商地址请求
    /// </summary>
    public class AddVendorAddressRequest
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
    /// 更新供应商地址请求
    /// </summary>
    public class UpdateVendorAddressRequest
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
    /// 添加供应商银行信息请求
    /// </summary>
    public class AddVendorBankRequest
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
    /// 更新供应商银行信息请求
    /// </summary>
    public class UpdateVendorBankRequest
    {
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? AccountName { get; set; }
        public string? BankBranch { get; set; }
        public short? Currency { get; set; }
        public bool? IsDefault { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 添加供应商联系历史请求
    /// </summary>
    public class AddVendorContactHistoryRequest
    {
        public string? Type { get; set; } = "call";
        /// <summary>ContactType 是 Type 的别名，前端兼容字段（可为数字或字符串）</summary>
        public string? ContactType
        {
            get => Type;
            set => Type = value is null ? null : int.TryParse(value, out var n) ? n switch
            {
                1 => "call",
                2 => "visit",
                3 => "email",
                4 => "meeting",
                _ => "other"
            } : value;
        }

        public string? Subject { get; set; }
        public string? Content { get; set; }
        public string? ContactPerson { get; set; }
        public DateTime? Time { get; set; }
        /// <summary>ContactTime 是 Time 的别名，与客户模块一致</summary>
        public DateTime? ContactTime { get => Time; set => Time = value; }
        public DateTime? NextFollowUpTime { get; set; }
        public string? Result { get; set; }
    }

    /// <summary>
    /// 更新供应商联系历史请求
    /// </summary>
    public class UpdateVendorContactHistoryRequest
    {
        public string? Type { get; set; }
        public string? ContactType
        {
            get => Type;
            set => Type = value is null ? null : int.TryParse(value, out var n) ? n switch
            {
                1 => "call",
                2 => "visit",
                3 => "email",
                4 => "meeting",
                _ => "other"
            } : value;
        }

        public string? Subject { get; set; }
        public string? Content { get; set; }
        public string? ContactPerson { get; set; }
        public DateTime? Time { get; set; }
        public DateTime? NextFollowUpTime { get; set; }
        public string? Result { get; set; }
    }

    /// <summary>
    /// Excel 批量导入供应商请求体
    /// </summary>
    public class VendorImportBatchRequest
    {
        public List<VendorImportBatchItem> Items { get; set; } = new();
    }

    /// <summary>
    /// 单条导入：一个供应商及其联系人
    /// </summary>
    public class VendorImportBatchItem
    {
        public CreateVendorRequest Vendor { get; set; } = new();
        public List<AddVendorContactRequest> Contacts { get; set; } = new();
    }

    /// <summary>
    /// 批量导入结果
    /// </summary>
    public class VendorImportBatchResult
    {
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<VendorImportItemResult> Items { get; set; } = new();
    }

    /// <summary>
    /// 单条导入结果
    /// </summary>
    public class VendorImportItemResult
    {
        public int Index { get; set; }
        public bool Success { get; set; }
        public string? VendorCode { get; set; }
        public string? VendorId { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 创建请求
    /// </summary>
    public class CreateVendorRequest
    {
        public string? Code { get; set; }
        /// <summary>公司全称（与 OfficialName 二选一）</summary>
        public string? Name { get; set; }
        /// <summary>前端表单字段 officialName，草稿 JSON 常用此键</summary>
        public string? OfficialName { get; set; }
        public string? NickName { get; set; }
        /// <summary>行业分类</summary>
        public string? Industry { get; set; }
        /// <summary>等级（VendorLevelCode → vendorinfo.Level）</summary>
        public short? Level { get; set; }
        /// <summary>身份（VendorIdentityCode → vendorinfo.Credit）</summary>
        public short? Credit { get; set; }
        /// <summary>状态：1新建 2待审核 10已审核 12待财务审核 20财务建档 -1审核失败</summary>
        public short? Status { get; set; }
        public string? OfficeAddress { get; set; }
        public string? Website { get; set; }
        public string? PurchaserName { get; set; }
        /// <summary>结算货币（与前端 CurrencyCode 一致）</summary>
        public short? TradeCurrency { get; set; }
        /// <summary>与 TradeCurrency 二选一（草稿 formData 使用 currency）</summary>
        public short? Currency { get; set; }
        /// <summary>付款方式编码（如 Prepaid、TT）</summary>
        public string? PaymentMethod { get; set; }
        /// <summary>账期天数</summary>
        public short? PaymentDays { get; set; }
        /// <summary>税号 / 统一社会信用代码</summary>
        public string? CreditCode { get; set; }
        /// <summary>与 CreditCode 二选一（前端表单 taxNumber）</summary>
        public string? TaxNumber { get; set; }
        /// <summary>公司简介</summary>
        public string? CompanyInfo { get; set; }
        /// <summary>其他备注</summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新请求
    /// </summary>
    public class UpdateVendorRequest
    {
        public string? Name { get; set; }
        public string? NickName { get; set; }
        /// <summary>行业分类</summary>
        public string? Industry { get; set; }
        /// <summary>供应商分类/主营产品</summary>
        public string? Product { get; set; }
        /// <summary>身份（VendorIdentityCode → vendorinfo.Credit）</summary>
        public short? Credit { get; set; }
        /// <summary>合作状态</summary>
        public short? Status { get; set; }
        /// <summary>办公地址</summary>
        public string? OfficeAddress { get; set; }
        public string? Website { get; set; }
        public string? PurchaserName { get; set; }
        /// <summary>供应商等级</summary>
        public short? Level { get; set; }
        /// <summary>贸易币种</summary>
        public short? TradeCurrency { get; set; }
        /// <summary>付款方式（字符串，存 PaymentMethod）</summary>
        public string? PaymentMethod { get; set; }
        /// <summary>账期（天）</summary>
        public short? PaymentDays { get; set; }
        /// <summary>兼容旧字段：与 PaymentDays 同义</summary>
        public short? Payment { get; set; }
        /// <summary>税号</summary>
        public string? CreditCode { get; set; }
        /// <summary>公司简介</summary>
        public string? CompanyInfo { get; set; }
        /// <summary>其他备注</summary>
        public string? Remark { get; set; }
        /// <summary>外部编号</summary>
        public string? ExternalNumber { get; set; }
    }

    /// <summary>
    /// 查询请求
    /// </summary>
    public class VendorQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public short? Status { get; set; }
        /// <summary>供应商等级（与 vendorinfo.Level 一致）</summary>
        public short? Level { get; set; }
        /// <summary>行业关键字（包含匹配）</summary>
        public string? Industry { get; set; }
        /// <summary>身份（vendorinfo.Credit，VendorIdentityCode）</summary>
        public short? Credit { get; set; }
        /// <summary>供应商归属：1 专属 2 公海（AscriptionType）</summary>
        public short? AscriptionType { get; set; }
        public string? PurchaseUserId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? CurrentUserId { get; set; }
    }

    /// <summary>
    /// 供应商操作日志
    /// </summary>
    public class VendorOperationLog
    {
        public string Id { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string? OperationDesc { get; set; }
        public string? OperatorUserId { get; set; }
        public string? OperatorUserName { get; set; }
        public DateTime OperationTime { get; set; }
        public string? Remark { get; set; }
        public string? BizType { get; set; }
        public string? RecordCode { get; set; }
    }

    /// <summary>
    /// 供应商字段变更日志
    /// </summary>
    public class VendorChangeLog
    {
        public string Id { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
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
}
