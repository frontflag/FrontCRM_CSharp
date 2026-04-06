-- 财务汇率表（与迁移 20260403120000_FinanceExchangeRateTables 一致；可手工执行）
CREATE TABLE IF NOT EXISTS financeexchangeratesetting (
    "FinanceExchangeRateSettingId" character varying(36) NOT NULL,
    "UsdToCny" numeric(12,4) NOT NULL,
    "UsdToHkd" numeric(12,4) NOT NULL,
    "UsdToEur" numeric(12,4) NOT NULL,
    "EditorUserId" character varying(36),
    "EditorUserName" character varying(100),
    "CreateTime" timestamp with time zone NOT NULL,
    "ModifyTime" timestamp with time zone,
    CONSTRAINT "PK_financeexchangeratesetting" PRIMARY KEY ("FinanceExchangeRateSettingId")
);

CREATE TABLE IF NOT EXISTS financeexchangeratechangelog (
    "FinanceExchangeRateChangeLogId" character varying(36) NOT NULL,
    "UsdToCny" numeric(12,4) NOT NULL,
    "UsdToHkd" numeric(12,4) NOT NULL,
    "UsdToEur" numeric(12,4) NOT NULL,
    "ChangeUserId" character varying(36),
    "ChangeUserName" character varying(100),
    "ChangeSummary" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_financeexchangeratechangelog" PRIMARY KEY ("FinanceExchangeRateChangeLogId")
);

CREATE INDEX IF NOT EXISTS "IX_financeexchangeratechangelog_CreateTime" ON financeexchangeratechangelog ("CreateTime");

INSERT INTO financeexchangeratesetting ("FinanceExchangeRateSettingId", "UsdToCny", "UsdToHkd", "UsdToEur", "EditorUserId", "EditorUserName", "CreateTime", "ModifyTime")
SELECT '00000000-0000-4000-8000-0000000000E1', 6.9194, 7.8367, 0.8725, NULL, NULL, NOW() AT TIME ZONE 'UTC', NULL
WHERE NOT EXISTS (
  SELECT 1 FROM financeexchangeratesetting WHERE "FinanceExchangeRateSettingId" = '00000000-0000-4000-8000-0000000000E1'
);
