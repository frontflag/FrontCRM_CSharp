# 销项发票核销 — DDL 与上线

**状态：** MVP（迁移 `20260831120000_FinanceSellInvoiceWriteOff`）  
**关联：** [销项发票核销-设计与实现](../System/财务/销项发票核销-设计与实现.md)  
**桌面：** [销项发票核销桌面-设计与实现](../System/财务/销项发票核销桌面-设计与实现.md)

---

## 1. 迁移内容概要

1. 新建 `finance_sell_invoice_write_off`（发票头 ↔ `finance_receivable_id`；可选预留发票明细 ID、冗余 `stock_out_id`）
2. `financesellinvoice` 增加 `MatchDone`、`MatchToBe`、`MatchStatus`
3. `finance_receivable` 增加 `invoice_match_done`、`invoice_match_to_be`、`invoice_match_status`
4. 初始化：
   - 应收：`invoice_match_to_be = Amount`，`invoice_match_done = 0`，`invoice_match_status = 0`
   - 发票：`MatchToBe = InvoiceTotal`，`MatchDone = 0`，`MatchStatus = 0`（再按流水重算；MVP 无历史匹配流水则保持全额待匹配）
5. **代码：** 移除/停用收款明细直改销项发票 `Receive*`；改为匹配与收款核销后的派生回写

以仓库内 EF 迁移 `CRM.Infrastructure/Migrations/20260831120000_FinanceSellInvoiceWriteOff.cs` 为准。

---

## 2. 列示意（实施时以迁移为准）

```sql
-- 示意，非最终脚本
-- ALTER TABLE financesellinvoice ADD "MatchDone" numeric(18,2) NOT NULL DEFAULT 0;
-- ALTER TABLE financesellinvoice ADD "MatchToBe" numeric(18,2) NOT NULL DEFAULT 0;
-- ALTER TABLE financesellinvoice ADD "MatchStatus" smallint NOT NULL DEFAULT 0;

-- ALTER TABLE finance_receivable ADD invoice_match_done numeric(18,2) NOT NULL DEFAULT 0;
-- ALTER TABLE finance_receivable ADD invoice_match_to_be numeric(18,2) NOT NULL DEFAULT 0;
-- ALTER TABLE finance_receivable ADD invoice_match_status smallint NOT NULL DEFAULT 0;

-- UPDATE finance_receivable SET invoice_match_to_be = "Amount", invoice_match_done = 0, invoice_match_status = 0
--   WHERE COALESCE(is_deleted, false) = false;

-- UPDATE financesellinvoice SET "MatchToBe" = "InvoiceTotal", "MatchDone" = 0, "MatchStatus" = 0
--   WHERE COALESCE(is_deleted, false) = false;
```

---

## 3. 上线顺序

1. 备份库  
2. 应用 EF/SQL 迁移并执行初始化 UPDATE  
3. 发布 API（含匹配 API + Receive* 派生、停用直改）→ 发布 Web（桌面 + 入口）  
4. 冒烟：入口、双边队列、足额匹配、超额拒绝、先收款后匹配的 Receive*、右扩展默认关  

回滚：迁移 Down；关闭菜单/路由；流水表可保留备查。

---

## 4. 冒烟清单

见 [销项发票核销桌面-测试对照说明](../QA/财务/销项发票核销桌面-测试对照说明.md) §八 P0。

---

*文档遵循 [文档生成规范](../System/文档生成规范.md)。*
