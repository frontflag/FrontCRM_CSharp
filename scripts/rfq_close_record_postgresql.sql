-- 需求关闭记录表（关闭需求 / 关闭记录 Tab）
CREATE TABLE IF NOT EXISTS rfq_close_record (
    rfq_close_record_id character varying(36) NOT NULL,
    rfq_id character varying(36) NOT NULL,
    close_type smallint NOT NULL,
    close_reason character varying(500) NOT NULL,
    remark character varying(500),
    closed_by_user_id character varying(36),
    "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreateUserId" bigint,
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" bigint,
    CONSTRAINT "PK_rfq_close_record" PRIMARY KEY (rfq_close_record_id)
);

CREATE INDEX IF NOT EXISTS ix_rfq_close_record_rfq_id ON rfq_close_record (rfq_id);
