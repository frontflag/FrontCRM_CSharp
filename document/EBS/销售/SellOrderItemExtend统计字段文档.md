# SellOrderItemExtend 统计字段文档

## 一、表概述

| 项目 | 内容 |
|------|------|
| **表名** | SellOrderItemExtend |
| **说明** | 销售订单明细扩展表，记录销售订单明细的各类统计信息 |
| **关联表** | SellOrderItem（一对一，Id 关联 SellOrderItemId） |
| **设计目的** | 汇总销售订单的采购、库存、收款、发票、利润等统计信息，避免实时计算 |

---

## 二、统计字段分类

### 1. 审核相关字段

| 字段名 | 数据类型 | 说明 | 使用场景 |
|--------|----------|------|----------|
| **LastCheckerId** | Guid? | 最近审核人 | 审核流程跟踪 |
| **LastCheckTime** | DateTime? | 最近审核时间 | 审核流程跟踪 |

---

### 2. 采购申请相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyNotPurchaseRequisition** | long | 未采购申请数量 | 销售数量 - 已采购申请数量 |
| **QtyAlreadyPurchasedRequisition** | long | 已采购申请数量 | 累计已生成采购申请的数量 |

**使用场景：**
- 查询需要生成采购申请的销售明细
- 采购申请进度跟踪

---

### 3. 采购订单相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyNotPurchase** | long | 未采购数 | 销售数量 - 已采购数量 |
| **QtyAlreadyPurchased** | long | 已采购数 | 累计已生成采购订单的数量 |
| **LastPurchaseOrderTime** | DateTime? | 最后采购时间 | 最后一次生成采购订单时间 |
| **LastPurchaseUserId** | Guid? | 最近采购员 | 最后一次采购操作人 |

**使用场景：**
- 查询需要采购的销售明细
- 采购进度跟踪
- 采购员绩效统计

---

### 4. 付款相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **PaymentAmount** | decimal | 付款总额 | 采购数量 × 采购单价 |
| **PaymentAmountDone** | decimal | 已付总额 | 累计已付款金额 |
| **PaymentAmountToBe** | decimal | 待付总额 | 付款总额 - 已付总额 |
| **LastPaymentTime** | DateTime? | 最后付款时间 | 最后一次付款时间 |
| **LastPaymentUserId** | Guid? | 最近付款人 | 最后一次付款操作人 |
| **PaymentProgress** | decimal | 付款进度 | 已付款采购订单数量 + 库存付款订单数量 |
| **PaymentAmountDetail** | string | 付款总额明细 | JSON格式明细 |
| **PaymentAmountDoneDetail** | string | 已付总额明细 | JSON格式明细 |

**使用场景：**
- 付款计划管理
- 采购对账
- 财务报表

---

### 5. 销项发票相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **InvoiceAmount** | decimal | 发票总额 | 销售数量 × 销售单价 |
| **InvoiceAmountNot** | decimal | 待开票额 | 发票总额 - 已开票额 |
| **InvoiceAmountFinish** | decimal | 已开票额 | 累计已开销项发票金额 |
| **LastInvoiceTime** | DateTime? | 最近开票时间 | 最后开票时间 |
| **LastInvoiceUserId** | Guid? | 最近开票员 | 最后开票操作人 |

**使用场景：**
- 销项发票管理
- 客户对账
- 税务统计

---

### 6. 进项发票相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **PurchaseInvoiceAmount** | decimal | 开票总额-进项发票 | 采购数量 × 采购单价 |
| **PurchaseInvoiceDone** | decimal | 已开票额-进项发票 | 累计已开进项发票金额 |
| **LastPurchaseInvoiceTime** | DateTime? | 最近开票时间-进项发票 | 最后开票时间 |
| **LastPurchaseInvoiceUserId** | Guid? | 最近开票员-进项发票 | 最后开票操作人 |

**使用场景：**
- 进项发票管理
- 采购对账
- 税务统计

---

### 7. 收款相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ReceiptCurrency** | ECurrencyType | 收款币别 | 销售订单币别 |
| **ReceiptAmount** | decimal | 收款总额 | 销售数量 × 销售单价 |
| **ReceiptAmountNot** | decimal | 待收总额 | 收款总额 - 已收总额 |
| **ReceiptAmountFinish** | decimal | 已收总额 | 累计已收款金额 |
| **LastReceiptTime** | DateTime? | 最后收款时间 | 最后收款时间 |
| **LastReceiptUserId** | Guid? | 最近收款人 | 最后收款操作人 |

**使用场景：**
- 收款管理
- 客户对账
- 应收账款统计

---

### 8. 大陆仓（SZ）入库相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockInNotSZ** | long | 大陆仓未入库数 | 预期到货 - 已入库 |
| **QtyStockInSZ** | long | 大陆仓入库数 | 累计大陆仓实际入库数量 |
| **QtyRepertorySZ** | long | 大陆仓现有库存 | 入库数 - 出库数 |
| **LastStockInSZUserId** | Guid? | 最近入库人-大陆 | 最后入库操作人 |
| **LastStockInSZTime** | DateTime? | 最近入库时间-大陆 | 最后入库时间 |
| **InAndOutSZAllCount** | decimal | 出入库大陆总数（分母） | 用于计算库存周转率 |

**使用场景：**
- 大陆仓库存跟踪
- 入库进度查询

---

### 9. 大陆仓（SZ）出库相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyExpectedStockOutSZ** | long | 大陆仓分拣占用量 | 已分配但未出库 |
| **QtyGetReadyStockOutSZ** | long | 大陆仓可用库存 | 现有库存 - 占用量 |
| **QtyStockOutSZ** | long | 大陆仓已出库数 | 累计大陆仓实际出库数量 |
| **LastStockOutSZUserId** | Guid? | 最近出库人 | 最后出库操作人 |
| **LastStockOutSZTime** | DateTime? | 最近出库时间 | 最后出库时间 |

**使用场景：**
- 大陆仓出库跟踪
- 可用库存判断
- 出库进度查询

---

### 10. 海外仓（HK）入库相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockInNotHK** | long | 海外仓未入库数 | 预期到货 - 已入库 |
| **QtyStockInHK** | long | 海外仓入库数 | 累计海外仓实际入库数量 |
| **LastStockInHKUserId** | Guid? | 最近入库人-海外 | 最后入库操作人 |
| **LastStockInHKTime** | DateTime? | 最近入库时间-海外 | 最后入库时间 |
| **InAndOutHKAllCount** | decimal | 出入库海外总数（分母） | 用于计算库存周转率 |

**使用场景：**
- 海外仓库存跟踪
- 入库进度查询

---

### 11. 海外仓（HK）出库相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyRepertoryHK** | long | 海外仓现有库存 | 入库数 - 出库数 |
| **QtyExpectedStockOutHK** | long | 海外仓分拣占用量 | 已分配但未出库 |
| **QtyGetReadyStockOutHK** | long | 海外仓可用库存 | 现有库存 - 占用量 |
| **QtyStockOutHK** | long | 海外仓已出库数 | 累计海外仓实际出库数量 |
| **LastStockOutHKUserId** | Guid? | 最近出库人 | 最后出库操作人 |
| **LastStockOutHKTime** | DateTime? | 最近出库时间 | 最后出库时间 |

**使用场景：**
- 海外仓出库跟踪
- 可用库存判断
- 出库进度查询

---

### 12. 利润相关字段

#### 12.1 报价利润

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QuoteProfitExpected** | decimal | 报价利润 | (销售单价 - 报价成本) × 数量 |
| **QuoteProfitRateExpected** | decimal | 报价利率 | 销售总额 / 报价成本 |
| **ReQuoteProfitExpected** | decimal | Re报价利润 | 重新计算的报价利润 |
| **ReQuoteProfitRateExpected** | decimal | Re报价利率 | 重新计算的报价利率 |

#### 12.2 采购利润

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitExpected** | decimal | 采购利润 | 销售总额 - 采购总额 |
| **ProfitRateExpected** | decimal | 采购利率 | 销售总额 / 采购总额 |
| **ReProfitExpected** | decimal | Re采购利润 | 重新计算的采购利润 |
| **ReProfitRateExpected** | decimal | Re采购利率 | 重新计算的采购利率 |

#### 12.3 出库利润-业务USD

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitPurchase** | decimal | 出库利润.业务USD | (销售单价 - 采购单价) × 出库数量 |
| **ProfitRatePurchase** | decimal | 出库利润率.业务USD | 销售总额 / 采购总额 |
| **ReProfitPurchase** | decimal | Re出库利润.业务USD | 重新计算的出库利润 |
| **ReProfitRatePurchase** | decimal | Re出库利润率.业务USD | 重新计算的出库利润率 |

#### 12.4 出库利润-财务USD

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **ProfitActual** | decimal | 出库利润.财务USD | 销售总额 - 采购总额（出库时汇率） |
| **ProfitRateActual** | decimal | 出库利润率.财务USD | 销售总额 / 采购总额 |
| **ReProfitActual** | decimal | Re出库利润.财务USD | 重新计算的出库利润 |
| **ReProfitRateActual** | decimal | Re出库利润率.财务USD | 重新计算的出库利润率 |

**计算公式：**
```
报价利润 = (销售单价 - 报价成本) × 数量
采购利润 = 销售总额 - 采购总额
出库利润.业务USD = (销售单价 - 采购单价) × 出库数量
出库利润.财务USD = 销售总额(出库汇率) - 采购总额(出库汇率)

利润率 = 销售总额 / 成本总额
```

**使用场景：**
- 销售利润分析
- 业务员业绩统计
- 财务报表
- 订单审批时的利润评估

---

### 13. 出货通知相关字段

| 字段名 | 数据类型 | 说明 | 计算逻辑 |
|--------|----------|------|----------|
| **QtyStockOutNotify** | long | 已出货通知数 | 累计已生成出货通知的数量 |
| **QtyStockOutNotifyNot** | long | 未出货通知数 | 销售数量 - 已出货通知数 |

**使用场景：**
- 出货通知进度跟踪
- 物流安排

---

### 14. 报价相关字段

| 字段名 | 数据类型 | 说明 | 使用场景 |
|--------|----------|------|----------|
| **QuoteCost** | decimal | 报价单价 | 记录报价时的成本价 |
| **QuoteCurrency** | ECurrencyType | 报价币别 | 报价币别 |
| **QuoteConvertCost** | decimal | 报价折算价 | 折算后的报价成本 |

---

### 15. 评价相关字段

| 字段名 | 数据类型 | 说明 | 枚举值 |
|--------|----------|------|--------|
| **BuyToPurchaseEvaluateStatus** | short | 买手评价采购员状态 | 1:未评价, 2:已评价, 100:已追评 |
| **CustomerToVendorEvaluateStatus** | short | 客户评价供应商状态 | 1:未评价, 2:已评价, 100:已追评 |
| **BuyToPNEvaluateStatus** | short | 买手评价物料状态 | 1:未评价, 2:已评价, 100:已追评 |

**使用场景：**
- 评价体系管理
- 供应商绩效考核

---

### 16. 其他字段

| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| **ShipAddressId** | Guid? | 收货地址ID |
| **Type** | short | 类型：1-正常, 2-使用库存 |
| **PurchaseUserId** | Guid | 报价采购员 |
| **VendorId** | Guid | 报价供应商 |
| **RfqItemId** | Guid | 需求明细id |
| **OverdueDays** | long | 逾期天数 |
| **DiscountAmount** | decimal | 优惠金额 |
| **BankFee** | decimal | 核销时的银行费用 |

---

### 17. 拓邦相关字段（EDI对接）

| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| **TBLineNumber** | string | 拓邦采购订单行号 |
| **TBOrderedQuantity** | string | 拓邦订单数量 |
| **TBProductDescription** | string | 拓邦物料描述 |
| **TBPOVersion** | string | 拓邦PO版本号 |
| **TBPOConfirmStatus** | string | 拓邦确认状态 |
| **TBACKLineNumber** | string | 拓邦确认行号 |
| **TBRequestedDeliveryDate** | string | 拓邦订单需求日期 |

**使用场景：**
- 与拓邦客户的EDI对接
- 订单变更管理

---

## 三、统计字段汇总

### 3.1 按功能分类

| 分类 | 字段数量 | 主要字段 |
|------|----------|----------|
| 审核相关 | 2 | LastCheckerId, LastCheckTime |
| 采购申请 | 2 | QtyNotPurchaseRequisition, QtyAlreadyPurchasedRequisition |
| 采购订单 | 4 | QtyNotPurchase, QtyAlreadyPurchased... |
| 付款相关 | 8 | PaymentAmount, PaymentAmountDone... |
| 销项发票 | 5 | InvoiceAmount, InvoiceAmountDone... |
| 进项发票 | 4 | PurchaseInvoiceAmount, PurchaseInvoiceDone... |
| 收款相关 | 6 | ReceiptAmount, ReceiptAmountNot... |
| 大陆仓入库 | 6 | QtyStockInSZ, QtyRepertorySZ... |
| 大陆仓出库 | 5 | QtyStockOutSZ, QtyGetReadyStockOutSZ... |
| 海外仓入库 | 5 | QtyStockInHK, LastStockInHK... |
| 海外仓出库 | 6 | QtyStockOutHK, QtyRepertoryHK... |
| 利润统计 | 16 | QuoteProfitExpected, ProfitExpected... |
| 出货通知 | 2 | QtyStockOutNotify, QtyStockOutNotifyNot |
| 报价相关 | 3 | QuoteCost, QuoteCurrency... |
| 评价相关 | 3 | BuyToPurchaseEvaluateStatus... |
| 拓邦对接 | 7 | TBLineNumber, TBPOVersion... |
| 其他 | 10 | ShipAddressId, Type, OverdueDays... |
| **合计** | **94+** | - |

### 3.2 按仓库分类

| 仓库 | 入库字段 | 出库字段 | 库存字段 |
|------|----------|----------|----------|
| 大陆仓(SZ) | QtyStockInSZ, LastStockInSZ... | QtyStockOutSZ, QtyGetReadyStockOutSZ... | QtyRepertorySZ |
| 海外仓(HK) | QtyStockInHK, LastStockInHK... | QtyStockOutHK, QtyGetReadyStockOutHK... | QtyRepertoryHK |

---

## 四、使用场景汇总

| 场景 | 使用字段 | 说明 |
|------|----------|------|
| **销售订单列表** | QtyAlreadyPurchased, QtyStockOutSZ/HK, ProfitActual | 显示采购/出库/利润状态 |
| **采购申请** | QtyNotPurchaseRequisition | 筛选需要申请采购的明细 |
| **采购订单** | QtyNotPurchase, LastPurchaseUserId | 筛选需要采购的明细 |
| **库存查询** | QtyRepertorySZ/HK, QtyGetReadyStockOutSZ/HK | 显示各仓库库存情况 |
| **付款管理** | PaymentAmount, PaymentAmountNot | 显示应付/已付金额 |
| **发票管理** | InvoiceAmount, InvoiceAmountFinish | 显示开票情况 |
| **收款管理** | ReceiptAmount, ReceiptAmountNot | 显示应收/已收金额 |
| **利润分析** | QuoteProfitExpected, ProfitExpected, ProfitActual | 显示各级利润 |
| **财务报表** | 各类金额字段 | 汇总统计 |
| **业绩统计** | ProfitActual, LastStockOut... | 业务员绩效 |
| **拓邦对接** | TBLineNumber, TBPOVersion... | EDI数据交换 |

---

## 五、相关代码位置

| 功能 | 文件路径 |
|------|----------|
| 实体定义 | `Service/TradeMain/ICVIP.TradeMain.Service/DbModels/SellOrders/SellOrderItemExtend.cs` |
| 销售订单服务 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/SO/SellOrderService.Private.cs` |
| 利润计算 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/SO/SellOrderService.Private.cs` |
| 入库更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/Stock/StockInService.cs` |
| 出库更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/Stock/StockOutService.cs` |
| 收款更新 | `Service/TradeMain/ICVIP.TradeMain.Service/Svc/SO/SellOrderService.Query.cs` |

---

## 六、与 PurchaseOrderItemExtend 的对比

| 对比项 | SellOrderItemExtend | PurchaseOrderItemExtend |
|--------|---------------------|-------------------------|
| **业务方向** | 销售端 | 采购端 |
| **核心关注** | 收款、利润、出库 | 付款、入库、到货 |
| **发票类型** | 销项发票 | 进项发票 |
| **库存方向** | 出库为主 | 入库为主 |
| **特殊字段** | 拓邦对接字段 | - |
| **评价字段** | 买手评价采购员/供应商/物料 | 采购员评价买手/物料 |

---

*文档生成时间：2026年3月29日*
