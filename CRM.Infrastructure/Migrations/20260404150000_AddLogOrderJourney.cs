using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404150000_AddLogOrderJourney")]
    public partial class AddLogOrderJourney : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.log_orderjourney (
    ""Id"" character varying(36) NOT NULL,
    ""EntityKind"" character varying(32) NOT NULL,
    ""EntityId"" character varying(36) NOT NULL,
    ""ParentEntityKind"" character varying(32),
    ""ParentEntityId"" character varying(36),
    ""DocumentCode"" character varying(64),
    ""LineHint"" character varying(200),
    ""EventCode"" character varying(64) NOT NULL,
    ""EventLabel"" character varying(200),
    ""FromState"" character varying(32),
    ""ToState"" character varying(32),
    ""EventTime"" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    ""Quantity"" numeric(18,4),
    ""Amount"" numeric(18,6),
    ""Currency"" smallint,
    ""Remark"" character varying(500),
    ""PayloadJson"" text,
    ""RelatedEntityKind"" character varying(32),
    ""RelatedEntityId"" character varying(36),
    ""ActorKind"" character varying(16),
    ""ActorUserId"" character varying(36),
    ""ActorUserName"" character varying(100),
    ""ActorVendorId"" character varying(36),
    ""Source"" character varying(64),
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT ""PK_log_orderjourney"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_log_orderjourney_entity""
    ON public.log_orderjourney (""EntityKind"", ""EntityId"", ""EventTime"");
CREATE INDEX IF NOT EXISTS ""IX_log_orderjourney_parent""
    ON public.log_orderjourney (""ParentEntityKind"", ""ParentEntityId"", ""EventTime"");
CREATE INDEX IF NOT EXISTS ""IX_log_orderjourney_document_code""
    ON public.log_orderjourney (""DocumentCode"", ""EventTime"")
    WHERE ""DocumentCode"" IS NOT NULL;
CREATE INDEX IF NOT EXISTS ""IX_log_orderjourney_event""
    ON public.log_orderjourney (""EventCode"", ""EventTime"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.log_orderjourney;");
        }
    }
}
