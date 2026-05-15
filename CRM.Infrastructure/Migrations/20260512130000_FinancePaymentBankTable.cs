using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 财务参数：付款银行表及初始名称列表（仅当表为空时插入种子数据）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512130000_FinancePaymentBankTable")]
    public partial class FinancePaymentBankTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS financepaymentbank (
    ""FinancePaymentBankId"" character varying(36) NOT NULL,
    ""BankName"" character varying(200) NOT NULL,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""IsDisabled"" boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT ""PK_financepaymentbank"" PRIMARY KEY (""FinancePaymentBankId"")
);

CREATE INDEX IF NOT EXISTS ""IX_financepaymentbank_SortOrder"" ON financepaymentbank (""SortOrder"");

INSERT INTO financepaymentbank (""FinancePaymentBankId"", ""BankName"", ""SortOrder"", ""IsDisabled"", ""CreateTime"", ""ModifyTime"")
SELECT gen_random_uuid()::text, trim(both from v), ord::integer, false, (NOW() AT TIME ZONE 'UTC'), NULL
FROM unnest(ARRAY[
'中国银行',
'工商银行',
'建设银行',
'农业银行',
'星展銀行(香港)有限公司',
'東亞銀行有限公司',
'香港上海汇丰银行有限公司',
'汇丰英国银行有限公司',
'建设银行(亚洲)有限公司',
'渣打银行(香港)有限公司',
'花旗银行(香港)有限公司',
'中国银行(香港)有限公司',
'大新银行有限公司',
'华侨银行有限公司',
'华侨永亨银行（中国）有限公司',
'大通银行香港分行',
'摩根大通银行',
'美国银行香港分行',
'恒生银行有限公司',
'中信银行国际有限公司',
'中国工商银行(亚洲)有限公司',
'招商银行(香港分行)',
'招商银行永隆银行有限公司',
'南洋商业银行有限公司',
'上海商業儲蓄銀行',
'中国商银行股份有限公司',
'第一公民银行和信托公司',
'富融银行',
'信托银行',
'民生银行香港分行',
'交通银行(香港)有限公司'
]::text[])
WITH ORDINALITY AS t(v, ord)
WHERE NOT EXISTS (SELECT 1 FROM financepaymentbank LIMIT 1);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS financepaymentbank;");
        }
    }
}
