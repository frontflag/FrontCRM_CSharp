# EBS 请款/付款业务逻辑文档

## 一、概述

### 1.1 业务背景

请款/付款是财务系统的核心业务模块，用于管理向供应商支付采购货款及相关费用的全流程。包括从采购订单入库后的请款申请、审批、付款确认到最终付款完成的完整流程。

### 1.2 核心概念

| 概念 | 说明 |
|------|------|
| **请款** | 向公司申请支付供应商货款的流程 |
| **付款** | 实际执行资金支付的操作 |
| **应付** | 异常业务产生的供应商应付款项（扣款/赔偿） |
| **抵扣** | 使用供应商退款或应付来冲抵本次请款金额 |
| **待请款金额** | 采购订单中尚未申请付款的金额 |
| **已请款金额** | 已经申请但尚未完成的付款金额 |

### 1.3 业务流程图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           请款/付款业务流程                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐             │
│   │ 采购入库 │───>│ 申请请款 │───>│ 提交审批 │───>│ 审批通过 │             │
│   └──────────┘    └──────────┘    └──────────┘    └──────────┘             │
│                                                        │                    │
│                                                        ▼                    │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐             │
│   │ 付款完成 │<───│ 确认付款 │<───│ 推送股份 │<───│ 应付款   │             │
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

**表名**: `fin_financepaymentchargesv1`

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

| 值 | 状态 | 说明 |
|----|------|------|
| 1 | **New** (新建) | 付款单刚创建 |
| 2 | **DAudit** (待审核) | 已提交审批 |
| 10 | **YPayment** (应付款) | 审批通过，等待付款 |
| 16 | **Paymenting** (付款中) | 付款正在进行 |
| 20 | **Finish** (付款完成) | 付款已完成 |
| 17 | **Refund** (已退款) | 已退款 |
| -2 | **Fail** (审核失败) | 审批被拒绝 |

### 4.2 状态流转图

```
                            ┌─────────────────────────────────────────────┐
                            │              付款单状态流转                   │
                            └─────────────────────────────────────────────┘

    ┌─────────┐   提交审批   ┌──────────┐   审批通过   ┌───────────┐   推送股份   ┌────────────┐
    │  新建   │ ───────────> │  待审核  │ ──────────> │  应付款   │ ──────────> │  付款中    │
    │  (1)   │              │   (2)    │             │   (10)    │             │   (16)     │
    └────┬────┘              └────┬─────┘             └─────┬─────┘             └─────┬──────┘
         │                        │                         │                         │
         │                        │ 审批拒绝                │                         │ 付款完成
         │                        ▼                         │                         ▼
         │                   ┌──────────┐                   │                    ┌────────────┐
         │                   │ 审核失败 │                   │                    │ 付款完成   │
         │                   │  (-2)    │                   │                    │   (20)     │
         │                   └────┬─────┘                   │                    └─────┬──────┘
         │                        │                         │                          │
         │                        │ 重新编辑                │                          │ 退款
         └────────────────────────┘                         │                          ▼
                                                            │                     ┌────────────┐
                                                            │                     │  已退款    │
                                                            │                     │   (17)     │
                                                            │                     └────────────┘
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
| New → DAudit | 用户点击"提交审批" | 采购员/财务 |
| DAudit → YPayment | 审批中心审批通过 | 审批系统 |
| DAudit → Fail | 审批中心审批拒绝 | 审批系统 |
| Fail → New | 用户编辑后重新保存 | 采购员/财务 |
| YPayment → Paymenting | 推送股份系统成功 | 系统自动 |
| YPayment → Finish | 手动确认付款完成 | 财务专员 |
| Paymenting → Finish | 股份系统回调付款完成 | 系统自动 |
| Finish → Refund | 供应商退款 | 财务专员 |

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

1. 采购订单已入库
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

#### 步骤4: 选择抵扣信息（可选）

**供应商退款抵扣**:
| 字段 | 说明 |
|------|------|
| VendorRefundToBeId | 供应商应退ID |
| ApplyAmount | 申请抵扣金额 |

**异常应付抵扣**:
| 字段 | 说明 |
|------|------|
| PaymentToBeId | 应付ID |
| ApplyAmount | 申请金额 |

**其他抵扣**:
| 字段 | 说明 |
|------|------|
| OtherDeductionAmount | 其它抵扣金额 |
| OtherDeductionRemark | 其它抵扣说明（金额>0时必填） |

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
4. 保存抵扣信息 (VendorRefundToBeTackOut)
5. 更新采购明细已请款金额
6. 生成业务编号
7. 发送MQ消息

---

## 七、审批流程

### 7.1 审批触发

用户点击"提交审批"后:
1. 付款单状态变更为 **DAudit (待审核)**
2. 发送审批中心消息
3. 创建审批流程实例

### 7.2 审批结果处理

**审批通过**:
```csharp
// PaymentServiceV1.cs 第653行
auditStatus = (short)FinancePaymentAudit.YPayment;  // 变更为应付款
```

**审批拒绝**:
```csharp
// PaymentServiceV1.cs 第659行
auditStatus = (short)FinancePaymentAudit.Fail;      // 变更为审核失败
```

**审批退回**:
```csharp
// PaymentServiceV1.cs 第665行
auditStatus = (short)FinancePaymentAudit.New;       // 退回新建状态
```

### 7.3 审批回调消息处理

**PaymentApprovalCallbackMessageHandler** 处理审批结果:
- 应付款状态推送到股份系统
- 发送邮件通知相关人员

---

## 八、付款流程

### 8.1 股份系统推送

审批通过后，自动推送至股份系统 (HQIndustry):

```csharp
// PaymentApprovalCallbackMessageHandler.cs 第41-52行
if (paymentInfo.FinancePaymentEntity.Status == FinancePaymentAudit.YPayment)
{
    GetService<PaymentExternalBusinessService>().SyncPaymentOrderInfoToHQIndustry(...);
}
```

### 8.2 手动确认付款

**前提条件**:
- 付款单状态为 **YPayment (应付款)**
- 股份审批状态为未审批或审批失败

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

2. **发送MQ消息**:
   - 业务流程消息 (BizFlowDataMessage)
   - 全文检索消息 (BizData2FinanceMessage)
   - 采购明细刷新消息 (FinPaymentToPurItemMessage)
   - 自动核销消息 (FinMQMessage)

3. **自动核销**:
   - 触发付款类自动核销逻辑

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

### 9.2 删除请款

**前提条件**:
- 付款单状态为 **New (新建)** 或 **Fail (审核失败)**

**删除后处理**:
1. 删除付款主表、明细表、费用表
2. 释放抵扣金额（供应商退款、应付）
3. 更新采购明细已请款金额
4. 发送MQ消息

---

## 十、退款流程

### 10.1 退款前提

- 付款单状态为 **Finish (付款完成)**

### 10.2 退款处理

```csharp
// VendorRefundService.cs 第654-695行
payment.Status = (short)FinancePaymentAudit.Refund;  // 状态变更为已退款
```

---

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
| 采购入库后 | 可发起请款 |
| 请款提交 | 更新采购明细已请款金额 |
| 请款删除 | 释放采购明细已请款金额 |
| 付款完成 | 更新采购明细已付金额、付款状态 |

### 12.2 与审批中心交互

| 交互点 | 说明 |
|--------|------|
| 提交审批 | 创建审批流程 |
| 审批回调 | 更新付款单状态 |
| 审批详情 | 提供付款详情供审批查看 |

### 12.3 与股份系统交互

| 交互点 | 说明 |
|--------|------|
| 推送请款 | 应付款推送到股份系统 |
| 付款回调 | 股份系统回调付款结果 |
| 状态同步 | 同步股份审批状态 |

### 12.4 与业务节点交互

付款完成后，更新销售订单业务进度节点:
- 付款节点状态更新
- 业务编号状态更新

---

## 十三、MQ消息类型

### 13.1 请款相关消息

| 消息类型 | 说明 | 处理方 |
|----------|------|--------|
| BizFlowDataMessage | 业务流程消息 | TradeMain模块 |
| BizData2FinanceMessage | 全文检索消息 | 搜索服务 |
| FinPaymentToPurItemMessage | 采购明细刷新消息 | 采购模块 |
| FinMQMessage (AutomaticPayWriteOff) | 自动核销消息 | 核销服务 |
| PaymentApprovalCallbackMessage | 审批回调消息 | PaymentService |

### 13.2 消息内容示例

```csharp
// 业务流程消息
new BizFlowDataMessage
{
    CreateUserId = CurrentUserId,
    EnterpriseId = EnterpriseId,
    BizType = BizDataType.FinPayment,
    BusType = (short)FinPaymentBizFlow.Edit,  // 新增/编辑/删除
    DataIds = new List<Guid> { paymentId }
}
```

---

## 十四、相关业务表

| 表名 | 说明 |
|------|------|
| fin_financepaymentv1 | 付款单主表 |
| fin_financepaymentitemv1 | 付款明细表 |
| fin_financepaymentchargesv1 | 付款费用表 |
| fin_vendorrefundtobetackout | 抵扣记录表 |
| fin_vendorrefundtobe | 供应商应退表 |
| fin_paymenttobe | 应付表 |
| trad_main_purchaseorderitem | 采购订单明细表 |
| trad_main_purchaseorderitemextend | 采购订单明细扩展表 |

---

## 十五、关键接口和服务

### 15.1 服务接口

| 接口 | 说明 |
|------|------|
| IPaymentServiceV2 | 付款单V2版本服务 |
| IPaymentServiceV1 | 付款单V1版本服务 |
| IPaymentToBeService | 应付服务 |
| IVendorRefundService | 供应商退款服务 |

### 15.2 核心方法

| 方法 | 说明 |
|------|------|
| ApplyPayment | 申请请款 |
| EditPayment | 编辑请款 |
| DeletePayment | 删除请款 |
| ConfirmPayment | 确认付款 |
| CancelAudit | 取消应付 |
| SyncPaymentOrderInfoToHQIndustry | 推送股份系统 |

---

## 十六、业务特点总结

### 16.1 核心特点

1. **多维度抵扣**: 支持供应商退款抵扣、异常应付抵扣、其他抵扣
2. **费用明细**: 支持中转行费用、银行手续费、运费、杂费、尾差
3. **多级审批**: 集成审批中心，支持复杂审批流程
4. **股份对接**: 与股份系统对接，实现资金统一管理
5. **自动核销**: 付款完成后自动触发核销流程

### 16.2 注意事项

1. **请款金额控制**: 严格控制请款金额不能超过待请款金额
2. **状态流转**: 状态流转有严格校验，避免非法操作
3. **文档要求**: 提交审批前必须上传付款相关文档
4. **供应商一致性**: 同一付款单必须是同一供应商、同一币别
5. **抵扣释放**: 删除请款时必须正确释放抵扣金额

### 16.3 业务公式汇总

| 公式 | 说明 |
|------|------|
| `待请款金额 = 采购总额 - 已请款金额` | 采购明细待请款计算 |
| `付款总额 = 请款金额 - 抵扣 + 费用 - 尾差` | 付款单总额计算 |
| `付款完成状态 = 已付金额 >= 请款金额` | 订单付款状态判断 |
