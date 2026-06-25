-- =============================================================================
-- FrontCRM：清除全部业务数据（含客户、供应商），保留系统参数与 RBAC
-- 数据库：PostgreSQL
--
-- 【保留】
--   职员与权限：user、sys_department、sys_role、sys_permission、
--               sys_user_role、sys_user_department、sys_role_permission
--   系统参数：sysparamgroup、sysparam、sysparamhistory、sys_dict_item、
--             sys_relation_map、sys_purchase_quoter_pool
--   公司与银行主数据（系统配置）：company_bankinfo、financepaymentbank
--   汇率配置：financeexchangeratesetting
--   单号规则行（序号归零）：sys_serial_number
--   AI 场景/厂商/模板配置：ai_provider、ai_prompt_template、ai_scenario、ai_global_config
--   迁移历史：__EFMigrationsHistory
--
-- 【清除】
--   客户/供应商、需求/报价/销采/库存/财务流水/报关/物料/仓库/品牌、
--   业务日志、AI 调用缓存与日志、上传文档索引、草稿/收藏/标签等
--
-- 【说明】
--   1) 仅 TRUNCATE 当前库中实际存在的表，缺表自动跳过
--   2) 不会删除职员账号；清库后仍用原账号登录
--   3) 物理上传文件（CRM.API/Uploads）需另行清理
--   4) 执行前务必备份；建议先 BEGIN，确认校验结果后再 COMMIT
--
-- 若已报错 25P02：ROLLBACK;
-- =============================================================================

BEGIN;

SET LOCAL lock_timeout = '120s';

-- ---------------------------------------------------------------------------
-- 1) 动态 TRUNCATE 业务表（存在才清）
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    all_tables text[] := ARRAY[
        -- 审批 / 旅程 / 日志
        'approval_record',
        'log_orderjourney',
        'log_login',
        'log_operation',
        'log_change_fldval',
        'log_recent',
        'sys_error_log',
        'debug',
        'businesslog',

        -- 草稿 / 收藏 / 标签 / 缓存
        'biz_draft',
        'user_favorite',
        'user_tag_preference',
        'tag_relation',
        'tag_definition',
        'component_cache',
        'document_daily_sequence',
        'upload_document',

        -- 微信临时
        'wechat_bind_request',
        'wechat_login_ticket',

        -- AI 运行时（保留 ai_* 配置表）
        'ai_invocation_cache',
        'ai_entity_parse_log',
        'ai_invocation_log',

        -- 客户
        'customercontacthistory',
        'customercontactinfo',
        'customerbankinfo',
        'customeraddress',
        'customerinfo',
        'customer_change_log',
        'customer_operation_log',

        -- 供应商
        'vendorcontacthistory',
        'vendorcontactinfo',
        'vendorbankinfo',
        'vendoraddress',
        'vendorinfo',
        'vendor',
        'vendor_address',
        'vendor_change_log',
        'vendor_operation_log',

        -- 品牌 / 物料 / 仓库主数据
        'biz_brand',
        'material',
        'materialcategory',
        'warehouseinfo',
        'warehousezone',
        'warehouselocation',
        'warehouseshelf',

        -- 需求 / 报价
        'rfqitem',
        'rfq',
        'quoteitem',
        'quote',

        -- 销售
        'sellorderitemextend',
        'sellorderextend',
        'sellorderitem',
        'sellorder',

        -- 采购
        'purchaseorderitemextend',
        'purchaseorderextend',
        'purchaseorderitem',
        'purchaseorder',
        'purchaserequisition',

        -- 财务流水（保留 financeexchangeratesetting / financepaymentbank / company_bankinfo）
        'finance_customer_advance_ledger',
        'finance_customer_advance',
        'finance_receivable_write_off',
        'finance_receivable',
        'financepaymentitem',
        'financepayment',
        'financereceiptitem',
        'financereceipt',
        'financepurchaseinvoiceitem',
        'financepurchaseinvoice',
        'sellinvoiceitem',
        'financesellinvoice',
        'financeexchangeratechangelog',
        'paymentrequest',

        -- 库存 / 物流 / 报关 / 盘点 / 拣货 / 装箱
        'pickingtaskitem',
        'pickingtask',
        'packing_item_extend',
        'packing_item',
        'packing_extend_ship',
        'packing_extend_box',
        'packing_extend',
        'packing',
        'inventorycountitem',
        'inventorycountplan',
        'customs_declaration_item',
        'customs_declaration',
        'customs_pendlist',
        'customs_broker',
        'stocktransfer_item_manual',
        'stocktransfer_manual',
        'stocktransfer_item_customers',
        'stocktransfer_customers',
        'stock_out_item_extend',
        'stock_out_item',
        'stock_out_batch',
        'stock_out',
        'stockoutitemextend',
        'stockoutitem',
        'stockout',
        'stockout_notify_item',
        'stockoutrequestitem',
        'stockoutrequest',
        'stockout_notify',
        'stock_in_item_extend',
        'stock_in_item',
        'stock_in_batch',
        'stock_in_extend',
        'stock_in',
        'stockinitemextend',
        'stockinitem',
        'stockinextend',
        'stockin',
        'stockinnotifyitem',
        'stockinnotify',
        'stockin_notify',
        'qcitem',
        'qcinfo',
        'stock_item',
        'stockledger',
        'inventoryledger',
        'stock_extend',
        'stock',

        -- 历史表名（旧库兼容）
        'receipt',
        'receiptitem',
        'payment',
        'paymentitem',
        'invoice',
        'invoiceitem'
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

    RAISE NOTICE 'TRUNCATE % 张业务表…', array_length(existing, 1);
    EXECUTE sql;
END $$;

-- ---------------------------------------------------------------------------
-- 2) 业务流水号归零（保留各模块 Prefix/规则行）
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
UNION ALL SELECT 'customerinfo', COUNT(*) FROM public.customerinfo
UNION ALL SELECT 'vendorinfo', COUNT(*) FROM public.vendorinfo
UNION ALL SELECT 'sellorder', COUNT(*) FROM public.sellorder
UNION ALL SELECT 'purchaseorder', COUNT(*) FROM public.purchaseorder
UNION ALL SELECT 'rfq', COUNT(*) FROM public.rfq
UNION ALL SELECT 'stock', COUNT(*) FROM public.stock
UNION ALL SELECT 'sysparam', COUNT(*) FROM public.sysparam
UNION ALL SELECT 'sys_dict_item', COUNT(*) FROM public.sys_dict_item
UNION ALL SELECT 'company_bankinfo', COUNT(*) FROM public.company_bankinfo
UNION ALL SELECT 'financeexchangeratesetting', COUNT(*) FROM public.financeexchangeratesetting
UNION ALL SELECT 'ai_scenario', COUNT(*) FROM public.ai_scenario
ORDER BY tbl;
