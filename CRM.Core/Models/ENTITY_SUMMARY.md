# EBS 业务实体类汇总

## 已创建的实体类（共 31 个）

### 1. 客户管理模块 (Customer/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| CustomerInfo | customerinfo | 客户主表 |
| CustomerContactInfo | customercontactinfo | 客户联系人表 |
| CustomerAddress | customeraddress | 客户地址表 |
| CustomerBankInfo | customerbankinfo | 客户银行账户表 |

### 2. 供应商管理模块 (Vendor/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| VendorInfo | vendorinfo | 供应商主表 |
| VendorContactInfo | vendorcontactinfo | 供应商联系人表 |
| VendorAddress | vendoraddress | 供应商地址表 |
| VendorBankInfo | vendorbankinfo | 供应商银行账户表 |

### 3. 询价管理模块 (RFQ/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| RFQ | rfq | 询价单主表 |
| RFQItem | rfqitem | 询价单明细表 |

### 4. 报价管理模块 (Quote/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| Quote | quote | 报价单主表 |
| QuoteItem | quoteitem | 报价单明细表 |

### 5. 销售管理模块 (Sales/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| SellOrder | sellorder | 销售订单主表 |
| SellOrderItem | sellorderitem | 销售订单明细表 |

### 6. 采购管理模块 (Purchase/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| PurchaseOrder | purchaseorder | 采购订单主表 |
| PurchaseOrderItem | purchaseorderitem | 采购订单明细表 |

### 7. 物料管理模块 (Material/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| MaterialInfo | material | 物料主表 |
| MaterialCategory | materialcategory | 物料分类表 |

### 8. 库存管理模块 (Inventory/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| StockInfo | stock | 库存主表 |
| StockIn | stockin | 入库单主表 |
| StockInItem | stockinitem | 入库单明细表 |
| StockOut | stockout | 出库单主表 |
| StockOutItem | stockoutitem | 出库单明细表 |

### 9. 财务管理模块 (Finance/) ⭐ 新增
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| Invoice | invoice | 发票主表 |
| InvoiceItem | invoiceitem | 发票明细表 |
| Payment | payment | 付款单主表 |
| PaymentItem | paymentitem | 付款单明细表 |
| Receipt | receipt | 收款单主表 |
| ReceiptItem | receiptitem | 收款单明细表 |

### 10. 系统管理模块 (System/)
| 实体类 | 数据库表 | 说明 |
|--------|---------|------|
| BusinessLog | businesslog | 业务操作日志主表 |
| BusinessLogDetail | businesslogdetail | 业务操作日志明细表 |

---

## 业务模块统计

| 业务模块 | 实体数量 | 文件路径 |
|---------|---------|---------|
| 客户管理 | 4 个 | `Models/Customer/` |
| 供应商管理 | 4 个 | `Models/Vendor/` |
| 询价管理 | 2 个 | `Models/RFQ/` |
| 报价管理 | 2 个 | `Models/Quote/` |
| 销售管理 | 2 个 | `Models/Sales/` |
| 采购管理 | 2 个 | `Models/Purchase/` |
| 物料管理 | 2 个 | `Models/Material/` |
| 库存管理 | 5 个 | `Models/Inventory/` |
| 财务管理 | 6 个 | `Models/Finance/` ⭐ |
| 系统管理 | 2 个 | `Models/System/` |
| **总计** | **31 个** | - |

---

## 业务流程关系

### 核心业务流程
```
客户询价流程:
RFQ (询价单) → Quote (报价单) → SellOrder (销售订单) → Receipt (收款)
   ↓                ↓                  ↓                      ↓
RFQItem          QuoteItem         SellOrderItem          ReceiptItem
                                          ↓
                                    Invoice (销项发票)

供应商询价流程:
RFQ (询价单) → Quote (报价单) → PurchaseOrder (采购订单) → Payment (付款)
   ↓                ↓                  ↓                        ↓
RFQItem          QuoteItem         PurchaseOrderItem        PaymentItem
                                          ↓
                                    Invoice (进项发票)
```

### 财务关联关系
```
发票 (Invoice)
    ├─ 进项发票 (InvoiceType=1) → 采购订单 (PurchaseOrder)
    │   └─ InvoiceItem
    └─ 销项发票 (InvoiceType=2) → 销售订单 (SellOrder)
        └─ InvoiceItem

付款 (Payment)
    └─ 供应商 (VendorInfo) ← 采购订单 (PurchaseOrder)
        └─ PaymentItem

收款 (Receipt)
    └─ 客户 (CustomerInfo) ← 销售订单 (SellOrder)
        └─ ReceiptItem
```

---

## 财务管理模块详解

### 发票管理 (Invoice/InvoiceItem)
- **进项发票**: 从供应商处收到的发票，关联采购订单
- **销项发票**: 开给客户的发票，关联销售订单
- 支持增值税专用发票、普通发票、电子发票
- 支持发票认证、抵扣管理

### 付款管理 (Payment/PaymentItem)
- 向供应商付款
- 支持多种付款方式：银行转账、现金、支票、承兑汇票、信用证
- 支持预付款管理
- 关联采购订单进行付款核销

### 收款管理 (Receipt/ReceiptItem)
- 向客户收款
- 支持多种收款方式：银行转账、现金、支票、支付宝、微信支付
- 支持预收款管理
- 关联销售订单进行收款核销

---

## 实体类特性

### 基础类
- **BaseEntity** - 包含 CreateTime, CreateUserId, ModifyTime, ModifyUserId
- **BaseGuidEntity** - 继承 BaseEntity，使用 GUID 作为主键

### 数据注解
- `[Table("tablename")]` - 映射到数据库表
- `[Key]` - 主键标识
- `[StringLength(n)]` - 字符串长度限制
- `[Column(TypeName = "numeric(18,2)")]` - 数值精度
- `[ForeignKey("FieldId")]` - 外键关系

---

## 使用方式

```csharp
// 在 DbContext 中配置
public DbSet<CustomerInfo> Customers { get; set; }
public DbSet<VendorInfo> Vendors { get; set; }
public DbSet<RFQ> RFQs { get; set; }
public DbSet<Quote> Quotes { get; set; }
public DbSet<SellOrder> SellOrders { get; set; }
public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
public DbSet<MaterialInfo> Materials { get; set; }
public DbSet<StockInfo> Stocks { get; set; }
public DbSet<Invoice> Invoices { get; set; }
public DbSet<Payment> Payments { get; set; }
public DbSet<Receipt> Receipts { get; set; }
public DbSet<BusinessLog> BusinessLogs { get; set; }
```
