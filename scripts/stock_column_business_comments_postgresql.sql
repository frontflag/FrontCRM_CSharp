-- =============================================================================
-- public.stock 库存分桶主表：全部列业务注释（PostgreSQL COMMENT ON）
-- 与迁移 20260727120000_StockColumnBusinessComments 一致，可单独在库上执行。
-- =============================================================================

COMMENT ON TABLE public.stock IS '库存分桶主表：按仓库/物料/批次/采销行等维度汇总数量；在库明细 stock_item 汇总至本表';
COMMENT ON COLUMN public.stock."StockId" IS '库存分桶主键（GUID）；stock_item.StockAggregateId 外键指向此列';
COMMENT ON COLUMN public.stock."StockCode" IS '库存业务编号（STK + 5 位 32 进制流水，如 STK00205）；与 stock_item.stock_item_code 前缀一致；历史行可空';
COMMENT ON COLUMN public.stock."MaterialId" IS '物料或采购明细行主键（GUID）；分桶维度之一，与入库明细 MaterialId 口径一致';
COMMENT ON COLUMN public.stock."WarehouseId" IS '仓库主键（GUID），关联 warehouse';
COMMENT ON COLUMN public.stock."LocationId" IS '库位主键（GUID，可选）；分桶维度之一';
COMMENT ON COLUMN public.stock."Unit" IS '计量单位（如 PCS）；展示用';
COMMENT ON COLUMN public.stock."BatchNo" IS '批次号（可选）；分桶维度之一';
COMMENT ON COLUMN public.stock."ProductionDate" IS '生产日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock."ExpiryDate" IS '过期日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock."Qty" IS '累计入库数量（总入库量；对应业务文档 Qty）';
COMMENT ON COLUMN public.stock."QtyStockOut" IS '累计已出库数量（QtyStockOut）';
COMMENT ON COLUMN public.stock."QtyOccupy" IS '拣货占用数量（QtyOccupy，已分配未实出）';
COMMENT ON COLUMN public.stock."QtySales" IS '销售预占数量（QtySales）';
COMMENT ON COLUMN public.stock."QtyRepertory" IS '在库数量（QtyRepertory = Qty − QtyStockOut，由服务层维护）';
COMMENT ON COLUMN public.stock."QtyRepertoryAvailable" IS '可用数量（QtyRepertory − QtyOccupy − QtySales）';
COMMENT ON COLUMN public.stock."Status" IS '分桶状态：1=正常 0=冻结';
COMMENT ON COLUMN public.stock."Type" IS '库存类型（StockType）：1=客单库存 2=备货库存 3=样品库存；与采销订单类型口径一致';
COMMENT ON COLUMN public.stock."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；入库过账自 stock_in.RegionType';
COMMENT ON COLUMN public.stock.purchase_order_item_code IS '采购订单明细业务编号（过账入库时自入库单/扩展表冗余，便于列表少联表）';
COMMENT ON COLUMN public.stock.purchase_order_item_id IS '采购订单明细主键（GUID，冗余）';
COMMENT ON COLUMN public.stock.sell_order_item_code IS '销售订单明细业务编号（过账入库时冗余）';
COMMENT ON COLUMN public.stock.sell_order_item_id IS '销售订单明细主键（GUID，冗余）';
COMMENT ON COLUMN public.stock.purchase_pn IS '采购型号 PN 冗余（过账时自 purchaseorderitem 解析）';
COMMENT ON COLUMN public.stock.purchase_brand IS '采购品牌冗余（过账时自 purchaseorderitem 解析）';
COMMENT ON COLUMN public.stock."Remark" IS '业务备注（自由文本）';
COMMENT ON COLUMN public.stock."CreateTime" IS '记录创建时间（UTC，首次过账写入）';
COMMENT ON COLUMN public.stock."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'stock' AND column_name = 'is_deleted'
  ) THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
  END IF;
END $$;
