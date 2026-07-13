-- =============================================================================
-- 补齐「默认分配方式」系统参数（可重复执行）
-- 用途：采购参数 / 默认分配方式；新建需求页分配方式下拉默认值
-- 取值：2 条目轮询 / 3 品牌轮询 / 5 采报优先（默认 5）
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
    '00000000-0000-4000-8000-000000000016',
    'System.RFQ.DefaultAssignMethod',
    '默认分配方式',
    (SELECT "GroupId" FROM public.sysparamgroup WHERE "GroupCode" = 'System.Display' LIMIT 1),
    2,
    '5',
    '5',
    '新建需求页「分配方式」下拉默认选中项（2 条目轮询 / 3 品牌轮询 / 5 采报优先）。',
    FALSE,
    TRUE,
    TRUE,
    TRUE,
    14,
    1,
    timezone('utc', now())
WHERE NOT EXISTS (
    SELECT 1 FROM public.sysparam p WHERE p."ParamCode" = 'System.RFQ.DefaultAssignMethod'
);

SELECT "ParamCode", "ParamName", "ValueString", "DefaultValue", "Description"
FROM public.sysparam
WHERE "ParamCode" = 'System.RFQ.DefaultAssignMethod';
