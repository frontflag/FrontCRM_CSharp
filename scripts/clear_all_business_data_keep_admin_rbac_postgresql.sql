-- =============================================================================
-- FrontCRM：克隆库清空为「初始业务空库」
-- 数据库：PostgreSQL
--
-- 【保留】sys_department、sys_role、sys_permission、sys_role_permission、
--         sys_serial_number 规则行（序号归零）
-- 【清除】全部职员账号（含 Admin/admin）、sys_user_role、sys_user_department
--
-- 【说明】仅 TRUNCATE 当前库中「实际存在」的表，缺表自动跳过（避免 42P01 中止事务）
--
-- 若已报错 25P02，请先执行：ROLLBACK;
-- 清库后需重新登录：执行 seed_initial_rbac_admin.sql（默认 Admin / Admin123）
-- =============================================================================

BEGIN;

SET LOCAL lock_timeout = '120s';

-- ---------------------------------------------------------------------------
-- 1) 清空业务 / 主数据 / 日志（动态：只 TRUNCATE 存在的表）
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    all_tables text[] := ARRAY[
        -- 业务与主数据（当前 + 历史表名一并列出，存在才清）
        'approval_record',
        'biz_draft',
        'company_bankinfo',
        'component_cache',
        'customs_broker',
        'customs_declaration',
        'customs_declaration_item',
        'customeraddress',
        'customerbankinfo',
        'customercontacthistory',
        'customercontactinfo',
        'customerinfo',
        'customer_change_log',
        'customer_operation_log',
        'debug',
        'document_daily_sequence',
        'financeexchangeratechangelog',
        'financeexchangeratesetting',
        'financepayment',
        'financepaymentbank',
        'financepaymentitem',
        'financepurchaseinvoice',
        'financepurchaseinvoiceitem',
        'financereceipt',
        'financereceiptitem',
        'financesellinvoice',
        'inventorycountitem',
        'inventorycountplan',
        'inventoryledger',
        'log_change_fldval',
        'log_login',
        'log_operation',
        'log_orderjourney',
        'log_recent',
        'material',
        'materialcategory',
        'packing',
        'packing_extend',
        'packing_extend_box',
        'packing_extend_ship',
        'packing_item',
        'packing_item_extend',
        'paymentrequest',
        'pickingtask',
        'pickingtaskitem',
        'purchaseorder',
        'purchaseorderextend',
        'purchaseorderitem',
        'purchaseorderitemextend',
        'purchaserequisition',
        'qcinfo',
        'qcitem',
        'quote',
        'quoteitem',
        'rfq',
        'rfqitem',
        'sellinvoiceitem',
        'sellorder',
        'sellorderextend',
        'sellorderitem',
        'sellorderitemextend',
        'stock',
        'stock_extend',
        'stock_in',
        'stock_in_batch',
        'stock_in_extend',
        'stock_in_item',
        'stock_in_item_extend',
        'stock_item',
        'stock_out',
        'stock_out_item',
        'stock_out_item_extend',
        'stockin',
        'stockinitem',
        'stockinextend',
        'stockinitemextend',
        'stockin_notify',
        'stockinnotify',
        'stockinnotifyitem',
        'stockout',
        'stockoutitem',
        'stockoutitemextend',
        'stockout_notify',
        'stockoutrequest',
        'stockoutrequestitem',
        'stockout_notify_item',
        'stockitem',
        'stockledger',
        'stocktransfer_customers',
        'stocktransfer_item_customers',
        'stocktransfer_manual',
        'stocktransfer_item_manual',
        'sys_dict_item',
        'sys_error_log',
        'sysparam',
        'sysparamgroup',
        'sysparamhistory',
        'tag_definition',
        'tag_relation',
        'upload_document',
        'user_favorite',
        'user_tag_preference',
        'vendoraddress',
        'vendorbankinfo',
        'vendorcontacthistory',
        'vendorcontactinfo',
        'vendorinfo',
        'vendor',
        'vendor_address',
        'vendor_change_log',
        'vendor_operation_log',
        'warehouseinfo',
        'warehouselocation',
        'warehouseshelf',
        'warehousezone',
        'wechat_bind_request',
        'wechat_login_ticket',
        'receipt',
        'receiptitem',
        'payment',
        'paymentitem',
        'invoice',
        'invoiceitem',
        'businesslog'
    ];
    existing text[];
    missing text[];
    sql text;
    t text;
BEGIN
    existing := ARRAY[]::text[];
    missing := ARRAY[]::text[];

    FOREACH t IN ARRAY all_tables LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = t
        ) THEN
            existing := array_append(existing, t);
        ELSE
            missing := array_append(missing, t);
        END IF;
    END LOOP;

    IF coalesce(array_length(existing, 1), 0) = 0 THEN
        RAISE NOTICE '没有需要 TRUNCATE 的业务表（请检查是否连错库）。';
        RETURN;
    END IF;

    IF coalesce(array_length(missing, 1), 0) > 0 THEN
        RAISE NOTICE '以下表不存在，已跳过：%', array_to_string(missing, ', ');
    END IF;

    sql := 'TRUNCATE TABLE '
        || (
            SELECT string_agg(format('public.%I', x), ', ' ORDER BY x)
            FROM unnest(existing) AS x
        )
        || ' RESTART IDENTITY CASCADE';

    RAISE NOTICE 'TRUNCATE % 张表…', array_length(existing, 1);
    EXECUTE sql;
END $$;

-- ---------------------------------------------------------------------------
-- 2) 删除全部职员账号（含 Admin / admin）及用户-角色/部门关联
-- ---------------------------------------------------------------------------
DELETE FROM public.sys_user_role;
DELETE FROM public.sys_user_department;
DELETE FROM public."user";

-- ---------------------------------------------------------------------------
-- 3) 业务流水号归零（保留各模块 Prefix/规则行）
-- ---------------------------------------------------------------------------
UPDATE public.sys_serial_number SET
    "CurrentSequence" = 0,
    "LastResetYear" = NULL,
    "LastResetMonth" = NULL,
    "UpdateTime" = NOW() AT TIME ZONE 'UTC';

COMMIT;

-- =============================================================================
-- 校验（COMMIT 后执行）
-- =============================================================================
SELECT 'user' AS tbl, COUNT(*)::bigint AS rows FROM public."user"
UNION ALL SELECT 'sys_department', COUNT(*) FROM public.sys_department
UNION ALL SELECT 'sys_role', COUNT(*) FROM public.sys_role
UNION ALL SELECT 'sys_permission', COUNT(*) FROM public.sys_permission
UNION ALL SELECT 'sys_role_permission', COUNT(*) FROM public.sys_role_permission
UNION ALL SELECT 'sys_user_role', COUNT(*) FROM public.sys_user_role
UNION ALL SELECT 'sys_user_department', COUNT(*) FROM public.sys_user_department
UNION ALL SELECT 'customerinfo', COUNT(*) FROM public.customerinfo
UNION ALL SELECT 'vendorinfo', COUNT(*) FROM public.vendorinfo
UNION ALL SELECT 'sellorder', COUNT(*) FROM public.sellorder
UNION ALL SELECT 'purchaseorder', COUNT(*) FROM public.purchaseorder
UNION ALL SELECT 'stock', COUNT(*) FROM public.stock
UNION ALL SELECT 'sysparam', COUNT(*) FROM public.sysparam
ORDER BY tbl;

SELECT "UserName", "Email", "IsActive", "Status"
FROM public."user";

SELECT "RoleCode", "RoleName" FROM public.sys_role ORDER BY "RoleCode";

SELECT "DepartmentName", "SaleDataScope", "PurchaseDataScope", "Status"
FROM public.sys_department
ORDER BY "Level", "DepartmentName";
