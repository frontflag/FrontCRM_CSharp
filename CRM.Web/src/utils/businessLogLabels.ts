/** 与后端 BusinessLogTypes / log_operation.BizType 一致 */
export function operationBizTypeLabel(bizType?: string | null): string {
  if (!bizType) return '';
  const m: Record<string, string> = {
    Customer: '客户',
    CustomerContact: '客户联系人',
    CustomerAddress: '客户地址',
    CustomerBank: '客户银行',
    CustomerContactHistory: '客户联系记录',
    Vendor: '供应商',
    VendorContact: '供应商联系人',
    VendorAddress: '供应商地址',
    VendorBank: '供应商银行',
    VendorContactHistory: '供应商联系记录',
    Quote: '报价',
    QuoteItem: '报价明细',
    Rfq: '需求',
    RfqItem: '需求明细',
    SalesOrder: '销售订单',
    SellOrderItem: '销售订单明细',
    PurchaseOrder: '采购订单',
    PurchaseOrderItem: '采购订单明细',
    PurchaseRequisition: '采购申请',
    InventoryStock: '库存桶',
    InventoryStockItem: '库存明细',
    PickingTask: '拣货单',
    StockIn: '入库单',
    StockInBatch: '入库批次',
    StockOutBatch: '出库批次',
    StockOut: '出库单',
    Packing: '装箱单',
    BatchReconciliation: '批次核销',
    QcInspection: '质检',
    CustomsDeclaration: '报关单',
    CustomsBroker: '报关行',
    FreightForwarderCompany: '货代公司',
    FinancePayment: '付款单',
    FinanceReceipt: '收款单',
    FinanceSellInvoice: '销项发票',
    FinancePurchaseInvoice: '进项发票',
    FinanceReceivable: '应收款',
    FinanceCustomerAdvance: '预收款',
    FinanceFreightForwarderPayable: '货代付款',
    Document: '文档'
  };
  return m[bizType] ?? bizType;
}

/** 客户/供应商变更日志表格「对象」列（与采销订单主表/子表展示一致） */
export function masterEntityChangeLogObjectLabel(
  bizType: string | null | undefined,
  recordCode: string | null | undefined,
  mainBizType: 'Customer' | 'Vendor'
): string {
  if (!bizType || bizType === mainBizType) return '主表';
  const code = recordCode?.trim();
  if (code) return code;
  const label = operationBizTypeLabel(bizType);
  return label || '子表';
}

/** 报价详情更改日志「对象」列 */
export function quoteChangeLogObjectLabel(row: {
  objectLabel?: string | null
  fieldName?: string | null
}): string {
  const obj = row?.objectLabel?.trim()
  if (obj) return obj
  if (row?.fieldName === 'lineAdded') return '明细'
  return '主表'
}

/** 需求详情更改日志「对象」列 */
export function rfqChangeLogObjectLabel(row: {
  objectLabel?: string | null
  fieldName?: string | null
}): string {
  const obj = row?.objectLabel?.trim()
  if (obj) return obj
  if (row?.fieldName === 'lineAdded') return '明细'
  return '主表'
}
