-- 供应商联系人表增加性别字段（与客户联系人一致：0=保密 1=男 2=女）
ALTER TABLE vendorcontactinfo
    ADD COLUMN IF NOT EXISTS "Gender" smallint;

COMMENT ON COLUMN vendorcontactinfo."Gender" IS '性别：0=保密 1=男 2=女';
