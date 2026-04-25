
-- 检查所有表
SELECT 
    table_name,
    CASE 
        WHEN table_name IN ('customerinfo', 'customercontactinfo', 'customeraddress',
                           'customerbankinfo', 'customercredit', 'customerfollowup') 
        THEN '客户模块'
        WHEN table_name IN ('vendorinfo', 'vendorcontactinfo', 'vendoraddress', 
                           'vendorbankinfo', 'vendorcredit', 'vendorfollowup', 'vendorassessment')
        THEN '供应商模块'
        WHEN table_name IN ('sellorder', 'sellorderitem', 'sellorderdelivery', 
                           'sellorderinvoice', 'sellquotation', 'sellcontract')
        THEN '销售模块'
        WHEN table_name IN ('purchaseorder', 'purchaseorderitem', 'purchaseorderdelivery',
                           'purchaseorderinvoice', 'purchasequotation', 'purchasecontract')
        THEN '采购模块'
        WHEN table_name IN ('material', 'materialcategory', 'materialbom', 
                           'materialprice', 'materialinventory', 'materialbarcode')
        THEN '物料模块'
        WHEN table_name IN ('stock', 'stockin', 'stockout', 'stocktransfer_customers', 'stocktransfer_item_customers',
                           'stocktransfer_manual', 'stocktransfer_item_manual', 'stocktransfer', 'stocktransferitem',
                           'stockcheck', 'warehouse', 'warehousearea', 'warehouselocation')
        THEN '库存模块'
        WHEN table_name IN ('financeaccount', 'financevoucher', 'financereceipt', 
                           'financepayment', 'invoice', 'invoiceitem', 'taxrate')
        THEN '财务模块'
        WHEN table_name IN ('department', 'employee', 'user', 'role', 'permission',
                           'companyinfo', 'systemconfig', 'datadictionary')
        THEN '基础数据'
        ELSE '其他'
    END as module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;

-- 统计
SELECT 
    COUNT(*) as total_tables,
    COUNT(CASE WHEN table_name LIKE 'customer%' THEN 1 END) as customer_tables,
    COUNT(CASE WHEN table_name LIKE 'vendor%' THEN 1 END) as vendor_tables,
    COUNT(CASE WHEN table_name LIKE 'sell%' THEN 1 END) as sell_tables,
    COUNT(CASE WHEN table_name LIKE 'purchase%' THEN 1 END) as purchase_tables,
    COUNT(CASE WHEN table_name LIKE 'material%' THEN 1 END) as material_tables,
    COUNT(CASE WHEN table_name LIKE 'stock%' THEN 1 END) as stock_tables,
    COUNT(CASE WHEN table_name LIKE 'finance%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN 1 END) as finance_tables
FROM information_schema.tables 
WHERE table_schema = 'public';
