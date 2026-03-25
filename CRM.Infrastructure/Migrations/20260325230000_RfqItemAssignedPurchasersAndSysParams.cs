using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 需求明细：轮询分配的两名询价采购员；系统参数：角色编码列表与轮询游标。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325230000_RfqItemAssignedPurchasersAndSysParams")]
    public partial class RfqItemAssignedPurchasersAndSysParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.rfqitem
    ADD COLUMN IF NOT EXISTS assigned_purchaser_user_id_1 character varying(36) NULL;
ALTER TABLE IF EXISTS public.rfqitem
    ADD COLUMN IF NOT EXISTS assigned_purchaser_user_id_2 character varying(36) NULL;

INSERT INTO public.sysparam (""ParamId"", ""ParamCode"", ""ParamName"", ""GroupId"", ""DataType"", ""ValueString"", ""DefaultValue"", ""Description"", ""IsArray"", ""IsSystem"", ""IsEditable"", ""IsVisible"", ""SortOrder"", ""Status"", ""CreateTime"")
SELECT '00000000-0000-4000-8000-000000000012', 'System.RFQ.RoundRobinPurchaserRoleCodes', '需求明细轮询采购员角色编码', (SELECT ""GroupId"" FROM public.sysparamgroup WHERE ""GroupCode"" = 'System.Display' LIMIT 1), 1, '', '', '逗号分隔 RBAC RoleCode，如 purchase_buyer。为空时后端按常见编码回退。', FALSE, TRUE, TRUE, TRUE, 10, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam p WHERE p.""ParamCode"" = 'System.RFQ.RoundRobinPurchaserRoleCodes');

INSERT INTO public.sysparam (""ParamId"", ""ParamCode"", ""ParamName"", ""GroupId"", ""DataType"", ""ValueString"", ""DefaultValue"", ""Description"", ""IsArray"", ""IsSystem"", ""IsEditable"", ""IsVisible"", ""SortOrder"", ""Status"", ""CreateTime"")
SELECT '00000000-0000-4000-8000-000000000013', 'System.RFQ.PurchaserRoundRobinCursor', '需求明细采购员轮询游标', (SELECT ""GroupId"" FROM public.sysparamgroup WHERE ""GroupCode"" = 'System.Display' LIMIT 1), 1, '0', '0', '非负整数，每分配一条明细的两个槽位后 +2。', FALSE, TRUE, TRUE, FALSE, 11, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam p WHERE p.""ParamCode"" = 'System.RFQ.PurchaserRoundRobinCursor');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
