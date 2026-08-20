using CRM.Core.Constants;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class VendorService
    {
        private async Task<(string? UserId, string UserName)> ResolveFieldChangeActorAsync(string? actingUserId) =>
            await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);

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

        private async Task AppendVendorFieldChangeLogAsync(
            VendorInfo vendor,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveFieldChangeActorAsync(actingUserId);
            await FieldChangeLogAppender.AppendIfChangedAsync(
                _unitOfWork,
                BusinessLogTypes.Vendor,
                vendor.Id,
                vendor.Code,
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

        private sealed record VendorHeaderSnapshot(
            string? OfficialName,
            string? NickName,
            string? EnglishOfficialName,
            string? Industry,
            string? Product,
            short? Credit,
            short Status,
            string? OfficeAddress,
            string? Website,
            string? PurchaserName,
            short? Level,
            short? TradeCurrency,
            string? PaymentMethod,
            short? Payment,
            string? CreditCode,
            string? DUNS,
            string? CompanyInfo,
            string? Remark,
            string? ExternalNumber,
            string? PurchaseUserId);

        private static VendorHeaderSnapshot CaptureVendorHeaderSnapshot(VendorInfo vendor) =>
            new(
                vendor.OfficialName,
                vendor.NickName,
                vendor.EnglishOfficialName,
                vendor.Industry,
                vendor.Product,
                vendor.Credit,
                vendor.Status,
                vendor.OfficeAddress,
                vendor.Website,
                vendor.PurchaserName,
                vendor.Level,
                vendor.TradeCurrency,
                vendor.PaymentMethod,
                vendor.Payment,
                vendor.CreditCode,
                vendor.DUNS,
                vendor.CompanyInfo,
                vendor.Remark,
                vendor.ExternalNumber,
                vendor.PurchaseUserId);

        private async Task LogVendorHeaderFieldChangesAsync(
            VendorInfo vendor,
            VendorHeaderSnapshot before,
            string? actingUserId)
        {
            var after = CaptureVendorHeaderSnapshot(vendor);
            await AppendVendorFieldChangeLogAsync(vendor, "officialName", "公司全称", before.OfficialName, after.OfficialName, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "nickName", "公司简称", before.NickName, after.NickName, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "englishOfficialName", "英文全称", before.EnglishOfficialName, after.EnglishOfficialName, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "industry", "行业", before.Industry, after.Industry, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "product", "主营产品", before.Product, after.Product, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "credit", "信用评级", before.Credit?.ToString(), after.Credit?.ToString(), actingUserId);
            await AppendVendorFieldChangeLogAsync(
                vendor,
                "status",
                "状态",
                MasterEntityStatusLabels.Format(before.Status),
                MasterEntityStatusLabels.Format(after.Status),
                actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "officeAddress", "办公地址", before.OfficeAddress, after.OfficeAddress, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "website", "网站", before.Website, after.Website, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "purchaserName", "采购员", before.PurchaserName, after.PurchaserName, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "level", "等级", before.Level?.ToString(), after.Level?.ToString(), actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "tradeCurrency", "交易币别", before.TradeCurrency?.ToString(), after.TradeCurrency?.ToString(), actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "paymentMethod", "付款方式", before.PaymentMethod, after.PaymentMethod, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "payment", "账期", before.Payment?.ToString(), after.Payment?.ToString(), actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "creditCode", "统一社会信用代码", before.CreditCode, after.CreditCode, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "duns", "邓白氏编码", before.DUNS, after.DUNS, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "companyInfo", "公司信息", before.CompanyInfo, after.CompanyInfo, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "remark", "备注", before.Remark, after.Remark, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "externalNumber", "外部编号", before.ExternalNumber, after.ExternalNumber, actingUserId);
            await AppendVendorFieldChangeLogAsync(vendor, "purchaseUserId", "归属采购员", before.PurchaseUserId, after.PurchaseUserId, actingUserId);
        }

        private async Task LogVendorStatusFieldChangeAsync(
            VendorInfo vendor,
            short oldStatus,
            short newStatus,
            string? actingUserId)
        {
            await AppendVendorFieldChangeLogAsync(
                vendor,
                "status",
                "状态",
                MasterEntityStatusLabels.Format(oldStatus),
                MasterEntityStatusLabels.Format(newStatus),
                actingUserId);
        }

        private sealed record VendorContactSnapshot(
            string? CName,
            string? EName,
            string? Title,
            string? Department,
            string? Mobile,
            string? Tel,
            string? Email,
            short? Gender,
            bool IsMain,
            string? Remark);

        private static VendorContactSnapshot CaptureVendorContactSnapshot(VendorContactInfo contact) =>
            new(
                contact.CName,
                contact.EName,
                contact.Title,
                contact.Department,
                contact.Mobile,
                contact.Tel,
                contact.Email,
                contact.Gender,
                contact.IsMain,
                contact.Remark);

        private static string VendorContactRecordCode(VendorContactInfo contact) =>
            string.IsNullOrWhiteSpace(contact.CName) ? contact.EName : contact.CName;

        private async Task LogVendorContactFieldChangesAsync(
            VendorContactInfo contact,
            VendorContactSnapshot before,
            string? actingUserId)
        {
            var after = CaptureVendorContactSnapshot(contact);
            var code = VendorContactRecordCode(contact);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "cName", "中文名", before.CName, after.CName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "eName", "英文名", before.EName, after.EName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "title", "职位", before.Title, after.Title, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "department", "部门", before.Department, after.Department, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "mobile", "手机", before.Mobile, after.Mobile, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "tel", "电话", before.Tel, after.Tel, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "email", "邮箱", before.Email, after.Email, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "gender", "性别", FormatGender(before.Gender), FormatGender(after.Gender), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "isMain", "主联系人", FormatBool(before.IsMain), FormatBool(after.IsMain), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContact, contact.Id, code, "remark", "备注", before.Remark, after.Remark, actingUserId);
        }

        private async Task LogVendorContactAddedAsync(VendorContactInfo contact, string? actingUserId)
        {
            var name = VendorContactRecordCode(contact) ?? contact.Id;
            var summary = $"{name} · {contact.Mobile ?? contact.Tel ?? "—"}";
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.VendorContact,
                contact.Id,
                name,
                "lineAdded",
                "新增联系人",
                null,
                summary,
                actingUserId);
        }

        private sealed record VendorAddressSnapshot(
            short AddressType,
            short? Country,
            string? Province,
            string? City,
            string? Area,
            string? Address,
            string? ContactName,
            string? ContactPhone,
            bool IsDefault);

        private static VendorAddressSnapshot CaptureVendorAddressSnapshot(VendorAddress address) =>
            new(
                address.AddressType,
                address.Country,
                address.Province,
                address.City,
                address.Area,
                address.Address,
                address.ContactName,
                address.ContactPhone,
                address.IsDefault);

        private static string VendorAddressRecordCode(VendorAddress address) =>
            $"{FormatAddressType(address.AddressType)} · {address.Address ?? address.City ?? address.Id}";

        private async Task LogVendorAddressFieldChangesAsync(
            VendorAddress address,
            VendorAddressSnapshot before,
            string? actingUserId)
        {
            var after = CaptureVendorAddressSnapshot(address);
            var code = VendorAddressRecordCode(address);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "addressType", "地址类型", FormatAddressType(before.AddressType), FormatAddressType(after.AddressType), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "country", "国家", before.Country?.ToString(), after.Country?.ToString(), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "province", "省", before.Province, after.Province, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "city", "市", before.City, after.City, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "area", "区", before.Area, after.Area, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "address", "详细地址", before.Address, after.Address, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "contactName", "联系人", before.ContactName, after.ContactName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "contactPhone", "联系电话", before.ContactPhone, after.ContactPhone, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorAddress, address.Id, code, "isDefault", "默认地址", FormatBool(before.IsDefault), FormatBool(after.IsDefault), actingUserId);
        }

        private async Task LogVendorAddressAddedAsync(VendorAddress address, string? actingUserId)
        {
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.VendorAddress,
                address.Id,
                VendorAddressRecordCode(address),
                "lineAdded",
                "新增地址",
                null,
                VendorAddressRecordCode(address),
                actingUserId);
        }

        private sealed record VendorBankSnapshot(
            string? FinancePaymentBankId,
            string? BankName,
            string? BankAccount,
            string? AccountName,
            string? BankBranch,
            string? BankAddress,
            string? Swift,
            string? Iban,
            string? BankCode,
            string? Country,
            string? AccountType,
            string? PurposeType,
            short? Currency,
            bool IsDefault,
            bool IsEnabled,
            string? Remark);

        private static VendorBankSnapshot CaptureVendorBankSnapshot(VendorBankInfo bank) =>
            new(
                bank.FinancePaymentBankId,
                bank.BankName,
                bank.BankAccount,
                bank.AccountName,
                bank.BankBranch,
                bank.BankAddress,
                bank.Swift,
                bank.Iban,
                bank.BankCode,
                bank.Country,
                bank.AccountType,
                bank.PurposeType,
                bank.Currency,
                bank.IsDefault,
                bank.IsEnabled,
                bank.Remark);

        private static string VendorBankRecordCode(VendorBankInfo bank) =>
            $"{bank.BankName ?? "银行"} · {bank.BankAccount ?? bank.Id}";

        private async Task LogVendorBankFieldChangesAsync(
            VendorBankInfo bank,
            VendorBankSnapshot before,
            string? actingUserId)
        {
            var after = CaptureVendorBankSnapshot(bank);
            var code = VendorBankRecordCode(bank);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "financePaymentBankId", "付款银行", before.FinancePaymentBankId, after.FinancePaymentBankId, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "bankName", "银行名称", before.BankName, after.BankName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "bankAccount", "银行账号", before.BankAccount, after.BankAccount, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "accountName", "户名", before.AccountName, after.AccountName, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "bankBranch", "开户行", before.BankBranch, after.BankBranch, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "bankAddress", "银行地址", before.BankAddress, after.BankAddress, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "swift", "SWIFT", before.Swift, after.Swift, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "iban", "IBAN", before.Iban, after.Iban, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "bankCode", "行号", before.BankCode, after.BankCode, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "country", "国家", before.Country, after.Country, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "accountType", "账户类型", before.AccountType, after.AccountType, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "purposeType", "用途", before.PurposeType, after.PurposeType, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "currency", "币别", before.Currency?.ToString(), after.Currency?.ToString(), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "isDefault", "默认账户", FormatBool(before.IsDefault), FormatBool(after.IsDefault), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "isEnabled", "启用", FormatBool(before.IsEnabled), FormatBool(after.IsEnabled), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorBank, bank.Id, code, "remark", "备注", before.Remark, after.Remark, actingUserId);
        }

        private async Task LogVendorBankAddedAsync(VendorBankInfo bank, string? actingUserId)
        {
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.VendorBank,
                bank.Id,
                VendorBankRecordCode(bank),
                "lineAdded",
                "新增银行账户",
                null,
                VendorBankRecordCode(bank),
                actingUserId);
        }

        private sealed record VendorContactHistorySnapshot(
            string? Type,
            string? Subject,
            string? Content,
            string? ContactPerson,
            DateTime? Time,
            DateTime? NextFollowUpTime,
            string? Result);

        private static VendorContactHistorySnapshot CaptureVendorContactHistorySnapshot(VendorContactHistory record) =>
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

        private async Task LogVendorContactHistoryFieldChangesAsync(
            VendorContactHistory record,
            VendorContactHistorySnapshot before,
            string? actingUserId)
        {
            var after = CaptureVendorContactHistorySnapshot(record);
            var code = string.IsNullOrWhiteSpace(record.Subject) ? record.Type : record.Subject;
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "type", "类型", before.Type, after.Type, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "subject", "主题", before.Subject, after.Subject, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "content", "内容", before.Content, after.Content, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "contactPerson", "联系人", before.ContactPerson, after.ContactPerson, actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "time", "联系时间", FormatDateTimeValue(before.Time), FormatDateTimeValue(after.Time), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "nextFollowUpTime", "下次跟进", FormatDateTimeValue(before.NextFollowUpTime), FormatDateTimeValue(after.NextFollowUpTime), actingUserId);
            await AppendSubEntityFieldChangeLogAsync(BusinessLogTypes.VendorContactHistory, record.Id, code, "result", "结果", before.Result, after.Result, actingUserId);
        }

        private async Task LogVendorContactHistoryAddedAsync(VendorContactHistory record, string? actingUserId)
        {
            var summary = string.IsNullOrWhiteSpace(record.Subject)
                ? $"{record.Type} · {FormatDateTimeValue(record.Time) ?? "—"}"
                : record.Subject;
            await AppendSubEntityFieldChangeLogAsync(
                BusinessLogTypes.VendorContactHistory,
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
