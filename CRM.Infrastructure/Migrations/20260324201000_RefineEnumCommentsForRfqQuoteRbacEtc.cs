using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 精确补充 RFQ/Quote/PurchaseRequisition/StockOutRequest/User/RBAC 的状态与类型字段注释。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324201000_RefineEnumCommentsForRfqQuoteRbacEtc")]
    public partial class RefineEnumCommentsForRfqQuoteRbacEtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec record;
BEGIN
    FOR rec IN
        SELECT *
        FROM (VALUES
            -- rfq
            ('rfq', 'rfq_type', '需求类型：1现货 2期货 3样品 4批量'),
            ('rfq', 'quote_method', '报价方式：1不接受任何消息 2仅邮件 3仅系统 4全部方式'),
            ('rfq', 'assign_method', '分配方式：1系统分配多人采购 2系统分配单人采购 3手动分配'),
            ('rfq', 'target_type', '目标类型：1比价需求 2独家需求 3紧急需求 4常规需求'),
            ('rfq', 'status', '状态：0待分配 1已分配 2报价中 3已报价 4已选价 5已转订单 6已关闭'),

            -- rfqitem
            ('rfqitem', 'price_currency', '目标价币种：1RMB 2USD 3EUR 4HKD'),
            ('rfqitem', 'status', '状态：0待报价 1已报价 2已接受 3已拒绝'),

            -- quote
            ('quote', 'status', '状态：0草稿 1待审核 2已审核 3已发送 4已接受 5已拒绝 6已过期 7已关闭'),

            -- quoteitem
            ('quoteitem', 'label_type', '涂标：0不涂标 1涂标 2待确定'),
            ('quoteitem', 'wafer_origin', '晶圆产地：0美产 1非美产 2待确定'),
            ('quoteitem', 'package_origin', '封装产地：0美产 1非美产 2待确定'),
            ('quoteitem', 'currency', '报价币别：0RMB 1USD 2EUR 3HKD'),
            ('quoteitem', 'status', '状态：0有效 1已取消'),

            -- purchaserequisition
            ('purchaserequisition', 'status', '状态：0新建 1部分完成 2全部完成 3已取消'),
            ('purchaserequisition', 'type', '类型：0专属 1公开备货'),

            -- stockoutrequest
            ('stockoutrequest', 'Status', '状态：0待出库 1已出库 2已取消'),

            -- user
            ('user', 'Status', '账号状态：1启用 0禁用'),

            -- rbac
            ('sys_role', 'Status', '状态：1启用 0禁用'),
            ('sys_permission', 'Status', '状态：1启用 0禁用'),
            ('sys_permission', 'PermissionType', '权限类型：menu菜单 api接口 button按钮 data数据'),
            ('sys_department', 'Status', '状态：1启用 0禁用'),
            ('sys_department', 'SaleDataScope', '销售数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止'),
            ('sys_department', 'PurchaseDataScope', '采购数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止'),
            ('sys_department', 'IdentityType', '业务身份：0None 1Sales 2Purchaser 3PurchaseAssistant 4CustService 5Finance 6Logistics')
        ) AS t(table_name, column_name, comment_text)
    LOOP
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns c
            WHERE c.table_schema = 'public'
              AND c.table_name = rec.table_name
              AND c.column_name = rec.column_name
        ) THEN
            EXECUTE format(
                'COMMENT ON COLUMN public.%I.%I IS %L',
                rec.table_name,
                rec.column_name,
                rec.comment_text
            );
        END IF;
    END LOOP;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 注释为文档元数据，不做自动回滚
        }
    }
}

