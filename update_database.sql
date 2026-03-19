-- =============================================
-- 数据库更新脚本
-- FrontCRM - 应用所有待处理的迁移
-- =============================================

-- 1. 检查并创建物料缓存表 (AddComponentCache)
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

-- 2. 检查并创建流水号管理表 (AddSysTables)
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

-- 3. 检查并创建错误日志表
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

-- 4. 检查并创建供应商相关表 (AddVendorAndInventoryTables)
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

-- 5. 检查并创建联系历史表新增字段 (AddContactHistoryFields)
DO $$
BEGIN
    -- 添加 ContactPerson 字段
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='customercontacthistory' AND column_name='contactperson') THEN
        ALTER TABLE customercontacthistory ADD COLUMN ContactPerson VARCHAR(100);
    END IF;

    -- 添加 NextFollowUpTime 字段
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='customercontacthistory' AND column_name='nextfollowuptime') THEN
        ALTER TABLE customercontacthistory ADD COLUMN NextFollowUpTime TIMESTAMP WITH TIME ZONE;
    END IF;

    -- 添加 OperatorId 字段
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='customercontacthistory' AND column_name='operatorid') THEN
        ALTER TABLE customercontacthistory ADD COLUMN OperatorId VARCHAR(36);
    END IF;

    -- 添加 Result 字段
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='customercontacthistory' AND column_name='result') THEN
        ALTER TABLE customercontacthistory ADD COLUMN Result VARCHAR(500);
    END IF;

    -- 添加 Subject 字段
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name='customercontacthistory' AND column_name='subject') THEN
        ALTER TABLE customercontacthistory ADD COLUMN Subject VARCHAR(200);
    END IF;
END $$;

-- 记录更新完成
SELECT '数据库更新完成！' AS result;
