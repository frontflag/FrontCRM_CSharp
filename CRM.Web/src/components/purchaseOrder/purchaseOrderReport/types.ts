export interface PoReportLineVm {
  index: number
  brand: string
  unit: string
  currency: string
  qty: string
  unitPrice: string
  taxRate: string
  lineTotal: string
  productName: string
  spec: string
}

export type PoPartySeller = {
  name: string
  address: string
  phone: string
  consignee: string
}

export type PoPartyBuyer = {
  name: string
  address: string
}

/** 采购订单打印文档三皮肤共用 props */
export interface PurchaseOrderReportDocumentProps {
  /** 顶栏居中大字：我方（采购方）公司名 */
  headerCompanyName: string
  orderCode: string
  orderDate: string
  deliveryDate: string
  deliveryMode: string
  /** 模版「卖方」= 供应商 */
  partySeller: PoPartySeller
  /** 模版「买方」= 我方 */
  partyBuyer: PoPartyBuyer
  currencyLabel: string
  lines: PoReportLineVm[]
  totalQty: string
  totalIncl: string
  exclTax: string
  taxAmount: string
  grandIncl: string
  taxRateLabel: string
  extraLines: string[]
  terms: string[]
  sealUrl: string | null
  logoUrl: string | null
  showAmounts: boolean
  /** 是否在买方签章区显示印章图 */
  showSeal?: boolean
  /** 买方签章日期 */
  buyerSignDate?: string
}

export const purchaseOrderReportDocumentPropDefaults = {
  buyerSignDate: '',
  showSeal: true
}
