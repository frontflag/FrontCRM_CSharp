/** 与后端 BusinessLogTypes / log_operation.BizType 一致 */
export function operationBizTypeLabel(bizType?: string | null): string {
  if (!bizType) return '';
  const m: Record<string, string> = {
    Customer: '客户',
    CustomerContact: '客户联系人',
    Vendor: '供应商',
    VendorContact: '供应商联系人',
    SalesOrder: '销售订单',
    PurchaseOrder: '采购订单',
  };
  return m[bizType] ?? bizType;
}
