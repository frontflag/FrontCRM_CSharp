using CRM.Core.Constants;
using CRM.Core.Models.Customer;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class CustomerService
    {
        private async Task<(string? UserId, string UserName)> ResolveFieldChangeActorAsync(string? actingUserId) =>
            await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);

        private async Task<string?> ResolveUserDisplayNameAsync(string? userId)
        {
            var id = ActingUserIdNormalizer.Normalize(userId);
            if (string.IsNullOrEmpty(id))
                return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            var user = await _userService.GetByIdAsync(id);
            return string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim();
        }

        private static string FormatBool(bool value) => value ? "是" : "否";

        private static string FormatAddressType(short value) => value switch
        {
            1 => "收货地址",
            2 => "账单地址",
            _ => value.ToString()
        };

        private static string FormatGender(short? value) => value switch
        {
            1 => "男",
            2 => "女",
            0 => "保密",
            _ => value?.ToString() ?? ""
        };

        private async Task AppendCustomerFieldChangeLogAsync(
            CustomerInfo customer,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                BusinessLogTypes.Customer,
                customer.Id,
                customer.CustomerCode,
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private async Task AppendSubEntityFieldChangeLogAsync(
            string bizType,
            string recordId,
            string? recordCode,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                bizType,
                recordId,
                recordCode,
                fieldName,
                fieldLabel,
                oldValue,
                newValue,
                userId,
                userName);
        }

        private sealed record CustomerHeaderSnapshot(
            string? OfficialName,
            string? StandardOfficialName,
            string? EnglishOfficialName,
            string? NickName,
            short Level,
            short? Type,
            string? Industry,
            string? Product,
            string? CompanyInfo,
            string? Remark,
            string? SalesUserId,
            decimal CreditLine,
            short? Payment,
            short? TradeCurrency,
            string? CreditCode,
            string? DUNS,
            string? Province,
            string? City,
            string? District);

        private static CustomerHeaderSnapshot CaptureCustomerHeaderSnapshot(CustomerInfo customer) =>
            new(
                customer.OfficialName,
                customer.StandardOfficialName,
                customer.EnglishOfficialName,
                customer.NickName,
                customer.Level,
                customer.Type,
                customer.Industry,
                customer.Product,
                customer.CompanyInfo,
                customer.Remark,
                customer.SalesUserId,
                customer.CreditLine,
                customer.Payment,
                customer.TradeCurrency,
                customer.CreditCode,
                customer.DUNS,
                customer.Province,
                customer.City,
                customer.District);

        private async Task LogCustomerHeaderFieldChangesAsync(
            CustomerInfo customer,
            CustomerHeaderSnapshot before,
            string? actingUserId)
        {
            var after = CaptureCustomerHeaderSnapshot(customer);
            await AppendCustomerFieldChangeLogAsync(customer, "officialName", "公司全称", before.OfficialName, after.OfficialName, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "standardOfficialName", "标准全称", before.StandardOfficialName, after.StandardOfficialName, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "englishOfficialName", "英文全称", before.EnglishOfficialName, after.EnglishOfficialName, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "nickName", "公司简称", before.NickName, after.NickName, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "level", "客户等级", before.Level.ToString(), after.Level.ToString(), actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "type", "客户类型", before.Type?.ToString(), after.Type?.ToString(), actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "industry", "行业", before.Industry, after.Industry, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "product", "主营产品", before.Product, after.Product, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "companyInfo", "公司信息", before.CompanyInfo, after.CompanyInfo, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "remark", "备注", before.Remark, after.Remark, actingUserId);
            var oldSales = await ResolveUserDisplayNameAsync(before.SalesUserId);
            var newSales = await ResolveUserDisplayNameAsync(after.SalesUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "salesUserId", "归属销售", oldSales, newSales, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "creditLine", "信用额度", before.CreditLine.ToString("0.##"), after.CreditLine.ToString("0.##"), actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "payment", "账期", before.Payment?.ToString(), after.Payment?.ToString(), actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "tradeCurrency", "交易币别", before.TradeCurrency?.ToString(), after.TradeCurrency?.ToString(), actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "creditCode", "统一社会信用代码", before.CreditCode, after.CreditCode, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "duns", "邓白氏编码", before.DUNS, after.DUNS, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "province", "省", before.Province, after.Province, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "city", "市", before.City, after.City, actingUserId);
            await AppendCustomerFieldChangeLogAsync(customer, "district", "区", before.District, after.District, actingUserId);
        }

        private async Task LogCustomerStatusFieldChangeAsync(
            CustomerInfo customer,
            short oldStatus,
            short newStatus,
            string? actingUserId)
        {
            await AppendCustomerFieldChangeLogAsync(
                customer,
                "status",
                "状态",
                MasterEntityStatusLabels.Format(oldStatus),
                MasterEntityStatusLabels.Format(newStatus),
                actingUserId);
        }

        private sealed record CustomerContactSnapshot(
            string? CName,
            string? EName,
            short? Gender,
            string? Department,
            string? Position,
            string? Phone,
            string? Mobile,
            string? Email,
            string? Fax,
            bool IsDefault);

        private static CustomerContactSnapshot CaptureCustomerContactSnapshot(CustomerContactInfo contact) =>
            new(
                contact.CName,
                contact.EName,
                contact.Gender,
                contact.Department,
                contact.Position,
                contact.Phone,
                contact.Mobile,
                contact.Email,
                contact.Fax,
                contact.IsDefault);

        private static string CustomerContactRecordCode(CustomerContactInfo contact) =>
            string.IsNullOrWhiteSpace(contact.CName) ? contact.EName : contact.CName;

        private async Task LogCustomerContactFieldChangesAsync(
            CustomerContactInfo contact,
            CustomerContactSnapshot before,
            string? actingUserId)
        {
            var after = CaptureCustomerContactSnapshot(contact);
            var code = CustomerContactRecordCode(contact);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "cName", "中文名", before.CName, after.CName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "eName", "英文名", before.EName, after.EName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "gender", "性别", FormatGender(before.Gender), FormatGender(after.Gender), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "department", "部门", before.Department, after.Department, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "position", "职位", before.Position, after.Position, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "phone", "电话", before.Phone, after.Phone, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "mobile", "手机", before.Mobile, after.Mobile, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "email", "邮箱", before.Email, after.Email, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "fax", "传真", before.Fax, after.Fax, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContact, contact.Id, code, "isDefault", "默认联系人", FormatBool(before.IsDefault), FormatBool(after.IsDefault), actingUserId);
        }

        private async Task LogCustomerContactAddedAsync(CustomerContactInfo contact, string? actingUserId)
        {
            var name = CustomerContactRecordCode(contact) ?? contact.Id;
            var summary = $"{name} · {contact.Mobile ?? contact.Phone ?? "—"}";
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.CustomerContact,
                contact.Id,
                name,
                "lineAdded",
                "新增联系人",
                null,
                summary,
                actingUserId);
        }

        private sealed record CustomerAddressSnapshot(
            short AddressType,
            short? Country,
            string? CountryName,
            string? Province,
            string? City,
            string? Area,
            string? Address,
            string? CompanyName,
            string? ZipCode,
            string? ContactName,
            string? ContactPhone,
            bool IsDefault);

        private static CustomerAddressSnapshot CaptureCustomerAddressSnapshot(CustomerAddress address) =>
            new(
                address.AddressType,
                address.Country,
                address.CountryName,
                address.Province,
                address.City,
                address.Area,
                address.Address,
                address.CompanyName,
                address.ZipCode,
                address.ContactName,
                address.ContactPhone,
                address.IsDefault);

        private static string CustomerAddressRecordCode(CustomerAddress address) =>
            $"{FormatAddressType(address.AddressType)} · {address.Address ?? address.City ?? address.Id}";

        private async Task LogCustomerAddressFieldChangesAsync(
            CustomerAddress address,
            CustomerAddressSnapshot before,
            string? actingUserId)
        {
            var after = CaptureCustomerAddressSnapshot(address);
            var code = CustomerAddressRecordCode(address);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "addressType", "地址类型", FormatAddressType(before.AddressType), FormatAddressType(after.AddressType), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "country", "国家代码", before.Country?.ToString(), after.Country?.ToString(), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "countryName", "国家", before.CountryName, after.CountryName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "province", "省", before.Province, after.Province, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "city", "市", before.City, after.City, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "area", "区", before.Area, after.Area, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "address", "详细地址", before.Address, after.Address, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "companyName", "公司名称", before.CompanyName, after.CompanyName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "zipCode", "邮编", before.ZipCode, after.ZipCode, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "contactName", "联系人", before.ContactName, after.ContactName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "contactPhone", "联系电话", before.ContactPhone, after.ContactPhone, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerAddress, address.Id, code, "isDefault", "默认地址", FormatBool(before.IsDefault), FormatBool(after.IsDefault), actingUserId);
        }

        private async Task LogCustomerAddressAddedAsync(CustomerAddress address, string? actingUserId)
        {
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.CustomerAddress,
                address.Id,
                CustomerAddressRecordCode(address),
                "lineAdded",
                "新增地址",
                null,
                CustomerAddressRecordCode(address),
                actingUserId);
        }

        private sealed record CustomerBankSnapshot(
            string? BankName,
            string? BankAccount,
            string? AccountName,
            string? BankBranch,
            string? BankAddress,
            string? BankCode,
            string? Swift,
            short? Currency,
            bool IsDefault,
            string? Remark);

        private static CustomerBankSnapshot CaptureCustomerBankSnapshot(CustomerBankInfo bank) =>
            new(
                bank.BankName,
                bank.BankAccount,
                bank.AccountName,
                bank.BankBranch,
                bank.BankAddress,
                bank.BankCode,
                bank.Swift,
                bank.Currency,
                bank.IsDefault,
                bank.Remark);

        private static string CustomerBankRecordCode(CustomerBankInfo bank) =>
            $"{bank.BankName ?? "银行"} · {bank.BankAccount ?? bank.Id}";

        private async Task LogCustomerBankFieldChangesAsync(
            CustomerBankInfo bank,
            CustomerBankSnapshot before,
            string? actingUserId)
        {
            var after = CaptureCustomerBankSnapshot(bank);
            var code = CustomerBankRecordCode(bank);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "bankName", "银行名称", before.BankName, after.BankName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "bankAccount", "银行账号", before.BankAccount, after.BankAccount, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "accountName", "户名", before.AccountName, after.AccountName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "bankBranch", "开户行", before.BankBranch, after.BankBranch, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "bankAddress", "银行地址", before.BankAddress, after.BankAddress, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "bankCode", "行号", before.BankCode, after.BankCode, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "swift", "SWIFT", before.Swift, after.Swift, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "currency", "币别", before.Currency?.ToString(), after.Currency?.ToString(), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "isDefault", "默认账户", FormatBool(before.IsDefault), FormatBool(after.IsDefault), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerBank, bank.Id, code, "remark", "备注", before.Remark, after.Remark, actingUserId);
        }

        private async Task LogCustomerBankAddedAsync(CustomerBankInfo bank, string? actingUserId)
        {
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.CustomerBank,
                bank.Id,
                CustomerBankRecordCode(bank),
                "lineAdded",
                "新增银行账户",
                null,
                CustomerBankRecordCode(bank),
                actingUserId);
        }

        private sealed record CustomerContactHistorySnapshot(
            string? Type,
            string? Subject,
            string? Content,
            string? ContactPerson,
            DateTime? Time,
            DateTime? NextFollowUpTime,
            string? Result);

        private static CustomerContactHistorySnapshot CaptureCustomerContactHistorySnapshot(CustomerContactHistory record) =>
            new(
                record.Type,
                record.Subject,
                record.Content,
                record.ContactPerson,
                record.Time,
                record.NextFollowUpTime,
                record.Result);

        private static string FormatDateTimeValue(DateTime? value) =>
            value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm") : null;

        private async Task LogCustomerContactHistoryFieldChangesAsync(
            CustomerContactHistory record,
            CustomerContactHistorySnapshot before,
            string? actingUserId)
        {
            var after = CaptureCustomerContactHistorySnapshot(record);
            var code = string.IsNullOrWhiteSpace(record.Subject) ? record.Type : record.Subject;
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "type", "类型", before.Type, after.Type, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "subject", "主题", before.Subject, after.Subject, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "content", "内容", before.Content, after.Content, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "contactPerson", "联系人", before.ContactPerson, after.ContactPerson, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "time", "联系时间", FormatDateTimeValue(before.Time), FormatDateTimeValue(after.Time), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "nextFollowUpTime", "下次跟进", FormatDateTimeValue(before.NextFollowUpTime), FormatDateTimeValue(after.NextFollowUpTime), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.CustomerContactHistory, record.Id, code, "result", "结果", before.Result, after.Result, actingUserId);
        }

        private async Task LogCustomerContactHistoryAddedAsync(CustomerContactHistory record, string? actingUserId)
        {
            var summary = string.IsNullOrWhiteSpace(record.Subject)
                ? $"{record.Type} · {FormatDateTimeValue(record.Time) ?? "—"}"
                : record.Subject;
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.CustomerContactHistory,
                record.Id,
                summary,
                "lineAdded",
                "新增联系记录",
                null,
                summary,
                actingUserId);
        }
    }
}
