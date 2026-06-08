using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>RFQ 报价员池表与分配人数系统参数。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604180000_SysPurchaseQuoterPool")]
    public partial class SysPurchaseQuoterPool : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sys_purchase_quoter_pool (
    user_id character varying(36) NOT NULL PRIMARY KEY,
    sort_order integer NOT NULL,
    create_time timestamp with time zone NOT NULL DEFAULT NOW(),
    update_time timestamp with time zone NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE public.sys_purchase_quoter_pool IS 'RFQ 轮询报价员池：仅存储已勾选参与轮询的采购员';
COMMENT ON COLUMN public.sys_purchase_quoter_pool.user_id IS '用户 UserId';
COMMENT ON COLUMN public.sys_purchase_quoter_pool.sort_order IS '轮询顺序（保存时按 User.Id 升序生成）';

CREATE INDEX IF NOT EXISTS ""IX_sys_purchase_quoter_pool_sort_order""
    ON public.sys_purchase_quoter_pool (sort_order);

INSERT INTO public.sysparam (""ParamId"", ""ParamCode"", ""ParamName"", ""GroupId"", ""DataType"", ""ValueString"", ""DefaultValue"", ""Description"", ""IsArray"", ""IsSystem"", ""IsEditable"", ""IsVisible"", ""SortOrder"", ""Status"", ""CreateTime"")
SELECT '00000000-0000-4000-8000-000000000014', 'System.RFQ.RoundRobinAssigneeCount', '需求轮询分配报价员人数', (SELECT ""GroupId"" FROM public.sysparamgroup WHERE ""GroupCode"" = 'System.Display' LIMIT 1), 2, '2', '2', '每条 RFQ 从报价员池连续取 N 人（1 或 2）。', FALSE, TRUE, TRUE, TRUE, 12, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam p WHERE p.""ParamCode"" = 'System.RFQ.RoundRobinAssigneeCount');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM public.sysparam WHERE ""ParamCode"" = 'System.RFQ.RoundRobinAssigneeCount';
DROP TABLE IF EXISTS public.sys_purchase_quoter_pool;
");
        }
    }
}
