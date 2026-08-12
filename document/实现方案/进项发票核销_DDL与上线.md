# 进项发票核销 — DDL 与上线

**状态：** MVP（实施中）  
**关联：** [进项发票核销-设计与实现](../System/财务/进项发票核销-设计与实现.md)

---

## 1. 迁移内容概要

1. 新建 `finance_purchase_invoice_write_off`
2. `financepurchaseinvoice` 增加 `Currency`, `VerifiedDone`, `VerifiedToBe`, `VerificationStatus`
3. `stock_in_item_extend` 增加 `InvoiceMatchDone`, `InvoiceMatchToBe`, `InvoiceMatchStatus`, `InvoiceMatchCurrency`
4. `stock_in_extend` 增加同上头缓存字段
5. 初始化：已入库明细 `InvoiceMatchToBe = Amount`；发票侧新建时初始化（历史 Demo 用下方清理 SQL，不依赖币别回填）

以仓库内 EF 迁移 `CRM.Infrastructure/Migrations/*PurchaseInvoiceWriteOff*` 为准。

---

## 2. 生产清理 Demo 进项（运维执行）

> 仅在确认库中进项均为可丢弃 Demo/测试数据后执行。执行前备份。

```sql
-- 预览
SELECT COUNT(*) FROM public.financepurchaseinvoiceitem WHERE COALESCE(is_deleted, false) = false;
SELECT COUNT(*) FROM public.financepurchaseinvoice WHERE COALESCE(is_deleted, false) = false;

-- 若已存在核销流水表，一并清理（迁移后）
-- DELETE FROM public.finance_purchase_invoice_write_off;

-- 硬删明细再主表（或按项目软删惯例 UPDATE is_deleted）
DELETE FROM public.financepurchaseinvoiceitem;
DELETE FROM public.financepurchaseinvoice;
```

软删环境可改为：

```sql
UPDATE public.financepurchaseinvoiceitem SET is_deleted = true, "ModifyTime" = timezone('utc', now());
UPDATE public.financepurchaseinvoice SET is_deleted = true, "ModifyTime" = timezone('utc', now());
```

---

## 3. 上线顺序

1. 备份库  
2. 执行 Demo 清理（如需要）  
3. 应用 EF/SQL 迁移  
4. 发布 API → 发布 Web  
5. 冒烟：入口、队列、单明细核销、币别新建  

回滚：迁移 Down；关闭菜单/路由；流水表可保留。

---

*文档遵循 [文档生成规范](../System/文档生成规范.md)。*
