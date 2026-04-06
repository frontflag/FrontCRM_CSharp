# 销售订单与采购订单利润计算公式

本文档基于ICVIP EBS系统代码分析，详细说明销售订单明细和采购订单明细的GP（毛利润）和Profit（利润）计算公式。

## 📊 销售订单明细（SO Item）计算公式

### 1. 毛利润（GP）计算公式
**公式**：`毛利润 = 销售总额 - 采购总额（含税）`

**代码依据**：
```csharp:476:DownFinanceSellInvoiceViewModel.cs
model.GrossProfit = model.InvoiceAmount - model.PurchaseTotalTaxIncluded;
```

**说明**：
- `InvoiceAmount`：发票金额（销售总额）
- `PurchaseTotalTaxIncluded`：采购总额（含税）
- 此公式计算的是**税前毛利润**

### 2. 净利润（Profit）计算公式
**公式**：`净利润 = (销售总额 + 销售费用) - (采购总额 + 采购费用) - (财务费用 + 物流费用 + 其他费用)`

**代码依据**：
```csharp:21-24:SalesStockOutProfitExpenseDetailsResponseDto.cs
public override decimal Amount =>
    decimal.Parse(
        $"{Items.Sum(x => x.SalesTotal + x.SalesExpense) - Items.Sum(x => x.PurchaseTotal + x.PurchaseExpense) - Items.Sum(x => x.FinanceExpense.Total + x.LogisticsExpense.Total + x.OtherExpense.Total):0.######}"
    );
```

**详细组成**：
| 项目 | 说明 |
|------|------|
| 销售总额 (SalesTotal) | 销售单价 × 数量 |
| 销售费用 (SalesExpense) | 销售过程中产生的额外费用 |
| 采购总额 (PurchaseTotal) | 采购单价 × 数量 |
| 采购费用 (PurchaseExpense) | 采购过程中产生的额外费用 |
| 财务费用 (FinanceExpense) | 银行手续费、转账费等 |
| 物流费用 (LogisticsExpense) | 快递费、运费、关税等 |
| 其他费用 (OtherExpense) | 杂项费用 |



### 3. 利润率计算公式
**公式**：`利润率 = 销售总额 / 采购总额`

**代码依据**：
```csharp:75:SalesStockOutProfitItem.cs
public decimal StockOutProfitRate = PurchaseTotal == 0 ? 0 : decimal.Parse($"{SalesTotal / PurchaseTotal:0.######}");
```

---

## 📦 采购订单明细（PO Item）计算公式

### 1. 毛利润（GP）计算公式
**公式**：`毛利润 = (销售价格 - 采购成本) × 数量`

**代码依据**：
```csharp:138:StockOutItemExtendCustomerService.cs
itemPackOut.ReProfitPurchase = ((reConvertPrice - cost) * qty + stockOutExpense).ToTotal();
```
*注：此处的`ReProfitPurchase`包含出库费用，纯毛利润应为`(reConvertPrice - cost) * qty`*

### 2. 净利润（Profit）计算公式
**公式**：`净利润 = ((销售价格 - 采购成本) × 数量) + 出库费用`

**代码依据**：
```csharp:104:StockOutItemExtendCustomerService.cs
itemPackOut.ReProfitActual = ((reConvertPrice - stockItem.ConvertCost) * qty + stockOutExpense).ToTotal();
```

**详细组成**：
| 项目 | 说明 |
|------|------|
| reConvertPrice | 折算后的销售价格（USD） |
| stockItem.ConvertCost | 库存物料的折算成本 |
| qty | 出库数量 |
| stockOutExpense | 出库相关费用 |

### 3. 采购订单利润分类
根据`POItemListViewModel.cs`，采购订单明细包含两种利润计算：
- **ProfitActual**：出库利润（不含报关费用）
- **ProfitActualStockIn**：出库利润（含报关费用）

---

## 🔄 利润类型说明

### 销售订单中的利润类型
| 属性名 | 说明 | 计算基准 |
|--------|------|----------|
| ProfitExpected | 预期采购利润 | 基于采购成本的预期利润 |
| ProfitActual | 实际出库利润（财务USD） | 实际出库后的财务利润 |
| QuoteProfitExpected | 报价预期利润 | 基于报价的预期利润 |
| ProfitPurchase | 业务出库利润（业务USD） | 业务部门计算的出库利润 |

### 采购订单中的利润类型
| 属性名 | 说明 | 计算基准 |
|--------|------|----------|
| ProfitExpected | 采购预期利润 | 采购时的预期利润 |
| ProfitActual | 出库实际利润（不含报关） | 实际出库后的利润，不含报关费用 |
| ProfitActualStockIn | 出库实际利润（含报关） | 实际出库后的利润，包含报关费用 |

---

## 📈 计算公式总结表

| 订单类型 | 利润类型 | 公式 | 备注 |
|----------|----------|------|------|
| 销售订单 | 毛利润 (GP) | `销售总额 - 采购总额（含税）` | 税前利润 |
| 销售订单 | 净利润 (Profit) | `(销售总额+销售费用)-(采购总额+采购费用)-(财务+物流+其他费用)` | 税后净利润 |
| 采购订单 | 毛利润 (GP) | `(销售价格 - 采购成本) × 数量` | 基本毛利 |
| 采购订单 | 净利润 (Profit) | `((销售价格-采购成本)×数量) + 出库费用` | 含出库费用 |

---

## 🎯 关键业务规则

1. **货币折算**：所有利润计算最终都折算为**USD**进行统一核算
2. **费用分类**：系统将费用细分为财务、物流、其他三类，确保成本核算准确性
3. **报关影响**：采购订单利润区分是否包含报关费用，适应不同业务场景
4. **预期 vs 实际**：系统同时计算预期利润和实际利润，便于业务分析和预测

> **注意**：以上公式基于ICVIP EBS系统当前代码实现。实际业务中可能因税务政策、会计准则等因素有所调整。

---

## 📁 相关代码文件

1. `DownFinanceSellInvoiceViewModel.cs` - 销售发票毛利计算
2. `SalesStockOutProfitExpenseDetailsResponseDto.cs` - 销售出库利润费用明细
3. `SalesStockOutProfitItem.cs` - 销售出库利润项
4. `StockOutItemExtendCustomerService.cs` - 出库项扩展客户服务
5. `POItemListViewModel.cs` - 采购订单项列表视图模型
6. `SellOrderCalculateService.cs` - 销售订单计算服务

---

*文档生成时间：2026-04-03*
*基于ICVIP EBS系统代码分析*