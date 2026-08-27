export interface SoReportLineVm {
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

export type SoPartySeller = {
  name: string
  address: string
  phone: string
  consignee: string
}

export type SoPartyBuyer = {
  name: string
  address: string
}

/** 销售订单打印文档三皮肤共用 props */
export interface SalesOrderReportDocumentProps {
  headerCompanyName: string
  orderCode: string
  orderDate: string
  deliveryDate: string
  deliveryMode: string
  /** 卖方（供方）= 我方 */
  partySeller: SoPartySeller
  /** 买方（客户） */
  partyBuyer: SoPartyBuyer
  currencyLabel: string
  lines: SoReportLineVm[]
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
  showSeal?: boolean
  sellerSignDate?: string
  /** V2：合同编号，无字段时为 — */
  contractNo?: string
  /** V2：付款条款占位，无字段时为 — */
  paymentTerms?: string
  /** V2：交货地址全文 */
  shipTo?: string
  /** V2：交付说明（如运输方式） */
  freightNote?: string
  /** V2：运费金额，无字段时为 — */
  freightAmount?: string
  /** V2：订单备注（交付区展示） */
  orderRemark?: string
}

export const salesOrderReportDocumentPropDefaults = {
  sellerSignDate: '',
  showSeal: true,
  contractNo: '—',
  paymentTerms: '—',
  shipTo: '—',
  freightNote: '—',
  freightAmount: '—',
  orderRemark: '—'
}
