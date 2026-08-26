-- =============================================================================
-- 补齐「允许指定采购」系统参数（可重复执行）
-- 用途：采购参数 / 默认分配方式页勾选；未勾选时新建/编辑需求不出现「指定采购」
-- 取值：Boolean，默认 false
-- 未执行本脚本时：GET 视为 false；首次在采购参数页保存也会自动建行
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
    '00000000-0000-4000-8000-000000000017',
    'System.RFQ.AllowDesignatedPurchaser',
    '允许指定采购',
    (SELECT "GroupId" FROM public.sysparamgroup WHERE "GroupCode" = 'System.Display' LIMIT 1),
    4,
    'false',
    'false',
    '勾选后，新建/编辑需求的「分配方式」下拉才会出现「指定采购」。默认关闭。',
    FALSE,
    TRUE,
    TRUE,
    TRUE,
    16,
    1,
    timezone('utc', now())
WHERE NOT EXISTS (
    SELECT 1 FROM public.sysparam p WHERE p."ParamCode" = 'System.RFQ.AllowDesignatedPurchaser'
);

SELECT "ParamCode", "ParamName", "ValueString", "DefaultValue", "Description"
FROM public.sysparam
WHERE "ParamCode" = 'System.RFQ.AllowDesignatedPurchaser';
