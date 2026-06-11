-- 入库批次 V2（与 Migration 20260605130000_StockInBatchV2Redesign 一致，供手工执行）
DROP TABLE IF EXISTS public.stock_in_batch;

CREATE TABLE public.stock_in_batch (
    id character varying(36) NOT NULL,
    stock_in_item_id character varying(36) NOT NULL,
    global_batch_no character varying(20) NOT NULL,
    batch_dimension character varying(32) NULL,
    batch_unit character varying(32) NULL,
    unit_no character varying(128) NULL,
    batch_qty integer NOT NULL DEFAULT 0,
    dc character varying(64) NULL,
    package_origin character varying(200) NULL,
    wafer_origin character varying(200) NULL,
    lot character varying(128) NULL,
    serial_number character varying(200) NULL,
    firmware_version character varying(128) NULL,
    part_code character varying(128) NULL,
    remark character varying(1000) NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint NULL,
    "ModifyTime" timestamp with time zone NULL,
    "ModifyUserId" bigint NULL,
    CONSTRAINT "PK_stock_in_batch" PRIMARY KEY (id),
    CONSTRAINT "FK_stock_in_batch_stock_in_item" FOREIGN KEY (stock_in_item_id)
        REFERENCES public.stock_in_item("ItemId") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "UX_stock_in_batch_global_batch_no" ON public.stock_in_batch (global_batch_no);
CREATE INDEX "IX_stock_in_batch_stock_in_item_id" ON public.stock_in_batch (stock_in_item_id);
CREATE INDEX "IX_stock_in_batch_lot" ON public.stock_in_batch (lot);
CREATE INDEX "IX_stock_in_batch_serial_number" ON public.stock_in_batch (serial_number);

INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "ResetByMonth", "ResetByYear")
SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number),
       NOW() AT TIME ZONE 'utc', 0, 'InventoryBatch', '入库批次全局编号', 'PC', 8, false, false
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number WHERE "ModuleCode" = 'InventoryBatch');
