# 申请出库业务逻辑文档

## 1. 概述

### 1.1 业务背景

申请出库（出库通知）是销售订单执行流程中的关键环节，业务人员根据销售订单明细创建出库通知，通知仓库准备发货。出库通知连接销售订单和实际物流出库操作。

### 1.2 核心概念

| 概念 | 说明 |
|------|------|
| **出库通知** | 业务人员向仓库发出的发货指令 |
| **出库通知数量** | 本次申请出库的物料数量 |
| **剩余出库通知数量** | 销售订单中还可申请出库的数量 |
| **已出库数量** | 已经完成出库操作的数量 |
| **排期** | 出库通知的物流排期状态 |

---

## 2. 数据模型

### 2.1 出库通知主表 (StockOutNotify)

| 字段名 | 类型 | 说明 |
|--------|------|------|
| Id | Guid | 主键 |
| ProductId | Guid | 物料Id |
| PN | string | 物料型号 |
| Brand | string | 品牌 |
| Unit | string | 物料单位 |
| SellOrderItemId | Guid | 销售明细Id |
| CustomerId | Guid | 客户Id |
| SalesUserId | Guid | 业务员Id |
| SellOrderId | Guid | 销售订单Id |
| SellOrderCode | string | 销售单号 |
| Qty | long | 出库通知数量 |
| QtyPackingSales | long | 已销售出库数量 |
| QtyPackingDeclare | long | 已报关出库数量 |
| ExpectedDeliveryDate | DateTime | 预计出库日期 |
| Status | short | 状态(枚举 EnumStockOutNotifyStatus) |
| Remark | string | 备注 |
| PackingCode | string | 装箱单号 |
| Type | short | 出货通知类型(正常/换货) |
| PayUnit | int | 付款方(1我方/2客户) |
| ExpressAccount | string | 快递账号 |
| ApplicationUnit | short | 申报方(1寄件方/2收件方) |
| Source | short | 目的地(1香港本地/2香港以外) |
| StockInNo | string | 入仓号 |
| FreightPayType | short | 运费支付方式 |
|

### 2.2 出库通知状态 (EnumStockOutNotifyStatus)

| 值 | 状态 | 说明 |
|----|------|------|
| 1 | New(新建) | 出库通知刚创建 |
| 10 | InPacking(装箱中) | 正在装箱处理中 |
| 100 | Finish(完成) | 出库完成 |

### 2.3 出库通知排期状态 (EnumStockOutNotifySchedule)

| 值 | 状态 | 说明 |
|----|------|------|
| 1 | Waiting(等待) | 等待排期 |
| 2 | Start(开始) | 排期开始 |
| 100 | Finish(结束) | 排期结束 |



---

## 3. 申请出库业务流程

### 3.1 业务流程图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            申请出库业务流程                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. 前置条件检查                                                         │
│     ├── 销售订单已审核通过                                               │
│     ├── 销售订单未完成                                                   │                                      │
│                              ↓                                          │
│  2. 获取出库数据                                                         │
│     ├── 查询销售订单明细                                                 │
│     ├── 查询已存在的出库通知                                             │                                                │
│     ├── 获取客户地址列表                                                 │
│                                           │
│                              ↓                                          │
│  3. 填写出库通知信息                                                     │
│     ├── 选择出货方式(快递/送货/自提)                                      │
│     ├── 选择收货地址                                                     │
│     ├── 填写出库数量                                                     │
│     ├── 选择运费支付方                                                   │
│     ├── 选择快递公司(快递模式)                                            │
│     ├── 填写预计出库日期                                                 │
│     └── 填写备注                                                         │
│                              ↓                                          │
│  4. 提交出库申请                                                         │
│     ├── 校验出库数量 ≤ 剩余可出库数量                                     │
│     ├── 校验快递模式下运费支付方                                          │                                                         
│     └── 创建出库通知记录                                                  │
│                              ↓                                          │
│  5. 后续处理                                                             │                               
│     ├── 保存收货地址信息                                                  │
│     ├── 保存出货要求                                                      │                               
│     └── 生成业务编号                                                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.2 核心方法流程

```csharp
// 1. 获取新建出库通知数据
GetAddModel(listSellOrderItemId)
    ├── 查询销售订单明细
    ├── 检查订单状态(已审核/非临时/非完成)
    ├── 检查合同上传(线下/PCB)
    ├── 查询已存在出库通知
    ├── 获取客户地址列表


// 2. 保存出库通知列表
AddSaveList(stockOutNotifies, addressSendType)
    ├── 校验出库数量 > 0
    ├── 校验快递模式下运费支付方
    ├── 循环处理每个出库通知
    │       ├── 获取销售订单明细
    │       ├── 校验出库数量 ≤ 剩余数量
    │       ├── 创建出库通知实体
    │       └── 添加到列表
    ├── 批量保存出库通知
    ├── 保存收货地址
    ├── 保存出货要求
    └── 生成业务编号
```

---

## 4. 核心业务规则

### 4.1 申请出库前置条件

| 规则编号 | 规则描述 | 校验逻辑 |
|----------|----------|----------|
| R001 | 销售订单状态 | 必须已审核通过(Status >= PassAudit) |
| R002 | 订单完成状态 | 不能是已完成订单(Status != Finish) |
| R006 | 合同上传 | 线下/PCB订单需已确认合同 |

### 4.2 出库数量规则

```csharp
// 剩余可出库通知数量计算
QtyStockOutNotifyNot = 销售数量 + 异常出库数量 - 已申请出库通知数量

// 出库数量校验
申请出库数量 ≤ 剩余可出库通知数量

// 销售订单扩展表更新
QtyStockOutNotify = 已存在的出库通知数量 + 本次申请数量
QtyStockOutNotifyNot = 销售数量 + 异常出库数量 - QtyStockOutNotify
```

### 4.3 出货方式规则

| 出货方式 | 必填字段 | 特殊校验 |
|----------|----------|----------|
| 快递(Express) | 快递公司、运费支付方 | PayUnit不能为空 |
| 送货(Delivery) | 收货地址 | - |
| 自提(Selflifting) | 提货信息 | - |


---

## 5. 出库通知类型

### 5.1 按业务类型分类 (EnumStockOutNotifyType)

| 值 | 类型 | 说明 |
|----|------|------|
| 1 | Normal(正常) | 正常销售出库 |
| 2 | Exchange(换货) | 售后换货出库 |

### 5.2 按物料类型分类 (OutMaterialType)

| 值 | 类型 | 说明 |
|----|------|------|
| 0 | 正常物料 | 普通销售物料 |
| 1 | 替换物料 | 换货使用的替换物料 |

### 5.3 按目的地分类 (Source)

| 值 | 类型 | 说明 |
|----|------|------|
| 1 | 香港本地 | 香港本地发货 |
| 2 | 香港以外 | 海外发货 |

---

## 6. 数据关联关系

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            数据关联关系                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   销售订单(SellOrder)                                                   │
│        │                                                                │
│        ├── 销售订单明细(SellOrderItem) ←── 一对多                       │
│        │       │                                                        │
│        │       ├── 出库通知(StockOutNotify) ←── 一对多                  │
│        │       │       ├── 收货地址(TradeAddress) ←── 一对一            │
│        │       │       ├── 出货要求(DeliveryAsk) ←── 一对一             │
│        │       │       └── 装箱单(Packing) ←── 多对多                   │
│        │       │                                                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. 业务节点更新

### 7.1 出库通知业务节点 (StockOutNotifyBusinessNodeService)

```csharp
// 更新时机：出库通知创建/变更/删除时

// 计算逻辑
fiStockOutTotalQty = 流程实例下所有出库通知数量之和
fiSoiTotalQty = 流程实例下所有销售明细数量之和

// 状态判断
if (fiStockOutTotalQty == 0) {
    状态 = 待出货通知(PendingNotificationOfDelivery)
} else {
    状态 = 已出货通知(NotifiedOfDelivery)
}

// 物流状态
if (fiPackingCustomerItems.Count == 0) {
    物流状态 = 物流未处理(LogisticsUntreated)
} else {
    物流状态 = 物流已处理(LogisticsProcessed)
}
```

### 7.2 节点状态定义

| 状态类型 | 状态值 | 说明 |
|----------|--------|------|
| 销售订单状态 | PendingNotificationOfDelivery | 待出货通知 |
| 销售订单状态 | NotifiedOfDelivery | 已出货通知 |
| 物流状态 | LogisticsUntreated | 物流未处理 |
| 物流状态 | LogisticsProcessed | 物流已处理 |

---

## 8. 核心业务校验

### 8.1 出库数量校验

```csharp
// 校验逻辑
foreach (var dto in stockOutNotifies) {
    // 1. 出库数量必须大于0
    if (dto.Qty <= 0) {
        throw new BusinessException("出库通知数量应该大于0！");
    }
    
    // 2. 出库数量不能超过剩余数量
    var existedQty = 已存在的出库通知数量之和;
    var totalQty = existedQty + dto.Qty;
    var maxQty = sellOrderItem.Qty + sellOrderItem.QtyAbnStockOut;
    
    if (totalQty > maxQty) {
        throw new BusinessException("申请出库通知数量大于销售订单明细中的剩余出库通知数量！");
    }
}
```

### 8.2 快递模式校验

```csharp
// 快递模式下必须选择运费支付方
if (deliveryAddress.Receivingway == EShippingMethod.Express && dto.PayUnit == 0) {
    throw new BusinessException("快递模式下请选择运费支付方！");
}
```

### 8.3 单据变更锁定校验

```csharp
// 通过Aspect拦截器检查
public class ApplyStockOutAspect : BaseServiceAspect {
    public override void ExecBefore(ServiceAspectContext context) {
        var msg = stockInNotifyService.CheckDocumentChange(
            ELimitWarehouseOperatorType.ApplyStockOut, 
            sellOrderItemIds
        );
        if (!string.IsNullOrEmpty(msg)) {
            throw new BusinessException(msg);
        }
    }
}
```

---

## 9. 相关业务表

| 表名 | 说明 | 关联关系 |
|------|------|----------|
| StockOutNotify | 出库通知主表 | 核心表 |
| SellOrderItem | 销售订单明细表 | 关联销售订单 |
| SellOrderItemExtend | 销售订单扩展表 | 存储出库数量统计 |
| TradeAddress | 交易地址表 | 存储收货/账单地址 |
| DeliveryAsk | 出货要求表 | 存储出货要求 |
| Packing | 装箱单表 | 关联出库通知和装箱 |
| PackingCustomerItem | 客户拣货单表 | 关联装箱和出库 |
| 

---

## 10. 关键接口和服务



---




---

## 12. 业务特点总结

### 12.1 核心特点

1. **严格的前置校验**：出库前需要检查订单状态、客户信息、逾期情况等多个条件
2. **灵活的出货方式**：支持快递、送货、自提等多种出货方式
3. **数量精确控制**：严格校验出库数量不超过销售订单剩余数量

5. **与库存强关联**：出库通知会影响库存的可用数量

### 12.2 业务流程特点

```
销售订单 → 入库 → 申请出库 → 装箱 → 报关 → 实际出库
                ↑
           出库通知(本环节)
```

### 12.3 数据一致性保证

1. **事务控制**：出库通知创建使用数据库事务

