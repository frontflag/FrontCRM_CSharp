-- 工作日程：需求已分配日、订单审核通过日、任务表与权限（幂等）
ALTER TABLE public.rfq ADD COLUMN IF NOT EXISTS assigned_at timestamp with time zone NULL;
ALTER TABLE public.sellorder ADD COLUMN IF NOT EXISTS approved_at timestamp with time zone NULL;

UPDATE public.rfq r
SET assigned_at = COALESCE(
  (
    SELECT MIN(c."ChangedAt")
    FROM log_change_fldval c
    WHERE c."BizType" = 'Rfq'
      AND c."RecordId" = r.rfq_id
      AND c."FieldName" = 'status'
      AND c."NewValue" = '已分配'
  ),
  CASE WHEN r.status IN (1, 2, 3, 4, 5, 7) THEN r."CreateTime" ELSE NULL END
)
WHERE r.assigned_at IS NULL;

UPDATE public.sellorder s
SET approved_at = (
  SELECT MIN(c."ChangedAt")
  FROM log_change_fldval c
  WHERE c."BizType" = 'SalesOrder'
    AND c."RecordId" = s."SellOrderId"
    AND c."FieldName" = 'status'
    AND c."NewValue" = '审核通过'
)
WHERE s.approved_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_rfq_sales_assigned
  ON public.rfq (sales_user_id, assigned_at)
  WHERE assigned_at IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_sellorder_sales_approved
  ON public.sellorder (sales_user_id, approved_at)
  WHERE approved_at IS NOT NULL;

CREATE TABLE IF NOT EXISTS public.work_task (
  id character varying(36) NOT NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  create_by_user_id character varying(36) NOT NULL,
  modify_time timestamp with time zone NULL,
  status smallint NOT NULL DEFAULT 10,
  object_type character varying(32) NOT NULL,
  object_id character varying(36) NOT NULL,
  title character varying(200) NOT NULL,
  content character varying(2000) NULL,
  start_date date NOT NULL,
  priority smallint NOT NULL DEFAULT 2,
  assignee_user_id character varying(36) NOT NULL,
  completed_at timestamp with time zone NULL,
  completed_by_user_id character varying(36) NULL,
  contact_history_id character varying(36) NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT "PK_work_task" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_work_task_assignee_start
  ON public.work_task (assignee_user_id, start_date)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_work_task_object
  ON public.work_task (object_type, object_id)
  WHERE is_deleted = false;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime") VALUES
('31000000-0000-4000-8000-000000000040', 'work-task.read', '工作任务-查看', 'api', 'work-task', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000041', 'work-task.write', '工作任务-维护', 'api', 'work-task', 'write', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER', 'commerce_operator', 'sale_operator', 'purchase_operator', 'purchase_buyer')
  AND p."PermissionCode" IN ('work-task.read', 'work-task.write')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260913180000_WorkCalendarAndTask', '9.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM public."__EFMigrationsHistory" h
  WHERE h."MigrationId" = '20260913180000_WorkCalendarAndTask'
);
