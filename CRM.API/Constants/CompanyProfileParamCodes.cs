namespace CRM.API.Constants
{
    /// <summary>
    /// 公司信息参数在 sysparam 表中的 ParamCode（ValueJson 存数组）。
    /// </summary>
    public static class CompanyProfileParamCodes
    {
        public const string BasicInfos = "Company.Profile.BasicInfos";
        public const string BankInfos = "Company.Profile.BankInfos";
        public const string Logos = "Company.Profile.Logos";
        public const string Seals = "Company.Profile.Seals";
        public const string Warehouses = "Company.Profile.Warehouses";

        /// <summary>SMTP / 发件人（单对象 JSON，公司信息页「公司邮箱」）。</summary>
        public const string SmtpEmail = "Company.Profile.SmtpEmail";

        /// <summary>Invoice 报表备注（中文，多行字符串）。</summary>
        public const string ReportInvoiceRemarkCn = "Company.Profile.ReportInfo.Invoice.Remark.CN";

        /// <summary>Invoice 报表备注（英文，多行字符串）。</summary>
        public const string ReportInvoiceRemarkEn = "Company.Profile.ReportInfo.Invoice.Remark.EN";

        /// <summary>Packing List 报表备注（中文，多行字符串）。</summary>
        public const string ReportPackingListRemarkCn = "Company.Profile.ReportInfo.PackingList.Remark.CN";

        /// <summary>Packing List 报表备注（英文，多行字符串）。</summary>
        public const string ReportPackingListRemarkEn = "Company.Profile.ReportInfo.PackingList.Remark.EN";
    }
}
