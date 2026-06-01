-- =============================================================================
-- 库存/入库/出库/移库相关表：全部列业务注释（PostgreSQL COMMENT ON）
-- 与迁移 20260726120000 / 20260727120000 一致，可单独在库上执行。
-- stock 分桶见 scripts/stock_column_business_comments_postgresql.sql（亦含于本脚本开头）。
-- stockin_notify、stockout_notify 亦在本脚本中（与 20260722120000 内容一致，可重复执行）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- stock 库存分桶主表
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- stock_in 入库单主表
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_in IS '入库单主表：记录采购/质检/到货通知驱动的入库业务单头';
COMMENT ON COLUMN public.stock_in."StockInId" IS '入库单主键（GUID）；stock_in_item.StockInId、stock_in_extend.StockInId、stockitem.StockInId 等外键指向此列';
COMMENT ON COLUMN public.stock_in."StockInCode" IS '入库单业务编号（模块 StockIn 流水，如 STI0020P）；列表、打印与库存过账关联';
COMMENT ON COLUMN public.stock_in."StockInType" IS '入库业务类型（StockInTypeCode）：10=采购入库 20=报关入库 30=退货入库 40=报废入库；3=移库虚拟入库（列表常排除）';
COMMENT ON COLUMN public.stock_in."SourceCode" IS '到货通知业务单号冗余（stockin_notify.NoticeCode）；无通知时可为空';
COMMENT ON COLUMN public.stock_in."SourceId" IS '到货通知主键（stockin_notify.UserId）；质检生成入库时写入';
COMMENT ON COLUMN public.stock_in."QcCode" IS '质检单业务编号冗余（qcinfo.QcCode）';
COMMENT ON COLUMN public.stock_in."QCID" IS '质检单主键（qcinfo 主键）；列名 QCID 为历史命名';
COMMENT ON COLUMN public.stock_in."WarehouseId" IS '入库目标仓库主键（GUID），关联 warehouse';
COMMENT ON COLUMN public.stock_in."VendorId" IS '供应商主键（GUID）；采购入库时取自采购单/到货通知';
COMMENT ON COLUMN public.stock_in."StockInDate" IS '入库日期（timestamptz，存 UTC）；质检生成时可取自 qcinfo.StockInPlanDate';
COMMENT ON COLUMN public.stock_in."TotalQuantity" IS '入库总数量（整数，明细 Quantity 合计）';
COMMENT ON COLUMN public.stock_in."TotalAmount" IS '入库总金额（numeric(18,2)，明细 Amount 合计）';
COMMENT ON COLUMN public.stock_in."Status" IS '入库单状态：0=草稿 1=待入库 2=已入库 3=已取消；状态 2 触发库存中心过账';
COMMENT ON COLUMN public.stock_in."InspectStatus" IS '质检状态冗余：0=未质检 1=合格 2=不合格（与 qcinfo 结论可不一致，以质检单为准）';
COMMENT ON COLUMN public.stock_in."CreatedBy" IS '创建人标识（varchar，业务操作人快照）';
COMMENT ON COLUMN public.stock_in."ApprovedBy" IS '审核人标识（varchar）';
COMMENT ON COLUMN public.stock_in."ApprovedTime" IS '审核时间（timestamptz）';
COMMENT ON COLUMN public.stock_in."Remark" IS '业务备注（自由文本）';
COMMENT ON COLUMN public.stock_in."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；默认自到货通知，影响库存分桶 RegionType';
COMMENT ON COLUMN public.stock_in."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_in."CreateUserId" IS '历史创建人 bigint（旧体系审计字段；业务追溯请优先 create_by_user_id）';
COMMENT ON COLUMN public.stock_in."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock_in."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_in.create_by_user_id IS '创建入库单时的登录用户 GUID（JWT 用户主键）';
COMMENT ON COLUMN public.stock_in.modify_by_user_id IS '最后修改入库单时的登录用户 GUID';

-- -----------------------------------------------------------------------------
-- stock_in_extend 入库单主单扩展（行号水位）
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_in_extend IS '入库单主单扩展（1:1 stock_in）：维护入库明细业务编号行序号水位';
COMMENT ON COLUMN public.stock_in_extend."StockInId" IS '入库单主键（GUID），与 stock_in.StockInId 一致，兼作本表主键';
COMMENT ON COLUMN public.stock_in_extend.last_item_line_seq IS '已分配的最大入库明细行序号（生成 stock_in_item_code 时递增）';
COMMENT ON COLUMN public.stock_in_extend."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_in_extend."ModifyTime" IS '记录最后修改时间（UTC）';

-- -----------------------------------------------------------------------------
-- stock_in_item 入库单明细
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_in_item IS '入库单明细：一行对应一次入库的物料/数量/单价；过账后生成 stock_item';
COMMENT ON COLUMN public.stock_in_item."ItemId" IS '入库明细主键（GUID）；stock_in_item_extend.StockInItemId、stock_in_batch.stock_in_item_id、stockitem.StockInItemId 外键';
COMMENT ON COLUMN public.stock_in_item."StockInId" IS '所属入库单主键（GUID），关联 stock_in.StockInId';
COMMENT ON COLUMN public.stock_in_item."MaterialId" IS '物料或采购明细行主键（GUID，最大 36 字符）；质检生成时常为 purchaseorderitem 主键';
COMMENT ON COLUMN public.stock_in_item.purchase_pn IS '采购明细型号 PN 快照（过账/展示冗余）';
COMMENT ON COLUMN public.stock_in_item.purchase_brand IS '采购明细品牌快照';
COMMENT ON COLUMN public.stock_in_item.stock_in_item_code IS '入库明细业务编号（{StockInCode}-{行序号}，如 STI0020P-1）';
COMMENT ON COLUMN public.stock_in_item.currency IS '采购币别（1=RMB 2=USD 3=EUR 4=HKD）；与采购明细 cost 币别一致';
COMMENT ON COLUMN public.stock_in_item."Quantity" IS '本行入库数量（整数）';
COMMENT ON COLUMN public.stock_in_item."OrderQty" IS '来源订单应收数量（整数，冗余）';
COMMENT ON COLUMN public.stock_in_item."QtyReceived" IS '累计已入库数量（支持多次部分入库场景）';
COMMENT ON COLUMN public.stock_in_item."Price" IS '采购单价（numeric(18,6)）';
COMMENT ON COLUMN public.stock_in_item."Amount" IS '本行入库金额（numeric(18,2)，一般为 Quantity×Price）';
COMMENT ON COLUMN public.stock_in_item."LocationId" IS '库位主键（GUID，可选）';
COMMENT ON COLUMN public.stock_in_item."BatchNo" IS '批次号（可选，与 stock_in_batch 可并存）';
COMMENT ON COLUMN public.stock_in_item."ProductionDate" IS '生产日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock_in_item."ExpiryDate" IS '过期日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock_in_item."IsQualified" IS '是否质检合格（默认 true）';
COMMENT ON COLUMN public.stock_in_item."Remark" IS '明细备注';
COMMENT ON COLUMN public.stock_in_item."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_in_item."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_in_item."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock_in_item."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- stock_in_batch 入库批次（LOT/SN）
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_in_batch IS '入库批次记录：按入库明细维护 LOT、SN、产地、固件版本等追溯维度';
COMMENT ON COLUMN public.stock_in_batch.id IS '批次记录主键（GUID）';
COMMENT ON COLUMN public.stock_in_batch.stock_in_id IS '所属入库单主键（GUID），冗余便于按单查询';
COMMENT ON COLUMN public.stock_in_batch.stock_in_item_id IS '所属入库明细主键（GUID），外键 stock_in_item.ItemId';
COMMENT ON COLUMN public.stock_in_batch.stock_in_item_code IS '入库明细业务编号冗余（与 stock_in_item.stock_in_item_code 一致）';
COMMENT ON COLUMN public.stock_in_batch.material_model IS '物料型号（PN）快照';
COMMENT ON COLUMN public.stock_in_batch.dc IS 'Date Code（周次/日期码）';
COMMENT ON COLUMN public.stock_in_batch.package_origin IS '封装产地';
COMMENT ON COLUMN public.stock_in_batch.wafer_origin IS '晶圆产地';
COMMENT ON COLUMN public.stock_in_batch.lot IS 'LOT 批次号';
COMMENT ON COLUMN public.stock_in_batch.lot_qty_in IS 'LOT 维度入库数量累计';
COMMENT ON COLUMN public.stock_in_batch.lot_qty_out IS 'LOT 维度已出库数量累计';
COMMENT ON COLUMN public.stock_in_batch.origin IS '产地（通用字段，可与 package/wafer 产地配合）';
COMMENT ON COLUMN public.stock_in_batch.serial_number IS '序列号（SN）';
COMMENT ON COLUMN public.stock_in_batch.sn_qty_in IS 'SN 维度入库数量累计';
COMMENT ON COLUMN public.stock_in_batch.sn_qty_out IS 'SN 维度已出库数量累计';
COMMENT ON COLUMN public.stock_in_batch.firmware_version IS '固件版本号';
COMMENT ON COLUMN public.stock_in_batch.remark IS '批次备注';
COMMENT ON COLUMN public.stock_in_batch."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_in_batch."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_in_batch."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock_in_batch."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- stock_item 在库明细层
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_item IS '在库明细层：与 stock_in_item 1:1，记录可出库数量、采销价与订单行追溯；汇总至 stock 分桶';
COMMENT ON COLUMN public.stock_item."StockItemId" IS '在库明细主键（GUID）；stock_out_item.StockItemId、stockledger.source_stock_item_id 等外键';
COMMENT ON COLUMN public.stock_item."StockInItemId" IS '对应入库明细主键（GUID），全局唯一；与 stock_in_item.ItemId 一致';
COMMENT ON COLUMN public.stock_item."StockInId" IS '来源入库单主键（GUID）';
COMMENT ON COLUMN public.stock_item."StockAggregateId" IS '库存分桶主键（stock.StockId），同仓同型号等同维度汇总';
COMMENT ON COLUMN public.stock_item.stock_item_code IS '在库明细业务编号（{StockCode}-{行序号}，与分桶 StockCode 规则一致）';
COMMENT ON COLUMN public.stock_item.stock_in_item_code IS '入库明细业务编号冗余（自 stock_in_item.stock_in_item_code）';
COMMENT ON COLUMN public.stock_item."MaterialId" IS '物料/采购行主键（GUID，与入库明细 MaterialId 一致）';
COMMENT ON COLUMN public.stock_item."WarehouseId" IS '所在仓库主键（GUID）';
COMMENT ON COLUMN public.stock_item."LocationId" IS '库位主键（GUID，可选）';
COMMENT ON COLUMN public.stock_item."BatchNo" IS '批次号（可选）';
COMMENT ON COLUMN public.stock_item."ProductionDate" IS '生产日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock_item."ExpiryDate" IS '过期日期（timestamptz，可选）';
COMMENT ON COLUMN public.stock_item."Type" IS '库存类型（StockType）：1=客单库存 2=备货库存 3=样品库存；与采销订单类型口径一致';
COMMENT ON COLUMN public.stock_item."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；入库过账自 stock_in.RegionType';
COMMENT ON COLUMN public.stock_item."TransferType" IS '移库标记：手工移库源行出清等特殊场景（见 StockItemTransferTypeCodes）；常规采购入库为 null';
COMMENT ON COLUMN public.stock_item.purchase_pn IS '采购型号 PN 冗余（过账时自采购明细解析）';
COMMENT ON COLUMN public.stock_item.purchase_brand IS '采购品牌冗余';
COMMENT ON COLUMN public.stock_item.sell_order_item_id IS '关联销售订单明细主键（GUID，冗余）';
COMMENT ON COLUMN public.stock_item.sell_order_item_code IS '销售订单明细业务编号冗余';
COMMENT ON COLUMN public.stock_item.purchase_order_item_id IS '采购订单明细主键（GUID，冗余）';
COMMENT ON COLUMN public.stock_item.purchase_order_item_code IS '采购订单明细业务编号冗余（列表/追溯展示）';
COMMENT ON COLUMN public.stock_item."VendorId" IS '供应商主键（GUID，展示冗余）';
COMMENT ON COLUMN public.stock_item."VendorName" IS '供应商名称冗余';
COMMENT ON COLUMN public.stock_item."PurchaserId" IS '采购业务员用户主键（GUID，冗余）';
COMMENT ON COLUMN public.stock_item."PurchaserName" IS '采购业务员姓名冗余';
COMMENT ON COLUMN public.stock_item."PurchasePrice" IS '采购单价原币（numeric(18,6)，过账时自入库明细 Price）';
COMMENT ON COLUMN public.stock_item."PurchaseCurrency" IS '采购单价币别（1=RMB 2=USD 3=EUR 4=HKD）';
COMMENT ON COLUMN public.stock_item."PurchasePriceUsd" IS '采购单价折合 USD（numeric(18,6)，按财务基准汇率计算）';
COMMENT ON COLUMN public.stock_item."PurchaseAmount" IS '入库金额原币（numeric(18,2)）';
COMMENT ON COLUMN public.stock_item."CustomerId" IS '客户主键（GUID，有销售行时冗余）';
COMMENT ON COLUMN public.stock_item."CustomerName" IS '客户名称冗余';
COMMENT ON COLUMN public.stock_item."SalespersonId" IS '销售业务员用户主键（GUID，冗余）';
COMMENT ON COLUMN public.stock_item."SalespersonName" IS '销售业务员姓名冗余';
COMMENT ON COLUMN public.stock_item."SalesPrice" IS '销售单价原币（numeric(18,6)，有销售行时冗余）';
COMMENT ON COLUMN public.stock_item."SalesCurrency" IS '销售单价币别；无销售行时为 null';
COMMENT ON COLUMN public.stock_item."SalesPriceUsd" IS '销售单价折合 USD；无销售行时为 null';
COMMENT ON COLUMN public.stock_item."ProfitOutBizUsd" IS '入库时 USD 毛利快照：(SalesPriceUsd−PurchasePriceUsd)×QtyInbound；出库利润见 stock_out_item_extend';
COMMENT ON COLUMN public.stock_item."QtyInbound" IS '入库数量（本层初始在库量）';
COMMENT ON COLUMN public.stock_item."QtyStockOut" IS '已出库数量累计';
COMMENT ON COLUMN public.stock_item."StockOutStatus" IS '出库状态：0=无有效入库 1=未出库 2=部分出库 3=出库完成（由数量推导持久化）';
COMMENT ON COLUMN public.stock_item."QtyOccupy" IS '拣货占用数量';
COMMENT ON COLUMN public.stock_item."QtySales" IS '销售预占数量';
COMMENT ON COLUMN public.stock_item."QtyRepertory" IS '在库数量（QtyInbound−QtyStockOut 等口径，与汇总逻辑一致）';
COMMENT ON COLUMN public.stock_item."QtyRepertoryAvailable" IS '可用数量（在库−占用−预占）';
COMMENT ON COLUMN public.stock_item."CreateTime" IS '记录创建时间（UTC，过账写入）';
COMMENT ON COLUMN public.stock_item."ModifyTime" IS '记录最后修改时间（UTC）';

-- -----------------------------------------------------------------------------
-- stock_out 出库单主表
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_out IS '出库单主表：销售/报关/拣货过账产生的出库业务单头';
COMMENT ON COLUMN public.stock_out."StockOutId" IS '出库单主键（GUID）；stock_out_item.StockOutId 外键';
COMMENT ON COLUMN public.stock_out."StockOutCode" IS '出库单业务编号（模块 StockOut 流水）';
COMMENT ON COLUMN public.stock_out."StockOutType" IS '出库业务类型（StockOutTypeCode）：10=销售出库 20=报关出库 30=退货出库 40=报废出库；3=移库虚拟出库';
COMMENT ON COLUMN public.stock_out."Type" IS '整型分类（数据库列 Type，与 StockOutType 独立；具体含义由业务模块定义）';
COMMENT ON COLUMN public.stock_out."SourceCode" IS '来源单号冗余（如装箱单、出库通知等，视创建场景）';
COMMENT ON COLUMN public.stock_out."SourceId" IS '来源单主键（GUID，视创建场景）';
COMMENT ON COLUMN public.stock_out."WarehouseId" IS '出库仓库主键（GUID）';
COMMENT ON COLUMN public.stock_out."CustomerId" IS '客户主键（GUID，销售出库）';
COMMENT ON COLUMN public.stock_out."SellOrderItemId" IS '销售订单明细主键（GUID，按行出库时冗余）';
COMMENT ON COLUMN public.stock_out."StockOutDate" IS '出库日期（timestamptz，存 UTC）';
COMMENT ON COLUMN public.stock_out."TotalQuantity" IS '出库总数量（整数）';
COMMENT ON COLUMN public.stock_out."TotalAmount" IS '出库总金额（numeric(18,2)）';
COMMENT ON COLUMN public.stock_out."Status" IS '出库单状态：0=草稿 1=待出库 2=已出库 3=已取消';
COMMENT ON COLUMN public.stock_out."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；执行出库时自库存分桶冗余';
COMMENT ON COLUMN public.stock_out."PickerId" IS '拣货人用户主键（GUID）';
COMMENT ON COLUMN public.stock_out."PickedTime" IS '拣货完成时间（timestamptz）';
COMMENT ON COLUMN public.stock_out."ConfirmedBy" IS '出库确认人标识';
COMMENT ON COLUMN public.stock_out."ConfirmedTime" IS '出库确认时间（timestamptz）';
COMMENT ON COLUMN public.stock_out."Remark" IS '业务备注';
COMMENT ON COLUMN public.stock_out."ShipmentMethod" IS '出货方式：数据字典 LogisticsArrivalMethod 的 ItemCode';
COMMENT ON COLUMN public.stock_out."CourierTrackingNo" IS '快递/物流单号';
COMMENT ON COLUMN public.stock_out.picking_task_id IS '关联拣货任务主键（pickingtask.Id）';
COMMENT ON COLUMN public.stock_out."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_out."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_out."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock_out."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_out.create_by_user_id IS '创建出库单时的登录用户 GUID';
COMMENT ON COLUMN public.stock_out.modify_by_user_id IS '最后修改出库单时的登录用户 GUID';

-- -----------------------------------------------------------------------------
-- stock_out_item 出库单明细
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stock_out_item IS '出库单明细：记录从哪条在库明细扣减多少数量';
COMMENT ON COLUMN public.stock_out_item."ItemId" IS '出库明细主键（GUID）；stock_out_item_extend.StockOutItemId 外键';
COMMENT ON COLUMN public.stock_out_item."StockOutId" IS '所属出库单主键（GUID）';
COMMENT ON COLUMN public.stock_out_item."MaterialId" IS '物料/采购行主键（GUID）';
COMMENT ON COLUMN public.stock_out_item.purchase_pn IS '物料型号 PN 冗余';
COMMENT ON COLUMN public.stock_out_item.purchase_brand IS '品牌冗余';
COMMENT ON COLUMN public.stock_out_item."Quantity" IS '出库数量（整数）';
COMMENT ON COLUMN public.stock_out_item."OrderQty" IS '订单应出数量';
COMMENT ON COLUMN public.stock_out_item."PlanQty" IS '计划出库数量';
COMMENT ON COLUMN public.stock_out_item."PickQty" IS '拣货占用数量';
COMMENT ON COLUMN public.stock_out_item."ActualQty" IS '实际出库数量';
COMMENT ON COLUMN public.stock_out_item."Price" IS '出库单价（numeric(18,6)）';
COMMENT ON COLUMN public.stock_out_item."Amount" IS '出库金额（numeric(18,2)）';
COMMENT ON COLUMN public.stock_out_item."LocationId" IS '库位主键（GUID，可选）';
COMMENT ON COLUMN public.stock_out_item."StockId" IS '库存分桶主键（stock.StockId，历史/冗余字段）';
COMMENT ON COLUMN public.stock_out_item."StockItemId" IS '扣减的在库明细主键（stock_item.StockItemId）；拣货出库绑定';
COMMENT ON COLUMN public.stock_out_item.picking_task_item_id IS '来源拣货明细主键（pickingtaskitem.Id）';
COMMENT ON COLUMN public.stock_out_item.packing_id IS '来源装箱单主键（packing.Id）';
COMMENT ON COLUMN public.stock_out_item."WarehouseId" IS '仓库主键冗余（便于查询）';
COMMENT ON COLUMN public.stock_out_item."BatchNo" IS '批次号';
COMMENT ON COLUMN public.stock_out_item."Remark" IS '明细备注';
COMMENT ON COLUMN public.stock_out_item."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stock_out_item."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stock_out_item."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stock_out_item."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- stockledger 库存流水
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stockledger IS '库存流水账：入库/出库/移库/盘点等数量与成本变动明细（原 inventoryledger）';
COMMENT ON COLUMN public.stockledger."Id" IS '流水主键（GUID）';
COMMENT ON COLUMN public.stockledger."BizType" IS '业务类型：STOCK_IN=入库 STOCK_OUT=出库 COUNT_ADJUST=盘点调整 STOCK_TRANS=移库等';
COMMENT ON COLUMN public.stockledger."BizId" IS '业务单主键（如 stock_in.StockInId、stock_out.StockOutId）';
COMMENT ON COLUMN public.stockledger."BizLineId" IS '业务明细主键（如 stock_in_item.ItemId）；与 BizType+BizId 组成唯一键';
COMMENT ON COLUMN public.stockledger."MaterialId" IS '物料/采购行主键（GUID）';
COMMENT ON COLUMN public.stockledger."WarehouseId" IS '仓库主键（GUID）';
COMMENT ON COLUMN public.stockledger."LocationId" IS '库位主键（GUID，可选）';
COMMENT ON COLUMN public.stockledger."BatchNo" IS '批次号（可选）';
COMMENT ON COLUMN public.stockledger."QtyIn" IS '入库数量（本条流水增加量）';
COMMENT ON COLUMN public.stockledger."QtyOut" IS '出库数量（本条流水减少量）';
COMMENT ON COLUMN public.stockledger."UnitCost" IS '单位成本（numeric(18,6)）';
COMMENT ON COLUMN public.stockledger."Amount" IS '金额（numeric(18,2)）';
COMMENT ON COLUMN public.stockledger.currency IS '币别（1=RMB 2=USD 3=EUR 4=HKD）';
COMMENT ON COLUMN public.stockledger.purchase_order_item_code IS '采购订单明细业务编号（写入时自 stock 冗余）';
COMMENT ON COLUMN public.stockledger.purchase_order_item_id IS '采购订单明细主键（GUID）';
COMMENT ON COLUMN public.stockledger.sell_order_item_code IS '销售订单明细业务编号';
COMMENT ON COLUMN public.stockledger.sell_order_item_id IS '销售订单明细主键（GUID）';
COMMENT ON COLUMN public.stockledger."Remark" IS '流水备注（常含业务单号说明）';
COMMENT ON COLUMN public.stockledger.from_warehouse_id IS '移库源仓库主键（GUID，移库流水使用）';
COMMENT ON COLUMN public.stockledger.to_warehouse_id IS '移库目标仓库主键（GUID，移库流水使用）';
COMMENT ON COLUMN public.stockledger.create_by_user_id IS '写入流水时的登录用户 GUID';
COMMENT ON COLUMN public.stockledger.customs_declaration_id IS '关联报关单主键（报关/移库场景）';
COMMENT ON COLUMN public.stockledger.stock_transfer_id IS '关联移库单主键（stocktransfer_customers.StockTransferId）';
COMMENT ON COLUMN public.stockledger.source_stock_item_id IS '源在库明细主键（移库出）';
COMMENT ON COLUMN public.stockledger.target_stock_item_id IS '目标在库明细主键（移库入）';
COMMENT ON COLUMN public.stockledger."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stockledger."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stockledger."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stockledger."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- stocktransfer_item_customers 报关移库明细（客供/客户库存场景）
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stocktransfer_item_customers IS '报关移库明细：将源在库明细从境内仓转移至境外仓（关联报关单与出库通知）';
COMMENT ON COLUMN public.stocktransfer_item_customers."StockTransferItemId" IS '移库明细主键（GUID）';
COMMENT ON COLUMN public.stocktransfer_item_customers."StockTransferId" IS '所属移库单主键（stocktransfer_customers.StockTransferId）';
COMMENT ON COLUMN public.stocktransfer_item_customers."SourceStockItemId" IS '源在库明细主键（stock_item.StockItemId，境内库存扣减）';
COMMENT ON COLUMN public.stocktransfer_item_customers."CustomsDeclarationItemId" IS '报关单明细主键（customs_declaration_item）';
COMMENT ON COLUMN public.stocktransfer_item_customers."StockOutRequestId" IS '出库通知主键（stockout_notify.ID）';
COMMENT ON COLUMN public.stocktransfer_item_customers."Qty" IS '本行移库数量（整数）';
COMMENT ON COLUMN public.stocktransfer_item_customers."TargetStockItemId" IS '目标在库明细主键（移库完成后写入的境外 stock_item）';
COMMENT ON COLUMN public.stocktransfer_item_customers."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stocktransfer_item_customers."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stocktransfer_item_customers."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stocktransfer_item_customers."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- stockout_notify 出库通知
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stockout_notify IS '出库通知（单表：一条通知对应一条销售订单明细的一次申请出库）';
COMMENT ON COLUMN public.stockout_notify."ID" IS '出库通知主键（GUID）；packing_item.stockout_notify_id、customs_declaration.StockOutRequestId 等外键指向此列';
COMMENT ON COLUMN public.stockout_notify."Code" IS '出库通知业务单号（模块 StockOutRequest/STOR 流水，如 STOR00001）；列表、打印与下游装箱关联';
COMMENT ON COLUMN public.stockout_notify."SalesOrderId" IS '所属销售订单主键（GUID），关联 sellorder.SellOrderId';
COMMENT ON COLUMN public.stockout_notify."SalesOrderItemId" IS '所属销售订单明细主键（GUID），关联 sellorderitem.SellOrderItemId；本表一条记录仅绑定一行销售明细';
COMMENT ON COLUMN public.stockout_notify."MaterialCode" IS '申请出库物料型号（PN）快照；创建时默认取自销售明细 pn，后续不随销售行变更自动刷新';
COMMENT ON COLUMN public.stockout_notify."MaterialName" IS '申请出库品牌快照；创建时默认取自销售明细 brand';
COMMENT ON COLUMN public.stockout_notify."Quantity" IS '本通知申请出库数量（整数）；与同销售明细其它未取消通知数量合计不得超过该行可出库余量';
COMMENT ON COLUMN public.stockout_notify."CustomerId" IS '发货客户主键（GUID），关联 customerinfo；创建时由请求体或销售订单客户解析写入';
COMMENT ON COLUMN public.stockout_notify."RequestUserId" IS '申请人用户主键（GUID，与 JWT 登录用户一致）';
COMMENT ON COLUMN public.stockout_notify."RequestDate" IS '申请/计划出库日期（timestamptz，存 UTC）';
COMMENT ON COLUMN public.stockout_notify."Status" IS '出库通知状态（StockOutRequestStatusCode）：10=待装箱 20=已装箱 100=已出库 -1=已取消；取消后不可再加入装箱篮子';
COMMENT ON COLUMN public.stockout_notify."Remark" IS '业务备注（自由文本）';
COMMENT ON COLUMN public.stockout_notify."ShipmentMethod" IS '出货方式：数据字典 LogisticsArrivalMethod 的 ItemCode（与物流「来货方式」同源，存编码非展示名）';
COMMENT ON COLUMN public.stockout_notify."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；与仓库、入库单、出库单共用枚举，影响物流/报关场景判断';
COMMENT ON COLUMN public.stockout_notify."StockOutType" IS '出库业务类型（StockOutTypeCode）：10=销售出库 20=报关出库 30=退货出库 40=报废出库；非法值归一为 10；装箱单要求同批通知类型一致';
COMMENT ON COLUMN public.stockout_notify."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stockout_notify."CreateUserId" IS '历史创建人 bigint（旧体系审计字段；业务追溯请优先 create_by_user_id）';
COMMENT ON COLUMN public.stockout_notify."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stockout_notify."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stockout_notify.create_by_user_id IS '创建本通知时的登录用户 GUID（JWT 用户主键，审计与权限追溯）';
COMMENT ON COLUMN public.stockout_notify.modify_by_user_id IS '最后修改本通知时的登录用户 GUID';

-- -----------------------------------------------------------------------------
-- stockin_notify 到货通知
-- -----------------------------------------------------------------------------
COMMENT ON TABLE public.stockin_notify IS '到货通知（单表：一条记录 = 采购订单明细上的一次到货批次）';
COMMENT ON COLUMN public.stockin_notify."UserId" IS '到货通知主键（GUID）；qcinfo.StockInNotifyId、入库单 SourceId 等外键指向此列（列名 UserId 为历史命名）';
COMMENT ON COLUMN public.stockin_notify."NoticeCode" IS '到货通知业务单号（模块 ArrivalNotice 流水）；列表展示与质检单 StockInNotifyCode 冗余关联';
COMMENT ON COLUMN public.stockin_notify."PurchaseOrderId" IS '所属采购订单主键（GUID），关联 purchaseorder';
COMMENT ON COLUMN public.stockin_notify."PurchaseOrderCode" IS '采购订单号冗余（创建时从采购主单复制，减少列表联表）';
COMMENT ON COLUMN public.stockin_notify."PurchaseOrderItemId" IS '所属采购订单明细主键（GUID）；同一采购行可有多条到货通知（分批到货）';
COMMENT ON COLUMN public.stockin_notify."SellOrderItemId" IS '关联销售订单明细主键（GUID，冗余自采购明细）；用于追溯销售需求与扩展表回算';
COMMENT ON COLUMN public.stockin_notify."VendorId" IS '供应商主键（GUID，创建时从采购订单复制）';
COMMENT ON COLUMN public.stockin_notify."VendorName" IS '供应商名称冗余（展示用）';
COMMENT ON COLUMN public.stockin_notify."PurchaseUserName" IS '采购业务员名称冗余（来自采购订单，展示用）';
COMMENT ON COLUMN public.stockin_notify."Status" IS '到货通知状态：1=新建（遗留）10=未到货 20=到货待检 30=已质检 100=已入库；由质检/入库及 purchaseorderitemextend 同步回写';
COMMENT ON COLUMN public.stockin_notify."ExpectedArrivalDate" IS '预计到货日期（timestamptz）；创建时默认取采购明细或采购主单交货日';
COMMENT ON COLUMN public.stockin_notify."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；与仓库档案、入库单 RegionType 语义一致';
COMMENT ON COLUMN public.stockin_notify."StockInType" IS '入库业务类型（StockInTypeCode）：10=采购入库 20=报关入库 30=退货入库 40=报废入库；下游 stock_in.StockInType 可取自此字段';
COMMENT ON COLUMN public.stockin_notify."Pn" IS '物料型号（PN）快照，创建时取自采购明细';
COMMENT ON COLUMN public.stockin_notify."Brand" IS '品牌快照，创建时取自采购明细';
COMMENT ON COLUMN public.stockin_notify."ExpectQty" IS '本批次预期到货数量（整数）；创建时不得超过采购行扩展 QtyStockInNotifyNot';
COMMENT ON COLUMN public.stockin_notify."ReceiveQty" IS '本批次实收到货数量；质检/收货流程回写，参与采购行「剩余可通知」余量计算';
COMMENT ON COLUMN public.stockin_notify."PassedQty" IS '本批次质检通过数量汇总（各质检明细通过量合计回写）';
COMMENT ON COLUMN public.stockin_notify."Cost" IS '采购单价快照（numeric(18,6)，创建时取自采购明细 cost）';
COMMENT ON COLUMN public.stockin_notify."ExpectTotal" IS '预期到货金额（numeric(18,2)），创建时按 ExpectQty×Cost 四舍五入';
COMMENT ON COLUMN public.stockin_notify."ReceiveTotal" IS '实收金额（numeric(18,2)），随收货/入库流程回写';
COMMENT ON COLUMN public.stockin_notify."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.stockin_notify."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
COMMENT ON COLUMN public.stockin_notify."ModifyTime" IS '记录最后修改时间（UTC）';
COMMENT ON COLUMN public.stockin_notify."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';

-- -----------------------------------------------------------------------------
-- 软删除列（部分表在 20260708120000 后才有 is_deleted）
-- -----------------------------------------------------------------------------
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_in' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_in.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_in_extend' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_in_extend.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_in_item' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_in_item.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_in_batch' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_in_batch.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_item' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_item.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_out' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_out.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_out_item' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stock_out_item.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktransfer_item_customers' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stocktransfer_item_customers.is_deleted IS '软删除标记：true 表示逻辑删除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stockout_notify.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockin_notify' AND column_name = 'is_deleted') THEN
    EXECUTE $c$COMMENT ON COLUMN public.stockin_notify.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
  END IF;
END $$;
