using System.Text.Json.Serialization;

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
    }

    public class CompanySmtpEmailSettingsDto
    {
        public bool Enabled { get; set; }
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string? User { get; set; }
        /// <summary>仅写入；读取 API 恒为空，改密码时填新值，留空表示保留原密码。</summary>
        public string? Password { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "FrontCRM";
        public bool UseSsl { get; set; } = true;

        /// <summary>GET 时由接口根据是否存有密码填充；PUT 可忽略；不写入 ValueJson。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
        public bool PasswordSet { get; set; }
    }

    public class CompanyBasicInfoRowDto : ICompanyProfileRow
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
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
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankAddress { get; set; } = string.Empty;
        public string Swift { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string Currency { get; set; } = "RMB";
        public string BankType { get; set; } = "rmb";
        public string PurposeType { get; set; } = "payment";
        public string Remark { get; set; } = string.Empty;
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
