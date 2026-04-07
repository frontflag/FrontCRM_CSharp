-- =============================================================================
-- FrontCRM：清除业务数据，保留客户、供应商、职员/RBAC、系统参数与数据字典等主数据
-- 数据库：PostgreSQL（表名与 EF Core 映射一致，小写）
--
-- 保留（不执行 DELETE）：
--   customerinfo, customeraddress, customercontactinfo, customerbankinfo
--   vendorinfo, vendoraddress, vendorcontactinfo, vendorbankinfo
--   user（职员）
--   sys_department, sys_role, sys_permission, sys_user_department, sys_user_role, sys_role_permission
--   sysparamgroup, sysparam, sys_dict_item
--   material, materialcategory（物料主数据）
--   warehouseinfo, warehousezone, warehouselocation, warehouseshelf（仓库主数据）
--   financeexchangeratesetting（汇率配置；仅删除变更日志表）
--
-- 会删除：需求/报价/销售/采购/库存/财务流水/审批/旅程/标签关联/草稿/Debug 等
-- 同时删除：客户/供应商「联系历史」表（视为业务痕迹）；若需保留请注释掉对应 DELETE。
--
-- 使用前：务必备份数据库。建议先 BEGIN; … 全部执行后检查行数再 COMMIT; 或 ROLLBACK;
-- 勿动：__EFMigrationsHistory
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) 库存 / 质检 / 拣货 / 盘点
-- ---------------------------------------------------------------------------
DELETE FROM pickingtaskitem;
DELETE FROM pickingtask;
DELETE FROM inventorycountitem;
DELETE FROM inventorycountplan;
DELETE FROM stockoutitem;
DELETE FROM stockout;
DELETE FROM stockinitem;
DELETE FROM stockin;
DELETE FROM qcitem;
DELETE FROM qcinfo;
DELETE FROM stockledger;
DELETE FROM stock;
DELETE FROM stockoutrequest;
DELETE FROM stockinnotify;

-- ---------------------------------------------------------------------------
-- 2) 财务（明细先于主表）
-- ---------------------------------------------------------------------------
DELETE FROM financepaymentitem;
DELETE FROM financepayment;

DELETE FROM financereceiptitem;
DELETE FROM financereceipt;

DELETE FROM financepurchaseinvoiceitem;
DELETE FROM financepurchaseinvoice;

DELETE FROM sellinvoiceitem;
DELETE FROM financesellinvoice;

DELETE FROM paymentrequest;

-- 仅删汇率变更日志；主表 financeexchangeratesetting 保留
DELETE FROM financeexchangeratechangelog;

-- ---------------------------------------------------------------------------
-- 3) 上传文档与文档序号
-- ---------------------------------------------------------------------------
DELETE FROM upload_document;
DELETE FROM document_daily_sequence;

-- ---------------------------------------------------------------------------
-- 4) 审批、订单旅程
-- ---------------------------------------------------------------------------
DELETE FROM approval_record;
DELETE FROM log_orderjourney;

-- ---------------------------------------------------------------------------
-- 5) 采购（扩展先于明细先于主表；采购申请）
-- ---------------------------------------------------------------------------
DELETE FROM purchaseorderitemextend;
DELETE FROM purchaseorderextend;
DELETE FROM purchaseorderitem;
DELETE FROM purchaseorder;
DELETE FROM purchaserequisition;

-- ---------------------------------------------------------------------------
-- 6) 销售（扩展先于明细先于主表）
-- ---------------------------------------------------------------------------
DELETE FROM sellorderitemextend;
DELETE FROM sellorderextend;
DELETE FROM sellorderitem;
DELETE FROM sellorder;

-- ---------------------------------------------------------------------------
-- 7) 报价、需求
-- ---------------------------------------------------------------------------
DELETE FROM quoteitem;
DELETE FROM quote;
DELETE FROM rfqitem;
DELETE FROM rfq;

-- ---------------------------------------------------------------------------
-- 8) 标签、收藏、草稿、微信临时表
-- ---------------------------------------------------------------------------
DELETE FROM user_favorite;
DELETE FROM user_tag_preference;
DELETE FROM tag_relation;
DELETE FROM tag_definition;
DELETE FROM biz_draft;

DELETE FROM wechat_login_ticket;
DELETE FROM wechat_bind_request;

-- ---------------------------------------------------------------------------
-- 9) 日志、缓存、Debug、客户/供应商联系历史（业务痕迹）
-- ---------------------------------------------------------------------------
DELETE FROM debug;
DELETE FROM sys_error_log;
DELETE FROM component_cache;
DELETE FROM customercontacthistory;
DELETE FROM vendorcontacthistory;

-- ---------------------------------------------------------------------------
-- 可选：重置流水号当前序号（新单号从规则允许的下一段开始；请按环境调整）
-- 若不清空，新单号可能在旧最大号之后继续递增（通常也可接受）
-- ---------------------------------------------------------------------------
-- UPDATE sys_serial_number SET "CurrentSequence" = 0, "ModifyTime" = NOW();

COMMIT;

-- =============================================================================
-- 若执行中报外键错误：
--   1) 在 DB 中查询：SELECT conrelid::regclass, confrelid::regclass, conname FROM pg_constraint WHERE contype='f' AND confrelid = '问题表'::regclass;
--   2) 将引用该表的子表 DELETE 提前到本脚本更靠前位置，或先 ALTER … DROP CONSTRAINT（不推荐生产随意删约束）
--
-- 若库中仍有历史表（未纳入当前 DbContext），请自行补充 DELETE，例如：
--   receipt / receiptitem / payment / paymentitem / invoice / invoiceitem / businesslog 等
-- =============================================================================
