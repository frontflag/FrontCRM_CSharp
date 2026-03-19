-- =============================================
-- 远程数据库更新脚本
-- 服务器: 129.226.161.3
-- 数据库: PostgreSQL (Docker)
-- 密码: adm@FF#1720
-- =============================================

-- 开始事务
BEGIN;

-- =============================================
-- 0. 创建迁移历史表和基础表
-- =============================================

-- 创建迁移历史表
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
    "ProductVersion" character varying(32) NOT NULL
);

-- =============================================
-- 0.1 创建基础表 (InitialCreate - 20260316063302)
-- =============================================

-- user 表
CREATE TABLE IF NOT EXISTS "user" (
    "UserId" character varying(36) NOT NULL PRIMARY KEY,
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
    "ModifyUserId" bigint
);

-- customerinfo 表
CREATE TABLE IF NOT EXISTS customerinfo (
    "CustomerId" character varying(36) NOT NULL PRIMARY KEY,
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
    "ModifyUserId" bigint
);

-- customercontacthistory 表
CREATE TABLE IF NOT EXISTS customercontacthistory (
    "HistoryId" character varying(36) NOT NULL PRIMARY KEY,
    "CustomerId" character varying(36) NOT NULL,
    "Type" character varying(50) NOT NULL,
    "Content" character varying(500),
    "Time" timestamp with time zone NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint,
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" bigint
);

-- customeraddress 表
CREATE TABLE IF NOT EXISTS customeraddress (
    "AddressId" character varying(36) NOT NULL PRIMARY KEY,
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
    CONSTRAINT "FK_customeraddress_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo("CustomerId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_customeraddress_CustomerId" ON customeraddress("CustomerId");

-- customerbankinfo 表
CREATE TABLE IF NOT EXISTS customerbankinfo (
    "BankId" character varying(36) NOT NULL PRIMARY KEY,
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
    CONSTRAINT "FK_customerbankinfo_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo("CustomerId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_customerbankinfo_CustomerId" ON customerbankinfo("CustomerId");

-- customercontactinfo 表
CREATE TABLE IF NOT EXISTS customercontactinfo (
    "ContactId" character varying(36) NOT NULL PRIMARY KEY,
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
    CONSTRAINT "FK_customercontactinfo_customerinfo_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES customerinfo("CustomerId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_customercontactinfo_CustomerId" ON customercontactinfo("CustomerId");

-- =============================================
-- 1. component_cache 表 (20260316095624_AddComponentCache)
-- =============================================
CREATE TABLE IF NOT EXISTS component_cache (
    Id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    Mpn character varying(100) NOT NULL,
    ManufacturerName character varying(200),
    ShortDescription character varying(500),
    Description text,
    LifecycleStatus character varying(50),
    PackageType character varying(100),
    IsRoHSCompliant boolean,
    SpecsJson text,
    SellersJson text,
    AlternativesJson text,
    ApplicationsJson text,
    PriceTrendJson text,
    NewsJson text,
    DataSource character varying(50) NOT NULL,
    FetchedAt timestamp with time zone NOT NULL,
    CreateTime timestamp with time zone NOT NULL,
    UpdateTime timestamp with time zone,
    QueryCount integer NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_component_cache_Mpn ON component_cache (Mpn);

-- =============================================
-- 2. sys_serial_number 表 (20260316100000_AddSysTables)
-- =============================================
CREATE TABLE IF NOT EXISTS sys_serial_number (
    Id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    ModuleCode character varying(50) NOT NULL,
    ModuleName character varying(100) NOT NULL,
    Prefix character varying(10) NOT NULL,
    SequenceLength integer NOT NULL DEFAULT 4,
    CurrentSequence integer NOT NULL DEFAULT 0,
    ResetByYear boolean NOT NULL DEFAULT false,
    ResetByMonth boolean NOT NULL DEFAULT false,
    LastResetYear integer,
    LastResetMonth integer,
    Remark character varying(200),
    CreateTime timestamp with time zone NOT NULL,
    UpdateTime timestamp with time zone
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_sys_serial_number_ModuleCode ON sys_serial_number (ModuleCode);

-- 插入种子数据
INSERT INTO sys_serial_number (Id, ModuleCode, ModuleName, Prefix, SequenceLength, CurrentSequence, ResetByYear, ResetByMonth, CreateTime)
VALUES 
    (1, 'Customer', '客户', 'Cus', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (2, 'Vendor', '供应商', 'Ven', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (3, 'Inquiry', '询价/需求', 'INQ', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (4, 'Quotation', '报价', 'QUO', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (5, 'SalesOrder', '销售订单', 'SO', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (6, 'PurchaseOrder', '采购订单', 'PO', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (7, 'StockIn', '入库', 'SIN', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (8, 'StockOut', '出库', 'SOUT', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (9, 'Inventory', '库存调整', 'INV', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (10, 'Receipt', '收款', 'REC', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (11, 'Payment', '付款', 'PAY', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (12, 'InputInvoice', '进项发票', 'VINV', 4, 0, false, false, '2026-01-01 00:00:00+00'),
    (13, 'OutputInvoice', '销项发票', 'SINV', 4, 0, false, false, '2026-01-01 00:00:00+00')
ON CONFLICT (Id) DO NOTHING;

-- =============================================
-- 3. sys_error_log 表 (20260316100000_AddSysTables)
-- =============================================
CREATE TABLE IF NOT EXISTS sys_error_log (
    Id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    OccurredAt timestamp with time zone NOT NULL,
    ModuleName character varying(100) NOT NULL,
    OperationType character varying(50),
    ErrorMessage character varying(500) NOT NULL,
    ErrorDetail text,
    DocumentNo character varying(50),
    DataId character varying(36),
    UserId character varying(36),
    UserName character varying(50),
    RequestPath character varying(200),
    RequestBody text,
    IsResolved boolean NOT NULL DEFAULT false,
    ResolveRemark character varying(200)
);

CREATE INDEX IF NOT EXISTS IX_sys_error_log_OccurredAt ON sys_error_log (OccurredAt);
CREATE INDEX IF NOT EXISTS IX_sys_error_log_IsResolved ON sys_error_log (IsResolved);

-- =============================================
-- 4. 供应商相关表 (20260317000000_AddVendorAndInventoryTables)
-- =============================================

-- vendor 表
CREATE TABLE IF NOT EXISTS vendor (
    Id character varying(36) NOT NULL PRIMARY KEY,
    Code character varying(16) NOT NULL,
    OfficialName character varying(64),
    NickName character varying(64),
    Level smallint NOT NULL,
    Type smallint NOT NULL,
    Status smallint NOT NULL DEFAULT 1,
    CreditLine numeric(18,2) NOT NULL,
    Payment smallint NOT NULL,
    TradeCurrency character varying(10),
    TaxRate numeric(5,2) NOT NULL,
    UniformNumber character varying(20),
    IsDeleted boolean NOT NULL DEFAULT false,
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId character varying(36),
    ModifyTime timestamp with time zone,
    ModifyUserId character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_vendor_Code ON vendor (Code);

-- vendor_address 表
CREATE TABLE IF NOT EXISTS vendor_address (
    Id character varying(36) NOT NULL PRIMARY KEY,
    vendor_id character varying(36) NOT NULL,
    address_type smallint NOT NULL,
    country character varying(50),
    province character varying(50),
    city character varying(50),
    area character varying(50),
    address character varying(200),
    contact_name character varying(50),
    contact_phone character varying(20),
    is_default boolean NOT NULL,
    remark character varying(500),
    create_time timestamp with time zone NOT NULL,
    create_user_id character varying(36),
    modify_time timestamp with time zone,
    modify_user_id character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_vendor_address_vendor_id ON vendor_address (vendor_id);

-- vendorcontactinfo 表
CREATE TABLE IF NOT EXISTS vendorcontactinfo (
    Id character varying(36) NOT NULL PRIMARY KEY,
    vendor_id character varying(36) NOT NULL,
    c_name character varying(50),
    e_name character varying(100),
    title character varying(50),
    department character varying(50),
    mobile character varying(20),
    tel character varying(30),
    email character varying(100),
    qq character varying(20),
    we_chat character varying(50),
    is_main boolean NOT NULL,
    remark character varying(500),
    create_time timestamp with time zone NOT NULL,
    create_user_id character varying(36),
    modify_time timestamp with time zone,
    modify_user_id character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_vendorcontactinfo_vendor_id ON vendorcontactinfo (vendor_id);

-- vendorbankinfo 表
CREATE TABLE IF NOT EXISTS vendorbankinfo (
    Id character varying(36) NOT NULL PRIMARY KEY,
    vendor_id character varying(36) NOT NULL,
    bank_name character varying(100),
    bank_account character varying(50),
    account_name character varying(50),
    bank_branch character varying(100),
    currency character varying(10),
    is_default boolean NOT NULL,
    remark character varying(500),
    create_time timestamp with time zone NOT NULL,
    create_user_id character varying(36),
    modify_time timestamp with time zone,
    modify_user_id character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_vendorbankinfo_vendor_id ON vendorbankinfo (vendor_id);

-- =============================================
-- 5. 库存相关表 (20260317000000_AddVendorAndInventoryTables)
-- =============================================

-- stock 表
CREATE TABLE IF NOT EXISTS stock (
    "StockId" character varying(36) NOT NULL PRIMARY KEY,
    material_id character varying(36) NOT NULL,
    warehouse_id character varying(36) NOT NULL,
    location_id character varying(36),
    quantity numeric(18,4) NOT NULL,
    available_quantity numeric(18,4) NOT NULL,
    locked_quantity numeric(18,4) NOT NULL,
    unit character varying(20),
    batch_no character varying(50),
    production_date timestamp with time zone,
    expiry_date timestamp with time zone,
    status smallint NOT NULL DEFAULT 1,
    remark character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_stock_material_id ON stock (material_id);
CREATE INDEX IF NOT EXISTS IX_stock_warehouse_id ON stock (warehouse_id);

-- stockin 表
CREATE TABLE IF NOT EXISTS stockin (
    "StockInId" character varying(36) NOT NULL PRIMARY KEY,
    stock_in_code character varying(32) NOT NULL,
    stock_in_type smallint NOT NULL,
    source_code character varying(32),
    warehouse_id character varying(36) NOT NULL,
    vendor_id character varying(36),
    stock_in_date timestamp with time zone NOT NULL,
    total_quantity numeric(18,4) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    status smallint NOT NULL,
    remark character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_stockin_stock_in_code ON stockin (stock_in_code);

-- stockinitem 表
CREATE TABLE IF NOT EXISTS stockinitem (
    "ItemId" character varying(36) NOT NULL PRIMARY KEY,
    stock_in_id character varying(36) NOT NULL,
    material_id character varying(36) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    price numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    location_id character varying(36),
    batch_no character varying(50),
    production_date timestamp with time zone,
    expiry_date timestamp with time zone,
    remark character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_stockinitem_stock_in_id ON stockinitem (stock_in_id);

-- stockout 表
CREATE TABLE IF NOT EXISTS stockout (
    "StockOutId" character varying(36) NOT NULL PRIMARY KEY,
    stock_out_code character varying(32) NOT NULL,
    stock_out_type smallint NOT NULL,
    source_code character varying(32),
    warehouse_id character varying(36) NOT NULL,
    customer_id character varying(36),
    stock_out_date timestamp with time zone NOT NULL,
    total_quantity numeric(18,4) NOT NULL,
    total_amount numeric(18,2) NOT NULL,
    status smallint NOT NULL,
    remark character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_stockout_stock_out_code ON stockout (stock_out_code);

-- stockoutitem 表
CREATE TABLE IF NOT EXISTS stockoutitem (
    "ItemId" character varying(36) NOT NULL PRIMARY KEY,
    stock_out_id character varying(36) NOT NULL,
    material_id character varying(36) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    price numeric(18,4) NOT NULL,
    amount numeric(18,2) NOT NULL,
    location_id character varying(36),
    batch_no character varying(50),
    remark character varying(500),
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId character varying(36),
    ModifyTime timestamp with time zone,
    ModifyUserId character varying(36)
);

CREATE INDEX IF NOT EXISTS IX_stockoutitem_StockOutId ON stockoutitem (StockOutId);

-- stockoutrequest 表
CREATE TABLE IF NOT EXISTS stockoutrequest (
    Id character varying(36) NOT NULL PRIMARY KEY,
    RequestCode character varying(50) NOT NULL,
    SalesOrderId character varying(36),
    CustomerId character varying(36),
    RequestUserId character varying(36),
    RequestDate timestamp with time zone NOT NULL,
    TotalQuantity numeric(18,4) NOT NULL,
    Status smallint NOT NULL,
    Remark character varying(500),
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId character varying(36),
    ModifyTime timestamp with time zone,
    ModifyUserId character varying(36)
);

-- =============================================
-- 6. customercontacthistory 扩展字段 (20260317024250_AddContactHistoryFields)
-- =============================================
ALTER TABLE customercontacthistory 
    ADD COLUMN IF NOT EXISTS ContactPerson character varying(100),
    ADD COLUMN IF NOT EXISTS NextFollowUpTime timestamp with time zone,
    ADD COLUMN IF NOT EXISTS OperatorId character varying(36),
    ADD COLUMN IF NOT EXISTS Result character varying(500),
    ADD COLUMN IF NOT EXISTS Subject character varying(200);

-- =============================================
-- 7. customerinfo 黑名单字段 (20260317145231_AddRFQTables)
-- =============================================
ALTER TABLE customerinfo 
    ADD COLUMN IF NOT EXISTS BlackListAt timestamp with time zone,
    ADD COLUMN IF NOT EXISTS BlackListByUserId character varying(36),
    ADD COLUMN IF NOT EXISTS BlackListByUserName character varying(64),
    ADD COLUMN IF NOT EXISTS BlackListReason character varying(500),
    ADD COLUMN IF NOT EXISTS DeleteReason character varying(500),
    ADD COLUMN IF NOT EXISTS DeletedByUserName character varying(64);

-- =============================================
-- 8. RFQ 相关表 (20260317145231_AddRFQTables)
-- =============================================

-- rfq 表
CREATE TABLE IF NOT EXISTS rfq (
    rfq_id character varying(36) NOT NULL PRIMARY KEY,
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
    rfq_date timestamp with time zone NOT NULL DEFAULT NOW(),
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId bigint,
    ModifyTime timestamp with time zone,
    ModifyUserId bigint
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_rfq_rfq_code ON rfq (rfq_code);

-- rfqitem 表
CREATE TABLE IF NOT EXISTS rfqitem (
    item_id character varying(36) NOT NULL PRIMARY KEY,
    rfq_id character varying(36) NOT NULL,
    line_no integer NOT NULL,
    customer_mpn character varying(100),
    mpn character varying(200) NOT NULL,
    customer_brand character varying(100) NOT NULL,
    brand character varying(100) NOT NULL,
    target_price numeric(18,4),
    price_currency smallint NOT NULL,
    quantity numeric(18,4) NOT NULL DEFAULT 1,
    production_date character varying(50),
    expiry_date timestamp with time zone,
    min_package_qty numeric(18,4),
    moq numeric(18,4),
    alternatives character varying(500),
    remark character varying(500),
    status smallint NOT NULL,
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId bigint,
    ModifyTime timestamp with time zone,
    ModifyUserId bigint,
    CONSTRAINT FK_rfqitem_rfq_rfq_id FOREIGN KEY (rfq_id) REFERENCES rfq(rfq_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_rfqitem_rfq_id ON rfqitem (rfq_id);

-- =============================================
-- 9. Quote 相关表 (20260317162715_AddQuoteTables)
-- =============================================

-- quote 表
CREATE TABLE IF NOT EXISTS quote (
    QuoteId character varying(36) NOT NULL PRIMARY KEY,
    quote_code character varying(32) NOT NULL,
    rfq_id character varying(36),
    rfq_item_id character varying(36),
    mpn character varying(200),
    customer_id character varying(36),
    sales_user_id character varying(36),
    purchase_user_id character varying(36),
    quote_date timestamp with time zone NOT NULL DEFAULT NOW(),
    status smallint NOT NULL DEFAULT 0,
    remark character varying(1000),
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId bigint,
    ModifyTime timestamp with time zone,
    ModifyUserId bigint
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_quote_quote_code ON quote (quote_code);

-- quoteitem 表
CREATE TABLE IF NOT EXISTS quoteitem (
    QuoteItemId character varying(36) NOT NULL PRIMARY KEY,
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
    quantity numeric(18,4) NOT NULL DEFAULT 0,
    unit_price numeric(18,6) NOT NULL DEFAULT 0,
    converted_price numeric(18,6),
    min_package_qty integer NOT NULL,
    min_package_unit character varying(50),
    stock_qty integer NOT NULL,
    moq integer NOT NULL,
    remark character varying(500),
    status smallint NOT NULL,
    CreateTime timestamp with time zone NOT NULL,
    CreateUserId bigint,
    ModifyTime timestamp with time zone,
    ModifyUserId bigint,
    CONSTRAINT FK_quoteitem_quote_quote_id FOREIGN KEY (quote_id) REFERENCES quote(QuoteId) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_quoteitem_quote_id ON quoteitem (quote_id);

-- =============================================
-- 10. 销售订单和采购订单表 (20260317170015_AddSalesAndPurchaseOrderTables)
-- =============================================

-- sellorder 表
CREATE TABLE IF NOT EXISTS sellorder (
    "SellOrderId" character varying(36) NOT NULL PRIMARY KEY,
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
    "ModifyUserId" bigint
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_sellorder_sell_order_code ON sellorder (sell_order_code);

-- sellorderitem 表
CREATE TABLE IF NOT EXISTS sellorderitem (
    "SellOrderItemId" character varying(36) NOT NULL PRIMARY KEY,
    sell_order_id character varying(36) NOT NULL,
    quote_id character varying(36),
    product_id character varying(36),
    pn character varying(200),
    brand character varying(200),
    customer_pn_no character varying(200),
    qty numeric(18,4) NOT NULL DEFAULT 0,
    price numeric(18,6) NOT NULL DEFAULT 0,
    cost numeric(18,6) NOT NULL DEFAULT 0,
    amount numeric(18,2) NOT NULL DEFAULT 0,
    currency smallint NOT NULL,
    comment character varying(500),
    status smallint NOT NULL,
    purchased_qty numeric(18,4) NOT NULL DEFAULT 0,
    stock_out_qty numeric(18,4) NOT NULL DEFAULT 0,
    stock_in_qty numeric(18,4) NOT NULL DEFAULT 0,
    receipt_amount numeric(18,2) NOT NULL DEFAULT 0,
    payment_amount numeric(18,2) NOT NULL DEFAULT 0,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint,
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" bigint,
    CONSTRAINT FK_sellorderitem_sellorder_sell_order_id FOREIGN KEY (sell_order_id) REFERENCES sellorder("SellOrderId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_sellorderitem_sell_order_id ON sellorderitem (sell_order_id);

-- purchaseorder 表
CREATE TABLE IF NOT EXISTS purchaseorder (
    "PurchaseOrderId" character varying(36) NOT NULL PRIMARY KEY,
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
    "ModifyUserId" bigint
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_purchaseorder_purchase_order_code ON purchaseorder (purchase_order_code);

-- purchaseorderitem 表
CREATE TABLE IF NOT EXISTS purchaseorderitem (
    "PurchaseOrderItemId" character varying(36) NOT NULL PRIMARY KEY,
    purchase_order_id character varying(36) NOT NULL,
    sell_order_item_id character varying(36) NOT NULL,
    product_id character varying(36),
    pn character varying(200),
    brand character varying(200),
    qty numeric(18,4) NOT NULL DEFAULT 0,
    cost numeric(18,6) NOT NULL DEFAULT 0,
    amount numeric(18,2) NOT NULL DEFAULT 0,
    currency smallint NOT NULL,
    comment character varying(500),
    status smallint NOT NULL,
    stock_in_qty numeric(18,4) NOT NULL DEFAULT 0,
    stock_out_qty numeric(18,4) NOT NULL DEFAULT 0,
    payment_amount numeric(18,2) NOT NULL DEFAULT 0,
    receipt_amount numeric(18,2) NOT NULL DEFAULT 0,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint,
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" bigint,
    CONSTRAINT FK_purchaseorderitem_purchaseorder_purchase_order_id FOREIGN KEY (purchase_order_id) REFERENCES purchaseorder("PurchaseOrderId") ON DELETE CASCADE,
    CONSTRAINT FK_purchaseorderitem_sellorderitem_sell_order_item_id FOREIGN KEY (sell_order_item_id) REFERENCES sellorderitem("SellOrderItemId") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_purchaseorderitem_purchase_order_id ON purchaseorderitem (purchase_order_id);
CREATE INDEX IF NOT EXISTS IX_purchaseorderitem_sell_order_item_id ON purchaseorderitem (sell_order_item_id);

-- =============================================
-- 11. 更新 __EFMigrationsHistory 表
-- =============================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20260316063302_InitialCreate', '8.0.2'),
    ('20260316095624_AddComponentCache', '8.0.2'),
    ('20260316100000_AddSysTables', '8.0.2'),
    ('20260317000000_AddVendorAndInventoryTables', '8.0.2'),
    ('20260317024250_AddContactHistoryFields', '8.0.2'),
    ('20260317030000_AddCustomerBlacklistFields', '8.0.2'),
    ('20260317145231_AddRFQTables', '8.0.2'),
    ('20260317162715_AddQuoteTables', '8.0.2'),
    ('20260317170015_AddSalesAndPurchaseOrderTables', '8.0.2')
ON CONFLICT ("MigrationId") DO NOTHING;

-- 提交事务
COMMIT;

-- 验证安装
SELECT '数据库更新完成！' AS message;
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;
