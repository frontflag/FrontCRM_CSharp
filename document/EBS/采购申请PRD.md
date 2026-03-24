# 采购申请业务逻辑文档

## 一、概述

采购申请是EBS系统中连接销售订单和采购订单的关键中间环节。它记录了销售订单需要采购的物料信息，是生成采购订单的前置单据。

### 核心功能

| 功能 | 说明 |
|------|------|
| **销售转采购** | 根据销售订单明细自动生成采购申请 |
| **库存清算** | 优先使用现有库存，减少采购数量 |
| **自动分单** | 系统自动将采购申请分配给采购员 |
| **状态跟踪** | 实时跟踪采购申请的处理状态 |

---

## 二、数据模型

### 1. 采购申请表 (PurchaseRequisition)

```csharp
public class PurchaseRequisition
{
    /// <summary>
    /// 主键Id
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 采购申请号 (系统生成唯一编码)
    /// </summary>
    public string BillCode { get; set; }
    
    /// <summary>
    /// 销售订单明细Id (关联销售订单)
    /// </summary>
    public Guid SellOrderItemId { get; set; }
    
    /// <summary>
    /// 销售订单Id
    /// </summary>
    public Guid SellOrderId { get; set; }
    
    /// <summary>
    /// 采购数量
    /// </summary>
    public long Qty { get; set; }
    
    /// <summary>
    /// 预计采购时间
    /// </summary>
    public DateTime ExpectedPurchaseTime { get; set; }
    
    /// <summary>
    /// 状态 (0:新建 1:部分完成 2:全部完成 3:已取消)
    /// </summary>
    public short Status { get; set; }
    
    /// <summary>
    /// 类型 (0:专属 1:公开)
    /// </summary>
    public short Type { get; set; }
    
    /// <summary>
    /// 采购员Id
    /// </summary>
    public Guid PurchaseUserId { get; set; }
    
    /// <summary>
    /// 业务员组Id
    /// </summary>
    public Guid SalesUserId { get; set; }
    
    /// <summary>
    /// 报价供应商Id
    /// </summary>
    public Guid QuoteVendorId { get; set; }
    
    /// <summary>
    /// 报价单价
    /// </summary>
    public decimal QuoteCost { get; set; }
    
    /// <summary>
    /// 报价币别
    /// </summary>
    public ECurrencyType QuoteVendorCurrency { get; set; }
    
    /// <summary>
    /// 物料Id
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// 物料型号
    /// </summary>
    public string PN { get; set; }
    
    /// <summary>
    /// 品牌
    /// </summary>
    public string Brand { get; set; }
    
    /// <summary>
    /// 已采购数 (关联采购订单数量之和)
    /// </summary>
    public long QtyPurchaseOrder { get; set; }
    
    /// <summary>
    /// 待采购数量
    /// </summary>
    public long QtyPurchaseOrderNot { get; set; }
    
    /// <summary>
    /// 销售取消数量
    /// </summary>
    public long SalesCancelQty { get; set; }
    
    /// <summary>
    /// 异常取消数量 (采购订单取消)
    /// </summary>
    public long AbnormalCancelQty { get; set; }
    
    /// <summary>
    /// 有效采购数量
    /// </summary>
    public long EffectivePurchaseQty { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }
}
```

---

## 三、业务流程

### 1. 整体业务流程图

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              采购申请业务流程                                     │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  销售订单   │     │  库存清算   │     │  采购申请   │     │  采购订单   │
│  审批通过   │────►│  检查库存   │────►│  生成/创建  │────►│  自动生成   │
└─────────────┘     └──────┬──────┘     └──────┬──────┘     └─────────────┘
                           │                    │
                           │                    │
                    ┌──────┴──────┐            │
                    │  有可用库存  │            │
                    │  优先使用   │            │
                    └─────────────┘            │
                                               │
                                               ▼
                                      ┌─────────────┐
                                      │  采购员处理  │
                                      │  找供应商   │
                                      └──────┬──────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │  完成采购   │
                                      └─────────────┘
```

### 2. 采购申请生成方式

| 生成方式 | 触发条件 | 说明 |
|---------|---------|------|
| **自动生成** | 销售订单审批通过 | 系统自动根据销售明细生成采购申请 |
| **手动创建** | 业务员主动发起 | 业务员选择销售明细手动创建 |
| **SRM自动** | SRM库存关联 | 销售明细关联SRM库存时自动生成 |

### 3. 状态流转

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           采购申请状态流转图                                 │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌─────────────┐
    │    新建     │ ◄──────────────────────────────────────────┐
    │   (New)     │                                            │
    │    状态=0   │                                            │
    └──────┬──────┘                                            │
           │                                                    │
           │ 创建采购订单（部分）                                │
           ▼                                                    │
    ┌─────────────┐         ┌─────────────┐                    │
    │   部分完成   │ ──────► │   全部完成   │                    │
    │(PurchaseSection)│      │(PurchaseFinish)                │
    │    状态=1   │  全部   │    状态=2   │                    │
    └──────┬──────┘ 完成   └─────────────┘                    │
           │                                                    │
           │ 取消                                              │
           ▼                                                    │
    ┌─────────────┐                                            │
    │   已取消    │ ◄──────────────────────────────────────────┘
    │   (Cancel)  │  销售取消/异常取消
    │    状态=3   │
    └─────────────┘
```

### 4. 状态说明

| 状态值 | 状态名称 | 说明 |
|-------|---------|------|
| 0 | **新建** | 刚创建的采购申请，尚未生成采购订单 |
| 1 | **部分完成** | 已生成部分采购订单，还有待采购数量 |
| 2 | **全部完成** | 已完成全部采购，待采购数量为0 |
| 3 | **已取消** | 销售取消或异常取消，有效采购数为0 |

---

## 四、核心算法

### 1. 数量计算公式

```csharp
/// <summary>
/// 计算采购申请各种数量
/// </summary>
public BuildCalcQtyResponseDto BuildCalcQty(
    long purchaseReqQty,              // 申请采购数量
    long purchaseReqSalesCancelQty,   // 销售取消数量
    long stockPurchaseRequQty,        // 清库存数量
    List<(long Qty, long CancelQty)> qtyPurchaseOrderItems  // 采购订单明细
)
{
    long purchaseOrderQty = qtyPurchaseOrderItems.Sum(s => s.Qty);
    long purchaseOrderCancelQty = qtyPurchaseOrderItems.Sum(s => s.CancelQty);

    // 1. 已采购数量 = 采购订单数量 + 清库存数量
    long qtyPurchaseOrder = purchaseOrderQty + stockPurchaseRequQty;
    
    // 2. 异常取消数量 = 采购订单取消数量之和
    long abnormalCancelQty = purchaseOrderCancelQty;
    
    // 3. 有效采购数 = 申请数量 - 销售取消 - 异常取消
    long effectivePurchaseQty = purchaseReqQty - purchaseReqSalesCancelQty - abnormalCancelQty;
    
    // 4. 待采购数 = 有效采购数 - (已采购数 - 异常取消)
    long qtyPurchaseOrderNot = effectivePurchaseQty - (qtyPurchaseOrder - abnormalCancelQty);

    return new BuildCalcQtyResponseDto(qtyPurchaseOrder, abnormalCancelQty, effectivePurchaseQty, qtyPurchaseOrderNot);
}
```

### 2. 状态计算逻辑

```csharp
/// <summary>
/// 计算采购申请状态
/// </summary>
public PurchaseRequisitionStatus GetCalcStatus(
    long effectivePurchaseQty,  // 有效采购数
    long qtyPurchaseOrder,      // 已采购数
    long qtyPurchaseOrderNot    // 待采购数量
)
{
    // 全部完成：待采购数为0 且 有效采购数 > 0
    if (qtyPurchaseOrderNot == 0 && effectivePurchaseQty > 0)
    {
        return PurchaseRequisitionStatus.PurchaseFinish;
    }

    // 部分完成：已采购数 > 0 且 待采购数 > 0
    if (qtyPurchaseOrder > 0 && qtyPurchaseOrderNot > 0)
    {
        return PurchaseRequisitionStatus.PurchaseSection;
    }

    // 已取消：有效采购数为0
    if (effectivePurchaseQty == 0)
    {
        return PurchaseRequisitionStatus.Cancel;
    }

    // 新建
    return PurchaseRequisitionStatus.New;
}
```

### 3. 数量关系图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           采购申请数量关系图                                 │
└─────────────────────────────────────────────────────────────────────────────┘

申请采购数量 (Qty)
    │
    ├──► 销售取消 (SalesCancelQty) ────────┐
    │                                        │
    ├──► 异常取消 (AbnormalCancelQty) ──────┤
    │   (采购订单取消)                        │
    │                                        ▼
    └──► 有效采购数 (EffectivePurchaseQty) = Qty - SalesCancelQty - AbnormalCancelQty
                                              │
                                              ├──► 已采购数 (QtyPurchaseOrder)
                                              │      ├──► 采购订单数量
                                              │      └──► 清库存数量
                                              │
                                              └──► 待采购数 (QtyPurchaseOrderNot)
                                                     = EffectivePurchaseQty - (QtyPurchaseOrder - AbnormalCancelQty)
```

---

## 五、核心功能实现

### 1. 自动生成采购申请

```csharp
/// <summary>
/// 自动生成采购申请
/// </summary>
public List<AutoGeneratePurchaseRequisitionResponseDto> AutoGeneratePurchaseRequisition(
    AutoGeneratePurchaseRequisitionRequestDto requestDto)
{
    // 1. 获取关联SRM库存的销售明细
    var sellOrderItems = dbContext.SellOrderItem.ExcludeDelete()
        .Where(x => x.SRMStockItemMergeId != null)
        .Where(x => x.SellOrderId == requestDto.SellOrderId)
        .ToList();

    if (sellOrderItems.Count == 0)
    {
        return new List<AutoGeneratePurchaseRequisitionResponseDto>();
    }

    // 2. 检查是否已生成过采购申请
    var checkResult = HasExistsAutoGeneratedPurchaseReqs(dbContext, requestDto.SellOrderId, ...);
    if (checkResult.HasExists)
    {
        return checkResult.ResponseResult;
    }

    // 3. 创建采购申请
    var addSaveResponse = AddSave(sellOrderItems.Select(x => new AddSavePurchaseRequisitionRequestDto
    {
        SellOrderItemId = x.SellOrderItemId,
        Qty = x.Qty,
        ExpectedPurchaseTime = DateTime.Now,
        Remark = "自动生成的采购申请",
        FromType = EDataFromType.Srm,
        Type = (short)AddPurchaseRequisition.SellOrderItem,
        PurchaseUserId = x.PurchaseUserId
    }).ToList());

    return addSaveResponse.Select(x => new AutoGeneratePurchaseRequisitionResponseDto(...)).ToList();
}
```

### 2. 清库存处理

```csharp
/// <summary>
/// 保存采购申请清库存
/// </summary>
public void SavePurchaseRequisitionStock(Guid id, List<StockItemPo> listItem)
{
    using (var dbContext = CreateContext())
    {
        var purchaseReq = dbContext.PurchaseRequisition.FindExcludeDelete(id);
        
        // 1. 验证不能是公开备货类型
        if (purchaseReq.Type == (short)EnumAscriptionType.Common)
        {
            throw new BusinessException("该采购申请为公开备货的采购申请单，不能进行清库存操作！");
        }

        // 2. 清库存数量验证
        var qtyAll = listItem.Sum(s => s.Qty);
        if (qtyAll > purchaseReq.Qty)
        {
            throw new BusinessException("清库存数量不能大于采购申请数量！");
        }

        // 3. 创建库存关联记录
        foreach (var item in listItem)
        {
            var relation = new StockPurchaseRequisitionRelation
            {
                PurchaseRequisitionId = id,
                StockItemId = item.StockItemId,
                Qty = item.Qty
            };
            dbContext.StockPurchaseRequisitionRelation.Add(relation);
            
            // 4. 扣减可用库存
            var stockItem = dbContext.StockItemV2.Find(item.StockItemId);
            stockItem.QtyRepertoryAvailable -= item.Qty;
            stockItem.QtySales += item.Qty;
        }

        // 5. 重新计算数量
        CalcQty(purchaseReq, dbContext);
        CalcStatus(purchaseReq);
        
        dbContext.SaveChanges();
    }
}
```

### 3. 反清库存处理

```csharp
/// <summary>
/// 反清库存
/// </summary>
public void ReversePurchaseRequisitionStock(ReverseStockClearRequestDto request)
{
    using (var dbContext = CreateContext())
    {
        var purchaseReq = dbContext.PurchaseRequisition.FindExcludeDelete(request.Id);
        
        if (request.ReverseStockType == EReverseStockType.Old)
        {
            // 1. 创建新的采购申请记录
            var newPurchaseReq = ObjectCopyUtil.CopyFrom<PurchaseRequisition>(purchaseReq);
            newPurchaseReq.Id = IdUtil.NewGuidId();
            newPurchaseReq.Qty = request.Qty;
            newPurchaseReq.QtyPurchaseOrder = 0;
            newPurchaseReq.QtyPurchaseOrderNot = request.Qty;
            newPurchaseReq.AbnormalCancelQty = 0;
            newPurchaseReq.EffectivePurchaseQty = request.Qty;
            newPurchaseReq.SalesCancelQty = 0;
            newPurchaseReq.Status = (short)PurchaseRequisitionStatus.New;
            newPurchaseReq.BillCode = GetSystemCode(nameof(PurchaseRequisition));
            dbContext.PurchaseRequisition.Add(newPurchaseReq);
            
            addPurRequireid = newPurchaseReq.Id;
        }
        
        // 2. 扣减原采购申请数量
        purchaseReq.Qty -= request.Qty;
        purchaseReq.EffectivePurchaseQty = purchaseReq.Qty 
            - purchaseReq.SalesCancelQty 
            - purchaseReq.AbnormalCancelQty;
        purchaseReq.QtyPurchaseOrderNot = purchaseReq.Qty 
            - purchaseReq.SalesCancelQty 
            - purchaseReq.QtyPurchaseOrder;
        
        // 3. 更新状态
        purchaseReq.Status = (short)PurchaseRequisitionStatus.PurchaseSection;
        if (purchaseReq.EffectivePurchaseQty > 0 && purchaseReq.QtyPurchaseOrderNot == 0)
        {
            purchaseReq.Status = (short)PurchaseRequisitionStatus.PurchaseFinish;
        }
        
        dbContext.SaveChanges();
    }
}
```

---

## 六、API接口列表

### 1. 查询接口

| 接口 | 说明 |
|------|------|
| `GetPurchaseRequisitionListBySearch` | 采购申请列表查询（支持分页和筛选） |
| `GetPurchaseRequisitionDetail` | 获取采购申请详情 |
| `GetPurchaseRequisitionEntity` | 获取采购申请实体 |
| `GetBySellOrderItemId` | 根据销售明细ID获取采购申请列表 |
| `GetModifyDataModel` | 获取采购申请编辑数据 |

### 2. 新增/编辑接口

| 接口 | 说明 |
|------|------|
| `AddSavePurchaseRequisition` | 保存单个采购申请 |
| `AddSavePurchaseRequisitionList` | 批量保存采购申请 |
| `ModifySavePurchaseRequisition` | 编辑保存采购申请 |
| `AutoGeneratePurchaseRequisition` | 自动生成采购申请 |

### 3. 删除接口

| 接口 | 说明 |
|------|------|
| `DeletePurchaseRequisition` | 删除采购申请 |
| `DeleteBizDataFlow` | 高级删除（包含下游数据） |

### 4. 库存相关接口

| 接口 | 说明 |
|------|------|
| `GetStockUseListByPurchaseRequisition` | 获取可用库存列表 |
| `SavePurchaseRequisitionStock` | 保存清库存 |
| `SavePurchaseRequisitionSRMStock` | 云仓清库存 |
| `ReversePurchaseRequisitionStock` | 反清库存 |

### 5. 其他接口

| 接口 | 说明 |
|------|------|
| `EditPurchase` | 修改采购员 |
| `CalcQty` | 计算数量 |
| `CalcStatus` | 计算状态 |
| `GetSummaryInfoByCode` | 获取汇总信息 |

---

## 七、关联表

| 表名 | 说明 |
|------|------|
| `PurchaseRequisition` | 采购申请主表 |
| `SellOrder` | 销售订单主表 |
| `SellOrderItem` | 销售订单明细表 |
| `PurchaseOrder` | 采购订单主表 |
| `PurchaseOrderItem` | 采购订单明细表 |
| `StockItemV2` | 库存表 |
| `StockPurchaseRequisitionRelation` | 库存与采购申请关联表 |
| `StockInV2` | 入库主表 |

---

## 八、业务规则

### 1. 创建规则

| 规则 | 说明 |
|------|------|
| 来源限制 | 采购申请必须关联销售明细或手动选择物料 |
| 数量限制 | 采购数量必须大于0 |
| 重复限制 | 同一销售明细不能重复生成采购申请 |
| 状态限制 | 已完成的销售订单不能创建采购申请 |

### 2. 编辑规则

| 规则 | 说明 |
|------|------|
| 数量限制 | 不能小于已采购数量 |
| 状态限制 | 已完成的采购申请不能编辑数量 |
| 关联限制 | 编辑时同步更新关联的采购订单 |

### 3. 删除规则

| 规则 | 说明 |
|------|------|
| 关联检查 | 已生成采购订单的不能删除 |
| 库存检查 | 已清库存的需先反清库存 |
| 权限检查 | 只有创建人或管理员可删除 |

### 4. 清库存规则

| 规则 | 说明 |
|------|------|
| 类型限制 | 公开备货类型不能清库存 |
| 数量限制 | 清库存数量不能大于申请数量 |
| 库存检查 | 必须有足够的可用库存 |
| 状态检查 | 已完成的不能清库存 |

---

## 九、消息事件

### 1. 发布的事件

| 事件 | 说明 |
|------|------|
| `PurchaseRequisitionAddedMessage` | 采购申请创建事件 |
| `PurchaseRequisitionUpdatedMessage` | 采购申请更新事件 |
| `PurchaseRequisitionDeletedMessage` | 采购申请删除事件 |

### 2. 事件处理

```csharp
// 采购申请创建事件处理
public class PurchaseRequisitionAddedMessageHandler 
{
    public void Handle(PurchaseRequisitionAddedMessage message)
    {
        // 1. 更新销售明细的采购申请数量
        UpdateSellOrderItemPurchaseRequisitionQty(message.PurchaseRequisitionId);
        
        // 2. 生成业务生命周期节点
        GenerateBusinessNode(message.PurchaseRequisitionId);
        
        // 3. 同步到CRM
        SyncToCrm(message.PurchaseRequisitionId);
    }
}
```

---

## 十、数据流图

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              采购申请数据流向                                     │
└─────────────────────────────────────────────────────────────────────────────────┘

销售订单审批通过
        │
        ▼
┌─────────────────────┐
│  自动生成采购申请    │
│  (销售订单明细)      │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐     ┌─────────────────────┐
│   PurchaseRequisition│     │   StockItemV2       │
│   (采购申请)         │◄────│   (库存清算)         │
│                     │ 关联 │                     │
│ - Qty (申请数量)     │     │ - QtyRepertoryAvailable│
│ - QtyPurchaseOrder   │     │ - QtySales          │
│ - QtyPurchaseOrderNot│     └─────────────────────┘
└──────────┬──────────┘
           │
           │ 生成
           ▼
┌─────────────────────┐
│   PurchaseOrder     │
│   (采购订单)         │
│                     │
│ - PurchaseRequisitionId (关联)│
│ - Qty (采购数量)     │
│ - Status (状态)      │
└─────────────────────┘
```

---

**文档创建时间**: 2025年3月
**版本**: v1.0
**适用范围**: EBS系统采购申请模块
