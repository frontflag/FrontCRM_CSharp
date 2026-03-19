-- =============================================
-- FrontCRM 数据库迁移脚本
-- 手动执行以应用所有待处理的迁移
-- =============================================

-- 1. 创建物料缓存表
CREATE TABLE IF NOT EXISTS component_cache (
    id BIGSERIAL PRIMARY KEY,
    mpn VARCHAR(100) NOT NULL UNIQUE,
    manufacturer_name VARCHAR(200),
    category VARCHAR(200),
    description TEXT,
    specs_json TEXT,
    sellers_json TEXT,
    images_json TEXT,
    datasheet_urls TEXT,
    source VARCHAR(50) DEFAULT 'Octopart',
    query_count INTEGER DEFAULT 1,
    last_query_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    cache_expiry TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_component_cache_mpn ON component_cache(mpn);
CREATE INDEX IF NOT EXISTS idx_component_cache_expiry ON component_cache(cache_expiry);

-- 2. 创建流水号管理表
CREATE TABLE IF NOT EXISTS sys_serial_number (
    id SERIAL PRIMARY KEY,
    module_code VARCHAR(50) NOT NULL UNIQUE,
    module_name VARCHAR(100),
    prefix VARCHAR(10) NOT NULL,
    sequence_length INTEGER DEFAULT 4,
    current_sequence INTEGER DEFAULT 0,
    reset_by_year BOOLEAN DEFAULT FALSE,
    reset_by_month BOOLEAN DEFAULT FALSE,
    last_reset_year INTEGER,
    last_reset_month INTEGER,
    remark VARCHAR(200),
    create_time TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP WITHOUT TIME ZONE
);

-- 插入流水号种子数据
INSERT INTO sys_serial_number (module_code, module_name, prefix, sequence_length, current_sequence)
VALUES 
    ('Customer', '客户管理', 'C', 4, 0),
    ('Vendor', '供应商管理', 'V', 4, 0),
    ('Inquiry', '询价管理', 'INQ', 4, 0),
    ('Quotation', '报价管理', 'QUO', 4, 0),
    ('SalesOrder', '销售订单', 'SO', 4, 0),
    ('PurchaseOrder', '采购订单', 'PO', 4, 0),
    ('StockIn', '入库管理', 'SI', 4, 0),
    ('StockOut', '出库管理', 'SO', 4, 0),
    ('Inventory', '库存调整', 'INV', 4, 0),
    ('Receipt', '收款管理', 'REC', 4, 0),
    ('Payment', '付款管理', 'PAY', 4, 0),
    ('InputInvoice', '进项发票', 'II', 4, 0),
    ('OutputInvoice', '销项发票', 'OI', 4, 0)
ON CONFLICT (module_code) DO NOTHING;

-- 3. 创建错误日志表
CREATE TABLE IF NOT EXISTS sys_error_log (
    id BIGSERIAL PRIMARY KEY,
    occurred_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    module_name VARCHAR(100),
    error_message VARCHAR(500) NOT NULL,
    error_detail TEXT,
    stack_trace TEXT,
    document_no VARCHAR(50),
    data_id VARCHAR(50),
    user_id VARCHAR(50),
    user_name VARCHAR(50),
    ip_address VARCHAR(50),
    request_url VARCHAR(500),
    request_method VARCHAR(10),
    request_params TEXT,
    is_resolved BOOLEAN DEFAULT FALSE,
    resolved_at TIMESTAMP WITHOUT TIME ZONE,
    resolved_by VARCHAR(50),
    remarks TEXT
);

CREATE INDEX IF NOT EXISTS idx_error_log_occurred_at ON sys_error_log(occurred_at);
CREATE INDEX IF NOT EXISTS idx_error_log_module ON sys_error_log(module_name);
CREATE INDEX IF NOT EXISTS idx_error_log_resolved ON sys_error_log(is_resolved);

-- 4. 创建供应商表
CREATE TABLE IF NOT EXISTS vendor (
    id BIGSERIAL PRIMARY KEY,
    vendor_no VARCHAR(20) NOT NULL UNIQUE,
    vendor_name VARCHAR(200) NOT NULL,
    short_name VARCHAR(100),
    vendor_type VARCHAR(50),
    vendor_level VARCHAR(20) DEFAULT '普通',
    business_scope VARCHAR(500),
    main_products VARCHAR(500),
    country VARCHAR(100),
    province VARCHAR(100),
    city VARCHAR(100),
    district VARCHAR(100),
    address VARCHAR(500),
    zip_code VARCHAR(20),
    contact_name VARCHAR(100),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(100),
    fax VARCHAR(50),
    website VARCHAR(200),
    tax_no VARCHAR(50),
    bank_name VARCHAR(200),
    bank_account VARCHAR(50),
    account_name VARCHAR(100),
    credit_limit DECIMAL(18,2) DEFAULT 0,
    credit_period INTEGER DEFAULT 0,
    payment_terms VARCHAR(100),
    delivery_terms VARCHAR(100),
    purchase_amount DECIMAL(18,2) DEFAULT 0,
    paid_amount DECIMAL(18,2) DEFAULT 0,
    payable_amount DECIMAL(18,2) DEFAULT 0,
    total_orders INTEGER DEFAULT 0,
    total_transactions INTEGER DEFAULT 0,
    first_transaction_date TIMESTAMP WITHOUT TIME ZONE,
    last_transaction_date TIMESTAMP WITHOUT TIME ZONE,
    rating INTEGER,
    status VARCHAR(20) DEFAULT '潜在',
    is_blacklisted BOOLEAN DEFAULT FALSE,
    blacklist_reason VARCHAR(500),
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP WITHOUT TIME ZONE,
    deleted_by VARCHAR(50),
    remarks TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50),
    updated_by VARCHAR(50)
);

-- 5. 创建供应商地址表
CREATE TABLE IF NOT EXISTS vendor_address (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    address_type VARCHAR(20) NOT NULL DEFAULT '办公',
    country VARCHAR(100) DEFAULT '中国',
    province VARCHAR(100),
    city VARCHAR(100),
    district VARCHAR(100),
    address VARCHAR(500) NOT NULL,
    zip_code VARCHAR(20),
    contact_name VARCHAR(100),
    contact_phone VARCHAR(50),
    is_default BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 6. 创建供应商联系人表
CREATE TABLE IF NOT EXISTS vendorcontactinfo (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    contact_name VARCHAR(100) NOT NULL,
    gender VARCHAR(10),
    department VARCHAR(100),
    position VARCHAR(100),
    phone VARCHAR(50),
    mobile VARCHAR(50),
    email VARCHAR(100),
    qq VARCHAR(50),
    wechat VARCHAR(50),
    is_primary BOOLEAN DEFAULT FALSE,
    is_decision_maker BOOLEAN DEFAULT FALSE,
    remarks VARCHAR(500),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 7. 创建供应商银行账户表
CREATE TABLE IF NOT EXISTS vendorbankinfo (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    account_name VARCHAR(200) NOT NULL,
    bank_name VARCHAR(200) NOT NULL,
    bank_branch VARCHAR(200),
    account_no VARCHAR(50) NOT NULL,
    currency VARCHAR(10) DEFAULT 'CNY',
    is_default BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 8. 创建库存主表
CREATE TABLE IF NOT EXISTS stock (
    id BIGSERIAL PRIMARY KEY,
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    location_id BIGINT,
    location_name VARCHAR(100),
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) DEFAULT 0,
    locked_quantity DECIMAL(18,4) DEFAULT 0,
    available_quantity DECIMAL(18,4) DEFAULT 0,
    unit_cost DECIMAL(18,4) DEFAULT 0,
    total_cost DECIMAL(18,4) DEFAULT 0,
    min_stock DECIMAL(18,4) DEFAULT 0,
    max_stock DECIMAL(18,4) DEFAULT 0,
    last_in_time TIMESTAMP WITHOUT TIME ZONE,
    last_out_time TIMESTAMP WITHOUT TIME ZONE,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_stock_warehouse ON stock(warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_item ON stock(item_id);
CREATE INDEX IF NOT EXISTS idx_stock_batch ON stock(batch_no);

-- 9. 创建入库单主表
CREATE TABLE IF NOT EXISTS stockin (
    id BIGSERIAL PRIMARY KEY,
    stock_in_no VARCHAR(50) NOT NULL UNIQUE,
    stock_in_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    supplier_id BIGINT,
    supplier_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    stock_in_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 10. 创建入库单明细表
CREATE TABLE IF NOT EXISTS stockinitem (
    id BIGSERIAL PRIMARY KEY,
    stock_in_id BIGINT NOT NULL REFERENCES stockin(id) ON DELETE CASCADE,
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) NOT NULL,
    unit_price DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    warehouse_id BIGINT,
    location_id BIGINT,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 11. 创建出库单主表
CREATE TABLE IF NOT EXISTS stockout (
    id BIGSERIAL PRIMARY KEY,
    stock_out_no VARCHAR(50) NOT NULL UNIQUE,
    stock_out_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    customer_id BIGINT,
    customer_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    stock_out_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 12. 创建出库单明细表
CREATE TABLE IF NOT EXISTS stockoutitem (
    id BIGSERIAL PRIMARY KEY,
    stock_out_id BIGINT NOT NULL REFERENCES stockout(id) ON DELETE CASCADE,
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) NOT NULL,
    unit_cost DECIMAL(18,4) DEFAULT 0,
    total_cost DECIMAL(18,2) DEFAULT 0,
    warehouse_id BIGINT,
    location_id BIGINT,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 13. 创建出库申请单表
CREATE TABLE IF NOT EXISTS stockoutrequest (
    id BIGSERIAL PRIMARY KEY,
    request_no VARCHAR(50) NOT NULL UNIQUE,
    request_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    customer_id BIGINT,
    customer_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    request_date TIMESTAMP WITHOUT TIME ZONE,
    required_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    approved_by VARCHAR(50),
    approved_at TIMESTAMP WITHOUT TIME ZONE,
    approval_remarks VARCHAR(500),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 14. 添加联系历史表字段
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='customercontacthistory' AND column_name='contactperson') THEN
        ALTER TABLE customercontacthistory ADD COLUMN ContactPerson VARCHAR(100);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='customercontacthistory' AND column_name='nextfollowuptime') THEN
        ALTER TABLE customercontacthistory ADD COLUMN NextFollowUpTime TIMESTAMP WITH TIME ZONE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='customercontacthistory' AND column_name='operatorid') THEN
        ALTER TABLE customercontacthistory ADD COLUMN OperatorId VARCHAR(36);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='customercontacthistory' AND column_name='result') THEN
        ALTER TABLE customercontacthistory ADD COLUMN Result VARCHAR(500);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='customercontacthistory' AND column_name='subject') THEN
        ALTER TABLE customercontacthistory ADD COLUMN Subject VARCHAR(200);
    END IF;
END $$;

SELECT '数据库更新完成！' AS result;

-- 15. 创建客户/供应商操作日志与字段变更日志表（兼容历史环境）
CREATE TABLE IF NOT EXISTS customer_operation_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "CustomerId" TEXT NOT NULL,
    "OperationType" VARCHAR(100) NOT NULL,
    "OperationDesc" TEXT,
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Remark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_customer_id ON customer_operation_log("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_operation_time ON customer_operation_log("OperationTime");

CREATE TABLE IF NOT EXISTS customer_change_log (
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
CREATE INDEX IF NOT EXISTS idx_customer_change_log_customer_id ON customer_change_log("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_change_log_changed_at ON customer_change_log("ChangedAt");

CREATE TABLE IF NOT EXISTS vendor_operation_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "VendorId" TEXT NOT NULL,
    "OperationType" VARCHAR(100) NOT NULL,
    "OperationDesc" TEXT,
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Remark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_vendor_operation_log_vendor_id ON vendor_operation_log("VendorId");
CREATE INDEX IF NOT EXISTS idx_vendor_operation_log_operation_time ON vendor_operation_log("OperationTime");

CREATE TABLE IF NOT EXISTS vendor_change_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "VendorId" TEXT NOT NULL,
    "FieldName" VARCHAR(100) NOT NULL,
    "FieldLabel" VARCHAR(200),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "ChangedByUserId" TEXT,
    "ChangedByUserName" VARCHAR(100),
    "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_vendor_change_log_vendor_id ON vendor_change_log("VendorId");
CREATE INDEX IF NOT EXISTS idx_vendor_change_log_changed_at ON vendor_change_log("ChangedAt");

-- =========================================================
-- FrontCRM 兼容迁移脚本（PostgreSQL）
-- 特点：
-- 1) 可重复执行（IF NOT EXISTS / 条件判断）
-- 2) 避免因旧库结构差异导致中断
-- 3) 重点补齐 vendor/customer 日志表，修复详情页日志接口 500
-- =========================================================

-- 0) 确保可用扩展（用于 gen_random_uuid）
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1) component_cache：先建表（若不存在）
CREATE TABLE IF NOT EXISTS component_cache (
    id BIGSERIAL PRIMARY KEY,
    mpn VARCHAR(100) NOT NULL UNIQUE,
    manufacturer_name VARCHAR(200),
    category VARCHAR(200),
    description TEXT,
    specs_json TEXT,
    sellers_json TEXT,
    images_json TEXT,
    datasheet_urls TEXT,
    source VARCHAR(50) DEFAULT 'Octopart',
    query_count INTEGER DEFAULT 1,
    last_query_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    cache_expiry TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 1.1) 兼容旧表：如果缺少列则补列（避免后续索引报 "mpn 不存在"）
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'component_cache'
    ) THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema='public' AND table_name='component_cache' AND column_name='mpn'
        ) THEN
            ALTER TABLE public.component_cache ADD COLUMN mpn VARCHAR(100);
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema='public' AND table_name='component_cache' AND column_name='cache_expiry'
        ) THEN
            ALTER TABLE public.component_cache ADD COLUMN cache_expiry TIMESTAMP WITH TIME ZONE;
        END IF;
    END IF;
END $$;

-- 1.2) 索引仅在列存在时创建
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='component_cache' AND column_name='mpn'
    ) THEN
        CREATE INDEX IF NOT EXISTS idx_component_cache_mpn ON public.component_cache(mpn);
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='component_cache' AND column_name='cache_expiry'
    ) THEN
        CREATE INDEX IF NOT EXISTS idx_component_cache_expiry ON public.component_cache(cache_expiry);
    END IF;
END $$;

-- 2) sys_serial_number
CREATE TABLE IF NOT EXISTS sys_serial_number (
    id SERIAL PRIMARY KEY,
    module_code VARCHAR(50) NOT NULL UNIQUE,
    module_name VARCHAR(100),
    prefix VARCHAR(10) NOT NULL,
    sequence_length INTEGER DEFAULT 4,
    current_sequence INTEGER DEFAULT 0,
    reset_by_year BOOLEAN DEFAULT FALSE,
    reset_by_month BOOLEAN DEFAULT FALSE,
    last_reset_year INTEGER,
    last_reset_month INTEGER,
    remark VARCHAR(200),
    create_time TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP WITHOUT TIME ZONE
);

INSERT INTO sys_serial_number (module_code, module_name, prefix, sequence_length, current_sequence)
VALUES
    ('Customer', '客户管理', 'C', 4, 0),
    ('Vendor', '供应商管理', 'V', 4, 0),
    ('Inquiry', '询价管理', 'INQ', 4, 0),
    ('Quotation', '报价管理', 'QUO', 4, 0),
    ('SalesOrder', '销售订单', 'SO', 4, 0),
    ('PurchaseOrder', '采购订单', 'PO', 4, 0),
    ('StockIn', '入库管理', 'SI', 4, 0),
    ('StockOut', '出库管理', 'SO', 4, 0),
    ('Inventory', '库存调整', 'INV', 4, 0),
    ('Receipt', '收款管理', 'REC', 4, 0),
    ('Payment', '付款管理', 'PAY', 4, 0),
    ('InputInvoice', '进项发票', 'II', 4, 0),
    ('OutputInvoice', '销项发票', 'OI', 4, 0)
ON CONFLICT (module_code) DO NOTHING;

-- 3) sys_error_log
CREATE TABLE IF NOT EXISTS sys_error_log (
    id BIGSERIAL PRIMARY KEY,
    occurred_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    module_name VARCHAR(100),
    error_message VARCHAR(500) NOT NULL,
    error_detail TEXT,
    stack_trace TEXT,
    document_no VARCHAR(50),
    data_id VARCHAR(50),
    user_id VARCHAR(50),
    user_name VARCHAR(50),
    ip_address VARCHAR(50),
    request_url VARCHAR(500),
    request_method VARCHAR(10),
    request_params TEXT,
    is_resolved BOOLEAN DEFAULT FALSE,
    resolved_at TIMESTAMP WITHOUT TIME ZONE,
    resolved_by VARCHAR(50),
    remarks TEXT
);

CREATE INDEX IF NOT EXISTS idx_error_log_occurred_at ON sys_error_log(occurred_at);
CREATE INDEX IF NOT EXISTS idx_error_log_module ON sys_error_log(module_name);
CREATE INDEX IF NOT EXISTS idx_error_log_resolved ON sys_error_log(is_resolved);

-- 4) vendor 主表
CREATE TABLE IF NOT EXISTS vendor (
    id BIGSERIAL PRIMARY KEY,
    vendor_no VARCHAR(20) NOT NULL UNIQUE,
    vendor_name VARCHAR(200) NOT NULL,
    short_name VARCHAR(100),
    vendor_type VARCHAR(50),
    vendor_level VARCHAR(20) DEFAULT '普通',
    business_scope VARCHAR(500),
    main_products VARCHAR(500),
    country VARCHAR(100),
    province VARCHAR(100),
    city VARCHAR(100),
    district VARCHAR(100),
    address VARCHAR(500),
    zip_code VARCHAR(20),
    contact_name VARCHAR(100),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(100),
    fax VARCHAR(50),
    website VARCHAR(200),
    tax_no VARCHAR(50),
    bank_name VARCHAR(200),
    bank_account VARCHAR(50),
    account_name VARCHAR(100),
    credit_limit DECIMAL(18,2) DEFAULT 0,
    credit_period INTEGER DEFAULT 0,
    payment_terms VARCHAR(100),
    delivery_terms VARCHAR(100),
    purchase_amount DECIMAL(18,2) DEFAULT 0,
    paid_amount DECIMAL(18,2) DEFAULT 0,
    payable_amount DECIMAL(18,2) DEFAULT 0,
    total_orders INTEGER DEFAULT 0,
    total_transactions INTEGER DEFAULT 0,
    first_transaction_date TIMESTAMP WITHOUT TIME ZONE,
    last_transaction_date TIMESTAMP WITHOUT TIME ZONE,
    rating INTEGER,
    status VARCHAR(20) DEFAULT '潜在',
    is_blacklisted BOOLEAN DEFAULT FALSE,
    blacklist_reason VARCHAR(500),
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP WITHOUT TIME ZONE,
    deleted_by VARCHAR(50),
    remarks TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50),
    updated_by VARCHAR(50)
);

-- 5) vendor_address
CREATE TABLE IF NOT EXISTS vendor_address (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    address_type VARCHAR(20) NOT NULL DEFAULT '办公',
    country VARCHAR(100) DEFAULT '中国',
    province VARCHAR(100),
    city VARCHAR(100),
    district VARCHAR(100),
    address VARCHAR(500) NOT NULL,
    zip_code VARCHAR(20),
    contact_name VARCHAR(100),
    contact_phone VARCHAR(50),
    is_default BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 6) vendorcontactinfo
CREATE TABLE IF NOT EXISTS vendorcontactinfo (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    contact_name VARCHAR(100) NOT NULL,
    gender VARCHAR(10),
    department VARCHAR(100),
    position VARCHAR(100),
    phone VARCHAR(50),
    mobile VARCHAR(50),
    email VARCHAR(100),
    qq VARCHAR(50),
    wechat VARCHAR(50),
    is_primary BOOLEAN DEFAULT FALSE,
    is_decision_maker BOOLEAN DEFAULT FALSE,
    remarks VARCHAR(500),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 7) vendorbankinfo
CREATE TABLE IF NOT EXISTS vendorbankinfo (
    id BIGSERIAL PRIMARY KEY,
    vendor_id BIGINT NOT NULL REFERENCES vendor(id) ON DELETE CASCADE,
    account_name VARCHAR(200) NOT NULL,
    bank_name VARCHAR(200) NOT NULL,
    bank_branch VARCHAR(200),
    account_no VARCHAR(50) NOT NULL,
    currency VARCHAR(10) DEFAULT 'CNY',
    is_default BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 8) stock
CREATE TABLE IF NOT EXISTS stock (
    id BIGSERIAL PRIMARY KEY,
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    location_id BIGINT,
    location_name VARCHAR(100),
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) DEFAULT 0,
    locked_quantity DECIMAL(18,4) DEFAULT 0,
    available_quantity DECIMAL(18,4) DEFAULT 0,
    unit_cost DECIMAL(18,4) DEFAULT 0,
    total_cost DECIMAL(18,4) DEFAULT 0,
    min_stock DECIMAL(18,4) DEFAULT 0,
    max_stock DECIMAL(18,4) DEFAULT 0,
    last_in_time TIMESTAMP WITHOUT TIME ZONE,
    last_out_time TIMESTAMP WITHOUT TIME ZONE,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_stock_warehouse ON stock(warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_item ON stock(item_id);
CREATE INDEX IF NOT EXISTS idx_stock_batch ON stock(batch_no);

-- 9) stockin
CREATE TABLE IF NOT EXISTS stockin (
    id BIGSERIAL PRIMARY KEY,
    stock_in_no VARCHAR(50) NOT NULL UNIQUE,
    stock_in_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    supplier_id BIGINT,
    supplier_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    stock_in_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 10) stockinitem
CREATE TABLE IF NOT EXISTS stockinitem (
    id BIGSERIAL PRIMARY KEY,
    stock_in_id BIGINT NOT NULL REFERENCES stockin(id) ON DELETE CASCADE,
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) NOT NULL,
    unit_price DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    warehouse_id BIGINT,
    location_id BIGINT,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 11) stockout
CREATE TABLE IF NOT EXISTS stockout (
    id BIGSERIAL PRIMARY KEY,
    stock_out_no VARCHAR(50) NOT NULL UNIQUE,
    stock_out_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    customer_id BIGINT,
    customer_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    stock_out_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 12) stockoutitem
CREATE TABLE IF NOT EXISTS stockoutitem (
    id BIGSERIAL PRIMARY KEY,
    stock_out_id BIGINT NOT NULL REFERENCES stockout(id) ON DELETE CASCADE,
    item_id BIGINT NOT NULL,
    item_code VARCHAR(50),
    item_name VARCHAR(200),
    item_spec VARCHAR(500),
    item_unit VARCHAR(20),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) NOT NULL,
    unit_cost DECIMAL(18,4) DEFAULT 0,
    total_cost DECIMAL(18,2) DEFAULT 0,
    warehouse_id BIGINT,
    location_id BIGINT,
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 13) stockoutrequest
CREATE TABLE IF NOT EXISTS stockoutrequest (
    id BIGSERIAL PRIMARY KEY,
    request_no VARCHAR(50) NOT NULL UNIQUE,
    request_type VARCHAR(50) NOT NULL,
    source_type VARCHAR(50),
    source_id BIGINT,
    source_no VARCHAR(50),
    warehouse_id BIGINT NOT NULL,
    warehouse_name VARCHAR(100),
    customer_id BIGINT,
    customer_name VARCHAR(200),
    total_quantity DECIMAL(18,4) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'pending',
    request_date TIMESTAMP WITHOUT TIME ZONE,
    required_date TIMESTAMP WITHOUT TIME ZONE,
    operator_id VARCHAR(50),
    operator_name VARCHAR(50),
    approved_by VARCHAR(50),
    approved_at TIMESTAMP WITHOUT TIME ZONE,
    approval_remarks VARCHAR(500),
    remarks VARCHAR(500),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 14) customercontacthistory 兼容补列
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema='public' AND table_name='customercontacthistory'
    ) THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='customercontacthistory' AND column_name='contactperson') THEN
            ALTER TABLE public.customercontacthistory ADD COLUMN ContactPerson VARCHAR(100);
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='customercontacthistory' AND column_name='nextfollowuptime') THEN
            ALTER TABLE public.customercontacthistory ADD COLUMN NextFollowUpTime TIMESTAMP WITH TIME ZONE;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='customercontacthistory' AND column_name='operatorid') THEN
            ALTER TABLE public.customercontacthistory ADD COLUMN OperatorId VARCHAR(36);
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='customercontacthistory' AND column_name='result') THEN
            ALTER TABLE public.customercontacthistory ADD COLUMN Result VARCHAR(500);
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='customercontacthistory' AND column_name='subject') THEN
            ALTER TABLE public.customercontacthistory ADD COLUMN Subject VARCHAR(200);
        END IF;
    END IF;
END $$;

-- 15) 关键修复：客户/供应商日志表
CREATE TABLE IF NOT EXISTS customer_operation_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "CustomerId" TEXT NOT NULL,
    "OperationType" VARCHAR(100) NOT NULL,
    "OperationDesc" TEXT,
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Remark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_customer_id ON customer_operation_log("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_operation_time ON customer_operation_log("OperationTime");

CREATE TABLE IF NOT EXISTS customer_change_log (
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
CREATE INDEX IF NOT EXISTS idx_customer_change_log_customer_id ON customer_change_log("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_change_log_changed_at ON customer_change_log("ChangedAt");

CREATE TABLE IF NOT EXISTS vendor_operation_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "VendorId" TEXT NOT NULL,
    "OperationType" VARCHAR(100) NOT NULL,
    "OperationDesc" TEXT,
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Remark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_vendor_operation_log_vendor_id ON vendor_operation_log("VendorId");
CREATE INDEX IF NOT EXISTS idx_vendor_operation_log_operation_time ON vendor_operation_log("OperationTime");

CREATE TABLE IF NOT EXISTS vendor_change_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "VendorId" TEXT NOT NULL,
    "FieldName" VARCHAR(100) NOT NULL,
    "FieldLabel" VARCHAR(200),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "ChangedByUserId" TEXT,
    "ChangedByUserName" VARCHAR(100),
    "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_vendor_change_log_vendor_id ON vendor_change_log("VendorId");
CREATE INDEX IF NOT EXISTS idx_vendor_change_log_changed_at ON vendor_change_log("ChangedAt");

-- 16) 验证结果
SELECT
    to_regclass('public.vendor_operation_log') AS vendor_operation_log_table,
    to_regclass('public.vendor_change_log') AS vendor_change_log_table,
    to_regclass('public.customer_operation_log') AS customer_operation_log_table,
    to_regclass('public.customer_change_log') AS customer_change_log_table;

SELECT '兼容迁移执行完成' AS result;

-- 17) 用户收藏表（客户/供应商收藏）
CREATE TABLE IF NOT EXISTS user_favorite (
    "FavoriteId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "UserId" BIGINT NOT NULL,
    "EntityType" VARCHAR(50) NOT NULL,
    "EntityId" TEXT NOT NULL,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_favorite_unique
ON user_favorite("UserId", "EntityType", "EntityId");
CREATE INDEX IF NOT EXISTS idx_user_favorite_entity
ON user_favorite("EntityType", "EntityId");

-- 18) 通用草稿箱表（Customer/Vendor/RFQ）
CREATE TABLE IF NOT EXISTS biz_draft (
    "DraftId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "UserId" BIGINT NOT NULL,
    "EntityType" VARCHAR(50) NOT NULL,
    "DraftName" VARCHAR(200),
    "PayloadJson" TEXT NOT NULL,
    "Status" SMALLINT NOT NULL DEFAULT 0,
    "Remark" VARCHAR(500),
    "ConvertedEntityId" TEXT,
    "ConvertedAt" TIMESTAMP WITH TIME ZONE,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE INDEX IF NOT EXISTS idx_biz_draft_user_entity_status
ON biz_draft("UserId", "EntityType", "Status");
CREATE INDEX IF NOT EXISTS idx_biz_draft_create_time
ON biz_draft("CreateTime");

-- 19) RBAC 基础表（用户/部门/角色/权限）
CREATE TABLE IF NOT EXISTS sys_department (
    "DepartmentId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "DepartmentName" VARCHAR(100) NOT NULL,
    "ParentId" TEXT,
    "Path" VARCHAR(500),
    "Level" INT NOT NULL DEFAULT 1,
    "SaleDataScope" SMALLINT NOT NULL DEFAULT 1,
    "PurchaseDataScope" SMALLINT NOT NULL DEFAULT 1,
    "IdentityType" SMALLINT NOT NULL DEFAULT 0,
    "Status" SMALLINT NOT NULL DEFAULT 1,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE INDEX IF NOT EXISTS idx_sys_department_parent ON sys_department("ParentId");

CREATE TABLE IF NOT EXISTS sys_role (
    "RoleId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "RoleCode" VARCHAR(50) NOT NULL,
    "RoleName" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "Status" SMALLINT NOT NULL DEFAULT 1,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_role_code ON sys_role("RoleCode");

CREATE TABLE IF NOT EXISTS sys_permission (
    "PermissionId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "PermissionCode" VARCHAR(100) NOT NULL,
    "PermissionName" VARCHAR(100) NOT NULL,
    "PermissionType" VARCHAR(20) NOT NULL DEFAULT 'api',
    "Resource" VARCHAR(200),
    "Action" VARCHAR(50),
    "Status" SMALLINT NOT NULL DEFAULT 1,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_permission_code ON sys_permission("PermissionCode");

CREATE TABLE IF NOT EXISTS sys_user_department (
    "UserDepartmentId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "UserId" TEXT NOT NULL,
    "DepartmentId" TEXT NOT NULL,
    "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_user_department_unique ON sys_user_department("UserId", "DepartmentId");
CREATE INDEX IF NOT EXISTS idx_sys_user_department_user ON sys_user_department("UserId");

CREATE TABLE IF NOT EXISTS sys_user_role (
    "UserRoleId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_user_role_unique ON sys_user_role("UserId", "RoleId");
CREATE INDEX IF NOT EXISTS idx_sys_user_role_user ON sys_user_role("UserId");

CREATE TABLE IF NOT EXISTS sys_role_permission (
    "RolePermissionId" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "RoleId" TEXT NOT NULL,
    "PermissionId" TEXT NOT NULL,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreateUserId" BIGINT,
    "ModifyTime" TIMESTAMP WITH TIME ZONE,
    "ModifyUserId" BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_role_permission_unique ON sys_role_permission("RoleId", "PermissionId");

-- 默认角色
INSERT INTO sys_role("RoleId", "RoleCode", "RoleName", "Description")
SELECT gen_random_uuid()::text, 'SYS_ADMIN', '系统管理员', '系统最高权限'
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'SYS_ADMIN');

INSERT INTO sys_role("RoleId", "RoleCode", "RoleName", "Description")
SELECT gen_random_uuid()::text, 'SALES', '销售', '销售业务角色'
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'SALES');

INSERT INTO sys_role("RoleId", "RoleCode", "RoleName", "Description")
SELECT gen_random_uuid()::text, 'PURCHASER', '采购', '采购业务角色'
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'PURCHASER');

INSERT INTO sys_role("RoleId", "RoleCode", "RoleName", "Description")
SELECT gen_random_uuid()::text, 'LOGISTICS', '物流', '物流（入库/质检/出库/库存）角色'
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'LOGISTICS');

-- 默认权限（最小集合）
INSERT INTO sys_permission("PermissionId", "PermissionCode", "PermissionName", "PermissionType")
SELECT gen_random_uuid()::text, p.code, p.name, 'api'
FROM (
    VALUES
      ('customer.read', '查看客户'),
      ('customer.write', '编辑客户'),
      ('vendor.read', '查看供应商'),
      ('vendor.write', '编辑供应商'),
      ('rfq.read', '查看RFQ'),
      ('rfq.write', '编辑RFQ'),
      ('sales-order.read', '查看销售订单'),
      ('sales-order.write', '编辑销售订单'),
      ('purchase-order.read', '查看采购订单'),
      ('purchase-order.write', '编辑采购订单'),
      ('finance-receipt.read', '查看收款单'),
      ('finance-receipt.write', '编辑收款单'),
      ('finance-payment.read', '查看付款单'),
      ('finance-payment.write', '编辑付款单'),
      ('finance-sell-invoice.read', '查看销项发票'),
      ('finance-sell-invoice.write', '编辑销项发票'),
      ('finance-purchase-invoice.read', '查看进项发票'),
      ('finance-purchase-invoice.write', '编辑进项发票'),
      ('customer.info.read', '查看客户信息字段'),
      ('vendor.info.read', '查看供应商信息字段'),
      ('sales.amount.read', '查看销售价格金额字段'),
      ('purchase.amount.read', '查看采购价格金额字段'),
      ('draft.read', '查看草稿'),
      ('draft.write', '编辑草稿'),
      ('rbac.manage', '权限管理')
) AS p(code, name)
WHERE NOT EXISTS (SELECT 1 FROM sys_permission sp WHERE sp."PermissionCode" = p.code);

-- 给系统管理员授予全部权限
INSERT INTO sys_role_permission("RolePermissionId", "RoleId", "PermissionId")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId"
FROM sys_role r
JOIN sys_permission p ON 1 = 1
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
      SELECT 1
      FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId"
        AND rp."PermissionId" = p."PermissionId"
  );

-- 给 SALES 补充字段权限（仅客户信息与销售金额）
INSERT INTO sys_role_permission("RolePermissionId", "RoleId", "PermissionId")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId"
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('customer.info.read', 'sales.amount.read')
WHERE r."RoleCode" = 'SALES'
  AND NOT EXISTS (
      SELECT 1
      FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId"
        AND rp."PermissionId" = p."PermissionId"
  );

-- 给 PURCHASER 补充字段权限（仅供应商信息与采购金额）
INSERT INTO sys_role_permission("RolePermissionId", "RoleId", "PermissionId")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId"
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('vendor.info.read', 'purchase.amount.read')
WHERE r."RoleCode" = 'PURCHASER'
  AND NOT EXISTS (
      SELECT 1
      FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId"
        AND rp."PermissionId" = p."PermissionId"
  );

-- 撤销 LOGISTICS 的字段权限（确保物流只能在“入库/质检/出库/库存”相关页面查看客户/供应商信息）
-- 由于销售/采购订单列表/详情是通过 customer.info.read / vendor.info.read 来做字段隐藏的，
-- 因此这里不再授予这两个字段权限（并删除历史已授权的数据）。
DELETE FROM sys_role_permission rp
USING sys_role r
JOIN sys_permission p ON p."PermissionId" = rp."PermissionId"
WHERE rp."RoleId" = r."RoleId"
  AND r."RoleCode" = 'LOGISTICS'
  AND p."PermissionCode" IN ('customer.info.read', 'vendor.info.read');

-- 给 LOGISTICS 授予“只读”业务查询权限：让物流可以查看销售/采购/财务全量数据
-- 但不授予 customer.info.read / vendor.info.read，从而保证客户/供应商字段不会在非库存页面展示。
INSERT INTO sys_role_permission("RolePermissionId", "RoleId", "PermissionId")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId"
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN (
    'sales-order.read',
    'purchase-order.read',
    'finance-receipt.read',
    'finance-payment.read',
    'finance-sell-invoice.read',
    'finance-purchase-invoice.read'
)
WHERE r."RoleCode" = 'LOGISTICS'
  AND NOT EXISTS (
      SELECT 1
      FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId"
        AND rp."PermissionId" = p."PermissionId"
  );

-- 物流部门默认数据范围：全部（SaleDataScope/PurchaseDataScope = 0）
INSERT INTO sys_department(
    "DepartmentId",
    "DepartmentName",
    "SaleDataScope",
    "PurchaseDataScope",
    "IdentityType",
    "Status"
)
SELECT
    gen_random_uuid()::text,
    '物流部',
    0,
    0,
    3,
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM sys_department d
    WHERE d."DepartmentName" = '物流部'
);

UPDATE sys_department
SET
    "SaleDataScope" = 0,
    "PurchaseDataScope" = 0
WHERE "DepartmentName" = '物流部';
