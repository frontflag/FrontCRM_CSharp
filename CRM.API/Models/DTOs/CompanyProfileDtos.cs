namespace CRM.API.Models.DTOs
{
    public interface ICompanyProfileRow
    {
        bool IsDefault { get; }
    }

    public class CompanyProfileBundleDto
    {
        public List<CompanyBasicInfoRowDto> BasicInfos { get; set; } = new();
        public List<CompanyBankInfoRowDto> BankInfos { get; set; } = new();
        public List<CompanyLogoRowDto> Logos { get; set; } = new();
        public List<CompanySealRowDto> Seals { get; set; } = new();
        public List<CompanyWarehouseRowDto> Warehouses { get; set; } = new();

        /// <summary>系统发信（SMTP）。GET 时 Password 为空，PasswordSet 表示库中是否已有密码；不入库字段。</summary>
        public CompanySmtpEmailSettingsDto? SmtpEmail { get; set; }

        /// <summary>Invoice / Packing 等打印报表页脚备注（各语言独立 sysparam 字符串）。</summary>
        public CompanyReportInfoDto ReportInfo { get; set; } = new();
    }

    public class CompanyReportInfoDto
    {
        public CompanyReportRemarksDto Invoice { get; set; } = new();
        public CompanyReportRemarksDto PackingList { get; set; } = new();
    }

    public class CompanyReportRemarksDto
    {
        public string RemarkCn { get; set; } = string.Empty;
        public string RemarkEn { get; set; } = string.Empty;
    }

    public class CompanySmtpEmailSettingsDto
    {
        public bool Enabled { get; set; }
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;

        /// <summary>租户平台邮箱后缀，如 @xxx.com。</summary>
        public string? PlatformEmailSuffix { get; set; }

        public string? PopHost { get; set; }
        public int PopPort { get; set; } = 995;
        public bool PopUseSsl { get; set; } = true;

        // 二期起业务不再使用；Upsert 仅从旧 JSON 原样保留，GET 时清空不回传
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public bool PasswordSet { get; set; }
    }

    public class UserMailboxDto
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>platform | personal</summary>
        public string Kind { get; set; } = "platform";
        public string Address { get; set; } = string.Empty;
        public string? LocalPart { get; set; }
        public string? DisplayName { get; set; }
        public bool PasswordSet { get; set; }
        public bool IsDefaultSend { get; set; }
        public string? PopHost { get; set; }
        public int? PopPort { get; set; }
        public bool PopUseSsl { get; set; } = true;
        /// <summary>none | ok | fail</summary>
        public string VerifyStatus { get; set; } = "none";
        public string? VerifyMessage { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class MailboxSendReadyDto
    {
        public bool Ready { get; set; }
        /// <summary>未就绪时为 <see cref="CRM.API.Services.MailboxSendErrorCodes"/> 之一。</summary>
        public string? BlockReason { get; set; }
    }

    /// <summary>验证邮箱接口返回：邮箱行 + POP/SMTP 分步结果。</summary>
    public class MailboxVerifyResponseDto
    {
        public UserMailboxDto Mailbox { get; set; } = new();
        public bool Success { get; set; }
        public bool PopOk { get; set; }
        public string PopMessage { get; set; } = string.Empty;
        public bool? SmtpOk { get; set; }
        public string? SmtpMessage { get; set; }
    }

    public class UserMailboxWriteRequest
    {
        /// <summary>platform | personal</summary>
        public string Kind { get; set; } = "platform";
        public string? LocalPart { get; set; }
        public string? Address { get; set; }
        public string? DisplayName { get; set; }
        /// <summary>新密码；空表示保留（更新时）。</summary>
        public string? Password { get; set; }
        public string? PopHost { get; set; }
        public int? PopPort { get; set; }
        public bool? PopUseSsl { get; set; }
    }

    public class VerifiedUserMailboxRowDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool PasswordSet { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class MailboxPasswordRevealDto
    {
        public string Password { get; set; } = string.Empty;
    }

    public class CompanyBasicInfoRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        /// <summary>默认含税公司抬头；与 <see cref="IsDefaultForeign"/> 互斥；全列表最多一组。</summary>
        public bool IsDefaultRmb { get; set; }
        /// <summary>默认外币公司抬头；与 <see cref="IsDefaultRmb"/> 互斥；全列表最多一组。</summary>
        public bool IsDefaultForeign { get; set; }
        public bool Enabled { get; set; } = true;
        public string CompanyName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string LegalPerson { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CompanyBankInfoRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool Enabled { get; set; } = true;
        public bool AvailableForPayment { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankAddress { get; set; } = string.Empty;
        public string Swift { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "RMB";
        public string Country { get; set; } = string.Empty;
        public string BankType { get; set; } = "rmb";
        public string PurposeType { get; set; } = "payment";
        public string Remark { get; set; } = string.Empty;
    }

    public class CompanyBankDeleteCheckDto
    {
        public bool CanDelete { get; set; }
    }

    public class CompanyLogoRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool Enabled { get; set; } = true;
        public string LogoName { get; set; } = string.Empty;
        public string? DocumentId { get; set; }
        public string? FileName { get; set; }
    }

    public class CompanySealRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        /// <summary>默认含税印章；与 <see cref="IsDefaultForeign"/> 互斥；全列表最多一组。</summary>
        public bool IsDefaultRmb { get; set; }
        /// <summary>默认外币印章；与 <see cref="IsDefaultRmb"/> 互斥；全列表最多一组。</summary>
        public bool IsDefaultForeign { get; set; }
        public bool Enabled { get; set; } = true;
        public string SealName { get; set; } = string.Empty;
        public string UseScene { get; set; } = string.Empty;
        public string? DocumentId { get; set; }
        public string? FileName { get; set; }
    }

    public class CompanyWarehouseRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool Enabled { get; set; } = true;
        public string WarehouseName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string WorkHours { get; set; } = string.Empty;
    }
}
