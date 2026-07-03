/** 待生成采购单篮子中的一条采购申请（列表/详情共用快照）。 */
export type PurchaseRequisitionBasketItem = {
  id: string
  billCode: string
  pn?: string
  brand?: string
  qty: number
  status: number
  sellOrderCode?: string
  quoteVendorId?: string
}
