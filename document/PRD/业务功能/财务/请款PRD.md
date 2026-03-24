# 请款/付款业务逻辑文档

## 一、概述

### 1.1 业务背景

请款/付款是财务系统的核心业务模块，用于管理向供应商支付采购货款及相关费用的全流程。包括从采购订单入库后的请款申请、审批、付款确认到最终付款完成的完整流程。

### 1.2 核心概念

| 概念 | 说明 |
|------|------|
| **请款** | 向公司申请支付供应商货款的流程 |
| **付款** | 实际执行资金支付的操作 |
| **待请款金额** | 采购订单中尚未申请付款的金额 |
| **已请款金额** | 已经申请但尚未完成的付款金额 |

### 1.3 业务流程图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           请款/付款业务流程                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐             │
│   │ 采购订单明细 ──>│ 申请请款 │───>│ 提交审批 │───>│ 审批通过 │             │
│   └──────────┘    └──────────┘    └──────────┘    └──────────┘             │
│                                                        │                    │
│                                                        ▼                    │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐             │
│   │ 付款完成  │<───│ 确认付款  │<───│          │<───│ 应付款   │             │
│   └──────────┘    └──────────┘    └──────────┘    └──────────┘             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 二、数据模型

### 2.1 付款单主表 (FinancePayment)

**表名**: `fin_financepaymentv1`

| 字段名 | 类型 | 说明 |
|--------|------|------|
| FinancePaymentId | Guid | 付款单ID（主键） |
| FinancePaymentCode | string | 付款单编号 |
| VendorId | Guid | 供应商ID |
| VendorBankId | Guid | 供应商银行ID |
| Status | short | 付款状态（FinancePaymentAudit枚举） |
| RequireMode | short | 请款方式 |
| PaymentMode | short? | 付款方式 |
| PaymentAmountToBe | decimal | 请款货款总额 |
| PaymentToBeTotalAmount | decimal | 请款所有费用总额 |
| PaymentAmount | decimal | 实际付款金额 |
| PaymentTotalAmount | decimal | 实际付款总额 |
| PaymentDate | DateTime? | 付款日期 |
| PaymentUserId | Guid? | 付款人ID |
| PaymentBank | string | 付款银行 |
| PaymentStatementNo | string | 付款流水号 |
| HQIndustryApprovalStatus | int? | 股份审批状态 |
| CreateTime | DateTime | 创建时间 |
| CreateUserId | Guid | 创建人ID |






### 2.2 付款明细表 (FinancePaymentItem)

**表名**: `fin_financepaymentitemv1`

| 字段名 | 类型 | 说明 |
|--------|------|------|
| FinancePaymentItemId | Guid | 付款明细ID |
| FinancePaymentId | Guid | 付款单ID |
| PurchaseOrderItemId | Guid | 采购明细ID |
| PaymentAmountToBe | decimal | 请款金额 |
| PaymentAmount | decimal | 已付金额 |
| Remark | string | 备注 |



### 2.3 付款费用表 (FinancePaymentCharges)

**表名**: `fin_financepaymentcharges`

| 字段名 | 类型 | 说明 |
|--------|------|------|
| FinancePaymentChargesId | Guid | 费用ID |
| FinancePaymentId | Guid | 付款单ID |
| TransBankFee | decimal | 中转行费用 |
| BankCharge | decimal | 银行手续费 |
| CarriageFee | decimal | 运费 |
| OtherFee | decimal | 杂费 |
| TailDiffFee | decimal | 尾差值 |
| DeductionFee | decimal | 抵扣费用 |
| TransferBankCostUndertakingParty | short | 中转行费用承担方 |

---

## 三、请款金额计算逻辑

### 3.1 采购订单待请款金额计算

```
待请款金额 = 采购总额 - 已请款金额

其中:
- 采购总额 = PurchaseOrderItem.Total (采购明细的采购总额)
- 已请款金额 = PurchaseOrderItemExtend.RequestPaymentAmountFinish (采购明细扩展表的已请款金额)
```

**代码逻辑** (`PaymentServiceV2.cs` 第2731-2734行):

```csharp
if (single.PurchaseOrderItem.Total - single.PurchaseOrderItemExtend.RequestPaymentAmountFinish + item.OldAmount < item.ApplyAmount)
    throw new BusinessException($"物料：{single.PurchaseOrderItem.PN} 请款金额不能大于待请款金额");
```

### 3.2 付款单总额计算

```
付款请款总额 = 订单明细请款金额总和
            - 供应商退款抵扣金额
            + 异常应付金额
            + 银行手续费
            + 运费
            + 杂费
            - 尾差值
            - 其他抵扣金额
```

**代码逻辑** (`PaymentServiceV2.cs` 第2756-2763行):

```csharp
var paymentAmount = param.AddPaymentItemParamList.Sum(a => a.ApplyAmount)        // 订单明细请款金额
                    - param.DeductionItemParamList.Sum(a => a.ApplyAmount)       // 抵扣金额
                    + param.AbnPaymentToBeItemParamList.Sum(a => a.ApplyAmount)  // 异常应付金额
                    + param.ApplyPaymentInfo.BankCharge                           // 银行手续费
                    + param.ApplyPaymentInfo.CarriageFee                          // 运费
                    + param.ApplyPaymentInfo.OtherFee                             // 杂费
                    - param.ApplyPaymentInfo.TailDiffFee                          // 尾差值
                    - param.OtherDeductionAmount;                                 // 其他抵扣
```

### 3.3 订单付款状态计算

```csharp
// 采购订单明细付款状态 (EnumFinancePaymentStatus)
IF 已付金额 == 0               -> 未付款 (None = 1)
IF 0 < 已付金额 < 请款金额      -> 部分付款 (Partial = 2)
IF 已付金额 >= 请款金额        -> 付款完成 (Finish = 100)
```

---

## 四、状态流转关系

### 4.1 付款单状态 (FinancePaymentAudit)

Status 付款状态 定义：
New =1 (新建)	付款单新建
PendingAudit =2  (待审核)	已提交审批
Approved = 10,  （审核通过）  等待付款
Completed  =100 (付款完成)	付款已完成
AuditFailed  =-1 (审核失败)	审批被拒绝

### 4.2 状态流转图

```
                            ┌─────────────────────────────────────────────┐
                            │              付款单状态流转                  │
                            └─────────────────────────────────────────────┘

    ┌─────────┐   提交审批   ┌──────────┐   审批通过   ┌───────────┐           
    │  新建   │ ───────────> │  待审核  │ ──────────> │  应付款   │ 
    │  (1)   │              │   (2)    │             │   (10)    │              
    └────┬────┘              └────┬─────┘             └─────┬─────┘            
         │                        │                         │                        
         │                        │ 审批拒绝                │                        
         │                        ▼                         │                        
         │                   ┌──────────┐                   │                   
         │                   │ 审核失败 │                   │                    
         │                   │  (-2)    │                   │                   
         │                   └────┬─────┘                   │                   
         │                        │                         │                          
         │                        │ 重新编辑                │                           
         └────────────────────────┘                         │                          
                                                            │                     
                                                            │                     
                                                            │
                                                            │ 手动确认付款
                                                            ▼
                                                       ┌────────────┐
                                                       │ 付款完成   │
                                                       │   (20)     │
                                                       └────────────┘
```

### 4.3 状态流转触发条件

| 流转路径 | 触发条件 | 操作人 |
|----------|----------|--------|
| New → PendingAudit| 用户点击"提交审批" | 采购员/财务 |
| PendingAudit → Approved | 审批中心
| PendingAudit → AuditFailed | 审批中心审批拒绝 
| AuditFailed → New | 用户编辑后重新保存 | 采购员/财务 |
| Approved → Completed | 手动确认付款完成 | 财务专员 |


---

## 五、请款方式 (RequireMode)

### 5.1 付款方式枚举 (FinancePaymentMode)

| 值 | 代码 | 名称 | 说明 |
|----|------|------|------|
| 1 | XJ | 现金 | 现金支付 |
| 2 | MJZP | 美金支票 | 美元支票 |
| 3 | MJCX | 美金储蓄 | 美元储蓄账户转账 |
| 4 | MJVisa | 美金Visa | 美元Visa卡 |
| 5 | MJMaster | 美金Master | 美元Master卡 |
| 6 | GYZP | 港元支票 | 港币支票 |
| 7 | GYCX | 港元储蓄 | 港币储蓄账户转账 |
| 8 | GYVisa | 港元Visa | 港币Visa卡 |
| 9 | GYMaster | 港元Master | 港币Master卡 |
| 10 | DK | 抵扣 | 使用退款/应付抵扣 |
| 11 | YCDC | 异常对冲 | 异常对冲付款 |
| 12 | VendorYPayment | 供应商预付款 | 预付给供应商的款项 |

### 5.2 付款方式选择规则

1. **根据币别选择**: 人民币请款只能选择人民币相关付款方式
2. **根据供应商选择**: 不同供应商支持的付款方式可能不同
3. **默认付款方式**: 系统可根据供应商历史记录默认选择常用付款方式

---

## 六、请款业务流程

### 6.1 申请请款前置条件

1. 采购订单已审核
2. 待请款金额 > 0
3. 同一供应商、同一币别的采购明细才能合并请款
4. 采购订单已上传付款相关文档

### 6.2 请款申请步骤

#### 步骤1: 选择采购明细
- 从采购订单列表中选择需要付款的明细
- 系统自动筛选同一供应商、同一币别的明细

#### 步骤2: 填写请款信息

**必填字段**:
| 字段 | 说明 |
|------|------|
| VendorBankId | 供应商银行ID（下拉选择） |
| RequireMode | 请款方式（付款方式） |

**可选字段**:
| 字段 | 说明 |
|------|------|
| Remark | 请款备注 |
| TransBankFee | 中转行费用 |
| BankCharge | 银行手续费 |
| CarriageFee | 运费 |
| OtherFee | 杂费 |
| TailDiffFee | 尾差值 |
| TransferBankCostUndertakingParty | 中转行费用承担方（1我方/2供应商） |

#### 步骤3: 填写订单明细请款金额

| 字段 | 说明 |
|------|------|
| PurchaseOrderItemId | 采购明细ID（系统带入） |
| ApplyAmount | **本次申请金额**（默认=待请款金额） |
| Remark | 明细备注 |

**校验规则**:
```
本次申请金额 > 0
本次申请金额 <= 待请款金额
```


#### 步骤5: 提交请款

**提交前校验**:
1. 请款金额必须大于0
2. 中转行费用>0时，承担方不能为空
3. 其他抵扣金额>0时，说明不能为空
4. 采购单必须上传付款相关文档
5. 请款金额不能超过待请款金额

**提交后处理**:
1. 保存付款单主表 (FinancePayment)
2. 保存付款明细表 (FinancePaymentItem)
3. 保存付款费用表 (FinancePaymentCharges)

5. 更新采购明细已请款金额
6. 生成业务编号



---

## 八、付款流程



### 8.2 手动确认付款

**前提条件**:
- 付款单状态为 ** (应付款)**


**操作字段**:
| 字段 | 说明 |
|------|------|
| PaymentDate | 付款日期（必填） |
| PaymentMode | 付款方式 |
| PaymentBank | 付款银行 |
| PaymentStatementNo | 付款流水号 |
| AuditComment | 付款确认备注 |
| PaymentRemark | 付款摘要 |

**确认后处理**:
```csharp
// PaymentServiceV2.cs 第2200-2206行
single.Status = (short)FinancePaymentAudit.Finish;                    // 状态变更为付款完成
single.HQIndustryApprovalStatus = EHQIndustryApprovalStatus.PaymentCompleted;
single.PaymentAmount = single.PaymentAmountToBe;                      // 实际付款金额 = 请款金额
single.PaymentTotalAmount = single.PaymentToBeTotalAmount;            // 实际付款总额 = 请款总额
```

### 8.3 付款完成后处理

1. **更新付款明细已付金额**:
```csharp
paymentItemList.ForEach(item =>
{
    item.PaymentAmount = item.PaymentAmountToBe;  // 已付金额 = 请款金额
});
```



---

## 九、取消请款/取消应付

### 9.1 取消应付

**前提条件**:
- 付款单状态为 **YPayment (应付款)**
- 股份审批状态为未审批或审批失败

**操作后处理**:
```csharp
// PaymentServiceV2.cs 第1794-1799行
single.Status = (short)FinancePaymentAudit.New;          // 状态退回新建
single.PaymentUserId = Guid.Empty;                       // 清空付款人
single.ApprovalFlowResult = 0;                           // 清空审批结果
single.PaymentDate = null;                               // 清空付款日期
single.PaymentAmount = 0;                                // 清空付款金额
single.PaymentTotalAmount = 0;                           // 清空付款总额
```



## 十一、核心业务校验

### 11.1 请款金额校验

| 校验项 | 错误提示 |
|--------|----------|
| 请款金额必须 > 0 | "请款金额必须大于0" |
| 请款金额 ≤ 待请款金额 | "物料：{PN} 请款金额不能大于待请款金额" |
| 请款总额必须 > 0 | "请款总金额必须大于0！" |

### 11.2 状态操作校验

| 操作 | 允许状态 | 错误提示 |
|------|----------|----------|
| 编辑请款 | New, Fail | "只能编辑新建或审批失败的数据" |
| 删除请款 | New, Fail | "只能删除新建或审批失败的数据" |
| 取消应付 | YPayment | "只能取消应付数据" |
| 确认付款 | YPayment | "只有应付款状态才能确认" |
| 退款 | Finish | "只有已付款单据才能退款" |

### 11.3 供应商/币别校验

| 校验项 | 错误提示 |
|--------|----------|
| 同一供应商 | "选中的采购必须是同一个供应商" |
| 同一币别 | "选中的采购必须是同一种币别" |

### 11.4 文档校验

```csharp
// PaymentServiceV2.cs 第2788-2812行
// 提交时必须上传付款相关文档
throw new BusinessException($"请款的采购单：{errorCode} 无付款相关文档，请先上传文档");
```

---

## 十二、与其他模块的交互

### 12.1 与采购模块交互

| 交互点 | 说明 |
|--------|------|
| 采购订单确认后 | 可发起请款 |
| 请款提交 | 更新采购明细已请款金额 |
| 请款删除 | 释放采购明细已请款金额 |
| 付款完成 | 更新采购明细已付金额、付款状态 |




```

---

## 十四、相关业务表

| 表名 | 说明 |
|------|------|
| fin_financepaymentv1 | 付款单主表 |
| fin_financepaymentitemv1 | 付款明细表 |
| fin_financepaymentcharges | 付款费用表 |
| purchaseorder | 采购订单表 |
| purchaseorderitem | 采购订单明细表 |
| purchaseorderitemextend | 采购订单明细扩展表 | ？？？

---


---

## 十六、业务特点总结

### 16.1 核心特点


2. **费用明细**: 支持中转行费用、银行手续费、运费、杂费、尾差


### 16.2 注意事项

1. **请款金额控制**: 严格控制请款金额不能超过待请款金额
2. **状态流转**: 状态流转有严格校验，避免非法操作
3. **文档要求**: 提交审批前必须上传付款相关文档
4. **供应商一致性**: 同一付款单必须是同一供应商、同一币别


### 16.3 业务公式汇总

| 公式 | 说明 |
|------|------|
| `待请款金额 = 采购总额 - 已请款金额` | 采购明细待请款计算 |
| `付款总额 = 请款金额 - 抵扣 + 费用 - 尾差` | 付款单总额计算 |
| `付款完成状态 = 已付金额 >= 请款金额` | 订单付款状态判断 |





# 采购请款界面

┌─────────────────────────────────────────────────────────────┐
│                      申请付款窗口                            │
├─────────────────────────────────────────────────────────────┤
│ 供应商信息: [供应商名称]  采购员: [采购员姓名]                │
├─────────────────────────────────────────────────────────────┤
│ 供应商银行: [下拉选择*]  请款方式: [下拉选择*]                │
│ 请款备注: [文本框]                                          │
├─────────────────────────────────────────────────────────────┤
│ 费用明细:                                                   │
│   中转行费用: [输入框]  银行手续费: [输入框]                  │
│   运费: [输入框]  杂费: [输入框]  尾差: [输入框]              │
│   中转行费用承担方: [单选:我方/供应商]                       │
├─────────────────────────────────────────────────────────────┤
│ 订单明细列表:                                               │
│   [采购单号] [型号] [品牌] [数量] [单价] [已请款] [待请款]   │
│   [本次请款金额*] [备注]                                     │
├─────────────────────────────────────────────────────────────┤
│ 供应商应退抵扣: [可选]                                       │
│   [退款单号] [异常单号] [物料] [待收金额] [抵扣金额*]        │
├─────────────────────────────────────────────────────────────┤
│ 异常应付: [可选]                                             │
│   [应付单号] [异常单号] [物料] [待付金额] [申请金额*]        │
├─────────────────────────────────────────────────────────────┤
│ 其他抵扣: [金额] [说明]                                      │
├─────────────────────────────────────────────────────────────┤
│ 合计: 请款总额 [自动计算]                                    │
├─────────────────────────────────────────────────────────────┤
│  [提交审批]  [保存草稿]  [取消]                              │
└─────────────────────────────────────────────────────────────

**注：带 * 号的为必填字段**