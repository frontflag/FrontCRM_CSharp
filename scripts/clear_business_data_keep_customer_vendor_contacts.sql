-- =============================================================================
-- 清空业务数据（PostgreSQL）
--
-- 保留（不执行任何 DELETE/TRUNCATE）：
--   - 客户：customerinfo, customeraddress, customercontactinfo, customerbankinfo,
--           customercontacthistory
--   - 供应商：vendorinfo, vendoraddress, vendorcontactinfo, vendorbankinfo,
--             vendorcontacthistory
--   - 账号与 RBAC：user, sys_role, sys_permission, sys_user_role, sys_user_department,
--                  sys_department, sys_role_permission
--   - 系统配置：sys_param_group, sys_param, sys_dict_item, sysparamgroup, sysparam,
--               financeexchangeratesetting, financeexchangeratechangelog
--   - 仓库主数据：warehouseinfo, warehousezone, warehouselocation, warehouseshelf
--   - 标签定义：tag_definition
--
-- 会清空：需求/报价/销售/采购/采购申请/库存流水/财务单据/审批/旅程日志/草稿/收藏/
--        标签关联/上传文档/流水号/物料与分类/调试与部分日志等（见下方列表）。
--
-- 危险：仅用于测试库或已备份环境。务必先备份；在事务中执行，确认后 COMMIT。
-- =============================================================================

BEGIN;

SET LOCAL lock_timeout = '120s';

-- 单条 TRUNCATE：PostgreSQL 会按外键依赖排序；CASCADE 会一并清空引用到这些表上的从表。
-- 若某表在当前库不存在，请从列表中删除该行后重试。

TRUNCATE TABLE
  -- 财务（新）
  public.sellinvoiceitem,
  public.financesellinvoice,
  public.financepurchaseinvoiceitem,
  public.financepurchaseinvoice,
  public.financepaymentitem,
  public.financepayment,
  public.financereceiptitem,
  public.financereceipt,
  public.paymentrequest,
  -- 库存中心 / 物流
  public.pickingtaskitem,
  public.pickingtask,
  public.inventorycountitem,
  public.inventorycountplan,
  public.stockledger,
  public.stock_out_item,
  public.stock_out,
  public.stock_in_item,
  public.stock_in,
  public.stock,
  public.qcitem,
  public.qcinfo,
  public.stockinnotify,
  public.stockoutrequest,
  -- 采购 / 销售
  public.purchaseorderitemextend,
  public.purchaseorderitem,
  public.purchaseorderextend,
  public.purchaseorder,
  public.purchaserequisition,
  public.sellorderitemextend,
  public.sellorderitem,
  public.sellorderextend,
  public.sellorder,
  public.quoteitem,
  public.quote,
  public.rfqitem,
  public.rfq,
  -- 其它业务
  public.approval_record,
  public.log_orderjourney,
  public.biz_draft,
  public.user_favorite,
  public.tag_relation,
  public.upload_document,
  public.document_daily_sequence,
  public.component_cache,
  public.material,
  public.materialcategory,
  public.sys_serial_number,
  public.wechat_login_ticket,
  public.wechat_bind_request,
  public.sys_error_log,
  public.debug
RESTART IDENTITY CASCADE;

-- 统一操作/变更日志（若表不存在请注释本段）
TRUNCATE TABLE public.log_recent RESTART IDENTITY CASCADE;
TRUNCATE TABLE public.log_operation RESTART IDENTITY CASCADE;
TRUNCATE TABLE public.log_change_fldval RESTART IDENTITY CASCADE;

COMMIT;

-- =============================================================================
-- 可选：若希望连「物料分类/物料」也保留，请从上方主 TRUNCATE 列表中移除
--       public.material, public.materialcategory 两行后执行。
--
-- 可选：若希望连「仓库档案」一并重建，可追加（会删除仓库结构，请谨慎）：
--   TRUNCATE public.warehouseshelf, public.warehouselocation, public.warehousezone,
--            public.warehouseinfo RESTART IDENTITY CASCADE;
-- =============================================================================

