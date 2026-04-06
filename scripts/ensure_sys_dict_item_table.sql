-- 幂等：建表 + 种子数据（与迁移 20260403120000_AddSysDictItemVendorSeed、20260404130000_AddSysDictItemCustomerSeed 逻辑一致）
-- 推荐仍优先：dotnet ef database update --project CRM.Infrastructure --startup-project CRM.API
--
-- 若前端「数据字典」报「加载列表失败 / 404」：与执行本 SQL 无关。404 表示 HTTP 未找到接口，常见原因：
--   1) 生产 CRM.API 仍为旧包，不含 DictionariesAdminController（需重新 dotnet publish 并部署）；
--   2) 反向代理未把 /api/v1/ 转到 Kestrel，或仅白名单了部分路径；
--   3) 浏览器实际请求的 API 基址错误（检查 Network 里完整 URL）。
-- 列表接口应为：GET /api/v1/dictionaries/admin/items?bizSegment=customer&category=CustomerType&page=1&pageSize=500
-- （需登录态与 rbac.manage 权限。）

CREATE TABLE IF NOT EXISTS public.sys_dict_item (
    "Id" character varying(36) NOT NULL,
    "Category" character varying(64) NOT NULL,
    "ItemCode" character varying(64) NOT NULL,
    "NameZh" character varying(200) NOT NULL,
    "NameEn" character varying(200) NULL,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT "PK_sys_dict_item" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_dict_item_Category_ItemCode"
    ON public.sys_dict_item ("Category", "ItemCode");

-- ========== VendorIndustry ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'Semiconductors', '半导体', 'Semiconductors', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'Semiconductors');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'TestMeasurement', '测试和测量', 'Test & measurement', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'TestMeasurement');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'CircuitProtection', '电路保护', 'Circuit protection', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'CircuitProtection');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'WiresCables', '电线及电缆', 'Wires & cables', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'WiresCables');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'CathodePower', '负极、电源', 'Cathode / power', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'CathodePower');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'ToolsEquipment', '工具及设备', 'Tools & equipment', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ToolsEquipment');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'IndustrialControl', '工控', 'Industrial control', 7, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'IndustrialControl');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'MechatronicEncoders', '机电编码器', 'Electromechanical encoders', 8, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'MechatronicEncoders');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'ComputerPeripheralsMech', '计算机外设、机电', 'Computer peripherals & electromechanics', 9, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ComputerPeripheralsMech');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'StructuralParts', '结构件', 'Structural parts', 10, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'StructuralParts');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'DevKitsTools', '开发套件和工具', 'Development kits & tools', 11, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'DevKitsTools');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'ThermalManagement', '热管理', 'Thermal management', 12, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ThermalManagement');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'NetworkCommDevices', '网络通讯器件', 'Network communication devices', 13, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'NetworkCommDevices');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'DisplayMarket', '显示市场', 'Display market', 14, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'DisplayMarket');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'IGUS', 'IGUS', 'IGUS', 15, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'IGUS');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIndustry', 'LedLightingOptoDisplay', 'LED照明、光电设备及显示器', 'LED lighting, optoelectronics & displays', 16, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'LedLightingOptoDisplay');

-- ========== VendorLevel ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '1', '1-', '1-', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '2', '1', '1', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '3', '1+', '1+', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '4', '2-', '2-', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '4');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '5', '2', '2', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '5');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '6', '2+', '2+', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '6');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '7', '3-', '3-', 7, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '7');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '8', '3', '3', 8, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '8');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '9', '3+', '3+', 9, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '9');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '10', 'A', 'A', 10, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '10');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '11', 'B', 'B', 11, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '11');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '12', 'C', 'C', 12, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '12');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorLevel', '13', 'D', 'D', 13, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '13');

-- ========== VendorIdentity ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '1', '目录商', 'Catalog vendor', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '2', '货代', 'Freight forwarder', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '3', '原厂', 'Original manufacturer', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '4', 'EMS工厂', 'EMS factory', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '4');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '5', '代理', 'Agent', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '5');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '6', 'IDH', 'IDH', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '6');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '7', '渠道商', 'Channel partner', 7, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '7');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '8', '现货贸易商', 'Spot trader', 8, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '8');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '9', '电商', 'E-commerce', 9, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '9');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorIdentity', '10', '制造商', 'Manufacturer', 10, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '10');

-- ========== VendorPaymentMethod ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Prepaid', '预付款', 'Prepaid', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Prepaid');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'COD', '货到付款', 'COD', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'COD');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Monthly', '月结', 'Monthly', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Monthly');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Credit', '账期', 'Credit', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Credit');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'TT', '电汇', 'Wire T/T', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'TT');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'LC', '信用证', 'L/C', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'LC');

-- ========== CustomerType ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '1', 'OEM', 'OEM', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '2', 'ODM', 'ODM', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '3', '终端', 'End user', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '4', 'IDH', 'IDH', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '4');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '5', '贸易商', 'Trader', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '5');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '6', '代理商', 'Agent', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '6');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '7', 'EMS', 'EMS', 7, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '7');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '8', '非行业', 'Non-industry', 8, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '8');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '9', '科研机构', 'Research', 9, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '9');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '10', '供应链', 'Supply chain', 10, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '10');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerType', '11', '原厂', 'Original factory', 11, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '11');

-- ========== CustomerLevel ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'D', 'D级', 'Grade D', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'D');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'C', 'C级', 'Grade C', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'C');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'B', 'B级', 'Grade B', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'B');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'BPO', 'BPO', 'BPO', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'BPO');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'VIP', 'VIP', 'VIP', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'VIP');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerLevel', 'VPO', 'VPO', 'VPO', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'VPO');

-- ========== CustomerIndustry ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'FinanceEquipment', '金融设备', 'Financial equipment', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'FinanceEquipment');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Telecom', '通讯', 'Telecom', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Telecom');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'RailTransit', '轨道交通', 'Rail transit', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'RailTransit');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Aerospace', '航空航天', 'Aerospace', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Aerospace');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'CyberSecurity', '网络安全', 'Cyber security', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'CyberSecurity');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Esports', '电竞', 'Esports', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Esports');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerSupply', '电源', 'Power supply', 7, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerSupply');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ElectronicComponentsTrading', '电子元器件贸易', 'EC trading', 8, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ElectronicComponentsTrading');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ElectronicComponentsManufacturing', '电子元器件制造', 'EC manufacturing', 9, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ElectronicComponentsManufacturing');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerTools', '电动工具', 'Power tools', 10, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerTools');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerElectrical', '电力电气', 'Power & electrical', 11, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerElectrical');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'IoT', '物联网', 'IoT', 12, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'IoT');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ConsumerElectronics', '消费电子', 'Consumer electronics', 13, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ConsumerElectronics');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Robotics', '机器人', 'Robotics', 14, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Robotics');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'SmartSecurity', '智能安防', 'Smart security', 15, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'SmartSecurity');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'SmartCity', '智慧城市', 'Smart city', 16, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'SmartCity');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'UAV', '无人机', 'UAV', 17, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'UAV');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'NewEnergyVehicles', '新能源汽车', 'NEV', 18, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'NewEnergyVehicles');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'NewEnergy', '新能源', 'New energy', 19, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'NewEnergy');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'IndustrialControl', '工业控制', 'Industrial control', 20, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'IndustrialControl');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'MedicalEquipment', '医疗设备', 'Medical equipment', 21, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'MedicalEquipment');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'DefenseMilitary', '军工', 'Defense', 22, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'DefenseMilitary');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'TraditionalVehicles', '传统车辆', 'Traditional vehicles', 23, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'TraditionalVehicles');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Instrumentation', '仪器仪表', 'Instrumentation', 24, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Instrumentation');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ArtificialIntelligence', '人工智能', 'AI', 25, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ArtificialIntelligence');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'CloudComputingIDC', '云计算IDC', 'Cloud / IDC', 26, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'CloudComputingIDC');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Manufacturing', '制造业', 'Manufacturing', 27, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Manufacturing');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Trading', '贸易/零售', 'Trading / retail', 28, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Trading');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Technology', '科技/IT', 'Technology / IT', 29, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Technology');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Construction', '建筑/工程', 'Construction', 30, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Construction');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Healthcare', '医疗/健康', 'Healthcare', 31, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Healthcare');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Education', '教育', 'Education', 32, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Education');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Finance', '金融', 'Finance', 33, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Finance');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Other', '其他', 'Other', 34, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Other');

-- ========== CustomerTaxRate ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '0', '0%', '0%', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '0');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '1', '1%', '1%', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '3', '3%', '3%', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '6', '6%', '6%', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '6');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '9', '9%', '9%', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '9');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerTaxRate', '13', '13%', '13%', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '13');

-- ========== CustomerInvoiceType ==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '0', '无需开票', 'No invoice', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '0');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '1', '增值税专用发票', 'VAT special invoice', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '2', '增值税普通发票', 'VAT ordinary invoice', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '3', '电子发票', 'E-invoice', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '3');

-- ========== MaterialProductionDate（物料-生产日期）==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'MaterialProductionDate', '1', '2年内', 'Within 2 years', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'MaterialProductionDate', '2', '1年内', 'Within 1 year', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'MaterialProductionDate', '3', '无要求', 'No requirement', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'MaterialProductionDate', '4', '25+', '25+', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '4');

-- ========== LogisticsArrivalMethod / LogisticsExpressMethod（物流-来货方式、快递方式）==========
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '1', '送货', 'Delivery', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '2', '自提', 'Self pickup', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '3', '快递', 'Express courier', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '4', '物流', 'Freight / logistics', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '4');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '1', 'UPS', 'UPS', 1, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '1');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '2', 'FEDEX', 'FedEx', 2, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '2');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '3', 'DHL', 'DHL', 3, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '3');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '4', '顺丰', 'SF Express', 4, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '4');
INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '5', '跨越', 'KYE Express', 5, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '5');
