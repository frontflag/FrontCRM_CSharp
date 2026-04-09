-- 将明显为采购组织但 IdentityType 仍为 0 的部门标为采购(2)，便于权限汇总与报表（幂等可重复执行）。
-- 若名称规则与贵司不符，请改为手工在「部门」中设置身份类型。
BEGIN;

UPDATE sys_department
SET "IdentityType" = 2
WHERE "IdentityType" = 0
  AND "Status" = 1
  AND (
    ("DepartmentName" LIKE '%采购部%' AND "DepartmentName" NOT LIKE '%销售%')
    OR "DepartmentName" LIKE '%采购中心%'
    OR TRIM("DepartmentName") = '采购'
  );

COMMIT;
