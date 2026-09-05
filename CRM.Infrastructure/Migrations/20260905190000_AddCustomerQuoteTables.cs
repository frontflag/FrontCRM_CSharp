using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260905190000_AddCustomerQuoteTables")]
public partial class AddCustomerQuoteTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.customer_quote_draft (
    ""CustomerQuoteDraftId"" character varying(36) NOT NULL,
    source_quote_item_id character varying(36) NOT NULL,
    source_quote_id character varying(36),
    rfq_item_id character varying(36),
    customer_id character varying(36),
    sales_user_id character varying(36),
    purchase_user_id character varying(36),
    mpn character varying(200),
    brand character varying(200),
    quantity numeric(18,4) NOT NULL DEFAULT 0,
    purchase_price numeric(18,6) NOT NULL DEFAULT 0,
    purchase_currency smallint NOT NULL DEFAULT 1,
    customer_mpn character varying(200),
    customer_brand character varying(200),
    source_quote_code character varying(32),
    source_quote_date timestamp with time zone,
    lead_time character varying(200),
    date_code character varying(100),
    remark character varying(500),
    status smallint NOT NULL DEFAULT 0,
    customer_quote_id character varying(36),
    create_by_user_id character varying(36),
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT pk_customer_quote_draft PRIMARY KEY (""CustomerQuoteDraftId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_customer_quote_draft_source_item
    ON public.customer_quote_draft (source_quote_item_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_customer_quote_draft_creator_status
    ON public.customer_quote_draft (create_by_user_id, status)
    WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.customer_quote (
    ""CustomerQuoteId"" character varying(36) NOT NULL,
    group_id character varying(36) NOT NULL,
    customer_quote_code character varying(32) NOT NULL,
    version_no integer NOT NULL DEFAULT 1,
    status smallint NOT NULL DEFAULT 0,
    customer_id character varying(36),
    customer_contact_id character varying(36),
    contact_name character varying(100),
    contact_email character varying(200),
    sales_user_id character varying(36),
    profit_factor numeric(8,2) NOT NULL DEFAULT 1.00,
    sent_at timestamp with time zone,
    sent_by_email boolean NOT NULL DEFAULT false,
    previous_version_id character varying(36),
    create_by_user_id character varying(36),
    modify_by_user_id character varying(36),
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT pk_customer_quote PRIMARY KEY (""CustomerQuoteId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_customer_quote_code_version
    ON public.customer_quote (customer_quote_code, version_no)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_customer_quote_group
    ON public.customer_quote (group_id)
    WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.customer_quote_item (
    ""CustomerQuoteItemId"" character varying(36) NOT NULL,
    customer_quote_id character varying(36) NOT NULL,
    line_no integer NOT NULL DEFAULT 1,
    source_quote_item_id character varying(36) NOT NULL,
    source_quote_id character varying(36),
    rfq_item_id character varying(36),
    mpn character varying(200),
    brand character varying(200),
    quantity numeric(18,4) NOT NULL DEFAULT 0,
    purchase_price numeric(18,6) NOT NULL DEFAULT 0,
    purchase_currency smallint NOT NULL DEFAULT 1,
    send_price numeric(18,6) NOT NULL DEFAULT 0,
    send_currency smallint NOT NULL DEFAULT 1,
    is_locked boolean NOT NULL DEFAULT false,
    customer_mpn character varying(200),
    customer_brand character varying(200),
    lead_time character varying(200),
    date_code character varying(100),
    remark character varying(500),
    source_quote_code character varying(32),
    source_quote_date timestamp with time zone,
    purchase_user_id character varying(36),
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT pk_customer_quote_item PRIMARY KEY (""CustomerQuoteItemId""),
    CONSTRAINT fk_customer_quote_item_header FOREIGN KEY (customer_quote_id)
        REFERENCES public.customer_quote (""CustomerQuoteId"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_customer_quote_item_header
    ON public.customer_quote_item (customer_quote_id)
    WHERE is_deleted = false;

DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomerQuote') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'CustomerQuote', '客户报价单', 'CQ', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.customer_quote_item;
DROP TABLE IF EXISTS public.customer_quote;
DROP TABLE IF EXISTS public.customer_quote_draft;
DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomerQuote';
");
    }
}
