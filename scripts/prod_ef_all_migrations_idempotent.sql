-- =============================================================================
-- FrontCRM：PostgreSQL 全量迁移 SQL（幂等，与当前 EF 迁移链一致）
-- 生成命令（在仓库根目录）：
--   $env:ConnectionStrings__DefaultConnection = "Host=...;Database=...;Username=...;Password=..."
--   dotnet ef migrations script --project CRM.Infrastructure --startup-project CRM.DbMigrator --idempotent --output scripts/prod_ef_all_migrations_idempotent.sql
--
-- 使用说明：
-- 1) 生产库已与开发库同源、仅缺部分对象时：在维护窗口备份后执行本脚本；已记录在 __EFMigrationsHistory 的迁移会跳过。
-- 2) 若生产库从未用过 EF 迁移、或历史与脚本中的 MigrationId 不一致，请先核对 __EFMigrationsHistory，必要时用手工修复或改用「从某迁移到最新」的增量脚本。
-- 3) 更推荐在可直连生产库时运行发布包中的迁移工具或 dotnet ef database update（需评估你们的安全策略）。
--
-- 【若出现 SQLSTATE 25P02 / “current transaction is aborted”】
-- 先在本会话执行：ROLLBACK;
-- 再往日志最前面找**第一条**真正的错误（多为 42P07 表已存在、42501 权限等）；25P02 只是后续语句的连锁反应。
-- 常见根因：__EFMigrationsHistory 里没有某条 MigrationId，但库里表/结构已经存在 → 脚本仍会执行 CREATE TABLE → 报错。
-- 处理思路：核对 SELECT * FROM "__EFMigrationsHistory"; 与真实库结构；对已存在的基线可补写历史记录或改用「从上一迁移到最新」的增量脚本，勿盲目整文件重跑。
-- =============================================================================

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE customercontacthistory (
        "HistoryId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Content" character varying(500),
        "Time" timestamp with time zone NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_customercontacthistory" PRIMARY KEY ("HistoryId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE customerinfo (
        "CustomerId" character varying(36) NOT NULL,
        "CustomerCode" character varying(16) NOT NULL,
        "OfficialName" character varying(128),
        "StandardOfficialName" character varying(128),
        "NickName" character varying(64),
        "Level" smallint NOT NULL,
        "Type" smallint,
        "Scale" smallint,
        "Background" smallint,
        "DealMode" smallint,
        "CompanyNature" smallint,
        "Country" smallint,
        "Industry" character varying(50),
        "Product" character varying(200),
        "Product2" character varying(200),
        "Application" character varying(200),
        "TradeCurrency" smallint,
        "TradeType" smallint,
        "Payment" smallint,
        "ExternalNumber" character varying(50),
        "CreditLine" numeric(18,2) NOT NULL,
        "CreditLineRemain" numeric(18,2) NOT NULL,
        "AscriptionType" smallint NOT NULL,
        "ProtectStatus" boolean NOT NULL,
        "ProtectFromUserId" character varying(36),
        "ProtectTime" timestamp with time zone,
        "Status" smallint NOT NULL DEFAULT 1,
        "BlackList" boolean NOT NULL,
        "DisenableStatus" boolean NOT NULL,
        "DisenableType" smallint,
        "CommonSeaAuditStatus" smallint,
        "Longitude" numeric(10,6),
        "Latitude" numeric(10,6),
        "CompanyInfo" text,
        "Remark" character varying(500),
        "DUNS" character varying(20),
        "IsControl" boolean NOT NULL,
        "CreditCode" character varying(18),
        "IdentityType" smallint,
        "SalesUserId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_customerinfo" PRIMARY KEY ("CustomerId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE "user" (
        "UserId" character varying(36) NOT NULL,
        "UserName" character varying(50) NOT NULL,
        "Email" character varying(100),
        "Password" character varying(256) NOT NULL,
        "Salt" character varying(50) NOT NULL,
        "PasswordPlain" character varying(100),
        "Status" smallint NOT NULL DEFAULT 1,
        "PasswordChangeTime" timestamp with time zone,
        "RealName" character varying(50),
        "Mobile" character varying(20),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_user" PRIMARY KEY ("UserId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE customeraddress (
        "AddressId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "AddressType" smallint NOT NULL,
        "Country" smallint,
        "Province" character varying(50),
        "City" character varying(50),
        "Area" character varying(50),
        "Address" character varying(256),
        "ContactName" character varying(50),
        "ContactPhone" character varying(20),
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_customeraddress" PRIMARY KEY ("AddressId"),
        CONSTRAINT "FK_customeraddress_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo ("CustomerId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE customerbankinfo (
        "BankId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "BankName" character varying(100),
        "BankAccount" character varying(50),
        "AccountName" character varying(50),
        "BankBranch" character varying(100),
        "Currency" smallint,
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_customerbankinfo" PRIMARY KEY ("BankId"),
        CONSTRAINT "FK_customerbankinfo_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo ("CustomerId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE TABLE customercontactinfo (
        "ContactId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "CName" character varying(50),
        "EName" character varying(100),
        "Gender" smallint,
        "Title" character varying(50),
        "Department" character varying(50),
        "Mobile" character varying(20),
        "Tel" character varying(30),
        "Email" character varying(100),
        "QQ" character varying(20),
        "WeChat" character varying(50),
        "Address" character varying(200),
        "IsMain" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_customercontactinfo" PRIMARY KEY ("ContactId"),
        CONSTRAINT "FK_customercontactinfo_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo ("CustomerId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE INDEX "IX_customeraddress_CustomerId" ON customeraddress ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE INDEX "IX_customerbankinfo_CustomerId" ON customerbankinfo ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    CREATE INDEX "IX_customercontactinfo_CustomerId" ON customercontactinfo ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316063302_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316063302_InitialCreate', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    ALTER TABLE customerinfo ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    ALTER TABLE customerinfo ADD "DeletedByUserId" character varying(36);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    ALTER TABLE customerinfo ADD "IsDeleted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    CREATE TABLE sys_error_log (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "OccurredAt" timestamp with time zone NOT NULL,
        "ModuleName" character varying(100) NOT NULL,
        "OperationType" character varying(50),
        "ErrorMessage" character varying(500) NOT NULL,
        "ErrorDetail" text,
        "DocumentNo" character varying(50),
        "DataId" character varying(36),
        "UserId" character varying(36),
        "UserName" character varying(50),
        "RequestPath" character varying(200),
        "RequestBody" text,
        "IsResolved" boolean NOT NULL,
        "ResolveRemark" character varying(200),
        CONSTRAINT "PK_sys_error_log" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    CREATE TABLE sys_serial_number (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "ModuleCode" character varying(50) NOT NULL,
        "ModuleName" character varying(100) NOT NULL,
        "Prefix" character varying(10) NOT NULL,
        "SequenceLength" integer NOT NULL,
        "CurrentSequence" integer NOT NULL,
        "ResetByYear" boolean NOT NULL,
        "ResetByMonth" boolean NOT NULL,
        "LastResetYear" integer,
        "LastResetMonth" integer,
        "Remark" character varying(200),
        "CreateTime" timestamp with time zone NOT NULL,
        "UpdateTime" timestamp with time zone,
        CONSTRAINT "PK_sys_serial_number" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (1, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Customer', '客户', 'Cus', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (2, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Vendor', '供应商', 'Ven', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (3, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Inquiry', '询价/需求', 'INQ', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (4, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Quotation', '报价', 'QUO', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (5, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'SalesOrder', '销售订单', 'SO', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (6, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'PurchaseOrder', '采购订单', 'PO', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (7, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'StockIn', '入库', 'SIN', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (8, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'StockOut', '出库', 'SOUT', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (9, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Inventory', '库存调整', 'INV', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (10, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Receipt', '收款', 'REC', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (11, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Payment', '付款', 'PAY', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (12, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'InputInvoice', '进项发票', 'VINV', NULL, FALSE, FALSE, 4, NULL);
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (13, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'OutputInvoice', '销项发票', 'SINV', NULL, FALSE, FALSE, 4, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    CREATE UNIQUE INDEX "IX_sys_serial_number_ModuleCode" ON sys_serial_number ("ModuleCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    PERFORM setval(
        pg_get_serial_sequence('sys_serial_number', 'Id'),
        GREATEST(
            (SELECT MAX("Id") FROM sys_serial_number) + 1,
            nextval(pg_get_serial_sequence('sys_serial_number', 'Id'))),
        false);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316072003_AddSerialNumberAndErrorLogAndSoftDelete') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316072003_AddSerialNumberAndErrorLogAndSoftDelete', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316095624_AddComponentCache') THEN
    CREATE TABLE component_cache (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "Mpn" character varying(100) NOT NULL,
        "ManufacturerName" character varying(200),
        "ShortDescription" character varying(500),
        "Description" text,
        "LifecycleStatus" character varying(50),
        "PackageType" character varying(100),
        "IsRoHSCompliant" boolean,
        "SpecsJson" text,
        "SellersJson" text,
        "AlternativesJson" text,
        "ApplicationsJson" text,
        "PriceTrendJson" text,
        "NewsJson" text,
        "DataSource" character varying(50) NOT NULL,
        "FetchedAt" timestamp with time zone NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "UpdateTime" timestamp with time zone,
        "QueryCount" integer NOT NULL,
        CONSTRAINT "PK_component_cache" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316095624_AddComponentCache') THEN
    CREATE UNIQUE INDEX "IX_component_cache_Mpn" ON component_cache ("Mpn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316095624_AddComponentCache') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316095624_AddComponentCache', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    CREATE TABLE sys_serial_number (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "ModuleCode" character varying(50) NOT NULL,
        "ModuleName" character varying(100) NOT NULL,
        "Prefix" character varying(10) NOT NULL,
        "SequenceLength" integer NOT NULL DEFAULT 4,
        "CurrentSequence" integer NOT NULL DEFAULT 0,
        "ResetByYear" boolean NOT NULL DEFAULT FALSE,
        "ResetByMonth" boolean NOT NULL DEFAULT FALSE,
        "LastResetYear" integer,
        "LastResetMonth" integer,
        "Remark" character varying(200),
        "CreateTime" timestamp with time zone NOT NULL,
        "UpdateTime" timestamp with time zone,
        CONSTRAINT "PK_sys_serial_number" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    CREATE UNIQUE INDEX "IX_sys_serial_number_ModuleCode" ON sys_serial_number ("ModuleCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (1, 'Customer', '客户', 'Cus', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (2, 'Vendor', '供应商', 'Ven', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (3, 'Inquiry', '询价/需求', 'INQ', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (4, 'Quotation', '报价', 'QUO', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (5, 'SalesOrder', '销售订单', 'SO', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (6, 'PurchaseOrder', '采购订单', 'PO', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (7, 'StockIn', '入库', 'SIN', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (8, 'StockOut', '出库', 'SOUT', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (9, 'Inventory', '库存调整', 'INV', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (10, 'Receipt', '收款', 'REC', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (11, 'Payment', '付款', 'PAY', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (12, 'InputInvoice', '进项发票', 'VINV', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (13, 'OutputInvoice', '销项发票', 'SINV', 4, 0, FALSE, FALSE, TIMESTAMPTZ '2026-01-01T00:00:00Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    CREATE TABLE sys_error_log (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "OccurredAt" timestamp with time zone NOT NULL,
        "ModuleName" character varying(100) NOT NULL,
        "OperationType" character varying(50),
        "ErrorMessage" character varying(500) NOT NULL,
        "ErrorDetail" text,
        "DocumentNo" character varying(50),
        "DataId" character varying(36),
        "UserId" character varying(36),
        "UserName" character varying(50),
        "RequestPath" character varying(200),
        "RequestBody" text,
        "IsResolved" boolean NOT NULL DEFAULT FALSE,
        "ResolveRemark" character varying(200),
        CONSTRAINT "PK_sys_error_log" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    CREATE INDEX "IX_sys_error_log_OccurredAt" ON sys_error_log ("OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    CREATE INDEX "IX_sys_error_log_IsResolved" ON sys_error_log ("IsResolved");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    PERFORM setval(
        pg_get_serial_sequence('sys_serial_number', 'Id'),
        GREATEST(
            (SELECT MAX("Id") FROM sys_serial_number) + 1,
            nextval(pg_get_serial_sequence('sys_serial_number', 'Id'))),
        false);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316100000_AddSysTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316100000_AddSysTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE vendor (
        "Id" character varying(36) NOT NULL,
        "Code" character varying(16) NOT NULL,
        "OfficialName" character varying(64),
        "NickName" character varying(64),
        "Level" smallint NOT NULL,
        "Type" smallint NOT NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreditLine" numeric(18,2) NOT NULL,
        "Payment" smallint NOT NULL,
        "TradeCurrency" character varying(10),
        "TaxRate" numeric(5,2) NOT NULL,
        "UniformNumber" character varying(20),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_vendor" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE vendor_address (
        "Id" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "AddressType" smallint NOT NULL,
        "Country" character varying(50),
        "Province" character varying(50),
        "City" character varying(50),
        "Area" character varying(50),
        "Address" character varying(200),
        "ContactName" character varying(50),
        "ContactPhone" character varying(20),
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_vendor_address" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE vendorcontactinfo (
        "Id" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "CName" character varying(50),
        "EName" character varying(100),
        "Title" character varying(50),
        "Department" character varying(50),
        "Mobile" character varying(20),
        "Tel" character varying(30),
        "Email" character varying(100),
        "QQ" character varying(20),
        "WeChat" character varying(50),
        "IsMain" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_vendorcontactinfo" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE vendorbankinfo (
        "Id" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "BankName" character varying(100),
        "BankAccount" character varying(50),
        "AccountName" character varying(50),
        "BankBranch" character varying(100),
        "Currency" character varying(10),
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_vendorbankinfo" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stock (
        "StockId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "LocationId" character varying(36),
        "Quantity" numeric(18,4) NOT NULL,
        "AvailableQuantity" numeric(18,4) NOT NULL,
        "LockedQuantity" numeric(18,4) NOT NULL,
        "Unit" character varying(20),
        "BatchNo" character varying(50),
        "ProductionDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "Status" smallint NOT NULL DEFAULT 1,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stock" PRIMARY KEY ("StockId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stockin (
        "StockInId" character varying(36) NOT NULL,
        "StockInCode" character varying(32) NOT NULL,
        "StockInType" smallint NOT NULL,
        "SourceCode" character varying(32),
        "WarehouseId" character varying(36) NOT NULL,
        "VendorId" character varying(36),
        "StockInDate" timestamp with time zone NOT NULL,
        "TotalQuantity" numeric(18,4) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Status" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stockin" PRIMARY KEY ("StockInId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stockinitem (
        "ItemId" character varying(36) NOT NULL,
        "StockInId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "LocationId" character varying(36),
        "BatchNo" character varying(50),
        "ProductionDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stockinitem" PRIMARY KEY ("ItemId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stockout (
        "StockOutId" character varying(36) NOT NULL,
        "StockOutCode" character varying(32) NOT NULL,
        "StockOutType" smallint NOT NULL,
        "SourceCode" character varying(32),
        "WarehouseId" character varying(36) NOT NULL,
        "CustomerId" character varying(36),
        "StockOutDate" timestamp with time zone NOT NULL,
        "TotalQuantity" numeric(18,4) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Status" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stockout" PRIMARY KEY ("StockOutId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stockoutitem (
        "ItemId" character varying(36) NOT NULL,
        "StockOutId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "LocationId" character varying(36),
        "BatchNo" character varying(50),
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stockoutitem" PRIMARY KEY ("ItemId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE TABLE stockoutrequest (
        "Id" character varying(36) NOT NULL,
        "RequestCode" character varying(50) NOT NULL,
        "SalesOrderId" character varying(36),
        "CustomerId" character varying(36),
        "RequestUserId" character varying(36),
        "RequestDate" timestamp with time zone NOT NULL,
        "TotalQuantity" numeric(18,4) NOT NULL,
        "Status" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" character varying(36),
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" character varying(36),
        CONSTRAINT "PK_stockoutrequest" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_vendor_Code" ON vendor ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_vendor_address_VendorId" ON vendor_address ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_vendorcontactinfo_VendorId" ON vendorcontactinfo ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_vendorbankinfo_VendorId" ON vendorbankinfo ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stock_MaterialId" ON stock ("MaterialId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stock_WarehouseId" ON stock ("WarehouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stockin_StockInCode" ON stockin ("StockInCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stockinitem_StockInId" ON stockinitem ("StockInId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stockout_StockOutCode" ON stockout ("StockOutCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    CREATE INDEX "IX_stockoutitem_StockOutId" ON stockoutitem ("StockOutId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317000000_AddVendorAndInventoryTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317000000_AddVendorAndInventoryTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    ALTER TABLE customercontacthistory ADD "ContactPerson" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    ALTER TABLE customercontacthistory ADD "NextFollowUpTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    ALTER TABLE customercontacthistory ADD "OperatorId" character varying(36);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    ALTER TABLE customercontacthistory ADD "Result" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    ALTER TABLE customercontacthistory ADD "Subject" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317024250_AddContactHistoryFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317024250_AddContactHistoryFields', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "BlackListAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "BlackListByUserId" character varying(36);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "BlackListByUserName" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "BlackListReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "DeleteReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    ALTER TABLE customerinfo ADD "DeletedByUserName" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    CREATE TABLE rfq (
        rfq_id character varying(36) NOT NULL,
        rfq_code character varying(32) NOT NULL,
        customer_id character varying(36),
        contact_id character varying(36),
        contact_email character varying(200),
        sales_user_id character varying(36),
        rfq_type smallint NOT NULL,
        quote_method smallint NOT NULL,
        assign_method smallint NOT NULL,
        industry character varying(100),
        product character varying(200),
        target_type smallint NOT NULL,
        importance smallint NOT NULL,
        is_last_inquiry boolean NOT NULL,
        project_background character varying(500),
        competitor character varying(200),
        status smallint NOT NULL DEFAULT 0,
        item_count integer NOT NULL,
        remark character varying(500),
        rfq_date timestamp with time zone NOT NULL DEFAULT (NOW()),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_rfq" PRIMARY KEY (rfq_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    CREATE TABLE rfqitem (
        item_id character varying(36) NOT NULL,
        rfq_id character varying(36) NOT NULL,
        line_no integer NOT NULL,
        customer_mpn character varying(100),
        mpn character varying(200) NOT NULL,
        customer_brand character varying(100) NOT NULL,
        brand character varying(100) NOT NULL,
        target_price numeric(18,4),
        price_currency smallint NOT NULL,
        quantity numeric(18,4) NOT NULL DEFAULT 1.0,
        production_date character varying(50),
        expiry_date timestamp with time zone,
        min_package_qty numeric(18,4),
        moq numeric(18,4),
        alternatives character varying(500),
        remark character varying(500),
        status smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_rfqitem" PRIMARY KEY (item_id),
        CONSTRAINT "FK_rfqitem_rfq_rfq_id" FOREIGN KEY (rfq_id) REFERENCES rfq (rfq_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    CREATE UNIQUE INDEX "IX_rfq_rfq_code" ON rfq (rfq_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    CREATE INDEX "IX_rfqitem_rfq_id" ON rfqitem (rfq_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317145231_AddRFQTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317145231_AddRFQTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317162715_AddQuoteTables') THEN
    CREATE TABLE quote (
        "QuoteId" character varying(36) NOT NULL,
        quote_code character varying(32) NOT NULL,
        rfq_id character varying(36),
        rfq_item_id character varying(36),
        mpn character varying(200),
        customer_id character varying(36),
        sales_user_id character varying(36),
        purchase_user_id character varying(36),
        quote_date timestamp with time zone NOT NULL DEFAULT (NOW()),
        status smallint NOT NULL DEFAULT 0,
        remark character varying(1000),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_quote" PRIMARY KEY ("QuoteId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317162715_AddQuoteTables') THEN
    CREATE TABLE quoteitem (
        "QuoteItemId" character varying(36) NOT NULL,
        quote_id character varying(36) NOT NULL,
        vendor_id character varying(36),
        vendor_name character varying(200),
        vendor_code character varying(50),
        contact_id character varying(36),
        contact_name character varying(100),
        price_type character varying(50),
        expiry_date timestamp with time zone,
        mpn character varying(200),
        brand character varying(200),
        brand_origin character varying(100),
        date_code character varying(100),
        lead_time character varying(200),
        label_type smallint NOT NULL,
        wafer_origin smallint NOT NULL,
        package_origin smallint NOT NULL,
        free_shipping boolean NOT NULL,
        currency smallint NOT NULL,
        quantity numeric(18,4) NOT NULL DEFAULT 0.0,
        unit_price numeric(18,6) NOT NULL DEFAULT 0.0,
        converted_price numeric(18,6),
        min_package_qty integer NOT NULL,
        min_package_unit character varying(50),
        stock_qty integer NOT NULL,
        moq integer NOT NULL,
        remark character varying(500),
        status smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_quoteitem" PRIMARY KEY ("QuoteItemId"),
        CONSTRAINT "FK_quoteitem_quote_quote_id" FOREIGN KEY (quote_id) REFERENCES quote ("QuoteId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317162715_AddQuoteTables') THEN
    CREATE UNIQUE INDEX "IX_quote_quote_code" ON quote (quote_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317162715_AddQuoteTables') THEN
    CREATE INDEX "IX_quoteitem_quote_id" ON quoteitem (quote_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317162715_AddQuoteTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317162715_AddQuoteTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE TABLE purchaseorder (
        "PurchaseOrderId" character varying(36) NOT NULL,
        purchase_order_code character varying(32) NOT NULL,
        vendor_id character varying(36) NOT NULL,
        vendor_name character varying(200),
        vendor_code character varying(50),
        vendor_contact_id character varying(36),
        purchase_user_id character varying(36),
        purchase_user_name character varying(100),
        purchase_group_id character varying(36),
        sales_group_id character varying(36),
        status smallint NOT NULL DEFAULT 0,
        err_status smallint NOT NULL,
        type smallint NOT NULL,
        currency smallint NOT NULL,
        total numeric(18,2) NOT NULL,
        convert_total numeric(18,2) NOT NULL,
        item_rows integer NOT NULL,
        stock_status smallint NOT NULL,
        finance_status smallint NOT NULL,
        stock_out_status smallint NOT NULL,
        invoice_status smallint NOT NULL,
        delivery_address character varying(500),
        delivery_date timestamp with time zone,
        comment character varying(500),
        inner_comment character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_purchaseorder" PRIMARY KEY ("PurchaseOrderId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE TABLE sellorder (
        "SellOrderId" character varying(36) NOT NULL,
        sell_order_code character varying(32) NOT NULL,
        customer_id character varying(36) NOT NULL,
        customer_name character varying(200),
        sales_user_id character varying(36),
        sales_user_name character varying(100),
        purchase_group_id character varying(36),
        status smallint NOT NULL DEFAULT 0,
        err_status smallint NOT NULL,
        type smallint NOT NULL,
        currency smallint NOT NULL,
        total numeric(18,2) NOT NULL,
        convert_total numeric(18,2) NOT NULL,
        item_rows integer NOT NULL,
        purchase_order_status smallint NOT NULL,
        stock_out_status smallint NOT NULL,
        stock_in_status smallint NOT NULL,
        finance_receipt_status smallint NOT NULL,
        finance_payment_status smallint NOT NULL,
        invoice_status smallint NOT NULL,
        delivery_address character varying(500),
        delivery_date timestamp with time zone,
        comment character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sellorder" PRIMARY KEY ("SellOrderId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE TABLE sellorderitem (
        "SellOrderItemId" character varying(36) NOT NULL,
        sell_order_id character varying(36) NOT NULL,
        quote_id character varying(36),
        product_id character varying(36),
        pn character varying(200),
        brand character varying(200),
        customer_pn_no character varying(200),
        qty numeric(18,4) NOT NULL DEFAULT 0.0,
        purchased_qty numeric(18,4) NOT NULL DEFAULT 0.0,
        price numeric(18,6) NOT NULL DEFAULT 0.0,
        currency smallint NOT NULL,
        date_code character varying(100),
        delivery_date timestamp with time zone,
        status smallint NOT NULL,
        comment character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sellorderitem" PRIMARY KEY ("SellOrderItemId"),
        CONSTRAINT "FK_sellorderitem_sellorder_sell_order_id" FOREIGN KEY (sell_order_id) REFERENCES sellorder ("SellOrderId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE TABLE purchaseorderitem (
        "PurchaseOrderItemId" character varying(36) NOT NULL,
        purchase_order_id character varying(36) NOT NULL,
        sell_order_item_id character varying(36) NOT NULL,
        vendor_id character varying(36) NOT NULL,
        product_id character varying(36),
        pn character varying(200),
        brand character varying(200),
        qty numeric(18,4) NOT NULL DEFAULT 0.0,
        cost numeric(18,6) NOT NULL DEFAULT 0.0,
        currency smallint NOT NULL,
        status smallint NOT NULL,
        stock_in_status smallint NOT NULL,
        finance_payment_status smallint NOT NULL,
        stock_out_status smallint NOT NULL,
        err_status smallint NOT NULL,
        delivery_date timestamp with time zone,
        comment character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_purchaseorderitem" PRIMARY KEY ("PurchaseOrderItemId"),
        CONSTRAINT "FK_purchaseorderitem_purchaseorder_purchase_order_id" FOREIGN KEY (purchase_order_id) REFERENCES purchaseorder ("PurchaseOrderId") ON DELETE CASCADE,
        CONSTRAINT "FK_purchaseorderitem_sellorderitem_sell_order_item_id" FOREIGN KEY (sell_order_item_id) REFERENCES sellorderitem ("SellOrderItemId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE UNIQUE INDEX "IX_purchaseorder_purchase_order_code" ON purchaseorder (purchase_order_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE INDEX "IX_purchaseorderitem_purchase_order_id" ON purchaseorderitem (purchase_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE INDEX "IX_purchaseorderitem_sell_order_item_id" ON purchaseorderitem (sell_order_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE UNIQUE INDEX "IX_sellorder_sell_order_code" ON sellorder (sell_order_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    CREATE INDEX "IX_sellorderitem_sell_order_id" ON sellorderitem (sell_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317170015_AddSalesAndPurchaseOrderTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317170015_AddSalesAndPurchaseOrderTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE document_daily_sequence (
        "TheDate" timestamp with time zone NOT NULL,
        "CurrentSequence" integer NOT NULL,
        CONSTRAINT "PK_document_daily_sequence" PRIMARY KEY ("TheDate")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financepayment (
        "FinancePaymentId" character varying(36) NOT NULL,
        "FinancePaymentCode" character varying(16) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "VendorName" character varying(200),
        "Status" smallint NOT NULL DEFAULT 0,
        "PaymentAmountToBe" numeric(18,2) NOT NULL,
        "PaymentAmount" numeric(18,2) NOT NULL,
        "PaymentTotalAmount" numeric(18,2) NOT NULL,
        "PaymentCurrency" smallint NOT NULL,
        "PaymentDate" timestamp with time zone,
        "PaymentUserId" character varying(36),
        "PaymentMode" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepayment" PRIMARY KEY ("FinancePaymentId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financepurchaseinvoice (
        "FinancePurchaseInvoiceId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "VendorName" character varying(200),
        "InvoiceNo" character varying(32),
        "InvoiceAmount" numeric(18,2) NOT NULL,
        "BillAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "ExcludTaxAmount" numeric(18,2) NOT NULL,
        "InvoiceDate" timestamp with time zone,
        "ConfirmDate" timestamp with time zone,
        "ConfirmStatus" smallint NOT NULL,
        "RedInvoiceStatus" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepurchaseinvoice" PRIMARY KEY ("FinancePurchaseInvoiceId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financereceipt (
        "FinanceReceiptId" character varying(36) NOT NULL,
        "FinanceReceiptCode" character varying(16) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "CustomerName" character varying(200),
        "SalesUserId" character varying(36),
        "PurchaseGroupId" character varying(36),
        "Status" smallint NOT NULL DEFAULT 0,
        "ReceiptAmount" numeric(18,2) NOT NULL,
        "ReceiptCurrency" smallint NOT NULL,
        "ReceiptDate" timestamp with time zone,
        "ReceiptUserId" character varying(36),
        "ReceiptMode" smallint NOT NULL,
        "ReceiptBankId" character varying(36),
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financereceipt" PRIMARY KEY ("FinanceReceiptId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financesellinvoice (
        "FinanceSellInvoiceId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "CustomerName" character varying(200),
        "InvoiceCode" character varying(32),
        "InvoiceNo" character varying(64),
        "InvoiceTotal" numeric(18,2) NOT NULL,
        "MakeInvoiceDate" timestamp with time zone,
        "ReceiveStatus" smallint NOT NULL,
        "ReceiveDone" numeric(18,2) NOT NULL,
        "ReceiveToBe" numeric(18,2) NOT NULL,
        "Currency" smallint NOT NULL,
        "Type" smallint NOT NULL,
        "InvoiceStatus" smallint NOT NULL,
        "SellInvoiceType" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financesellinvoice" PRIMARY KEY ("FinanceSellInvoiceId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stock (
        "StockId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "LocationId" character varying(36),
        "Unit" character varying(20),
        "BatchNo" character varying(50),
        "ProductionDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "Qty" numeric(18,4) NOT NULL,
        "QtyStockOut" numeric(18,4) NOT NULL,
        "QtyOccupy" numeric(18,4) NOT NULL,
        "QtySales" numeric(18,4) NOT NULL,
        "QtyRepertory" numeric(18,4) NOT NULL,
        "QtyRepertoryAvailable" numeric(18,4) NOT NULL,
        "Status" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stock" PRIMARY KEY ("StockId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stockin (
        "StockInId" character varying(36) NOT NULL,
        "StockInCode" character varying(32) NOT NULL,
        "StockInType" smallint NOT NULL,
        "SourceCode" character varying(32),
        "WarehouseId" character varying(36) NOT NULL,
        "VendorId" character varying(36),
        "StockInDate" timestamp with time zone NOT NULL,
        "SourceId" character varying(36),
        "TotalQuantity" numeric(18,4) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Status" smallint NOT NULL,
        "InspectStatus" smallint NOT NULL,
        "CreatedBy" character varying(36),
        "ApprovedBy" character varying(36),
        "ApprovedTime" timestamp with time zone,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockin" PRIMARY KEY ("StockInId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stockout (
        "StockOutId" character varying(36) NOT NULL,
        "StockOutCode" character varying(32) NOT NULL,
        "StockOutType" smallint NOT NULL,
        "SourceCode" character varying(32),
        "SourceId" character varying(36),
        "WarehouseId" character varying(36) NOT NULL,
        "CustomerId" character varying(36),
        "StockOutDate" timestamp with time zone NOT NULL,
        "TotalQuantity" numeric(18,4) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Status" smallint NOT NULL,
        "PickerId" character varying(36),
        "PickedTime" timestamp with time zone,
        "ConfirmedBy" character varying(36),
        "ConfirmedTime" timestamp with time zone,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockout" PRIMARY KEY ("StockOutId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stockoutrequest (
        "UserId" character varying(36) NOT NULL,
        "RequestCode" character varying(50) NOT NULL,
        "SalesOrderId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "RequestUserId" character varying(36) NOT NULL,
        "RequestDate" timestamp with time zone NOT NULL,
        "Status" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockoutrequest" PRIMARY KEY ("UserId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE upload_document (
        "DocumentId" character varying(36) NOT NULL,
        "BizType" character varying(50) NOT NULL,
        "BizId" character varying(36) NOT NULL,
        "OriginalFileName" character varying(255) NOT NULL,
        "StoredFileName" character varying(255) NOT NULL,
        "RelativePath" character varying(500) NOT NULL,
        "FileSize" bigint NOT NULL,
        "FileExtension" character varying(20),
        "MimeType" character varying(100),
        "ThumbnailRelativePath" character varying(500),
        "Remark" character varying(256),
        "IsDeleted" boolean NOT NULL,
        "DeleteTime" timestamp with time zone,
        "DeleteUserId" character varying(36),
        "UploadUserId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_upload_document" PRIMARY KEY ("DocumentId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE vendorcontacthistory (
        "HistoryId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Subject" character varying(200),
        "Content" character varying(500),
        "ContactPerson" character varying(100),
        "Time" timestamp with time zone NOT NULL,
        "NextFollowUpTime" timestamp with time zone,
        "Result" character varying(500),
        "OperatorId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_vendorcontacthistory" PRIMARY KEY ("HistoryId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE vendorinfo (
        "VendorId" character varying(36) NOT NULL,
        "Code" character varying(16) NOT NULL,
        "OfficialName" character varying(64),
        "NickName" character varying(64),
        "VendorIdCrm" character varying(50),
        "Level" smallint,
        "Scale" smallint,
        "Background" smallint,
        "CompanyClass" smallint,
        "Country" smallint,
        "LocationType" smallint,
        "Industry" character varying(50),
        "Product" character varying(200),
        "OfficeAddress" character varying(200),
        "TradeCurrency" smallint,
        "TradeType" smallint,
        "Payment" smallint,
        "ExternalNumber" character varying(50),
        "Credit" smallint,
        "QualityPrejudgement" smallint,
        "Traceability" smallint,
        "AfterSalesService" smallint,
        "DegreeAdaptability" smallint,
        "ISCPFlag" boolean NOT NULL,
        "Strategy" smallint,
        "SelfSupport" boolean NOT NULL,
        "BlackList" boolean NOT NULL,
        "IsDisenable" boolean NOT NULL,
        "Longitude" numeric(10,6),
        "Latitude" numeric(10,6),
        "CompanyInfo" text,
        "ListingCode" character varying(50),
        "VendorScope" character varying(200),
        "IsControl" boolean NOT NULL,
        "CreditCode" character varying(50),
        "AscriptionType" smallint NOT NULL,
        "PurchaseUserId" character varying(36),
        "PurchaseGroupId" character varying(36),
        "Status" smallint NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeleteTime" timestamp with time zone,
        "DeleteReason" character varying(200),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_vendorinfo" PRIMARY KEY ("VendorId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financepaymentitem (
        "FinancePaymentItemId" character varying(36) NOT NULL,
        "FinancePaymentId" character varying(36) NOT NULL,
        "PurchaseOrderId" character varying(36),
        "PurchaseOrderItemId" character varying(36),
        "PaymentAmount" numeric(18,2) NOT NULL,
        "PaymentAmountToBe" numeric(18,2) NOT NULL,
        "ProductId" character varying(36),
        "PN" character varying(64),
        "Brand" character varying(64),
        "VerificationStatus" smallint NOT NULL,
        "VerificationDone" numeric(18,2) NOT NULL,
        "VerificationToBe" numeric(18,2) NOT NULL,
        "PaymentId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepaymentitem" PRIMARY KEY ("FinancePaymentItemId"),
        CONSTRAINT "FK_financepaymentitem_financepayment_FinancePaymentId" FOREIGN KEY ("FinancePaymentId") REFERENCES financepayment ("FinancePaymentId") ON DELETE CASCADE,
        CONSTRAINT "FK_financepaymentitem_financepayment_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES financepayment ("FinancePaymentId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financepurchaseinvoiceitem (
        "FinancePurchaseInvoiceItemId" character varying(36) NOT NULL,
        "FinancePurchaseInvoiceId" character varying(36) NOT NULL,
        "StockInId" character varying(36),
        "StockInCode" character varying(32),
        "PurchaseOrderCode" character varying(32),
        "StockInCost" numeric(18,4) NOT NULL,
        "BillCost" numeric(18,4) NOT NULL,
        "BillQty" bigint NOT NULL,
        "BillAmount" numeric(18,2) NOT NULL,
        "TaxRate" numeric(18,4) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "ExcludTaxAmount" numeric(18,2) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepurchaseinvoiceitem" PRIMARY KEY ("FinancePurchaseInvoiceItemId"),
        CONSTRAINT "FK_financepurchaseinvoiceitem_financepurchaseinvoice_FinancePu~" FOREIGN KEY ("FinancePurchaseInvoiceId") REFERENCES financepurchaseinvoice ("FinancePurchaseInvoiceId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE financereceiptitem (
        "FinanceReceiptItemId" character varying(36) NOT NULL,
        "FinanceReceiptId" character varying(36) NOT NULL,
        "SellOrderId" character varying(36),
        "SellOrderItemId" character varying(36),
        "FinanceSellInvoiceId" character varying(36),
        "FinanceSellInvoiceItemId" character varying(36),
        "ReceiptAmount" numeric(18,2) NOT NULL,
        "ReceiptConvertAmount" numeric(18,2) NOT NULL,
        "StockOutItemId" character varying(36),
        "ProductId" character varying(36),
        "PN" character varying(64),
        "Brand" character varying(64),
        "VerificationStatus" smallint NOT NULL,
        "ReceiptId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financereceiptitem" PRIMARY KEY ("FinanceReceiptItemId"),
        CONSTRAINT "FK_financereceiptitem_financereceipt_FinanceReceiptId" FOREIGN KEY ("FinanceReceiptId") REFERENCES financereceipt ("FinanceReceiptId") ON DELETE CASCADE,
        CONSTRAINT "FK_financereceiptitem_financereceipt_ReceiptId" FOREIGN KEY ("ReceiptId") REFERENCES financereceipt ("FinanceReceiptId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE sellinvoiceitem (
        "SellInvoiceItemId" character varying(36) NOT NULL,
        "FinanceSellInvoiceId" character varying(36) NOT NULL,
        "InvoiceTotal" numeric(18,2) NOT NULL,
        "TaxRate" numeric(18,4) NOT NULL,
        "ValueAddedTax" numeric(18,2) NOT NULL,
        "TaxFreeTotal" numeric(18,2) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Qty" bigint NOT NULL,
        "StockOutItemId" character varying(36),
        "Currency" smallint NOT NULL,
        "ReceiveStatus" smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sellinvoiceitem" PRIMARY KEY ("SellInvoiceItemId"),
        CONSTRAINT "FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId" FOREIGN KEY ("FinanceSellInvoiceId") REFERENCES financesellinvoice ("FinanceSellInvoiceId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stockinitem (
        "ItemId" character varying(36) NOT NULL,
        "StockInId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "OrderQty" numeric(18,4) NOT NULL,
        "QtyReceived" numeric(18,4) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "LocationId" character varying(36),
        "BatchNo" character varying(50),
        "ProductionDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "IsQualified" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockinitem" PRIMARY KEY ("ItemId"),
        CONSTRAINT "FK_stockinitem_stockin_StockInId" FOREIGN KEY ("StockInId") REFERENCES stockin ("StockInId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE stockoutitem (
        "ItemId" character varying(36) NOT NULL,
        "StockOutId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "Quantity" numeric(18,4) NOT NULL,
        "OrderQty" numeric(18,4) NOT NULL,
        "PlanQty" numeric(18,4) NOT NULL,
        "PickQty" numeric(18,4) NOT NULL,
        "ActualQty" numeric(18,4) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "LocationId" character varying(36),
        "StockId" character varying(36),
        "WarehouseId" character varying(36),
        "BatchNo" character varying(50),
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockoutitem" PRIMARY KEY ("ItemId"),
        CONSTRAINT "FK_stockoutitem_stockout_StockOutId" FOREIGN KEY ("StockOutId") REFERENCES stockout ("StockOutId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE vendoraddress (
        "AddressId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "AddressType" smallint NOT NULL,
        "Country" smallint,
        "Province" character varying(50),
        "City" character varying(50),
        "Area" character varying(50),
        "Address" character varying(200),
        "ContactName" character varying(50),
        "ContactPhone" character varying(20),
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_vendoraddress" PRIMARY KEY ("AddressId"),
        CONSTRAINT "FK_vendoraddress_vendorinfo_VendorId" FOREIGN KEY ("VendorId") REFERENCES vendorinfo ("VendorId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE vendorbankinfo (
        "BankId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "BankName" character varying(100),
        "BankAccount" character varying(50),
        "AccountName" character varying(50),
        "BankBranch" character varying(100),
        "Currency" smallint,
        "IsDefault" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_vendorbankinfo" PRIMARY KEY ("BankId"),
        CONSTRAINT "FK_vendorbankinfo_vendorinfo_VendorId" FOREIGN KEY ("VendorId") REFERENCES vendorinfo ("VendorId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE TABLE vendorcontactinfo (
        "ContactId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "CName" character varying(50),
        "EName" character varying(100),
        "Title" character varying(50),
        "Department" character varying(50),
        "Mobile" character varying(20),
        "Tel" character varying(30),
        "Email" character varying(100),
        "QQ" character varying(20),
        "WeChat" character varying(50),
        "Address" character varying(200),
        "IsMain" boolean NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_vendorcontactinfo" PRIMARY KEY ("ContactId"),
        CONSTRAINT "FK_vendorcontactinfo_vendorinfo_VendorId" FOREIGN KEY ("VendorId") REFERENCES vendorinfo ("VendorId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE UNIQUE INDEX "IX_financepayment_FinancePaymentCode" ON financepayment ("FinancePaymentCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_financepaymentitem_FinancePaymentId" ON financepaymentitem ("FinancePaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_financepaymentitem_PaymentId" ON financepaymentitem ("PaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId" ON financepurchaseinvoiceitem ("FinancePurchaseInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE UNIQUE INDEX "IX_financereceipt_FinanceReceiptCode" ON financereceipt ("FinanceReceiptCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_financereceiptitem_FinanceReceiptId" ON financereceiptitem ("FinanceReceiptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_financereceiptitem_ReceiptId" ON financereceiptitem ("ReceiptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_sellinvoiceitem_FinanceSellInvoiceId" ON sellinvoiceitem ("FinanceSellInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_stockinitem_StockInId" ON stockinitem ("StockInId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_stockoutitem_StockOutId" ON stockoutitem ("StockOutId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_upload_document_BizType_BizId" ON upload_document ("BizType", "BizId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_upload_document_CreateTime" ON upload_document ("CreateTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_vendoraddress_VendorId" ON vendoraddress ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_vendorbankinfo_VendorId" ON vendorbankinfo ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    CREATE INDEX "IX_vendorcontactinfo_VendorId" ON vendorcontactinfo ("VendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319021703_AddVendorTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260319021703_AddVendorTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financepaymentitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financepurchaseinvoiceitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financereceiptitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE sellinvoiceitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financepayment;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financepurchaseinvoice;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financereceipt;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DROP TABLE financesellinvoice;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    DELETE FROM sys_serial_number
    WHERE "Id" = 10;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE TABLE tag_definition (
        "TagId" character varying(36) NOT NULL,
        "Name" character varying(50) NOT NULL,
        "Code" character varying(50),
        "Color" character varying(20),
        "Icon" character varying(100),
        "Type" smallint NOT NULL DEFAULT 2,
        "Category" character varying(50),
        "Scope" character varying(200),
        "Status" smallint NOT NULL DEFAULT 1,
        "SortOrder" integer NOT NULL,
        "UsageCount" bigint NOT NULL,
        "OwnerUserId" bigint,
        "Visibility" smallint NOT NULL DEFAULT 3,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_tag_definition" PRIMARY KEY ("TagId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE TABLE tag_relation (
        "RelationId" character varying(36) NOT NULL,
        "TagId" character varying(36) NOT NULL,
        "EntityType" character varying(50) NOT NULL,
        "EntityId" character varying(36) NOT NULL,
        "AppliedByUserId" bigint NOT NULL,
        "AppliedTime" timestamp with time zone NOT NULL,
        "Source" character varying(20),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_tag_relation" PRIMARY KEY ("RelationId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE TABLE user_tag_preference (
        "UserId" bigint NOT NULL,
        "TagId" character varying(36) NOT NULL,
        "UseCount" bigint NOT NULL,
        "LastUsedTime" timestamp with time zone,
        "IsFavorite" boolean NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_user_tag_preference" PRIMARY KEY ("UserId", "TagId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'CUS'
    WHERE "Id" = 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'VEN'
    WHERE "Id" = 2;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "ModuleCode" = 'RFQ', "Prefix" = 'RFQ'
    WHERE "Id" = 3;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'STI'
    WHERE "Id" = 7;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'STO'
    WHERE "Id" = 8;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "ModuleCode" = 'Receipt', "ModuleName" = '收款', "Prefix" = 'REC'
    WHERE "Id" = 9;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'INVI'
    WHERE "Id" = 12;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    UPDATE sys_serial_number SET "Prefix" = 'INVO'
    WHERE "Id" = 13;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (14, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Stock', '库存', 'STK', NULL, FALSE, FALSE, 4, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_tag_definition_Code" ON tag_definition ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_tag_relation_EntityType_EntityId" ON tag_relation ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_tag_relation_EntityType_TagId" ON tag_relation ("EntityType", "TagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_tag_relation_TagId" ON tag_relation ("TagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_user_tag_preference_UserId" ON user_tag_preference ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    CREATE INDEX "IX_user_tag_preference_UserId_IsFavorite" ON user_tag_preference ("UserId", "IsFavorite");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    PERFORM setval(
        pg_get_serial_sequence('sys_serial_number', 'Id'),
        GREATEST(
            (SELECT MAX("Id") FROM sys_serial_number) + 1,
            nextval(pg_get_serial_sequence('sys_serial_number', 'Id'))),
        false);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260319034349_FixRFQList') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260319034349_FixRFQList', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financepaymentitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financepurchaseinvoiceitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financereceiptitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE sellinvoiceitem;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financepayment;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financepurchaseinvoice;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financereceipt;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DROP TABLE financesellinvoice;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    DELETE FROM sys_serial_number
    WHERE "Id" = 10;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "IsActive" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "WechatAvatarUrl" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "WechatBindTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "WechatNickname" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "WechatOpenId" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    ALTER TABLE "user" ADD "WechatUnionId" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE biz_draft (
        "DraftId" character varying(36) NOT NULL,
        "UserId" bigint NOT NULL,
        "EntityType" character varying(50) NOT NULL,
        "DraftName" character varying(200),
        "PayloadJson" text NOT NULL,
        "Status" smallint NOT NULL DEFAULT 0,
        "Remark" character varying(500),
        "ConvertedEntityId" character varying(36),
        "ConvertedAt" timestamp with time zone,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_biz_draft" PRIMARY KEY ("DraftId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE debug (
        "Name" character varying(32) NOT NULL,
        "Value" character varying(128) NOT NULL,
        CONSTRAINT "PK_debug" PRIMARY KEY ("Name")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_department (
        "DepartmentId" character varying(36) NOT NULL,
        "DepartmentName" character varying(100) NOT NULL,
        "ParentId" character varying(36),
        "Path" character varying(500),
        "Level" integer NOT NULL,
        "SaleDataScope" smallint NOT NULL,
        "PurchaseDataScope" smallint NOT NULL,
        "IdentityType" smallint NOT NULL,
        "Status" smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_department" PRIMARY KEY ("DepartmentId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_permission (
        "PermissionId" character varying(36) NOT NULL,
        "PermissionCode" character varying(100) NOT NULL,
        "PermissionName" character varying(100) NOT NULL,
        "PermissionType" character varying(20) NOT NULL,
        "Resource" character varying(200),
        "Action" character varying(50),
        "Status" smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_permission" PRIMARY KEY ("PermissionId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_role (
        "RoleId" character varying(36) NOT NULL,
        "RoleCode" character varying(50) NOT NULL,
        "RoleName" character varying(100) NOT NULL,
        "Description" character varying(500),
        "Status" smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_role" PRIMARY KEY ("RoleId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_role_permission (
        "RolePermissionId" character varying(36) NOT NULL,
        "RoleId" character varying(36) NOT NULL,
        "PermissionId" character varying(36) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_role_permission" PRIMARY KEY ("RolePermissionId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_user_department (
        "UserDepartmentId" character varying(36) NOT NULL,
        "UserId" character varying(36) NOT NULL,
        "DepartmentId" character varying(36) NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_user_department" PRIMARY KEY ("UserDepartmentId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE sys_user_role (
        "UserRoleId" character varying(36) NOT NULL,
        "UserId" character varying(36) NOT NULL,
        "RoleId" character varying(36) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sys_user_role" PRIMARY KEY ("UserRoleId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE tag_definition (
        "TagId" character varying(36) NOT NULL,
        "Name" character varying(50) NOT NULL,
        "Code" character varying(50),
        "Color" character varying(20),
        "Icon" character varying(100),
        "Type" smallint NOT NULL DEFAULT 2,
        "Category" character varying(50),
        "Scope" character varying(200),
        "Status" smallint NOT NULL DEFAULT 1,
        "SortOrder" integer NOT NULL,
        "UsageCount" bigint NOT NULL,
        "OwnerUserId" bigint,
        "Visibility" smallint NOT NULL DEFAULT 3,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_tag_definition" PRIMARY KEY ("TagId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE tag_relation (
        "RelationId" character varying(36) NOT NULL,
        "TagId" character varying(36) NOT NULL,
        "EntityType" character varying(50) NOT NULL,
        "EntityId" character varying(36) NOT NULL,
        "AppliedByUserId" bigint NOT NULL,
        "AppliedTime" timestamp with time zone NOT NULL,
        "Source" character varying(20),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_tag_relation" PRIMARY KEY ("RelationId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE user_favorite (
        "FavoriteId" character varying(36) NOT NULL,
        "UserId" bigint NOT NULL,
        "EntityType" character varying(50) NOT NULL,
        "EntityId" character varying(36) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_user_favorite" PRIMARY KEY ("FavoriteId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE user_tag_preference (
        "UserId" bigint NOT NULL,
        "TagId" character varying(36) NOT NULL,
        "UseCount" bigint NOT NULL,
        "LastUsedTime" timestamp with time zone,
        "IsFavorite" boolean NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_user_tag_preference" PRIMARY KEY ("UserId", "TagId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE wechat_bind_request (
        "Id" character varying(64) NOT NULL,
        "UserId" character varying(100) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "OpenId" character varying(100),
        "UnionId" character varying(100),
        "Nickname" character varying(100),
        "AvatarUrl" character varying(500),
        "ExpireTime" timestamp with time zone NOT NULL,
        "CompleteTime" timestamp with time zone,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_wechat_bind_request" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE TABLE wechat_login_ticket (
        "Ticket" character varying(64) NOT NULL,
        "QrCodeUrl" character varying(500) NOT NULL,
        "Status" smallint NOT NULL,
        "OpenId" character varying(100),
        "UnionId" character varying(100),
        "UserId" character varying(100),
        "ExpireTime" timestamp with time zone NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_wechat_login_ticket" PRIMARY KEY ("Ticket")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'CUS'
    WHERE "Id" = 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'VEN'
    WHERE "Id" = 2;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "ModuleCode" = 'RFQ', "Prefix" = 'RFQ'
    WHERE "Id" = 3;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'STI'
    WHERE "Id" = 7;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'STO'
    WHERE "Id" = 8;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "ModuleCode" = 'Receipt', "ModuleName" = '收款', "Prefix" = 'REC'
    WHERE "Id" = 9;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'INVI'
    WHERE "Id" = 12;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    UPDATE sys_serial_number SET "Prefix" = 'INVO'
    WHERE "Id" = 13;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    VALUES (14, TIMESTAMPTZ '2026-01-01T00:00:00Z', 0, NULL, NULL, 'Stock', '库存', 'STK', NULL, FALSE, FALSE, 4, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_biz_draft_CreateTime" ON biz_draft ("CreateTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_biz_draft_UserId_EntityType_Status" ON biz_draft ("UserId", "EntityType", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_sys_department_ParentId" ON sys_department ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_sys_permission_PermissionCode" ON sys_permission ("PermissionCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_sys_role_RoleCode" ON sys_role ("RoleCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_sys_role_permission_RoleId_PermissionId" ON sys_role_permission ("RoleId", "PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_sys_user_department_UserId_DepartmentId" ON sys_user_department ("UserId", "DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_sys_user_role_UserId_RoleId" ON sys_user_role ("UserId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_tag_definition_Code" ON tag_definition ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_tag_relation_EntityType_EntityId" ON tag_relation ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_tag_relation_EntityType_TagId" ON tag_relation ("EntityType", "TagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_tag_relation_TagId" ON tag_relation ("TagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_user_favorite_EntityType_EntityId" ON user_favorite ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE UNIQUE INDEX "IX_user_favorite_UserId_EntityType_EntityId" ON user_favorite ("UserId", "EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_user_tag_preference_UserId" ON user_tag_preference ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    CREATE INDEX "IX_user_tag_preference_UserId_IsFavorite" ON user_tag_preference ("UserId", "IsFavorite");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    PERFORM setval(
        pg_get_serial_sequence('sys_serial_number', 'Id'),
        GREATEST(
            (SELECT MAX("Id") FROM sys_serial_number) + 1,
            nextval(pg_get_serial_sequence('sys_serial_number', 'Id'))),
        false);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260320161913_AddDebugRecord') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260320161913_AddDebugRecord', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322094614_ModelAlignment_20260322') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322094614_ModelAlignment_20260322', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323082644_PurchaseRequisitionModule') THEN
    CREATE TABLE purchaserequisition (
        purchase_requisition_id character varying(36) NOT NULL,
        bill_code character varying(32) NOT NULL,
        sell_order_item_id character varying(36) NOT NULL,
        sell_order_id character varying(36) NOT NULL,
        qty numeric(18,4) NOT NULL,
        expected_purchase_time timestamp with time zone NOT NULL,
        status smallint NOT NULL,
        type smallint NOT NULL,
        purchase_user_id character varying(36),
        sales_user_id character varying(36),
        quote_vendor_id character varying(36),
        quote_cost numeric(18,6) NOT NULL,
        pn character varying(200),
        brand character varying(200),
        remark character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_purchaserequisition" PRIMARY KEY (purchase_requisition_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323082644_PurchaseRequisitionModule') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323082644_PurchaseRequisitionModule', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323160000_AddPurchaseOrderItemInnerCommentColumn') THEN

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'purchaseorderitem'
              AND column_name = 'inner_comment'
        ) THEN
            ALTER TABLE public.purchaseorderitem
            ADD COLUMN inner_comment varchar(500);
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323160000_AddPurchaseOrderItemInnerCommentColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323160000_AddPurchaseOrderItemInnerCommentColumn', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323180000_AddSellOrderAuditRemark') THEN

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'sellorder'
              AND column_name = 'audit_remark'
        ) THEN
            ALTER TABLE public.sellorder
            ADD COLUMN audit_remark varchar(500);
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323180000_AddSellOrderAuditRemark') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323180000_AddSellOrderAuditRemark', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323193000_AlignPurchaseOrderStatusWorkflow') THEN

    UPDATE public.purchaseorder
    SET status = CASE status
        WHEN 0 THEN 20
        WHEN 1 THEN 30
        WHEN 2 THEN 50
        WHEN 3 THEN 50
        WHEN 4 THEN 50
        WHEN 5 THEN 100
        WHEN -1 THEN -2
        ELSE status
    END;

    UPDATE public.purchaseorderitem i
    SET status = CASE o.status
        WHEN 1 THEN 1
        WHEN 2 THEN 2
        WHEN 10 THEN 10
        WHEN 20 THEN 20
        WHEN 30 THEN 30
        WHEN 50 THEN 50
        WHEN 100 THEN 100
        WHEN -1 THEN -1
        WHEN -2 THEN -2
        ELSE i.status
    END
    FROM public.purchaseorder o
    WHERE i.purchase_order_id = o."PurchaseOrderId";

    ALTER TABLE public.purchaseorder
    ALTER COLUMN status SET DEFAULT 1;

    ALTER TABLE public.purchaseorderitem
    ALTER COLUMN status SET DEFAULT 1;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323193000_AlignPurchaseOrderStatusWorkflow') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323193000_AlignPurchaseOrderStatusWorkflow', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324120000_EnsureFinanceTablesIfMissing') THEN

    -- financepayment
    CREATE TABLE IF NOT EXISTS public.financepayment (
        "FinancePaymentId" character varying(36) NOT NULL,
        "FinancePaymentCode" character varying(16) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "VendorName" character varying(200),
        "Status" smallint NOT NULL DEFAULT 0,
        "PaymentAmountToBe" numeric(18,2) NOT NULL,
        "PaymentAmount" numeric(18,2) NOT NULL,
        "PaymentTotalAmount" numeric(18,2) NOT NULL,
        "PaymentCurrency" smallint NOT NULL,
        "PaymentDate" timestamp with time zone,
        "PaymentUserId" character varying(36),
        "PaymentMode" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepayment" PRIMARY KEY ("FinancePaymentId")
    );

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_financepayment_FinancePaymentCode"
        ON public.financepayment ("FinancePaymentCode");

    -- financepurchaseinvoice
    CREATE TABLE IF NOT EXISTS public.financepurchaseinvoice (
        "FinancePurchaseInvoiceId" character varying(36) NOT NULL,
        "VendorId" character varying(36) NOT NULL,
        "VendorName" character varying(200),
        "InvoiceNo" character varying(32),
        "InvoiceAmount" numeric(18,2) NOT NULL,
        "BillAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "ExcludTaxAmount" numeric(18,2) NOT NULL,
        "InvoiceDate" timestamp with time zone,
        "ConfirmDate" timestamp with time zone,
        "ConfirmStatus" smallint NOT NULL,
        "RedInvoiceStatus" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepurchaseinvoice" PRIMARY KEY ("FinancePurchaseInvoiceId")
    );

    -- financereceipt
    CREATE TABLE IF NOT EXISTS public.financereceipt (
        "FinanceReceiptId" character varying(36) NOT NULL,
        "FinanceReceiptCode" character varying(16) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "CustomerName" character varying(200),
        "SalesUserId" character varying(36),
        "PurchaseGroupId" character varying(36),
        "Status" smallint NOT NULL DEFAULT 0,
        "ReceiptAmount" numeric(18,2) NOT NULL,
        "ReceiptCurrency" smallint NOT NULL,
        "ReceiptDate" timestamp with time zone,
        "ReceiptUserId" character varying(36),
        "ReceiptMode" smallint NOT NULL,
        "ReceiptBankId" character varying(36),
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financereceipt" PRIMARY KEY ("FinanceReceiptId")
    );

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_financereceipt_FinanceReceiptCode"
        ON public.financereceipt ("FinanceReceiptCode");

    -- financesellinvoice
    CREATE TABLE IF NOT EXISTS public.financesellinvoice (
        "FinanceSellInvoiceId" character varying(36) NOT NULL,
        "CustomerId" character varying(36) NOT NULL,
        "CustomerName" character varying(200),
        "InvoiceCode" character varying(32),
        "InvoiceNo" character varying(64),
        "InvoiceTotal" numeric(18,2) NOT NULL,
        "MakeInvoiceDate" timestamp with time zone,
        "ReceiveStatus" smallint NOT NULL,
        "ReceiveDone" numeric(18,2) NOT NULL,
        "ReceiveToBe" numeric(18,2) NOT NULL,
        "Currency" smallint NOT NULL,
        "Type" smallint NOT NULL,
        "InvoiceStatus" smallint NOT NULL,
        "SellInvoiceType" smallint NOT NULL,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financesellinvoice" PRIMARY KEY ("FinanceSellInvoiceId")
    );

    -- financepaymentitem
    CREATE TABLE IF NOT EXISTS public.financepaymentitem (
        "FinancePaymentItemId" character varying(36) NOT NULL,
        "FinancePaymentId" character varying(36) NOT NULL,
        "PurchaseOrderId" character varying(36),
        "PurchaseOrderItemId" character varying(36),
        "PaymentAmount" numeric(18,2) NOT NULL,
        "PaymentAmountToBe" numeric(18,2) NOT NULL,
        "ProductId" character varying(36),
        "PN" character varying(64),
        "Brand" character varying(64),
        "VerificationStatus" smallint NOT NULL,
        "VerificationDone" numeric(18,2) NOT NULL,
        "VerificationToBe" numeric(18,2) NOT NULL,
        "PaymentId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepaymentitem" PRIMARY KEY ("FinancePaymentItemId")
    );

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_financepaymentitem_financepayment_FinancePaymentId') THEN
            ALTER TABLE public.financepaymentitem
                ADD CONSTRAINT "FK_financepaymentitem_financepayment_FinancePaymentId"
                FOREIGN KEY ("FinancePaymentId") REFERENCES public.financepayment ("FinancePaymentId") ON DELETE CASCADE;
        END IF;
    END $$;

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_financepaymentitem_financepayment_PaymentId') THEN
            ALTER TABLE public.financepaymentitem
                ADD CONSTRAINT "FK_financepaymentitem_financepayment_PaymentId"
                FOREIGN KEY ("PaymentId") REFERENCES public.financepayment ("FinancePaymentId");
        END IF;
    END $$;

    CREATE INDEX IF NOT EXISTS "IX_financepaymentitem_FinancePaymentId"
        ON public.financepaymentitem ("FinancePaymentId");

    -- financepurchaseinvoiceitem
    CREATE TABLE IF NOT EXISTS public.financepurchaseinvoiceitem (
        "FinancePurchaseInvoiceItemId" character varying(36) NOT NULL,
        "FinancePurchaseInvoiceId" character varying(36) NOT NULL,
        "StockInId" character varying(36),
        "StockInCode" character varying(32),
        "PurchaseOrderCode" character varying(32),
        "StockInCost" numeric(18,4) NOT NULL,
        "BillCost" numeric(18,4) NOT NULL,
        "BillQty" bigint NOT NULL,
        "BillAmount" numeric(18,2) NOT NULL,
        "TaxRate" numeric(18,4) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "ExcludTaxAmount" numeric(18,2) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financepurchaseinvoiceitem" PRIMARY KEY ("FinancePurchaseInvoiceItemId")
    );

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname LIKE 'FK_financepurchaseinvoiceitem_financepurchaseinvoice%') THEN
            ALTER TABLE public.financepurchaseinvoiceitem
                ADD CONSTRAINT "FK_financepurchaseinvoiceitem_financepurchaseinvoice_FinancePurchaseInvoiceId"
                FOREIGN KEY ("FinancePurchaseInvoiceId") REFERENCES public.financepurchaseinvoice ("FinancePurchaseInvoiceId") ON DELETE CASCADE;
        END IF;
    END $$;

    CREATE INDEX IF NOT EXISTS "IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId"
        ON public.financepurchaseinvoiceitem ("FinancePurchaseInvoiceId");

    -- financereceiptitem
    CREATE TABLE IF NOT EXISTS public.financereceiptitem (
        "FinanceReceiptItemId" character varying(36) NOT NULL,
        "FinanceReceiptId" character varying(36) NOT NULL,
        "SellOrderId" character varying(36),
        "SellOrderItemId" character varying(36),
        "FinanceSellInvoiceId" character varying(36),
        "FinanceSellInvoiceItemId" character varying(36),
        "ReceiptAmount" numeric(18,2) NOT NULL,
        "ReceiptConvertAmount" numeric(18,2) NOT NULL,
        "StockOutItemId" character varying(36),
        "ProductId" character varying(36),
        "PN" character varying(64),
        "Brand" character varying(64),
        "VerificationStatus" smallint NOT NULL,
        "ReceiptId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_financereceiptitem" PRIMARY KEY ("FinanceReceiptItemId")
    );

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_financereceiptitem_financereceipt_FinanceReceiptId') THEN
            ALTER TABLE public.financereceiptitem
                ADD CONSTRAINT "FK_financereceiptitem_financereceipt_FinanceReceiptId"
                FOREIGN KEY ("FinanceReceiptId") REFERENCES public.financereceipt ("FinanceReceiptId") ON DELETE CASCADE;
        END IF;
    END $$;

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_financereceiptitem_financereceipt_ReceiptId') THEN
            ALTER TABLE public.financereceiptitem
                ADD CONSTRAINT "FK_financereceiptitem_financereceipt_ReceiptId"
                FOREIGN KEY ("ReceiptId") REFERENCES public.financereceipt ("FinanceReceiptId");
        END IF;
    END $$;

    CREATE INDEX IF NOT EXISTS "IX_financereceiptitem_FinanceReceiptId"
        ON public.financereceiptitem ("FinanceReceiptId");

    -- sellinvoiceitem
    CREATE TABLE IF NOT EXISTS public.sellinvoiceitem (
        "SellInvoiceItemId" character varying(36) NOT NULL,
        "FinanceSellInvoiceId" character varying(36) NOT NULL,
        "InvoiceTotal" numeric(18,2) NOT NULL,
        "TaxRate" numeric(18,4) NOT NULL,
        "ValueAddedTax" numeric(18,2) NOT NULL,
        "TaxFreeTotal" numeric(18,2) NOT NULL,
        "Price" numeric(18,4) NOT NULL,
        "Qty" bigint NOT NULL,
        "StockOutItemId" character varying(36),
        "Currency" smallint NOT NULL,
        "ReceiveStatus" smallint NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_sellinvoiceitem" PRIMARY KEY ("SellInvoiceItemId")
    );

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId') THEN
            ALTER TABLE public.sellinvoiceitem
                ADD CONSTRAINT "FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId"
                FOREIGN KEY ("FinanceSellInvoiceId") REFERENCES public.financesellinvoice ("FinanceSellInvoiceId") ON DELETE CASCADE;
        END IF;
    END $$;

    CREATE INDEX IF NOT EXISTS "IX_sellinvoiceitem_FinanceSellInvoiceId"
        ON public.sellinvoiceitem ("FinanceSellInvoiceId");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324120000_EnsureFinanceTablesIfMissing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324120000_EnsureFinanceTablesIfMissing', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324143000_AlignFinancePaymentStatusToPRD') THEN

    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = 'financepayment'
        ) THEN
            -- 旧状态映射到 PRD 新状态
            UPDATE public.financepayment SET "Status" = 1   WHERE "Status" = 0; -- 草稿 -> 新建
            UPDATE public.financepayment SET "Status" = 2   WHERE "Status" = 1; -- 待审核
            UPDATE public.financepayment SET "Status" = 10  WHERE "Status" = 2; -- 已审核 -> 审核通过
            UPDATE public.financepayment SET "Status" = 100 WHERE "Status" = 3; -- 已付款 -> 付款完成
            UPDATE public.financepayment SET "Status" = -2  WHERE "Status" IN (4, 5); -- 已取消/已作废 -> 取消

            ALTER TABLE public.financepayment
                ALTER COLUMN "Status" SET DEFAULT 1;
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324143000_AlignFinancePaymentStatusToPRD') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324143000_AlignFinancePaymentStatusToPRD', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324173000_AddLogisticsDocuments') THEN

    CREATE TABLE IF NOT EXISTS public.stockinnotify (
        "UserId" character varying(36) NOT NULL,
        "NoticeCode" character varying(32) NOT NULL,
        "PurchaseOrderId" character varying(36) NOT NULL,
        "PurchaseOrderCode" character varying(32) NOT NULL,
        "VendorId" character varying(36),
        "VendorName" character varying(64),
        "PurchaseUserName" character varying(64),
        "Status" smallint NOT NULL DEFAULT 10,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockinnotify" PRIMARY KEY ("UserId")
    );

    CREATE TABLE IF NOT EXISTS public.stockinnotifyitem (
        "UserId" character varying(36) NOT NULL,
        "StockInNotifyId" character varying(36) NOT NULL,
        "PurchaseOrderItemId" character varying(36) NOT NULL,
        "Pn" character varying(128),
        "Brand" character varying(64),
        "Qty" numeric(18,4) NOT NULL,
        "ArrivedQty" numeric(18,4) NOT NULL,
        "PassedQty" numeric(18,4) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockinnotifyitem" PRIMARY KEY ("UserId")
    );

    CREATE TABLE IF NOT EXISTS public.qcinfo (
        "UserId" character varying(36) NOT NULL,
        "QcCode" character varying(32) NOT NULL,
        "StockInNotifyId" character varying(36) NOT NULL,
        "StockInNotifyCode" character varying(32) NOT NULL,
        "Status" smallint NOT NULL DEFAULT 10,
        "StockInStatus" smallint NOT NULL DEFAULT 1,
        "PassQty" numeric(18,4) NOT NULL,
        "RejectQty" numeric(18,4) NOT NULL,
        "StockInId" character varying(36),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_qcinfo" PRIMARY KEY ("UserId")
    );

    CREATE TABLE IF NOT EXISTS public.qcitem (
        "UserId" character varying(36) NOT NULL,
        "QcInfoId" character varying(36) NOT NULL,
        "StockInNotifyItemId" character varying(36) NOT NULL,
        "ArrivedQty" numeric(18,4) NOT NULL,
        "PassedQty" numeric(18,4) NOT NULL,
        "RejectQty" numeric(18,4) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_qcitem" PRIMARY KEY ("UserId")
    );

    CREATE INDEX IF NOT EXISTS "IX_stockinnotifyitem_StockInNotifyId" ON public.stockinnotifyitem ("StockInNotifyId");
    CREATE INDEX IF NOT EXISTS "IX_qcinfo_StockInNotifyId" ON public.qcinfo ("StockInNotifyId");
    CREATE INDEX IF NOT EXISTS "IX_qcitem_QcInfoId" ON public.qcitem ("QcInfoId");

    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockinnotifyitem_stockinnotify_StockInNotifyId') THEN
            ALTER TABLE public.stockinnotifyitem
            ADD CONSTRAINT "FK_stockinnotifyitem_stockinnotify_StockInNotifyId"
            FOREIGN KEY ("StockInNotifyId") REFERENCES public.stockinnotify ("UserId") ON DELETE CASCADE;
        END IF;
    END $$;

    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_qcinfo_stockinnotify_StockInNotifyId') THEN
            ALTER TABLE public.qcinfo
            ADD CONSTRAINT "FK_qcinfo_stockinnotify_StockInNotifyId"
            FOREIGN KEY ("StockInNotifyId") REFERENCES public.stockinnotify ("UserId") ON DELETE CASCADE;
        END IF;
    END $$;

    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_qcitem_qcinfo_QcInfoId') THEN
            ALTER TABLE public.qcitem
            ADD CONSTRAINT "FK_qcitem_qcinfo_QcInfoId"
            FOREIGN KEY ("QcInfoId") REFERENCES public.qcinfo ("UserId") ON DELETE CASCADE;
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324173000_AddLogisticsDocuments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324173000_AddLogisticsDocuments', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324193000_AddDatabaseFieldComments') THEN

    -- =============================
    -- 1) 关键业务表：精确注释
    -- =============================
    COMMENT ON TABLE public.purchaseorder IS '采购订单主表';
    COMMENT ON COLUMN public.purchaseorder."status" IS '订单状态：1新建 2待审核 10审核通过 20待确认 30已确认 50进行中 100采购完成 -1审核失败 -2取消';
    COMMENT ON COLUMN public.purchaseorder."type" IS '订单类型：1普通 2紧急 3样品';
    COMMENT ON COLUMN public.purchaseorder."currency" IS '币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP';
    COMMENT ON COLUMN public.purchaseorder."stock_status" IS '入库状态：0未入库 1部分入库 2全部入库';
    COMMENT ON COLUMN public.purchaseorder."finance_status" IS '付款状态：0未付款 1部分付款 2全部付款';
    COMMENT ON COLUMN public.purchaseorder."stock_out_status" IS '出库状态：0未出库 1部分出库 2全部出库';
    COMMENT ON COLUMN public.purchaseorder."invoice_status" IS '开票状态：0未开票 1部分开票 2全部开票';

    COMMENT ON TABLE public.purchaseorderitem IS '采购订单明细表';
    COMMENT ON COLUMN public.purchaseorderitem."status" IS '明细状态：1新建 2待审核 10审核通过 20待确认 30已确认 40已付款 50已发货 60已入库 100采购完成 -1审核失败 -2取消';
    COMMENT ON COLUMN public.purchaseorderitem."currency" IS '币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP';
    COMMENT ON COLUMN public.purchaseorderitem."stock_in_status" IS '入库状态：0未入库 1部分入库 2全部入库';
    COMMENT ON COLUMN public.purchaseorderitem."finance_payment_status" IS '付款状态：0未付款 1部分付款 2全部付款';
    COMMENT ON COLUMN public.purchaseorderitem."stock_out_status" IS '出库状态：0未出库 1部分出库 2全部出库';

    COMMENT ON TABLE public.stockinnotify IS '到货通知主表';
    COMMENT ON COLUMN public.stockinnotify."Status" IS '到货通知状态：1新建 10未到货 20到货待检 30已质检 100已入库';

    COMMENT ON TABLE public.stockinnotifyitem IS '到货通知明细表';
    COMMENT ON COLUMN public.stockinnotifyitem."Qty" IS '采购数量';
    COMMENT ON COLUMN public.stockinnotifyitem."ArrivedQty" IS '到货数量';
    COMMENT ON COLUMN public.stockinnotifyitem."PassedQty" IS '质检通过数量';

    COMMENT ON TABLE public.qcinfo IS '质检主表';
    COMMENT ON COLUMN public.qcinfo."Status" IS '质检结果：-1未通过 10部分通过 100已通过';
    COMMENT ON COLUMN public.qcinfo."StockInStatus" IS '入库状态：-1拒收 1未入库 10部分入库 100全部入库';
    COMMENT ON COLUMN public.qcinfo."PassQty" IS '通过数量';
    COMMENT ON COLUMN public.qcinfo."RejectQty" IS '拒收数量';

    COMMENT ON TABLE public.qcitem IS '质检明细表';
    COMMENT ON COLUMN public.qcitem."ArrivedQty" IS '到货数量';
    COMMENT ON COLUMN public.qcitem."PassedQty" IS '通过数量';
    COMMENT ON COLUMN public.qcitem."RejectQty" IS '拒收数量';

    COMMENT ON TABLE public.stockin IS '入库单主表';
    COMMENT ON COLUMN public.stockin."Status" IS '入库单状态：0草稿 1待入库 2已入库 3已取消';
    COMMENT ON COLUMN public.stockin."InspectStatus" IS '质检状态：0未质检 1合格 2不合格';

    COMMENT ON TABLE public.stockout IS '出库单主表';
    COMMENT ON COLUMN public.stockout."Status" IS '出库单状态：0草稿 1待出库 2已出库 3已取消';

    COMMENT ON TABLE public.purchaserequisition IS '采购申请单主表';
    COMMENT ON COLUMN public.purchaserequisition."status" IS '采购申请状态：0草稿 1待审核 10审核通过 20待下单 30部分下单 100已完成 -1审核失败 -2取消';

    -- =============================
    -- 2) 通用补齐：对无注释表/字段按命名规则自动添加
    -- =============================
    DO $$
    DECLARE
        r record;
        v_comment text;
    BEGIN
        -- 表注释
        FOR r IN
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        LOOP
            IF obj_description(format('%I.%I', r.table_schema, r.table_name)::regclass, 'pg_class') IS NULL THEN
                EXECUTE format('COMMENT ON TABLE %I.%I IS %L',
                    r.table_schema,
                    r.table_name,
                    '业务表：' || r.table_name);
            END IF;
        END LOOP;

        -- 字段注释
        FOR r IN
            SELECT c.table_schema, c.table_name, c.column_name
            FROM information_schema.columns c
            WHERE c.table_schema = 'public'
            ORDER BY c.table_name, c.ordinal_position
        LOOP
            IF col_description(format('%I.%I', r.table_schema, r.table_name)::regclass,
                               (SELECT ordinal_position::int
                                FROM information_schema.columns
                                WHERE table_schema = r.table_schema
                                  AND table_name = r.table_name
                                  AND column_name = r.column_name)) IS NULL THEN
                v_comment :=
                    CASE lower(r.column_name)
                        WHEN 'id' THEN '主键ID'
                        WHEN 'status' THEN '状态字段（具体枚举见业务定义）'
                        WHEN 'type' THEN '类型字段（具体枚举见业务定义）'
                        WHEN 'currency' THEN '币别字段（具体枚举见业务定义）'
                        WHEN 'createtime' THEN '创建时间'
                        WHEN 'create_time' THEN '创建时间'
                        WHEN 'modifytime' THEN '修改时间'
                        WHEN 'modify_time' THEN '修改时间'
                        WHEN 'createuserid' THEN '创建人ID'
                        WHEN 'create_user_id' THEN '创建人ID'
                        WHEN 'modifyuserid' THEN '修改人ID'
                        WHEN 'modify_user_id' THEN '修改人ID'
                        WHEN 'remark' THEN '备注'
                        WHEN 'comment' THEN '备注'
                        WHEN 'inner_comment' THEN '内部备注'
                        WHEN 'code' THEN '业务编码'
                        WHEN 'name' THEN '名称'
                        WHEN 'qty' THEN '数量'
                        WHEN 'quantity' THEN '数量'
                        WHEN 'price' THEN '单价'
                        WHEN 'amount' THEN '金额'
                        WHEN 'total' THEN '总计金额'
                        WHEN 'totalamount' THEN '总金额'
                        WHEN 'totalquantity' THEN '总数量'
                        ELSE
                            CASE
                                WHEN lower(r.column_name) LIKE '%_id' OR lower(r.column_name) LIKE '%id'
                                    THEN '关联ID：' || r.column_name
                                WHEN lower(r.column_name) LIKE '%_code' OR lower(r.column_name) LIKE '%code'
                                    THEN '业务编码：' || r.column_name
                                WHEN lower(r.column_name) LIKE '%_name' OR lower(r.column_name) LIKE '%name'
                                    THEN '名称：' || r.column_name
                                WHEN lower(r.column_name) LIKE '%date%' OR lower(r.column_name) LIKE '%time%'
                                    THEN '时间字段：' || r.column_name
                                WHEN lower(r.column_name) LIKE '%status%'
                                    THEN '状态字段：' || r.column_name || '（具体枚举见业务定义）'
                                ELSE '业务字段：' || r.column_name
                            END
                    END;

                EXECUTE format('COMMENT ON COLUMN %I.%I.%I IS %L',
                    r.table_schema, r.table_name, r.column_name, v_comment);
            END IF;
        END LOOP;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324193000_AddDatabaseFieldComments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324193000_AddDatabaseFieldComments', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324195000_RefineEnumCommentsForStatusTypeFields') THEN

    DO $$
    DECLARE
        rec record;
    BEGIN
        FOR rec IN
            SELECT *
            FROM (VALUES
                -- financepayment
                ('financepayment','Status','付款状态：1新建 2待审核 10审核通过 100付款完成 -1审核失败 -2取消'),
                ('financepayment','PaymentCurrency','付款币别：1人民币 2美元 3欧元'),
                ('financepayment','PaymentMode','付款方式：1银行转账 2现金 3支票 4承兑汇票'),

                -- financepaymentitem
                ('financepaymentitem','VerificationStatus','核销状态：0未核销 1部分核销 2核销完成'),

                -- financereceipt
                ('financereceipt','Status','收款状态：0草稿 1待审核 2已审核 3已收款 4已取消'),
                ('financereceipt','ReceiptCurrency','收款币别：1人民币 2美元 3欧元'),
                ('financereceipt','ReceiptMode','收款方式：1银行转账 2现金 3支票 4承兑汇票'),

                -- financereceiptitem
                ('financereceiptitem','VerificationStatus','核销状态：0未核销 1部分核销 2核销完成'),

                -- financepurchaseinvoice
                ('financepurchaseinvoice','ConfirmStatus','认证状态：0未认证 1已认证'),
                ('financepurchaseinvoice','RedInvoiceStatus','冲红状态：0正常 1已冲红'),

                -- financesellinvoice
                ('financesellinvoice','ReceiveStatus','收款状态：0未收款 1部分收款 2收款完成'),
                ('financesellinvoice','Currency','币别：1人民币 2美元 3欧元'),
                ('financesellinvoice','Type','发票类型：10蓝字发票 20红字发票'),
                ('financesellinvoice','InvoiceStatus','发票状态：1未申请 2申请中 100已开票 101开票失败 -1已作废'),
                ('financesellinvoice','SellInvoiceType','销项发票类别：100增值税专用发票 200增值税普通发票'),

                -- sellinvoiceitem
                ('sellinvoiceitem','ReceiveStatus','收款状态：0未收款 1部分收款 2收款完成'),
                ('sellinvoiceitem','Currency','币别：1人民币 2美元 3欧元'),

                -- sellorder
                ('sellorder','status','订单状态：1新建 2待审核 10审核通过 20进行中 100完成 -1审核失败 -2取消'),
                ('sellorder','type','订单类型：1普通 2紧急 3样品'),
                ('sellorder','currency','币别：1RMB 2USD 3EUR'),
                ('sellorder','purchase_order_status','采购状态：0未采购 1部分采购 2全部采购'),
                ('sellorder','stock_out_status','出库状态：0未出库 1部分出库 2全部出库'),
                ('sellorder','stock_in_status','入库状态：0未入库 1部分入库 2全部入库'),
                ('sellorder','finance_receipt_status','收款状态：0未收款 1部分收款 2全部收款'),
                ('sellorder','finance_payment_status','付款状态：0未付款 1部分付款 2全部付款'),
                ('sellorder','invoice_status','开票状态：0未开票 1部分开票 2全部开票'),

                -- sellorderitem
                ('sellorderitem','status','明细状态：0正常 1已取消'),
                ('sellorderitem','currency','币别：1RMB 2USD 3EUR'),

                -- payment（旧付款模型）
                ('payment','Status','状态：0草稿 1待审核 2已审核 3已付款 4已取消'),
                ('payment','PaymentType','付款类型：1采购付款 2费用付款 3预付款 4其他'),
                ('payment','PaymentMethod','付款方式：1银行转账 2现金 3支票 4承兑汇票 5信用证'),
                ('payment','Currency','币别：1人民币 2美元 3欧元'),

                -- receipt（旧收款模型）
                ('receipt','Status','状态：0草稿 1待审核 2已审核 3已收款 4已取消'),
                ('receipt','ReceiptType','收款类型：1销售收款 2预收款 3退款 4其他'),
                ('receipt','ReceiptMethod','收款方式：1银行转账 2现金 3支票 4承兑汇票 5信用证 6支付宝 7微信'),
                ('receipt','Currency','币别：1人民币 2美元 3欧元')
            ) AS t(table_name, column_name, comment_text)
        LOOP
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns c
                WHERE c.table_schema = 'public'
                  AND c.table_name = rec.table_name
                  AND c.column_name = rec.column_name
            ) THEN
                EXECUTE format(
                    'COMMENT ON COLUMN public.%I.%I IS %L',
                    rec.table_name,
                    rec.column_name,
                    rec.comment_text
                );
            END IF;
        END LOOP;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324195000_RefineEnumCommentsForStatusTypeFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324195000_RefineEnumCommentsForStatusTypeFields', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324201000_RefineEnumCommentsForRfqQuoteRbacEtc') THEN

    DO $$
    DECLARE
        rec record;
    BEGIN
        FOR rec IN
            SELECT *
            FROM (VALUES
                -- rfq
                ('rfq', 'rfq_type', '需求类型：1现货 2期货 3样品 4批量'),
                ('rfq', 'quote_method', '报价方式：1不接受任何消息 2仅邮件 3仅系统 4全部方式'),
                ('rfq', 'assign_method', '分配方式：1系统分配多人采购 2系统分配单人采购 3手动分配'),
                ('rfq', 'target_type', '目标类型：1比价需求 2独家需求 3紧急需求 4常规需求'),
                ('rfq', 'status', '状态：0待分配 1已分配 2报价中 3已报价 4已选价 5已转订单 6已关闭'),

                -- rfqitem
                ('rfqitem', 'price_currency', '目标价币种：1RMB 2USD 3EUR 4HKD'),
                ('rfqitem', 'status', '状态：0待报价 1已报价 2已接受 3已拒绝'),

                -- quote
                ('quote', 'status', '状态：0草稿 1待审核 2已审核 3已发送 4已接受 5已拒绝 6已过期 7已关闭'),

                -- quoteitem
                ('quoteitem', 'label_type', '涂标：0不涂标 1涂标 2待确定'),
                ('quoteitem', 'wafer_origin', '晶圆产地：0美产 1非美产 2待确定'),
                ('quoteitem', 'package_origin', '封装产地：0美产 1非美产 2待确定'),
                ('quoteitem', 'currency', '报价币别：0RMB 1USD 2EUR 3HKD'),
                ('quoteitem', 'status', '状态：0有效 1已取消'),

                -- purchaserequisition
                ('purchaserequisition', 'status', '状态：0新建 1部分完成 2全部完成 3已取消'),
                ('purchaserequisition', 'type', '类型：0专属 1公开备货'),

                -- stockoutrequest
                ('stockoutrequest', 'Status', '状态：0待出库 1已出库 2已取消'),

                -- user
                ('user', 'Status', '账号状态：1启用 0禁用'),

                -- rbac
                ('sys_role', 'Status', '状态：1启用 0禁用'),
                ('sys_permission', 'Status', '状态：1启用 0禁用'),
                ('sys_permission', 'PermissionType', '权限类型：menu菜单 api接口 button按钮 data数据'),
                ('sys_department', 'Status', '状态：1启用 0禁用'),
                ('sys_department', 'SaleDataScope', '销售数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止'),
                ('sys_department', 'PurchaseDataScope', '采购数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止'),
                ('sys_department', 'IdentityType', '业务身份：0None 1Sales 2Purchaser 3PurchaseAssistant 4CustService 5Finance 6Logistics')
            ) AS t(table_name, column_name, comment_text)
        LOOP
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns c
                WHERE c.table_schema = 'public'
                  AND c.table_name = rec.table_name
                  AND c.column_name = rec.column_name
            ) THEN
                EXECUTE format(
                    'COMMENT ON COLUMN public.%I.%I IS %L',
                    rec.table_name,
                    rec.column_name,
                    rec.comment_text
                );
            END IF;
        END LOOP;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324201000_RefineEnumCommentsForRfqQuoteRbacEtc') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324201000_RefineEnumCommentsForRfqQuoteRbacEtc', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324223000_AddInventoryCenterTables') THEN

    CREATE TABLE IF NOT EXISTS public.warehouseinfo (
        "Id" character varying(36) NOT NULL,
        "WarehouseCode" character varying(32) NOT NULL,
        "WarehouseName" character varying(100) NOT NULL,
        "Address" character varying(200),
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_warehouseinfo" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_warehouseinfo_WarehouseCode" ON public.warehouseinfo ("WarehouseCode");

    CREATE TABLE IF NOT EXISTS public.warehousezone (
        "Id" character varying(36) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "ZoneCode" character varying(32) NOT NULL,
        "ZoneName" character varying(100) NOT NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_warehousezone" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_warehousezone_WarehouseId_ZoneCode" ON public.warehousezone ("WarehouseId", "ZoneCode");

    CREATE TABLE IF NOT EXISTS public.warehouselocation (
        "Id" character varying(36) NOT NULL,
        "ZoneId" character varying(36) NOT NULL,
        "LocationCode" character varying(32) NOT NULL,
        "LocationName" character varying(100) NOT NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_warehouselocation" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_warehouselocation_ZoneId_LocationCode" ON public.warehouselocation ("ZoneId", "LocationCode");

    CREATE TABLE IF NOT EXISTS public.warehouseshelf (
        "Id" character varying(36) NOT NULL,
        "LocationId" character varying(36) NOT NULL,
        "ShelfCode" character varying(32) NOT NULL,
        "ShelfName" character varying(100) NOT NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_warehouseshelf" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_warehouseshelf_LocationId_ShelfCode" ON public.warehouseshelf ("LocationId", "ShelfCode");

    CREATE TABLE IF NOT EXISTS public.stockledger (
        "Id" character varying(36) NOT NULL,
        "BizType" character varying(20) NOT NULL,
        "BizId" character varying(36) NOT NULL,
        "BizLineId" character varying(36),
        "MaterialId" character varying(36) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "LocationId" character varying(36),
        "BatchNo" character varying(50),
        "QtyIn" numeric(18,4) NOT NULL DEFAULT 0,
        "QtyOut" numeric(18,4) NOT NULL DEFAULT 0,
        "UnitCost" numeric(18,6) NOT NULL DEFAULT 0,
        "Amount" numeric(18,2) NOT NULL DEFAULT 0,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockledger" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_stockledger_BizType_BizId_BizLineId" ON public.stockledger ("BizType", "BizId", "BizLineId");

    CREATE TABLE IF NOT EXISTS public.pickingtask (
        "Id" character varying(36) NOT NULL,
        "TaskCode" character varying(32) NOT NULL,
        "StockOutRequestId" character varying(36) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "OperatorId" character varying(36) NOT NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_pickingtask" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_pickingtask_TaskCode" ON public.pickingtask ("TaskCode");

    CREATE TABLE IF NOT EXISTS public.pickingtaskitem (
        "Id" character varying(36) NOT NULL,
        "PickingTaskId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "StockId" character varying(36),
        "BatchNo" character varying(50),
        "LocationId" character varying(36),
        "PlanQty" numeric(18,4) NOT NULL DEFAULT 0,
        "PickedQty" numeric(18,4) NOT NULL DEFAULT 0,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_pickingtaskitem" PRIMARY KEY ("Id")
    );
    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_pickingtaskitem_pickingtask_PickingTaskId') THEN
            ALTER TABLE public.pickingtaskitem
                ADD CONSTRAINT "FK_pickingtaskitem_pickingtask_PickingTaskId"
                FOREIGN KEY ("PickingTaskId") REFERENCES public.pickingtask ("Id") ON DELETE CASCADE;
        END IF;
    END $$;

    CREATE TABLE IF NOT EXISTS public.inventorycountplan (
        "Id" character varying(36) NOT NULL,
        "PlanMonth" character varying(7) NOT NULL,
        "WarehouseId" character varying(36) NOT NULL,
        "CreatorId" character varying(36) NOT NULL,
        "SubmitterId" character varying(36),
        "Status" smallint NOT NULL DEFAULT 1,
        "Remark" character varying(500),
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_inventorycountplan" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_inventorycountplan_PlanMonth_WarehouseId" ON public.inventorycountplan ("PlanMonth", "WarehouseId");

    CREATE TABLE IF NOT EXISTS public.inventorycountitem (
        "Id" character varying(36) NOT NULL,
        "PlanId" character varying(36) NOT NULL,
        "MaterialId" character varying(36) NOT NULL,
        "LocationId" character varying(36),
        "BookQty" numeric(18,4) NOT NULL DEFAULT 0,
        "CountQty" numeric(18,4) NOT NULL DEFAULT 0,
        "DiffQty" numeric(18,4) NOT NULL DEFAULT 0,
        "BookAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "CountAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "DiffAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_inventorycountitem" PRIMARY KEY ("Id")
    );
    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_inventorycountitem_inventorycountplan_PlanId') THEN
            ALTER TABLE public.inventorycountitem
                ADD CONSTRAINT "FK_inventorycountitem_inventorycountplan_PlanId"
                FOREIGN KEY ("PlanId") REFERENCES public.inventorycountplan ("Id") ON DELETE CASCADE;
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324223000_AddInventoryCenterTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324223000_AddInventoryCenterTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325100000_SerialNumberPrefixesAndBusinessCodes') THEN

    ALTER TABLE IF EXISTS financereceipt
        ALTER COLUMN "FinanceReceiptCode" TYPE character varying(32);

    UPDATE sys_serial_number SET "Prefix" = 'QT' WHERE "ModuleCode" = 'Quotation';
    UPDATE sys_serial_number SET "Prefix" = 'SI' WHERE "ModuleCode" = 'StockIn';
    UPDATE sys_serial_number SET "Prefix" = 'SOUT' WHERE "ModuleCode" = 'StockOut';

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PurchaseRequisition', '采购申请', 'PR', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'PurchaseRequisition');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'StockOutRequest', '出库申请', 'SON', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'StockOutRequest');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PickingTask', '拣货任务', 'PK', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'PickingTask');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'ArrivalNotice', '到货通知', 'AN', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'ArrivalNotice');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'QcRecord', '质检', 'QC', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'QcRecord');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PaymentRequest', '请款', 'PRQ', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'PaymentRequest');

    INSERT INTO sys_serial_number ("Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime")
    SELECT (SELECT COALESCE(MAX("Id"), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'FinancePayment', '财务付款', 'FPY', NULL, false, false, 4, NULL
    WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s."ModuleCode" = 'FinancePayment');

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325100000_SerialNumberPrefixesAndBusinessCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325100000_SerialNumberPrefixesAndBusinessCodes', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325120000_AddStockOutRequestItem') THEN

    CREATE TABLE IF NOT EXISTS public.stockoutrequestitem (
        "UserId" character varying(36) NOT NULL,
        "StockOutRequestId" character varying(36) NOT NULL,
        "LineNo" integer NOT NULL,
        "MaterialCode" character varying(200) NOT NULL,
        "MaterialName" character varying(200),
        "Quantity" numeric(18,4) NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint,
        "ModifyTime" timestamp with time zone,
        "ModifyUserId" bigint,
        CONSTRAINT "PK_stockoutrequestitem" PRIMARY KEY ("UserId")
    );

    CREATE INDEX IF NOT EXISTS "IX_stockoutrequestitem_StockOutRequestId" ON public.stockoutrequestitem ("StockOutRequestId");

    DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockoutrequestitem_stockoutrequest_StockOutRequestId') THEN
            ALTER TABLE public.stockoutrequestitem
            ADD CONSTRAINT "FK_stockoutrequestitem_stockoutrequest_StockOutRequestId"
            FOREIGN KEY ("StockOutRequestId") REFERENCES public.stockoutrequest ("UserId") ON DELETE CASCADE;
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325120000_AddStockOutRequestItem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325120000_AddStockOutRequestItem', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325120000_EnsureSysParamAndDisplayTimeZone') THEN

    CREATE TABLE IF NOT EXISTS public.sysparamgroup (
        "GroupId" character varying(36) NOT NULL,
        "GroupCode" character varying(50) NOT NULL,
        "GroupName" character varying(100) NOT NULL,
        "ParentId" character varying(36) NULL,
        "Level" smallint NOT NULL DEFAULT 1,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "Description" character varying(200) NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_sysparamgroup" PRIMARY KEY ("GroupId")
    );

    CREATE TABLE IF NOT EXISTS public.sysparam (
        "ParamId" character varying(36) NOT NULL,
        "ParamCode" character varying(100) NOT NULL,
        "ParamName" character varying(200) NOT NULL,
        "GroupId" character varying(36) NULL,
        "DataType" smallint NOT NULL,
        "ValueString" character varying(500) NULL,
        "ValueInt" bigint NULL,
        "ValueDecimal" numeric(18,4) NULL,
        "ValueJson" text NULL,
        "DefaultValue" character varying(500) NULL,
        "Description" character varying(500) NULL,
        "IsArray" boolean NOT NULL DEFAULT FALSE,
        "IsSystem" boolean NOT NULL DEFAULT FALSE,
        "IsEditable" boolean NOT NULL DEFAULT TRUE,
        "IsVisible" boolean NOT NULL DEFAULT TRUE,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "Status" smallint NOT NULL DEFAULT 1,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_sysparam" PRIMARY KEY ("ParamId")
    );

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_sysparam_ParamCode" ON public.sysparam ("ParamCode");

    INSERT INTO public.sysparamgroup ("GroupId", "GroupCode", "GroupName", "ParentId", "Level", "SortOrder", "Description", "Status", "CreateTime")
    SELECT '00000000-0000-4000-8000-000000000001', 'System.Display', '显示与格式', NULL, 1, 0, '界面日期时间显示等', 1, NOW()
    WHERE NOT EXISTS (SELECT 1 FROM public.sysparamgroup WHERE "GroupId" = '00000000-0000-4000-8000-000000000001');

    INSERT INTO public.sysparam ("ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueString", "DefaultValue", "Description", "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime")
    SELECT '00000000-0000-4000-8000-000000000002', 'System.Display.TimeZoneId', '前端显示用 IANA 时区', '00000000-0000-4000-8000-000000000001', 1, 'Asia/Shanghai', 'Asia/Shanghai', '例如 Asia/Shanghai、UTC。全站展示日期时间按此时区格式化。', FALSE, TRUE, TRUE, TRUE, 0, 1, NOW()
    WHERE NOT EXISTS (SELECT 1 FROM public.sysparam WHERE "ParamCode" = 'System.Display.TimeZoneId');

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325120000_EnsureSysParamAndDisplayTimeZone') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325120000_EnsureSysParamAndDisplayTimeZone', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325183000_StockOutRequestFlattenSingleTable') THEN

    ALTER TABLE IF EXISTS public.stockoutrequest
      ADD COLUMN IF NOT EXISTS "SalesOrderItemId" character varying(36) NOT NULL DEFAULT '';
    ALTER TABLE IF EXISTS public.stockoutrequest
      ADD COLUMN IF NOT EXISTS "MaterialCode" character varying(200) NOT NULL DEFAULT '';
    ALTER TABLE IF EXISTS public.stockoutrequest
      ADD COLUMN IF NOT EXISTS "MaterialName" character varying(200);
    ALTER TABLE IF EXISTS public.stockoutrequest
      ADD COLUMN IF NOT EXISTS "Quantity" numeric(18,4) NOT NULL DEFAULT 0;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325183000_StockOutRequestFlattenSingleTable') THEN

    DO $BODY$
    BEGIN
      IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'stockoutrequestitem'
      ) THEN
        UPDATE public.stockoutrequest r
        SET
          "MaterialCode" = COALESCE(sub."MaterialCode", ''),
          "MaterialName" = sub."MaterialName",
          "Quantity" = sub."Quantity"
        FROM (
          SELECT DISTINCT ON ("StockOutRequestId")
            "StockOutRequestId",
            "LineNo",
            "MaterialCode",
            "MaterialName",
            "Quantity"
          FROM public.stockoutrequestitem
          ORDER BY "StockOutRequestId", "LineNo"
        ) AS sub
        WHERE r."UserId" = sub."StockOutRequestId";

        DROP TABLE IF EXISTS public.stockoutrequestitem CASCADE;
      END IF;
    END $BODY$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325183000_StockOutRequestFlattenSingleTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325183000_StockOutRequestFlattenSingleTable', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325200000_DropStockOutRequestSalesOrderLineNo') THEN
    ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS "SalesOrderLineNo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325200000_DropStockOutRequestSalesOrderLineNo') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325200000_DropStockOutRequestSalesOrderLineNo', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325210000_EnsureMaterialTablesIfMissing') THEN

    CREATE TABLE IF NOT EXISTS public.materialcategory (
        "CategoryId" character varying(36) NOT NULL,
        "CategoryCode" character varying(50) NOT NULL,
        "CategoryName" character varying(100) NOT NULL,
        "ParentId" character varying(36) NULL,
        "Level" smallint NOT NULL DEFAULT 1,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "Status" smallint NOT NULL DEFAULT 1,
        "Remark" character varying(500) NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_materialcategory" PRIMARY KEY ("CategoryId")
    );

    CREATE TABLE IF NOT EXISTS public.material (
        "MaterialId" character varying(36) NOT NULL,
        "MaterialCode" character varying(50) NOT NULL,
        "MaterialName" character varying(200) NOT NULL,
        "MaterialModel" character varying(100) NULL,
        "BrandId" character varying(36) NULL,
        "CategoryId" character varying(36) NULL,
        "Unit" character varying(20) NULL,
        "Status" smallint NOT NULL DEFAULT 1,
        "Remark" character varying(500) NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_material" PRIMARY KEY ("MaterialId")
    );

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_material_MaterialCode" ON public.material ("MaterialCode");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325210000_EnsureMaterialTablesIfMissing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325210000_EnsureMaterialTablesIfMissing', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325230000_RfqItemAssignedPurchasersAndSysParams') THEN

    ALTER TABLE IF EXISTS public.rfqitem
        ADD COLUMN IF NOT EXISTS assigned_purchaser_user_id_1 character varying(36) NULL;
    ALTER TABLE IF EXISTS public.rfqitem
        ADD COLUMN IF NOT EXISTS assigned_purchaser_user_id_2 character varying(36) NULL;

    INSERT INTO public.sysparam ("ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueString", "DefaultValue", "Description", "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime")
    SELECT '00000000-0000-4000-8000-000000000012', 'System.RFQ.RoundRobinPurchaserRoleCodes', '需求明细轮询采购员角色编码', (SELECT "GroupId" FROM public.sysparamgroup WHERE "GroupCode" = 'System.Display' LIMIT 1), 1, '', '', '逗号分隔 RBAC RoleCode，如 purchase_buyer。为空时后端按常见编码回退。', FALSE, TRUE, TRUE, TRUE, 10, 1, NOW()
    WHERE NOT EXISTS (SELECT 1 FROM public.sysparam p WHERE p."ParamCode" = 'System.RFQ.RoundRobinPurchaserRoleCodes');

    INSERT INTO public.sysparam ("ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueString", "DefaultValue", "Description", "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime")
    SELECT '00000000-0000-4000-8000-000000000013', 'System.RFQ.PurchaserRoundRobinCursor', '需求明细采购员轮询游标', (SELECT "GroupId" FROM public.sysparamgroup WHERE "GroupCode" = 'System.Display' LIMIT 1), 1, '0', '0', '非负整数，每分配一条明细的两个槽位后 +2。', FALSE, TRUE, TRUE, FALSE, 11, 1, NOW()
    WHERE NOT EXISTS (SELECT 1 FROM public.sysparam p WHERE p."ParamCode" = 'System.RFQ.PurchaserRoundRobinCursor');

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325230000_RfqItemAssignedPurchasersAndSysParams') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325230000_RfqItemAssignedPurchasersAndSysParams', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326090000_UnifyCurrencyCodes') THEN

    -- 1) Data migration: quoteitem.currency was historically 0-based (0..3).
    --    Convert to unified 1-based codes (1..4).
    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns c
            WHERE c.table_schema = 'public'
              AND c.table_name = 'quoteitem'
              AND c.column_name = 'currency'
        ) THEN
            UPDATE public.quoteitem
            SET "currency" = "currency" + 1
            WHERE "currency" BETWEEN 0 AND 3;
        END IF;
    END $$;

    -- 2) Column comments: align to unified currency codes everywhere.
    DO $$
    BEGIN
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='purchaseorder' AND column_name='currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.purchaseorder."currency" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='purchaseorderitem' AND column_name='currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.purchaseorderitem."currency" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='sellorder' AND column_name='currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.sellorder."currency" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='sellorderitem' AND column_name='currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.sellorderitem."currency" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='rfqitem' AND column_name='price_currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.rfqitem."price_currency" IS ''目标价币种：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='quoteitem' AND column_name='currency') THEN
            EXECUTE 'COMMENT ON COLUMN public.quoteitem."currency" IS ''报价币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326090000_UnifyCurrencyCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326090000_UnifyCurrencyCodes', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326103000_AddCustomerRegionColumns') THEN
    ALTER TABLE customerinfo ADD "Province" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326103000_AddCustomerRegionColumns') THEN
    ALTER TABLE customerinfo ADD "City" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326103000_AddCustomerRegionColumns') THEN
    ALTER TABLE customerinfo ADD "District" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326103000_AddCustomerRegionColumns') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326103000_AddCustomerRegionColumns', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod') THEN
    ALTER TABLE vendorinfo ADD "Remark" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod') THEN
    ALTER TABLE vendorinfo ADD "Website" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod') THEN
    ALTER TABLE vendorinfo ADD "PurchaserName" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod') THEN
    ALTER TABLE vendorinfo ADD "PaymentMethod" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326120000_AddVendorRemarkWebsitePaymentMethod', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults') THEN
    ALTER TABLE customerinfo ADD "AuditRemark" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults') THEN

    UPDATE public.customerinfo
    SET "Status" = CASE "Status"
        WHEN 1 THEN 10
        WHEN 0 THEN 1
        ELSE "Status"
    END;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults') THEN
    ALTER TABLE public.customerinfo ALTER COLUMN "Status" SET DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults') THEN
    ALTER TABLE vendorinfo ADD "AuditRemark" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults') THEN

    UPDATE public.vendorinfo
    SET "Status" = CASE "Status"
        WHEN 0 THEN 1
        WHEN 1 THEN 2
        WHEN 2 THEN 10
        ELSE "Status"
    END;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults') THEN
    ALTER TABLE public.vendorinfo ALTER COLUMN "Status" SET DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326203000_AddApprovalRecordTable') THEN
    CREATE TABLE approval_record (
        "Id" character varying(36) NOT NULL,
        "BizType" character varying(50) NOT NULL,
        "BusinessId" character varying(36) NOT NULL,
        "DocumentCode" character varying(64),
        "ActionType" character varying(20) NOT NULL,
        "FromStatus" smallint,
        "ToStatus" smallint,
        "SubmitRemark" character varying(500),
        "AuditRemark" character varying(500),
        "SubmitterUserId" character varying(36),
        "SubmitterUserName" character varying(100),
        "ApproverUserId" character varying(36),
        "ApproverUserName" character varying(100),
        "ActionTime" timestamp with time zone NOT NULL,
        "CreateTime" timestamp with time zone NOT NULL,
        "ModifyTime" timestamp with time zone,
        CONSTRAINT "PK_approval_record" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326203000_AddApprovalRecordTable') THEN
    CREATE INDEX "IX_approval_record_ActionTime" ON approval_record ("ActionTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326203000_AddApprovalRecordTable') THEN
    CREATE INDEX "IX_approval_record_BizType_BusinessId" ON approval_record ("BizType", "BusinessId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326203000_AddApprovalRecordTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326203000_AddApprovalRecordTable', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326212000_AddApprovalRecordItemDescription') THEN
    ALTER TABLE approval_record ADD "ItemDescription" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326212000_AddApprovalRecordItemDescription') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326212000_AddApprovalRecordItemDescription', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327120000_AddStockInNotifyExpectedArrivalDate') THEN

    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectedArrivalDate" timestamp with time zone NULL;
    COMMENT ON COLUMN public.stockinnotify."ExpectedArrivalDate" IS '预计到货日期（通知物流关注）';

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327120000_AddStockInNotifyExpectedArrivalDate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260327120000_AddStockInNotifyExpectedArrivalDate', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327173000_AddFinancePaymentBankSlipNo') THEN

    ALTER TABLE public.financepayment ADD COLUMN IF NOT EXISTS "BankSlipNo" character varying(100) NULL;
    COMMENT ON COLUMN public.financepayment."BankSlipNo" IS '银行水单号码';

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327173000_AddFinancePaymentBankSlipNo') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260327173000_AddFinancePaymentBankSlipNo', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328140000_EnsureCustomerOperationLogTables') THEN

    CREATE TABLE IF NOT EXISTS public.customer_operation_log (
        "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
        "CustomerId" TEXT NOT NULL,
        "OperationType" VARCHAR(100) NOT NULL,
        "OperationDesc" TEXT,
        "OperatorUserId" TEXT,
        "OperatorUserName" VARCHAR(100),
        "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
        "Remark" TEXT
    );
    CREATE INDEX IF NOT EXISTS idx_customer_operation_log_customer_id ON public.customer_operation_log("CustomerId");
    CREATE INDEX IF NOT EXISTS idx_customer_operation_log_operation_time ON public.customer_operation_log("OperationTime");

    CREATE TABLE IF NOT EXISTS public.customer_change_log (
        "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
        "CustomerId" TEXT NOT NULL,
        "FieldName" VARCHAR(100) NOT NULL,
        "FieldLabel" VARCHAR(200),
        "OldValue" TEXT,
        "NewValue" TEXT,
        "ChangedByUserId" TEXT,
        "ChangedByUserName" VARCHAR(100),
        "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
    );
    CREATE INDEX IF NOT EXISTS idx_customer_change_log_customer_id ON public.customer_change_log("CustomerId");
    CREATE INDEX IF NOT EXISTS idx_customer_change_log_changed_at ON public.customer_change_log("ChangedAt");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328140000_EnsureCustomerOperationLogTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260328140000_EnsureCustomerOperationLogTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    CREATE TABLE IF NOT EXISTS public.log_operation (
        "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
        "BizType" VARCHAR(64) NOT NULL,
        "RecordId" TEXT NOT NULL,
        "RecordCode" VARCHAR(128),
        "ActionType" VARCHAR(100) NOT NULL,
        "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
        "OperatorUserId" TEXT,
        "OperatorUserName" VARCHAR(100),
        "Reason" TEXT,
        "ExtraInfo" TEXT,
        "SysRemark" TEXT,
        "OperationDesc" TEXT
    );
    CREATE INDEX IF NOT EXISTS idx_log_operation_biz_record ON public.log_operation("BizType", "RecordId");
    CREATE INDEX IF NOT EXISTS idx_log_operation_time ON public.log_operation("OperationTime");

    CREATE TABLE IF NOT EXISTS public.log_change_fldval (
        "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
        "BizType" VARCHAR(64) NOT NULL,
        "RecordId" TEXT NOT NULL,
        "RecordCode" VARCHAR(128),
        "FieldName" VARCHAR(100) NOT NULL,
        "FieldLabel" VARCHAR(200),
        "OldValue" TEXT,
        "NewValue" TEXT,
        "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
        "ChangedByUserId" TEXT,
        "ChangedByUserName" VARCHAR(100),
        "ExtraInfo" TEXT,
        "SysRemark" TEXT
    );
    CREATE INDEX IF NOT EXISTS idx_log_change_fldval_biz_record ON public.log_change_fldval("BizType", "RecordId");
    CREATE INDEX IF NOT EXISTS idx_log_change_fldval_changed_at ON public.log_change_fldval("ChangedAt");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'customer_operation_log') THEN
        INSERT INTO public.log_operation ("Id", "BizType", "RecordId", "RecordCode", "ActionType", "OperationTime", "OperatorUserId", "OperatorUserName", "Reason", "ExtraInfo", "SysRemark", "OperationDesc")
        SELECT col."Id", 'Customer', col."CustomerId", c."CustomerCode", col."OperationType", col."OperationTime", col."OperatorUserId", col."OperatorUserName", col."Remark", NULL, NULL, col."OperationDesc"
        FROM public.customer_operation_log col
        LEFT JOIN public.customerinfo c ON c."CustomerId" = col."CustomerId"
        WHERE NOT EXISTS (SELECT 1 FROM public.log_operation o WHERE o."Id" = col."Id");
      END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'customer_change_log') THEN
        INSERT INTO public.log_change_fldval ("Id", "BizType", "RecordId", "RecordCode", "FieldName", "FieldLabel", "OldValue", "NewValue", "ChangedAt", "ChangedByUserId", "ChangedByUserName", "ExtraInfo", "SysRemark")
        SELECT ch."Id", 'Customer', ch."CustomerId", c."CustomerCode", ch."FieldName", ch."FieldLabel", ch."OldValue", ch."NewValue", ch."ChangedAt", ch."ChangedByUserId", ch."ChangedByUserName", NULL, NULL
        FROM public.customer_change_log ch
        LEFT JOIN public.customerinfo c ON c."CustomerId" = ch."CustomerId"
        WHERE NOT EXISTS (SELECT 1 FROM public.log_change_fldval x WHERE x."Id" = ch."Id");
      END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'vendor_operation_log') THEN
        INSERT INTO public.log_operation ("Id", "BizType", "RecordId", "RecordCode", "ActionType", "OperationTime", "OperatorUserId", "OperatorUserName", "Reason", "ExtraInfo", "SysRemark", "OperationDesc")
        SELECT v."Id", 'Vendor', v."VendorId", vi."Code", v."OperationType", v."OperationTime", v."OperatorUserId", v."OperatorUserName", v."Remark", NULL, NULL, v."OperationDesc"
        FROM public.vendor_operation_log v
        LEFT JOIN public.vendorinfo vi ON vi."VendorId" = v."VendorId"
        WHERE NOT EXISTS (SELECT 1 FROM public.log_operation o WHERE o."Id" = v."Id");
      END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    DO $$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'vendor_change_log') THEN
        INSERT INTO public.log_change_fldval ("Id", "BizType", "RecordId", "RecordCode", "FieldName", "FieldLabel", "OldValue", "NewValue", "ChangedAt", "ChangedByUserId", "ChangedByUserName", "ExtraInfo", "SysRemark")
        SELECT ch."Id", 'Vendor', ch."VendorId", vi."Code", ch."FieldName", ch."FieldLabel", ch."OldValue", ch."NewValue", ch."ChangedAt", ch."ChangedByUserId", ch."ChangedByUserName", NULL, NULL
        FROM public.vendor_change_log ch
        LEFT JOIN public.vendorinfo vi ON vi."VendorId" = ch."VendorId"
        WHERE NOT EXISTS (SELECT 1 FROM public.log_change_fldval x WHERE x."Id" = ch."Id");
      END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN

    DROP TABLE IF EXISTS public.customer_operation_log;
    DROP TABLE IF EXISTS public.customer_change_log;
    DROP TABLE IF EXISTS public.vendor_operation_log;
    DROP TABLE IF EXISTS public.vendor_change_log;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328210000_UnifiedLogOperationAndChangeFldval') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260328210000_UnifiedLogOperationAndChangeFldval', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328230000_AddLogRecentTable') THEN

    CREATE TABLE IF NOT EXISTS public.log_recent (
        "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
        "BizType" VARCHAR(64) NOT NULL,
        "RecordId" TEXT NOT NULL,
        "RecordCode" VARCHAR(128),
        "AccessedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
        "UserId" TEXT NOT NULL DEFAULT '',
        "OpenKind" VARCHAR(16) NOT NULL DEFAULT 'detail'
    );
    CREATE UNIQUE INDEX IF NOT EXISTS ux_log_recent_user_biz_record ON public.log_recent("UserId", "BizType", "RecordId");
    CREATE INDEX IF NOT EXISTS idx_log_recent_user_biz_time ON public.log_recent("UserId", "BizType", "AccessedAt" DESC);

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328230000_AddLogRecentTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260328230000_AddLogRecentTable', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329140000_StockInNotifySingleTableAndItemExtends') THEN

    -- 物流单表重构：清空旧主从数据（到货/质检）
    TRUNCATE TABLE public.qcitem RESTART IDENTITY CASCADE;
    TRUNCATE TABLE public.qcinfo RESTART IDENTITY CASCADE;
    TRUNCATE TABLE public.stockinnotifyitem RESTART IDENTITY CASCADE;
    TRUNCATE TABLE public.stockinnotify RESTART IDENTITY CASCADE;

    -- 到货通知：行级字段
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "PurchaseOrderItemId" character varying(36) NOT NULL DEFAULT '';
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "SellOrderItemId" character varying(36) NULL;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Pn" character varying(128) NULL;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Brand" character varying(64) NULL;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectQty" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ReceiveQty" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "PassedQty" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Cost" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectTotal" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ReceiveTotal" numeric(18,2) NOT NULL DEFAULT 0;

    ALTER TABLE public.stockinnotify ALTER COLUMN "PurchaseOrderItemId" DROP DEFAULT;

    DROP TABLE IF EXISTS public.stockinnotifyitem;

    -- 质检明细：关联到货单行
    DO $$
    BEGIN
      IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='qcitem' AND column_name='StockInNotifyItemId'
      ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='qcitem' AND column_name='ArrivalStockInNotifyId'
      ) THEN
        ALTER TABLE public.qcitem RENAME COLUMN "StockInNotifyItemId" TO "ArrivalStockInNotifyId";
      END IF;
    END $$;

    CREATE TABLE IF NOT EXISTS public.purchaseorderitemextend (
        "PurchaseOrderItemId" character varying(36) NOT NULL,
        "QtyStockInNotifyNot" numeric(18,4) NOT NULL DEFAULT 0,
        "QtyStockInNotifyExpectSum" numeric(18,4) NOT NULL DEFAULT 0,
        "QtyReceiveTotal" numeric(18,4) NOT NULL DEFAULT 0,
        "PurchaseInvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "PurchaseInvoiceDone" numeric(18,2) NOT NULL DEFAULT 0,
        "PurchaseInvoiceToBe" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_purchaseorderitemextend" PRIMARY KEY ("PurchaseOrderItemId")
    );

    CREATE TABLE IF NOT EXISTS public.sellorderitemextend (
        "SellOrderItemId" character varying(36) NOT NULL,
        "QtyStockOutNotify" numeric(18,4) NOT NULL DEFAULT 0,
        "QtyStockOutNotifyNot" numeric(18,4) NOT NULL DEFAULT 0,
        "InvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "InvoiceAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
        "InvoiceAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
        "ReceiptAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmountDone" numeric(18,2) NOT NULL DEFAULT 0,
        "PaymentAmountToBe" numeric(18,2) NOT NULL DEFAULT 0,
        "PurchaseInvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
        "PurchaseInvoiceDone" numeric(18,2) NOT NULL DEFAULT 0,
        "CreateTime" timestamp with time zone NOT NULL,
        "CreateUserId" bigint NULL,
        "ModifyTime" timestamp with time zone NULL,
        "ModifyUserId" bigint NULL,
        CONSTRAINT "PK_sellorderitemextend" PRIMARY KEY ("SellOrderItemId")
    );

    -- 已有采购/销售明细：补扩展行
    INSERT INTO public.purchaseorderitemextend (
      "PurchaseOrderItemId", "QtyStockInNotifyNot", "QtyStockInNotifyExpectSum", "QtyReceiveTotal",
      "PurchaseInvoiceAmount", "PurchaseInvoiceDone", "PurchaseInvoiceToBe",
      "PaymentAmount", "PaymentAmountNot", "PaymentAmountFinish",
      "ReceiptAmount", "ReceiptAmountNot", "ReceiptAmountFinish",
      "CreateTime"
    )
    SELECT
      i."PurchaseOrderItemId",
      i.qty,
      0,
      0,
      ROUND((i.qty * i.cost)::numeric, 2),
      0,
      ROUND((i.qty * i.cost)::numeric, 2),
      ROUND((i.qty * i.cost)::numeric, 2),
      ROUND((i.qty * i.cost)::numeric, 2),
      0,
      0,
      0,
      0,
      NOW() AT TIME ZONE 'utc'
    FROM public.purchaseorderitem i
    WHERE NOT EXISTS (
      SELECT 1 FROM public.purchaseorderitemextend e WHERE e."PurchaseOrderItemId" = i."PurchaseOrderItemId"
    );

    INSERT INTO public.sellorderitemextend (
      "SellOrderItemId", "QtyStockOutNotify", "QtyStockOutNotifyNot",
      "InvoiceAmount", "InvoiceAmountNot", "InvoiceAmountFinish",
      "ReceiptAmount", "ReceiptAmountNot", "ReceiptAmountFinish",
      "PaymentAmount", "PaymentAmountDone", "PaymentAmountToBe",
      "PurchaseInvoiceAmount", "PurchaseInvoiceDone",
      "CreateTime"
    )
    SELECT
      s."SellOrderItemId",
      0,
      s.qty,
      ROUND((s.qty * s.price)::numeric, 2),
      ROUND((s.qty * s.price)::numeric, 2),
      0,
      ROUND((s.qty * s.price)::numeric, 2),
      ROUND((s.qty * s.price)::numeric, 2),
      0,
      0,
      0,
      0,
      0,
      0,
      NOW() AT TIME ZONE 'utc'
    FROM public.sellorderitem s
    WHERE NOT EXISTS (
      SELECT 1 FROM public.sellorderitemextend e WHERE e."SellOrderItemId" = s."SellOrderItemId"
    );

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329140000_StockInNotifySingleTableAndItemExtends') THEN

    CREATE INDEX IF NOT EXISTS "IX_stockinnotify_PurchaseOrderItemId" ON public.stockinnotify ("PurchaseOrderItemId");
    CREATE INDEX IF NOT EXISTS "IX_stockinnotify_PurchaseOrderId" ON public.stockinnotify ("PurchaseOrderId");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329140000_StockInNotifySingleTableAndItemExtends') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260329140000_StockInNotifySingleTableAndItemExtends', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331120000_SerialNumberBase32RuleUpgrade') THEN

    -- 须先 UPDATE 再收缩 Prefix，否则 PAY_DEL 等 >4 字符会 22001
    UPDATE sys_serial_number SET "Prefix" = 'CUS'  WHERE "ModuleCode" = 'Customer';
    UPDATE sys_serial_number SET "Prefix" = 'VEN'  WHERE "ModuleCode" = 'Vendor';
    UPDATE sys_serial_number SET "Prefix" = 'RFQ'  WHERE "ModuleCode" = 'RFQ';
    UPDATE sys_serial_number SET "Prefix" = 'QUO'  WHERE "ModuleCode" = 'Quotation';
    UPDATE sys_serial_number SET "Prefix" = 'SOR'  WHERE "ModuleCode" = 'SalesOrder';
    UPDATE sys_serial_number SET "Prefix" = 'PUR'  WHERE "ModuleCode" = 'PurchaseOrder';
    UPDATE sys_serial_number SET "Prefix" = 'STI'  WHERE "ModuleCode" = 'StockIn';
    UPDATE sys_serial_number SET "Prefix" = 'SOUT' WHERE "ModuleCode" = 'StockOut';
    UPDATE sys_serial_number SET "Prefix" = 'REC'  WHERE "ModuleCode" = 'Receipt';
    UPDATE sys_serial_number SET "Prefix" = 'PAY'  WHERE "ModuleCode" = 'Payment';
    UPDATE sys_serial_number SET "Prefix" = 'INVI' WHERE "ModuleCode" = 'InputInvoice';
    UPDATE sys_serial_number SET "Prefix" = 'OUTI' WHERE "ModuleCode" = 'OutputInvoice';
    UPDATE sys_serial_number SET "Prefix" = 'STK'  WHERE "ModuleCode" = 'Stock';
    UPDATE sys_serial_number SET "Prefix" = 'PRQ'  WHERE "ModuleCode" = 'PurchaseRequisition';
    UPDATE sys_serial_number SET "Prefix" = 'SORQ' WHERE "ModuleCode" = 'StockOutRequest';
    UPDATE sys_serial_number SET "Prefix" = 'PKT'  WHERE "ModuleCode" = 'PickingTask';
    UPDATE sys_serial_number SET "Prefix" = 'ARN'  WHERE "ModuleCode" = 'ArrivalNotice';
    UPDATE sys_serial_number SET "Prefix" = 'QCR'  WHERE "ModuleCode" = 'QcRecord';
    UPDATE sys_serial_number SET "Prefix" = 'PMR'  WHERE "ModuleCode" = 'PaymentRequest';
    UPDATE sys_serial_number SET "Prefix" = 'FNP'  WHERE "ModuleCode" = 'FinancePayment';

    ALTER TABLE IF EXISTS sys_serial_number
        ALTER COLUMN "Prefix" TYPE character varying(4);

    -- 新规则固定5位数值位，不再按年月重置
    UPDATE sys_serial_number SET
        "SequenceLength" = 5,
        "ResetByYear" = FALSE,
        "ResetByMonth" = FALSE,
        "LastResetYear" = NULL,
        "LastResetMonth" = NULL;

    -- 客户/供应商从0开始（CurrentSequence = -1 后首次生成即 00000）
    UPDATE sys_serial_number
    SET "CurrentSequence" = -1
    WHERE "ModuleCode" IN ('Customer', 'Vendor') AND "CurrentSequence" < 0;

    -- 其他业务从十进制2026开始（CurrentSequence = 2025 后首次生成即 2026）
    UPDATE sys_serial_number
    SET "CurrentSequence" = 2025
    WHERE "ModuleCode" NOT IN ('Customer', 'Vendor') AND "CurrentSequence" < 2025;

    DO $$
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_sys_serial_number_Prefix'
        ) THEN
            CREATE UNIQUE INDEX "IX_sys_serial_number_Prefix" ON sys_serial_number ("Prefix");
        END IF;
    END $$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331120000_SerialNumberBase32RuleUpgrade') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331120000_SerialNumberBase32RuleUpgrade', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401130000_UpdateSysSerialPrefixes202604') THEN

    ALTER TABLE IF EXISTS sys_serial_number
        ALTER COLUMN "Prefix" TYPE character varying(16);

    UPDATE sys_serial_number SET "Prefix" = 'CUS'     WHERE "ModuleCode" = 'Customer';
    UPDATE sys_serial_number SET "Prefix" = 'VEN'     WHERE "ModuleCode" = 'Vendor';
    UPDATE sys_serial_number SET "Prefix" = 'RFQ'     WHERE "ModuleCode" = 'RFQ';
    UPDATE sys_serial_number SET "Prefix" = 'QUO'     WHERE "ModuleCode" = 'Quotation';
    UPDATE sys_serial_number SET "Prefix" = 'SO'      WHERE "ModuleCode" = 'SalesOrder';
    UPDATE sys_serial_number SET "Prefix" = 'PO'      WHERE "ModuleCode" = 'PurchaseOrder';
    UPDATE sys_serial_number SET "Prefix" = 'STI'     WHERE "ModuleCode" = 'StockIn';
    UPDATE sys_serial_number SET "Prefix" = 'STO'     WHERE "ModuleCode" = 'StockOut';
    UPDATE sys_serial_number SET "Prefix" = 'REC'     WHERE "ModuleCode" = 'Receipt';
    UPDATE sys_serial_number SET "Prefix" = 'PAY_DEL' WHERE "ModuleCode" = 'Payment';
    UPDATE sys_serial_number SET "Prefix" = 'INVI'    WHERE "ModuleCode" = 'InputInvoice';
    UPDATE sys_serial_number SET "Prefix" = 'INVO'    WHERE "ModuleCode" = 'OutputInvoice';
    UPDATE sys_serial_number SET "Prefix" = 'STK'     WHERE "ModuleCode" = 'Stock';
    UPDATE sys_serial_number SET "Prefix" = 'POR'     WHERE "ModuleCode" = 'PurchaseRequisition';
    UPDATE sys_serial_number SET "Prefix" = 'STOR'    WHERE "ModuleCode" = 'StockOutRequest';
    UPDATE sys_serial_number SET "Prefix" = 'PAK'     WHERE "ModuleCode" = 'PickingTask';
    UPDATE sys_serial_number SET "Prefix" = 'STIR'    WHERE "ModuleCode" = 'ArrivalNotice';
    UPDATE sys_serial_number SET "Prefix" = 'QC'      WHERE "ModuleCode" = 'QcRecord';
    UPDATE sys_serial_number SET "Prefix" = 'PAYR'    WHERE "ModuleCode" = 'PaymentRequest';
    UPDATE sys_serial_number SET "Prefix" = 'PAY'     WHERE "ModuleCode" = 'FinancePayment';

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401130000_UpdateSysSerialPrefixes202604') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260401130000_UpdateSysSerialPrefixes202604', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401140000_UnitPriceNumeric18Scale6') THEN

    DO $BODY$
    BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockinitem') THEN
        ALTER TABLE public.stockinitem ALTER COLUMN "Price" TYPE numeric(18,6);
      END IF;
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutitem') THEN
        ALTER TABLE public.stockoutitem ALTER COLUMN "Price" TYPE numeric(18,6);
      END IF;
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'rfqitem') THEN
        ALTER TABLE public.rfqitem ALTER COLUMN target_price TYPE numeric(18,6);
      END IF;
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoiceitem') THEN
        ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN "StockInCost" TYPE numeric(18,6);
        ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN "BillCost" TYPE numeric(18,6);
      END IF;
      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'sellinvoiceitem') THEN
        ALTER TABLE public.sellinvoiceitem ALTER COLUMN "Price" TYPE numeric(18,6);
      END IF;
    END
    $BODY$;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401140000_UnitPriceNumeric18Scale6') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260401140000_UnitPriceNumeric18Scale6', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402120000_AddCustomerVendorEnglishOfficialName') THEN
    ALTER TABLE customerinfo ADD "EnglishOfficialName" character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402120000_AddCustomerVendorEnglishOfficialName') THEN
    ALTER TABLE vendorinfo ADD "EnglishOfficialName" character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402120000_AddCustomerVendorEnglishOfficialName') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260402120000_AddCustomerVendorEnglishOfficialName', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    CREATE TABLE IF NOT EXISTS public.sys_dict_item (
        "Id" character varying(36) NOT NULL,
        "Category" character varying(64) NOT NULL,
        "ItemCode" character varying(64) NOT NULL,
        "NameZh" character varying(200) NOT NULL,
        "NameEn" character varying(200) NULL,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "IsActive" boolean NOT NULL DEFAULT true,
        "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
        CONSTRAINT "PK_sys_dict_item" PRIMARY KEY ("Id")
    );
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_dict_item_Category_ItemCode" ON public.sys_dict_item ("Category", "ItemCode");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'Semiconductors', '半导体', 'Semiconductors', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'Semiconductors');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'TestMeasurement', '测试和测量', 'Test & measurement', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'TestMeasurement');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'CircuitProtection', '电路保护', 'Circuit protection', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'CircuitProtection');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'WiresCables', '电线及电缆', 'Wires & cables', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'WiresCables');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'CathodePower', '负极、电源', 'Cathode / power', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'CathodePower');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'ToolsEquipment', '工具及设备', 'Tools & equipment', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ToolsEquipment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'IndustrialControl', '工控', 'Industrial control', 7, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'IndustrialControl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'MechatronicEncoders', '机电编码器', 'Electromechanical encoders', 8, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'MechatronicEncoders');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'ComputerPeripheralsMech', '计算机外设、机电', 'Computer peripherals & electromechanics', 9, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ComputerPeripheralsMech');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'StructuralParts', '结构件', 'Structural parts', 10, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'StructuralParts');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'DevKitsTools', '开发套件和工具', 'Development kits & tools', 11, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'DevKitsTools');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'ThermalManagement', '热管理', 'Thermal management', 12, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'ThermalManagement');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'NetworkCommDevices', '网络通讯器件', 'Network communication devices', 13, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'NetworkCommDevices');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'DisplayMarket', '显示市场', 'Display market', 14, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'DisplayMarket');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'IGUS', 'IGUS', 'IGUS', 15, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'IGUS');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIndustry', 'LedLightingOptoDisplay', 'LED照明、光电设备及显示器', 'LED lighting, optoelectronics & displays', 16, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIndustry' AND d."ItemCode" = 'LedLightingOptoDisplay');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '1', '1-', '1-', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '2', '1', '1', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '3', '1+', '1+', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '4', '2-', '2-', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '5', '2', '2', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '5');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '6', '2+', '2+', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '6');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '7', '3-', '3-', 7, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '7');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '8', '3', '3', 8, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '8');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '9', '3+', '3+', 9, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '10', 'A', 'A', 10, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '10');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '11', 'B', 'B', 11, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '12', 'C', 'C', 12, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '12');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorLevel', '13', 'D', 'D', 13, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '13');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '1', '目录商', 'Catalog vendor', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '2', '货代', 'Freight forwarder', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '3', '原厂', 'Original manufacturer', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '4', 'EMS工厂', 'EMS factory', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '5', '代理', 'Agent', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '5');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '6', 'IDH', 'IDH', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '6');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '7', '渠道商', 'Channel partner', 7, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '7');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '8', '现货贸易商', 'Spot trader', 8, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '8');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '9', '电商', 'E-commerce', 9, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorIdentity', '10', '制造商', 'Manufacturer', 10, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorIdentity' AND d."ItemCode" = '10');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Prepaid', '预付款', 'Prepaid', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Prepaid');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'COD', '货到付款', 'COD', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'COD');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Monthly', '月结', 'Monthly', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Monthly');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'Credit', '账期', 'Credit', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'Credit');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'TT', '电汇', 'Wire T/T', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'TT');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'VendorPaymentMethod', 'LC', '信用证', 'L/C', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorPaymentMethod' AND d."ItemCode" = 'LC');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_AddSysDictItemVendorSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260403120000_AddSysDictItemVendorSeed', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_FinanceExchangeRateTables') THEN

    CREATE TABLE financeexchangeratesetting (
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

    CREATE TABLE financeexchangeratechangelog (
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

    CREATE INDEX "IX_financeexchangeratechangelog_CreateTime" ON financeexchangeratechangelog ("CreateTime");

    INSERT INTO financeexchangeratesetting ("FinanceExchangeRateSettingId", "UsdToCny", "UsdToHkd", "UsdToEur", "EditorUserId", "EditorUserName", "CreateTime", "ModifyTime")
    VALUES ('00000000-0000-4000-8000-0000000000E1', 6.9194, 7.8367, 0.8725, NULL, NULL, NOW() AT TIME ZONE 'UTC', NULL);

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403120000_FinanceExchangeRateTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260403120000_FinanceExchangeRateTables', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403140000_AddOrderItemConvertPrice') THEN

    ALTER TABLE public.sellorderitem
      ADD COLUMN IF NOT EXISTS convert_price numeric(18,6) NOT NULL DEFAULT 0;

    ALTER TABLE public.purchaseorderitem
      ADD COLUMN IF NOT EXISTS convert_price numeric(18,6) NOT NULL DEFAULT 0;

    UPDATE public.sellorderitem AS i
    SET convert_price = CASE i.currency
      WHEN 2 THEN round(i.price, 6)
      WHEN 1 THEN CASE WHEN f."UsdToCny" > 0 THEN round(i.price / f."UsdToCny", 6) ELSE 0 END
      WHEN 3 THEN CASE WHEN f."UsdToEur" > 0 THEN round(i.price / f."UsdToEur", 6) ELSE 0 END
      WHEN 4 THEN CASE WHEN f."UsdToHkd" > 0 THEN round(i.price / f."UsdToHkd", 6) ELSE 0 END
      ELSE 0
    END
    FROM public.financeexchangeratesetting AS f
    WHERE f."FinanceExchangeRateSettingId" = '00000000-0000-4000-8000-0000000000E1';

    UPDATE public.purchaseorderitem AS i
    SET convert_price = CASE i.currency
      WHEN 2 THEN round(i.cost, 6)
      WHEN 1 THEN CASE WHEN f."UsdToCny" > 0 THEN round(i.cost / f."UsdToCny", 6) ELSE 0 END
      WHEN 3 THEN CASE WHEN f."UsdToEur" > 0 THEN round(i.cost / f."UsdToEur", 6) ELSE 0 END
      WHEN 4 THEN CASE WHEN f."UsdToHkd" > 0 THEN round(i.cost / f."UsdToHkd", 6) ELSE 0 END
      ELSE 0
    END
    FROM public.financeexchangeratesetting AS f
    WHERE f."FinanceExchangeRateSettingId" = '00000000-0000-4000-8000-0000000000E1';

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403140000_AddOrderItemConvertPrice') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260403140000_AddOrderItemConvertPrice', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '1', 'OEM', 'OEM', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '2', 'ODM', 'ODM', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '3', '终端', 'End user', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '4', 'IDH', 'IDH', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '5', '贸易商', 'Trader', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '5');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '6', '代理商', 'Agent', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '6');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '7', 'EMS', 'EMS', 7, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '7');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '8', '非行业', 'Non-industry', 8, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '8');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '9', '科研机构', 'Research', 9, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '10', '供应链', 'Supply chain', 10, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '10');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerType', '11', '原厂', 'Original factory', 11, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerType' AND d."ItemCode" = '11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'D', 'D级', 'Grade D', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'D');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'C', 'C级', 'Grade C', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'C');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'B', 'B级', 'Grade B', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'B');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'BPO', 'BPO', 'BPO', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'BPO');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'VIP', 'VIP', 'VIP', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'VIP');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerLevel', 'VPO', 'VPO', 'VPO', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerLevel' AND d."ItemCode" = 'VPO');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'FinanceEquipment', '金融设备', 'Financial equipment', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'FinanceEquipment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Telecom', '通讯', 'Telecom', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Telecom');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'RailTransit', '轨道交通', 'Rail transit', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'RailTransit');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Aerospace', '航空航天', 'Aerospace', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Aerospace');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'CyberSecurity', '网络安全', 'Cyber security', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'CyberSecurity');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Esports', '电竞', 'Esports', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Esports');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerSupply', '电源', 'Power supply', 7, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerSupply');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ElectronicComponentsTrading', '电子元器件贸易', 'EC trading', 8, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ElectronicComponentsTrading');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ElectronicComponentsManufacturing', '电子元器件制造', 'EC manufacturing', 9, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ElectronicComponentsManufacturing');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerTools', '电动工具', 'Power tools', 10, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerTools');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'PowerElectrical', '电力电气', 'Power & electrical', 11, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'PowerElectrical');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'IoT', '物联网', 'IoT', 12, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'IoT');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ConsumerElectronics', '消费电子', 'Consumer electronics', 13, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ConsumerElectronics');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Robotics', '机器人', 'Robotics', 14, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Robotics');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'SmartSecurity', '智能安防', 'Smart security', 15, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'SmartSecurity');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'SmartCity', '智慧城市', 'Smart city', 16, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'SmartCity');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'UAV', '无人机', 'UAV', 17, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'UAV');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'NewEnergyVehicles', '新能源汽车', 'NEV', 18, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'NewEnergyVehicles');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'NewEnergy', '新能源', 'New energy', 19, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'NewEnergy');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'IndustrialControl', '工业控制', 'Industrial control', 20, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'IndustrialControl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'MedicalEquipment', '医疗设备', 'Medical equipment', 21, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'MedicalEquipment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'DefenseMilitary', '军工', 'Defense', 22, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'DefenseMilitary');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'TraditionalVehicles', '传统车辆', 'Traditional vehicles', 23, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'TraditionalVehicles');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Instrumentation', '仪器仪表', 'Instrumentation', 24, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Instrumentation');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'ArtificialIntelligence', '人工智能', 'AI', 25, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'ArtificialIntelligence');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'CloudComputingIDC', '云计算IDC', 'Cloud / IDC', 26, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'CloudComputingIDC');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Manufacturing', '制造业', 'Manufacturing', 27, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Manufacturing');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Trading', '贸易/零售', 'Trading / retail', 28, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Trading');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Technology', '科技/IT', 'Technology / IT', 29, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Technology');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Construction', '建筑/工程', 'Construction', 30, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Construction');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Healthcare', '医疗/健康', 'Healthcare', 31, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Healthcare');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Education', '教育', 'Education', 32, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Education');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Finance', '金融', 'Finance', 33, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Finance');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerIndustry', 'Other', '其他', 'Other', 34, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerIndustry' AND d."ItemCode" = 'Other');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '0', '0%', '0%', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '1', '1%', '1%', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '3', '3%', '3%', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '6', '6%', '6%', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '6');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '9', '9%', '9%', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerTaxRate', '13', '13%', '13%', 6, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerTaxRate' AND d."ItemCode" = '13');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '0', '无需开票', 'No invoice', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '1', '增值税专用发票', 'VAT special invoice', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '2', '增值税普通发票', 'VAT ordinary invoice', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'CustomerInvoiceType', '3', '电子发票', 'E-invoice', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'CustomerInvoiceType' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404130000_AddSysDictItemCustomerSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404130000_AddSysDictItemCustomerSeed', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404150000_AddLogOrderJourney') THEN

    CREATE TABLE IF NOT EXISTS public.log_orderjourney (
        "Id" character varying(36) NOT NULL,
        "EntityKind" character varying(32) NOT NULL,
        "EntityId" character varying(36) NOT NULL,
        "ParentEntityKind" character varying(32),
        "ParentEntityId" character varying(36),
        "DocumentCode" character varying(64),
        "LineHint" character varying(200),
        "EventCode" character varying(64) NOT NULL,
        "EventLabel" character varying(200),
        "FromState" character varying(32),
        "ToState" character varying(32),
        "EventTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
        "Quantity" numeric(18,4),
        "Amount" numeric(18,6),
        "Currency" smallint,
        "Remark" character varying(500),
        "PayloadJson" text,
        "RelatedEntityKind" character varying(32),
        "RelatedEntityId" character varying(36),
        "ActorKind" character varying(16),
        "ActorUserId" character varying(36),
        "ActorUserName" character varying(100),
        "ActorVendorId" character varying(36),
        "Source" character varying(64),
        "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
        "ModifyTime" timestamp with time zone,
        CONSTRAINT "PK_log_orderjourney" PRIMARY KEY ("Id")
    );

    CREATE INDEX IF NOT EXISTS "IX_log_orderjourney_entity"
        ON public.log_orderjourney ("EntityKind", "EntityId", "EventTime");
    CREATE INDEX IF NOT EXISTS "IX_log_orderjourney_parent"
        ON public.log_orderjourney ("ParentEntityKind", "ParentEntityId", "EventTime");
    CREATE INDEX IF NOT EXISTS "IX_log_orderjourney_document_code"
        ON public.log_orderjourney ("DocumentCode", "EventTime")
        WHERE "DocumentCode" IS NOT NULL;
    CREATE INDEX IF NOT EXISTS "IX_log_orderjourney_event"
        ON public.log_orderjourney ("EventCode", "EventTime");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404150000_AddLogOrderJourney') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404150000_AddLogOrderJourney', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405120000_RemoveLegacyVendorIndustryDictItems') THEN

    DELETE FROM public.sys_dict_item
    WHERE "Category" = 'VendorIndustry'
      AND "ItemCode" IN (
        'Electronics','Machinery','Chemical','Textile','Food',
        'Construction','Trading','Technology','Healthcare','Other'
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405120000_RemoveLegacyVendorIndustryDictItems') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260405120000_RemoveLegacyVendorIndustryDictItems', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405120000_SellOrderItemExtendProgressP0') THEN

    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "QtyAlreadyPurchased" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "QtyNotPurchase" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "QtyStockOutActual" numeric(18,4) NOT NULL DEFAULT 0;

    ALTER TABLE IF EXISTS public.financereceiptitem
        ADD COLUMN IF NOT EXISTS "VerifiedAmount" numeric(18,2) NOT NULL DEFAULT 0;

    -- 历史：已核销完成的收款明细，累计核销额视为全额
    UPDATE public.financereceiptitem
    SET "VerifiedAmount" = "ReceiptConvertAmount"
    WHERE "VerificationStatus" = 2 AND ("VerifiedAmount" IS NULL OR "VerifiedAmount" = 0);

    -- 采购进度 + 主表 PurchasedQty（purchaseorderitem 列为 sell_order_item_id / qty；sellorderitem 为 purchased_qty / qty）
    UPDATE public.sellorderitem i
    SET purchased_qty = COALESCE(sub.sum_po, 0)
    FROM (
        SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
        FROM public.purchaseorderitem
        GROUP BY sell_order_item_id
    ) sub
    WHERE sub.sell_order_item_id = i."SellOrderItemId";

    UPDATE public.sellorderitemextend e
    SET
        "QtyAlreadyPurchased" = COALESCE(po.sum_po, 0),
        "QtyNotPurchase" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(po.sum_po, 0))
    FROM public.sellorderitem i
    LEFT JOIN (
        SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
        FROM public.purchaseorderitem
        GROUP BY sell_order_item_id
    ) po ON po.sell_order_item_id = i."SellOrderItemId"
    WHERE e."SellOrderItemId" = i."SellOrderItemId";

    -- 出库通知进度（Status<>2 计入已占用通知量）
    UPDATE public.sellorderitemextend e
    SET
        "QtyStockOutNotify" = COALESCE(nr.sum_q, 0),
        "QtyStockOutNotifyNot" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(nr.sum_q, 0))
    FROM public.sellorderitem i
    LEFT JOIN (
        SELECT "SalesOrderItemId" AS sid, SUM("Quantity")::numeric AS sum_q
        FROM public.stockoutrequest
        WHERE "Status" IS DISTINCT FROM 2
        GROUP BY "SalesOrderItemId"
    ) nr ON nr.sid = i."SellOrderItemId"
    WHERE e."SellOrderItemId" = i."SellOrderItemId";

    -- 实出数量：已出库通知(Status=1) 且 存在已确认出库单(SourceId=通知主键, Status=2)
    UPDATE public.sellorderitemextend e
    SET "QtyStockOutActual" = COALESCE(sa.sum_out, 0)
    FROM public.sellorderitem i
    LEFT JOIN (
        SELECT r."SalesOrderItemId" AS sid, SUM(so."TotalQuantity")::numeric AS sum_out
        FROM public.stockoutrequest r
        INNER JOIN public.stockout so
            ON so."SourceId" = r."UserId" AND so."Status" = 2
        WHERE r."Status" = 1
        GROUP BY r."SalesOrderItemId"
    ) sa ON sa.sid = i."SellOrderItemId"
    WHERE e."SellOrderItemId" = i."SellOrderItemId";

    -- 收款核销汇总到扩展表
    UPDATE public.sellorderitemextend e
    SET
        "ReceiptAmountFinish" = COALESCE(rv.sum_v, 0),
        "ReceiptAmountNot" = GREATEST(0::numeric, e."ReceiptAmount" - COALESCE(rv.sum_v, 0))
    FROM public.sellorderitem i
    LEFT JOIN (
        SELECT "SellOrderItemId" AS sid, SUM("VerifiedAmount")::numeric AS sum_v
        FROM public.financereceiptitem
        WHERE "SellOrderItemId" IS NOT NULL AND BTRIM("SellOrderItemId") <> ''
        GROUP BY "SellOrderItemId"
    ) rv ON rv.sid = i."SellOrderItemId"
    WHERE e."SellOrderItemId" = i."SellOrderItemId";

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405120000_SellOrderItemExtendProgressP0') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260405120000_SellOrderItemExtendProgressP0', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406100000_SellOrderItemExtendProfitP1P3') THEN

    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteItemId" character varying(36);
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteCost" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteCurrency" smallint NOT NULL DEFAULT 1;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteConvertCost" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToCnySnapshot" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToHkdSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToEurSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SellConvertUsdUnitSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SellLineAmountUsdSnapshot" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ReQuoteProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ReQuoteProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PoCostUsdTotal" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PoCostUsdConfirmed" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SalesProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutBizUsd" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutRateBiz" numeric(18,6) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutFinUsd" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutRateFin" numeric(18,6) NOT NULL DEFAULT 0;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406100000_SellOrderItemExtendProfitP1P3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260406100000_SellOrderItemExtendProfitP1P3', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406120000_AddSysDictItemMaterialProductionDateSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'MaterialProductionDate', '1', '2年内', 'Within 2 years', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406120000_AddSysDictItemMaterialProductionDateSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'MaterialProductionDate', '2', '1年内', 'Within 1 year', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406120000_AddSysDictItemMaterialProductionDateSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'MaterialProductionDate', '3', '无要求', 'No requirement', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406120000_AddSysDictItemMaterialProductionDateSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'MaterialProductionDate', '4', '25+', '25+', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'MaterialProductionDate' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260406120000_AddSysDictItemMaterialProductionDateSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260406120000_AddSysDictItemMaterialProductionDateSeed', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407120000_PurchaseOrderItemExtendProgressStatus') THEN

    ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProgressQty" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "StockInProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PaymentProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "InvoiceProgressStatus" smallint NOT NULL DEFAULT 0;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407120000_PurchaseOrderItemExtendProgressStatus') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260407120000_PurchaseOrderItemExtendProgressStatus', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '1', '送货', 'Delivery', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '2', '自提', 'Self pickup', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '3', '快递', 'Express courier', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsArrivalMethod', '4', '物流', 'Freight / logistics', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsArrivalMethod' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '1', 'UPS', 'UPS', 1, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '2', 'FEDEX', 'FedEx', 2, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '3', 'DHL', 'DHL', 3, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '4', '顺丰', 'SF Express', 4, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN

    INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
    SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '5', '跨越', 'KYE Express', 5, true, NOW() AT TIME ZONE 'utc'
    WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'LogisticsExpressMethod' AND d."ItemCode" = '5');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407140000_AddSysDictItemLogisticsSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260407140000_AddSysDictItemLogisticsSeed', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408100000_SellOrderItemExtendProgressStatuses') THEN

    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "PurchaseProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "StockInProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "StockOutProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "ReceiptProgressStatus" smallint NOT NULL DEFAULT 0;
    ALTER TABLE IF EXISTS public.sellorderitemextend
        ADD COLUMN IF NOT EXISTS "InvoiceProgressStatus" smallint NOT NULL DEFAULT 0;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408100000_SellOrderItemExtendProgressStatuses') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260408100000_SellOrderItemExtendProgressStatuses', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409120000_SellOrderPurchaseOrderExtendAndLineCodes') THEN

    CREATE TABLE IF NOT EXISTS public.sellorderextend (
      "SellOrderId" character varying(36) NOT NULL,
      last_item_line_seq integer NOT NULL DEFAULT 0,
      "CreateTime" timestamp with time zone NOT NULL,
      "ModifyTime" timestamp with time zone NULL,
      CONSTRAINT "PK_sellorderextend" PRIMARY KEY ("SellOrderId"),
      CONSTRAINT "FK_sellorderextend_sellorder" FOREIGN KEY ("SellOrderId")
        REFERENCES public.sellorder ("SellOrderId") ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS public.purchaseorderextend (
      "PurchaseOrderId" character varying(36) NOT NULL,
      last_item_line_seq integer NOT NULL DEFAULT 0,
      "CreateTime" timestamp with time zone NOT NULL,
      "ModifyTime" timestamp with time zone NULL,
      CONSTRAINT "PK_purchaseorderextend" PRIMARY KEY ("PurchaseOrderId"),
      CONSTRAINT "FK_purchaseorderextend_purchaseorder" FOREIGN KEY ("PurchaseOrderId")
        REFERENCES public.purchaseorder ("PurchaseOrderId") ON DELETE CASCADE
    );

    ALTER TABLE IF EXISTS public.sellorderitem
        ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
    ALTER TABLE IF EXISTS public.purchaseorderitem
        ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;

    INSERT INTO public.sellorderextend ("SellOrderId", last_item_line_seq, "CreateTime")
    SELECT so."SellOrderId", 0, NOW()
    FROM public.sellorder so
    WHERE NOT EXISTS (
      SELECT 1 FROM public.sellorderextend x WHERE x."SellOrderId" = so."SellOrderId");

    INSERT INTO public.purchaseorderextend ("PurchaseOrderId", last_item_line_seq, "CreateTime")
    SELECT po."PurchaseOrderId", 0, NOW()
    FROM public.purchaseorder po
    WHERE NOT EXISTS (
      SELECT 1 FROM public.purchaseorderextend x WHERE x."PurchaseOrderId" = po."PurchaseOrderId");

    WITH numbered AS (
      SELECT si."SellOrderItemId",
        so.sell_order_code,
        ROW_NUMBER() OVER (PARTITION BY si.sell_order_id ORDER BY si."CreateTime", si."SellOrderItemId") AS rn
      FROM public.sellorderitem si
      INNER JOIN public.sellorder so ON so."SellOrderId" = si.sell_order_id
    )
    UPDATE public.sellorderitem u
    SET sell_order_item_code = n.sell_order_code || '-' || n.rn::text
    FROM numbered n
    WHERE u."SellOrderItemId" = n."SellOrderItemId";

    WITH numbered AS (
      SELECT pi."PurchaseOrderItemId",
        po.purchase_order_code,
        ROW_NUMBER() OVER (PARTITION BY pi.purchase_order_id ORDER BY pi."CreateTime", pi."PurchaseOrderItemId") AS rn
      FROM public.purchaseorderitem pi
      INNER JOIN public.purchaseorder po ON po."PurchaseOrderId" = pi.purchase_order_id
    )
    UPDATE public.purchaseorderitem u
    SET purchase_order_item_code = n.purchase_order_code || '-' || n.rn::text
    FROM numbered n
    WHERE u."PurchaseOrderItemId" = n."PurchaseOrderItemId";

    UPDATE public.sellorderextend e
    SET last_item_line_seq = COALESCE((
      SELECT COUNT(*)::integer FROM public.sellorderitem si WHERE si.sell_order_id = e."SellOrderId"), 0);

    UPDATE public.purchaseorderextend e
    SET last_item_line_seq = COALESCE((
      SELECT COUNT(*)::integer FROM public.purchaseorderitem pi WHERE pi.purchase_order_id = e."PurchaseOrderId"), 0);

    ALTER TABLE public.sellorderitem ALTER COLUMN sell_order_item_code SET NOT NULL;
    ALTER TABLE public.purchaseorderitem ALTER COLUMN purchase_order_item_code SET NOT NULL;

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_sellorderitem_order_linecode"
      ON public.sellorderitem (sell_order_id, sell_order_item_code);
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_purchaseorderitem_order_linecode"
      ON public.purchaseorderitem (purchase_order_id, purchase_order_item_code);

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409120000_SellOrderPurchaseOrderExtendAndLineCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260409120000_SellOrderPurchaseOrderExtendAndLineCodes', '9.0.11');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260421100000_AddPickingTaskItemStockingSupplement') THEN
    ALTER TABLE IF EXISTS public.pickingtaskitem ADD COLUMN IF NOT EXISTS "IsStockingSupplement" boolean NOT NULL DEFAULT false;
    COMMENT ON COLUMN public.pickingtaskitem."IsStockingSupplement" IS '备货补充拣货：true=按销单行型号品牌匹配的备货库存';
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260421100000_AddPickingTaskItemStockingSupplement', '9.0.11');
    END IF;
END $EF$;
COMMIT;

