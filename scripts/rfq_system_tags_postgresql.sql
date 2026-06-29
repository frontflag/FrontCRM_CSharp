-- 需求主表（RFQ）系统预设标签
-- 文档：document/PRD/业务功能/需求报价/需求主表标签PRD.md

INSERT INTO tag_definition (
    "TagId", "Name", "Code", "Color", "Type", "Category", "Scope", "Status", "SortOrder", "UsageCount", "Visibility",
    "CreateTime", "is_deleted"
)
SELECT gen_random_uuid()::text, v.name, v.code, v.color, 1, 'RFQ跟进', 'RFQ', 1, v.sort_order, 0, 3,
       NOW() AT TIME ZONE 'UTC', false
FROM (VALUES
    ('加急', 'RFQ_URGENT', '#E53935', 40),
    ('重点跟进', 'RFQ_KEY_FOLLOW', '#FB8C00', 30),
    ('需二次寻源', 'RFQ_RE_SOURCE', '#1E88E5', 20),
    ('追单', 'RFQ_FOLLOW_ORDER', '#8E24AA', 10)
) AS v(name, code, color, sort_order)
WHERE NOT EXISTS (
    SELECT 1 FROM tag_definition t WHERE t."Code" = v.code AND t.is_deleted = false
);
