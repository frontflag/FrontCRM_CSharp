# PurchaseOrderItemExtend 统计字段文档

## 一、表概述

| 项目 | 内容 |
|------|------|
| **表名** | PurchaseOrderItemExtend |
| **说明** | 采购明细扩展表，记录采购订单明细的各类统计信息 |
| **关联表** | PurchaseOrderItem（一对一，Id 关联 PurchaseOrderItemId） |
| **设计目的** | 汇总采购订单的库存、付款、发票、利润等统计信息，避免实时计算 |

---

## 二、统计字段分类

### 1. 到货通知相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockInNotifyNot** | long | 剩余到货通知数 | 采购数量 - 已到货数 - 在途数 |

**使用场景：**
- 查询可生成到货通知的采购明细
- 生成到货通知时的数量校验

**更新时机：**
```csharp
// 入库完成后更新
purchaseOrderItemExtend.QtyStockInNotifyNot = purchaseOrderItem.Qty - stockInNotify.ReceiveQty;
```

---

### 2. 入库相关字段（分仓库）

#### 2.1 海外仓（HK）入库

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockInHK** | long | 海外仓入库数量 | 累计海外仓实际入库数量 |
| **QtyStockInNotHK** | long | 海外仓未入库数 | 预期到货 - 已入库 |
| **LastStockInHKUserId** | Guid? | 最近入库人-海外 | 最后入库操作人 |
| **LastStockInHKTime** | DateTime? | 最近入库时间-海外 | 最后入库时间 |
| **InAndOutHKAllCount** | decimal | 出入库海外总数（分母） | 用于计算库存周转率 |

#### 2.2 大陆仓（SZ）入库

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockInSZ** | long | 大陆仓入库数量 | 累计大陆仓实际入库数量 |
| **QtyStockInNotSZ** | long | 大陆仓未入库数 | 预期到货 - 已入库 |
| **LastStockInSZUserId** | Guid? | 最近入库人-大陆 | 最后入库操作人 |
| **LastStockInSZTime** | DateTime? | 最近入库时间-大陆 | 最后入库时间 |
| **InAndOutSZAllCount** | decimal | 出入库大陆总数（分母） | 用于计算库存周转率 |

**使用场景：**
- 查询采购订单入库状态
- 计算入库完成率
- 库存报表统计

---

### 3. 出库相关字段（分仓库）

#### 3.1 海外仓（HK）出库

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockOutHK** | long | 海外仓已出库数 | 累计海外仓实际出库数量 |
| **LastStockOutHKUserId** | Guid? | 最近出库人-海外 | 最后出库操作人 |
| **LastStockOutHKTime** | DateTime? | 最近出库时间-海外 | 最后出库时间 |

#### 3.2 大陆仓（SZ）出库

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockOutSZ** | long | 大陆仓已出库数 | 累计大陆仓实际出库数量 |
| **LastStockOutSZUserId** | Guid? | 最近出库人-大陆 | 最后出库操作人 |
| **LastStockOutSZTime** | DateTime? | 最近出库时间-大陆 | 最后出库时间 |

**使用场景：**
- 查询采购订单出库状态
- 计算销售出库匹配情况
- 利润计算基础数据

---

### 4. 库存相关字段（分仓库）

#### 4.1 海外仓（HK）库存

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyRepertoryHK** | long | 海外仓现有库存 | 入库数 - 出库数 |
| **QtyOnTheWayHK** | long | 海外仓在途数 | 已生成到货通知但未入库 |
| **QtyActualArrivalHK** | long | 海外仓已到货数 | 已到货通知数量 |
| **QtyOccupyHK** | long | 海外仓分拣占用量 | 已分配但未出库 |
| **QtyRepertoryAvailableHK** | long | 海外仓可用库存 | 现有库存 - 占用量 |

#### 4.2 大陆仓（SZ）库存

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyRepertorySZ** | long | 大陆仓现有库存 | 入库数 - 出库数 |
| **QtyOnTheWaySZ** | long | 大陆仓在途数 | 已生成到货通知但未入库 |
| **QtyActualArrivalSZ** | long | 大陆仓已到货数 | 已到货通知数量 |
| **QtyOccupySZ** | long | 大陆仓分拣占用量 | 已分配但未出库 |
| **QtyRepertoryAvailableSZ** | long | 大陆仓可用库存 | 现有库存 - 占用量 |

**使用场景：**
- 查询各仓库库存情况
- 可用库存判断（能否接单）
- 库存预警

**计算公式：**
```
现有库存 = 入库数量 - 出库数量
可用库存 = 现有库存 - 分拣占用量
在途数量 = 已生成到货通知 - 已入库数量
```

---

### 5. 进项发票相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **PurchaseInvoiceAmount** | decimal? | 开票总额-进项发票 | 采购数量 × 采购单价 |
| **PurchaseInvoiceDone** | decimal? | 已开票额-进项发票 | 累计已开进项发票金额 |
| **PurchaseInvoiceToBe** | decimal? | 待开票额-进项发票 | 开票总额 - 已开票额 |
| **LastPurchaseInvoiceTime** | DateTime? | 最近开票时间-进项发票 | 最后开票时间 |
| **LastPurchaseInvoiceUserId** | Guid? | 最近开票员-进项发票 | 最后开票人 |
| **PurInvoiceNoticeDone** | decimal | 进项发票收票已收票金额 | 已收到供应商发票金额 |

**使用场景：**
- 进项发票管理
- 采购对账
- 税务统计

**计算公式：**
```
开票总额 = 采购数量 × 采购单价
待开票额 = 开票总额 - 已开票额
```

---

### 6. 采购付款相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **PaymentAmount** | decimal | 应付总额 | 采购数量 × 采购单价 |
| **PaymentAmountNot** | decimal | 未付总额 | 应付总额 + 应退 - 已付 - 异常金额 |
| **PaymentAmountFinish** | decimal | 已付金额 | 累计已付款金额 |
| **RequestPaymentAmountFinish** | decimal | 已请款金额 | 累计已发起请款金额 |
| **LastPaymentTime** | DateTime? | 最近付款时间 | 最后付款时间 |
| **LastPaymentUserId** | Guid? | 最近付款人 | 最后付款操作人 |
| **PaymentBankFee** | decimal | 付款银行手续费 | 累计银行手续费 |
| **PaymentTransferBankFee** | decimal | 付款中专银行费 | 累计中转银行费用 |
| **PaymentCurrency** | ECurrencyType | 付款币别 | 采购订单币别 |

**使用场景：**
- 付款计划管理
- 采购对账
- 财务报表

**计算公式：**
```
应付总额 = 采购数量 × 采购单价
未付总额 = 应付总额 + 供应商应退 - 已付金额 - 异常金额
异常金额 = (取消数量 + 退货数量) × 采购单价
```

---

### 7. 利润相关字段

#### 7.1 采购利润（预期利润）

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitExpected** | decimal | 采购利润 | 销售总额 - 采购总额 |
| **ProfitRateExpected** | decimal | 采购利润率 | 销售总额 / 采购总额 |
| **ReProfitExpected** | decimal | Re采购利润 | 重新计算的采购利润 |
| **ReProfitRateExpected** | decimal | Re采购利润率 | 重新计算的采购利润率 |

#### 7.2 出库利润-业务USD（以入库汇率为基准）

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitActualStockIn** | decimal | 出库利润.业务USD | (销售单价 - 采购单价) × 出库数量 |
| **ProfitRateActualStockIn** | decimal | 出库利润率.业务USD | 销售总额 / 采购总额 |
| **ReProfitActualStockIn** | decimal | Re出库利润.业务USD | 重新计算的出库利润 |
| **ReProfitRateActualStockIn** | decimal | Re出库利润率.业务USD | 重新计算的出库利润率 |

#### 7.3 出库利润-财务USD（以出库汇率为基准）

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitActual** | decimal | 出库利润.财务USD | 销售总额 - 采购总额（出库时汇率） |
| **ProfitRateActual** | decimal | 出库利润率.财务USD | 销售总额 / 采购总额 |
| **ReProfitActual** | decimal | Re出库利润.财务USD | 重新计算的出库利润 |
| **ReProfitRateActual** | decimal | Re出库利润率.财务USD | 重新计算的出库利润率 |

**计算公式：**
```
销售总额 = sum(销售单价 × 已出库数量)
采购总额 = sum(入库单价 × 已出库数量)

出库利润.财务USD = 销售总额 - 采购总额
出库利润率.财务USD = 销售总额 / 采购总额

出库利润.业务USD = (销售单价 - 采购单价) × 出库数量
出库利润率.业务USD = 销售总额 / 采购总额
```

**使用场景：**
- 销售利润分析
- 业务员业绩统计
- 财务报表

---

### 8. 收款相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ReceiptAmount** | decimal | 收款总额 | 销售订单应收总额 |
| **ReceiptAmountNot** | decimal | 待收总额 | 收款总额 - 已收总额 |
| **ReceiptAmountFinish** | decimal | 已收总额 | 累计已收款金额 |
| **ReceiptCurrency** | short | 收款币别 | 销售订单币别 |

**使用场景：**
- 收款管理
- 财务对账

---

### 9. 其他字段

| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| **FlagNeedInvoice** | bool | 是否需要进项发票 |
| **InvoiceCurrency** | ECurrencyType | 开票币别（默认人民币） |
| **PurchaseToBuyEvaluateStatus** | short | 采购员评价买手状态 |
| **PurchaseToPNEvaluateStatus** | short | 采购员评物料状态 |
| **IsNeedPushOrder** | bool | 是否需要推订单 |
| **PushOrderStatus** | EnumPushOrderToVendorStatus? | 推送订单状态 |
| **PushOrderDate** | DateTime? | 推订单时间 |

---

## 三、统计字段汇总

### 3.1 按功能分类

| 分类 | 字段数量 | 主要字段 |
|------|----------|----------|
| 到货通知 | 1 | QtyStockInNotifyNot |
| 入库统计 | 10 | QtyStockInHK, QtyStockInSZ, LastStockIn... |
| 出库统计 | 6 | QtyStockOutHK, QtyStockOutSZ, LastStockOut... |
| 库存统计 | 10 | QtyRepertoryHK, QtyRepertorySZ, QtyRepertoryAvailable... |
| 进项发票 | 6 | PurchaseInvoiceAmount, PurchaseInvoiceDone... |
| 采购付款 | 8 | PaymentAmount, PaymentAmountNot, PaymentAmountFinish... |
| 利润统计 | 12 | ProfitExpected, ProfitActual, ProfitRate... |
| 收款统计 | 4 | ReceiptAmount, ReceiptAmountNot... |
| **合计** | **57+** | - |

### 3.2 按仓库分类

| 仓库 | 入库字段 | 出库字段 | 库存字段 |
|------|----------|----------|----------|
| 海外仓(HK) | QtyStockInHK, LastStockInHK... | QtyStockOutHK, LastStockOutHK... | QtyRepertoryHK, QtyRepertoryAvailableHK... |
| 大陆仓(SZ) | QtyStockInSZ, LastStockInSZ... | QtyStockOutSZ, LastStockOutSZ... | QtyRepertorySZ, QtyRepertoryAvailableSZ... |

---

## 四、使用场景汇总

| 场景 | 使用字段 | 说明 |
|------|----------|------|
| **采购订单列表** | QtyStockInNotifyNot, QtyStockInHK/SZ, QtyStockOutHK/SZ | 显示入库/出库/剩余到货状态 |
| **库存查询** | QtyRepertoryHK/SZ, QtyRepertoryAvailableHK/SZ | 显示各仓库库存情况 |
| **付款管理** | PaymentAmount, PaymentAmountNot, PaymentAmountFinish | 显示应付/已付/未付金额 |
| **发票管理** | PurchaseInvoiceAmount, PurchaseInvoiceDone | 显示开票情况 |
| **利润分析** | ProfitExpected, ProfitActual, ProfitRate... | 显示采购/出库利润 |
| **财务报表** | 各类金额字段 | 汇总统计 |
| **业绩统计** | ProfitActual, LastStockIn/Out... | 业务员绩效 |

---

## 五、相关代码位置

| 功能 | 文件路径 |
|------|----------|
| 实体定义 | `Service/TradeMain/ICVIP.TradeMain.Service/DbModels/PurchaseOrders/PurchaseOrderItemExtend.cs` |
| 入库更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/Stock/StockInService.cs` |
| 出库更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/Stock/StockOutService.Logistics.cs` |
| 利润计算 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/Stock/StockOutService.Logistics.cs` |
| 付款更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/PO/PurchaseOrderService.cs` |
| 发票更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/PO/PurchaseOrderItemService.cs` |

---

*文档生成时间：2026年3月29日*
