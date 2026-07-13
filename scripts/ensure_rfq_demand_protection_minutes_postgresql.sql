-- =============================================================================
-- 补齐「需求保护时长」系统参数（可重复执行）
-- 用途：采购参数 / 需求保护时长；无需 ALTER TABLE
--
-- 说明：
--   - 不新增业务表字段；保护时长存 sysparam，计算基准为 rfqitem.create_time（已有）
--   - 未插入时后端读取默认 30 分钟；首次在后台保存也会自动创建本行
--   - 0 = 无保护期（采购员可看/报分配给其他人的明细）；>0 = 保护分钟数
-- =============================================================================

INSERT INTO public.sysparam (
    "ParamId",
    "ParamCode",
    "ParamName",
    "GroupId",
    "DataType",
    "ValueString",
    "DefaultValue",
    "Description",
    "IsArray",
    "IsSystem",
    "IsEditable",
    "IsVisible",
    "SortOrder",
    "Status",
    "CreateTime"
)
SELECT
    '00000000-0000-4000-8000-000000000015',
    'System.RFQ.DemandProtectionMinutes',
    '需求保护时长',
    (SELECT "GroupId" FROM public.sysparamgroup WHERE "GroupCode" = 'System.Display' LIMIT 1),
    2,
    '30',
    '30',
    '需求明细创建后在此分钟数内仅分配采购员可见/可报价；超过后任意采购员可见/可报价。0 表示无保护期。',
    FALSE,
    TRUE,
    TRUE,
    TRUE,
    13,
    1,
    timezone('utc', now())
WHERE NOT EXISTS (
    SELECT 1 FROM public.sysparam p WHERE p."ParamCode" = 'System.RFQ.DemandProtectionMinutes'
);

SELECT "ParamCode", "ParamName", "ValueString", "DefaultValue", "Description"
FROM public.sysparam
WHERE "ParamCode" = 'System.RFQ.DemandProtectionMinutes';
