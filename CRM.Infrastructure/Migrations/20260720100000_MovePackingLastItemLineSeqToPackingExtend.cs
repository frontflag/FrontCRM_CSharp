using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 装箱明细行序号水位：由 packing.last_item_line_seq 迁至 packing_extend.last_item_line_seq（1:1 扩展表）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260720100000_MovePackingLastItemLineSeqToPackingExtend")]
public partial class MovePackingLastItemLineSeqToPackingExtend : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.packing_extend (
                "PackingId" character varying(36) NOT NULL,
                last_item_line_seq integer NOT NULL DEFAULT 0,
                "CreateTime" timestamp with time zone NOT NULL DEFAULT NOW(),
                "ModifyTime" timestamp with time zone NULL,
                CONSTRAINT "PK_packing_extend" PRIMARY KEY ("PackingId")
            );

            COMMENT ON TABLE public.packing_extend IS '装箱单主单扩展：明细行序号水位';
            COMMENT ON COLUMN public.packing_extend.last_item_line_seq IS '装箱明细行序号水位（删除行不回收）';

            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_extend_packing_PackingId'
              ) THEN
                ALTER TABLE public.packing_extend
                  ADD CONSTRAINT "FK_packing_extend_packing_PackingId"
                  FOREIGN KEY ("PackingId") REFERENCES public.packing ("Id") ON DELETE CASCADE;
              END IF;
            END $$;

            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'packing' AND column_name = 'last_item_line_seq'
              ) THEN
                INSERT INTO public.packing_extend ("PackingId", last_item_line_seq, "CreateTime", "ModifyTime")
                SELECT p."Id",
                       COALESCE(p.last_item_line_seq, 0),
                       COALESCE(p."CreateTime", NOW()),
                       p."ModifyTime"
                FROM public.packing p
                ON CONFLICT ("PackingId") DO UPDATE SET
                  last_item_line_seq = GREATEST(
                    packing_extend.last_item_line_seq,
                    EXCLUDED.last_item_line_seq);

                ALTER TABLE public.packing DROP COLUMN IF EXISTS last_item_line_seq;
              END IF;
            END $$;

            -- 按已有 packing_item.item_code 回填序号水位（库已手工迁列时）
            UPDATE public.packing_extend pe
            SET last_item_line_seq = sub.max_seq
            FROM (
                SELECT pi."PackingId" AS packing_id,
                       COALESCE(MAX(
                           CASE
                               WHEN pi.item_code ~ '-[0-9]+$' THEN
                                   NULLIF(regexp_replace(pi.item_code, '^.*-([0-9]+)$', '\1'), '')::integer
                               ELSE NULL
                           END
                       ), 0) AS max_seq
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.item_code IS NOT NULL
                  AND TRIM(pi.item_code) <> ''
                GROUP BY pi."PackingId"
            ) sub
            WHERE pe."PackingId" = sub.packing_id
              AND sub.max_seq > pe.last_item_line_seq;

            INSERT INTO public.packing_extend ("PackingId", last_item_line_seq, "CreateTime")
            SELECT sub.packing_id, sub.max_seq, NOW()
            FROM (
                SELECT pi."PackingId" AS packing_id,
                       COALESCE(MAX(
                           CASE
                               WHEN pi.item_code ~ '-[0-9]+$' THEN
                                   NULLIF(regexp_replace(pi.item_code, '^.*-([0-9]+)$', '\1'), '')::integer
                               ELSE NULL
                           END
                       ), 0) AS max_seq
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.item_code IS NOT NULL
                  AND TRIM(pi.item_code) <> ''
                GROUP BY pi."PackingId"
            ) sub
            WHERE sub.max_seq > 0
              AND NOT EXISTS (
                SELECT 1 FROM public.packing_extend pe2 WHERE pe2."PackingId" = sub.packing_id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.packing
                ADD COLUMN IF NOT EXISTS last_item_line_seq integer NOT NULL DEFAULT 0;

            UPDATE public.packing p
            SET last_item_line_seq = COALESCE(pe.last_item_line_seq, 0)
            FROM public.packing_extend pe
            WHERE pe."PackingId" = p."Id";

            DROP TABLE IF EXISTS public.packing_extend;
            """);
    }
}
