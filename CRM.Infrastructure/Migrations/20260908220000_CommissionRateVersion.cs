using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>提成系数版本头；现有行挂到各类型 V1。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908220000_CommissionRateVersion")]
public partial class CommissionRateVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.commission_rate_version (
              id character varying(36) NOT NULL,
              role_type smallint NOT NULL,
              version_no integer NOT NULL,
              remark character varying(200) NULL,
              status smallint NOT NULL,
              created_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
              created_by character varying(36) NULL,
              updated_by character varying(36) NULL,
              CONSTRAINT "PK_commission_rate_version" PRIMARY KEY (id),
              CONSTRAINT "CK_commission_rate_version_role_type" CHECK (role_type IN (1, 2)),
              CONSTRAINT "CK_commission_rate_version_status" CHECK (status IN (1, 2, 3)),
              CONSTRAINT "CK_commission_rate_version_no" CHECK (version_no >= 1),
              CONSTRAINT "UQ_commission_rate_version_role_no" UNIQUE (role_type, version_no)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "UQ_commission_rate_version_active"
              ON public.commission_rate_version (role_type)
              WHERE status = 2;

            INSERT INTO public.commission_rate_version (id, role_type, version_no, remark, status, created_at, updated_at)
            VALUES
              ('c2000000-0000-4000-8000-000000000001', 1, 1, '初始版本', 2, NOW(), NOW()),
              ('c2000000-0000-4000-8000-000000000002', 2, 1, '初始版本', 2, NOW(), NOW())
            ON CONFLICT (role_type, version_no) DO NOTHING;

            ALTER TABLE public.commission_rate ADD COLUMN IF NOT EXISTS version_id character varying(36);

            DO $mig$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'commission_rate' AND column_name = 'role_type'
              ) THEN
                UPDATE public.commission_rate r
                SET version_id = v.id
                FROM public.commission_rate_version v
                WHERE r.version_id IS NULL
                  AND v.role_type = r.role_type
                  AND v.version_no = 1;
              END IF;
            END
            $mig$;

            UPDATE public.commission_rate
            SET version_id = 'c2000000-0000-4000-8000-000000000001'
            WHERE version_id IS NULL;

            ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "UQ_commission_rate_role_level";
            DROP INDEX IF EXISTS "IX_commission_rate_RoleType_UserLevel";
            DROP INDEX IF EXISTS "IX_commission_rate_role_type_user_level";
            ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "CK_commission_rate_role_type";
            ALTER TABLE public.commission_rate DROP COLUMN IF EXISTS role_type;
            ALTER TABLE public.commission_rate ALTER COLUMN version_id SET NOT NULL;

            DO $mig$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'UQ_commission_rate_version_level') THEN
                ALTER TABLE public.commission_rate
                  ADD CONSTRAINT "UQ_commission_rate_version_level" UNIQUE (version_id, user_level);
              END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_commission_rate_version') THEN
                ALTER TABLE public.commission_rate
                  ADD CONSTRAINT "FK_commission_rate_version"
                  FOREIGN KEY (version_id) REFERENCES public.commission_rate_version (id);
              END IF;
            END
            $mig$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "FK_commission_rate_version";
            ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "UQ_commission_rate_version_level";
            ALTER TABLE public.commission_rate ADD COLUMN IF NOT EXISTS role_type smallint;
            UPDATE public.commission_rate r
            SET role_type = v.role_type
            FROM public.commission_rate_version v
            WHERE r.version_id = v.id AND r.role_type IS NULL;
            ALTER TABLE public.commission_rate DROP COLUMN IF EXISTS version_id;
            DROP TABLE IF EXISTS public.commission_rate_version;
            """);
    }
}
