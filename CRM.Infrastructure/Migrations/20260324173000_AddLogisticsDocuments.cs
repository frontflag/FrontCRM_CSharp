using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324173000_AddLogisticsDocuments")]
    public partial class AddLogisticsDocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockinnotify (
    ""UserId"" character varying(36) NOT NULL,
    ""NoticeCode"" character varying(32) NOT NULL,
    ""PurchaseOrderId"" character varying(36) NOT NULL,
    ""PurchaseOrderCode"" character varying(32) NOT NULL,
    ""VendorId"" character varying(36),
    ""VendorName"" character varying(64),
    ""PurchaseUserName"" character varying(64),
    ""Status"" smallint NOT NULL DEFAULT 10,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_stockinnotify"" PRIMARY KEY (""UserId"")
);

CREATE TABLE IF NOT EXISTS public.stockinnotifyitem (
    ""UserId"" character varying(36) NOT NULL,
    ""StockInNotifyId"" character varying(36) NOT NULL,
    ""PurchaseOrderItemId"" character varying(36) NOT NULL,
    ""Pn"" character varying(128),
    ""Brand"" character varying(64),
    ""Qty"" numeric(18,4) NOT NULL,
    ""ArrivedQty"" numeric(18,4) NOT NULL,
    ""PassedQty"" numeric(18,4) NOT NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_stockinnotifyitem"" PRIMARY KEY (""UserId"")
);

CREATE TABLE IF NOT EXISTS public.qcinfo (
    ""UserId"" character varying(36) NOT NULL,
    ""QcCode"" character varying(32) NOT NULL,
    ""StockInNotifyId"" character varying(36) NOT NULL,
    ""StockInNotifyCode"" character varying(32) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 10,
    ""StockInStatus"" smallint NOT NULL DEFAULT 1,
    ""PassQty"" numeric(18,4) NOT NULL,
    ""RejectQty"" numeric(18,4) NOT NULL,
    ""StockInId"" character varying(36),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_qcinfo"" PRIMARY KEY (""UserId"")
);

CREATE TABLE IF NOT EXISTS public.qcitem (
    ""UserId"" character varying(36) NOT NULL,
    ""QcInfoId"" character varying(36) NOT NULL,
    ""StockInNotifyItemId"" character varying(36) NOT NULL,
    ""ArrivedQty"" numeric(18,4) NOT NULL,
    ""PassedQty"" numeric(18,4) NOT NULL,
    ""RejectQty"" numeric(18,4) NOT NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_qcitem"" PRIMARY KEY (""UserId"")
);

CREATE INDEX IF NOT EXISTS ""IX_stockinnotifyitem_StockInNotifyId"" ON public.stockinnotifyitem (""StockInNotifyId"");
CREATE INDEX IF NOT EXISTS ""IX_qcinfo_StockInNotifyId"" ON public.qcinfo (""StockInNotifyId"");
CREATE INDEX IF NOT EXISTS ""IX_qcitem_QcInfoId"" ON public.qcitem (""QcInfoId"");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockinnotifyitem_stockinnotify_StockInNotifyId') THEN
        ALTER TABLE public.stockinnotifyitem
        ADD CONSTRAINT ""FK_stockinnotifyitem_stockinnotify_StockInNotifyId""
        FOREIGN KEY (""StockInNotifyId"") REFERENCES public.stockinnotify (""UserId"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_qcinfo_stockinnotify_StockInNotifyId') THEN
        ALTER TABLE public.qcinfo
        ADD CONSTRAINT ""FK_qcinfo_stockinnotify_StockInNotifyId""
        FOREIGN KEY (""StockInNotifyId"") REFERENCES public.stockinnotify (""UserId"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_qcitem_qcinfo_QcInfoId') THEN
        ALTER TABLE public.qcitem
        ADD CONSTRAINT ""FK_qcitem_qcinfo_QcInfoId""
        FOREIGN KEY (""QcInfoId"") REFERENCES public.qcinfo (""UserId"") ON DELETE CASCADE;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // avoid destructive rollback
        }
    }
}
