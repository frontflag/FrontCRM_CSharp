-- 入库明细扩展表（与 EF 迁移 20260627120000_StockInItemExtendTable 一致；可单独手工执行）
CREATE TABLE IF NOT EXISTS public.stock_in_item_extend (
    ""StockInItemId"" character varying(36) NOT NULL,
    ""StockInId"" character varying(36) NOT NULL,
    ""sell_order_item_id"" character varying(36) NULL,
    ""sell_order_item_code"" character varying(64) NULL,
    ""purchase_order_item_id"" character varying(36) NULL,
    ""purchase_order_item_code"" character varying(64) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_stockinitemextend"" PRIMARY KEY (""StockInItemId""),
    CONSTRAINT ""FK_stockinitemextend_stockinitem"" FOREIGN KEY (""StockInItemId"")
        REFERENCES public.stock_in_item (""ItemId"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_stockinitemextend_StockInId""
    ON public.stock_in_item_extend (""StockInId"");

COMMENT ON TABLE public.stock_in_item_extend IS '入库明细扩展：与 stockinitem 一对一（StockInItemId = ItemId）；冗余 StockInId 及采销订单行关联';
COMMENT ON COLUMN public.stock_in_item_extend.""StockInId"" IS '入库单主键（与 stockinitem.StockInId 一致）';
COMMENT ON COLUMN public.stock_in_item_extend.""sell_order_item_id"" IS '销售订单明细主键';
COMMENT ON COLUMN public.stock_in_item_extend.""sell_order_item_code"" IS '销售订单明细业务编号';
COMMENT ON COLUMN public.stock_in_item_extend.""purchase_order_item_id"" IS '采购订单明细主键';
COMMENT ON COLUMN public.stock_in_item_extend.""purchase_order_item_code"" IS '采购订单明细业务编号';

