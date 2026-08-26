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
  /** 批号 / DateCode；V2 打印，空则 — */
  lotNo?: string
}

export type PoPartySeller = {
  name: string
  address: string
  phone: string
  consignee: string
  contact?: string
  /** V2 单独展示的联系电话；V1 仍用 phone（联系人+电话） */
  contactPhone?: string
  email?: string
}

export type PoPartyBuyer = {
  name: string
  address: string
  contact?: string
  phone?: string
  email?: string
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
  /** V2：合同编号，无独立字段时为 — */
  contractNo?: string
  /** V2：付款条款（订单备注） */
  paymentTerms?: string
  /** V2：交货地址全文 */
  shipTo?: string
  /** V2：运费承担说明（非金额） */
  freightNote?: string
  /** V2：运费金额格，无独立字段时为 — */
  freightAmount?: string
}

export const purchaseOrderReportDocumentPropDefaults = {
  buyerSignDate: '',
  showSeal: true,
  contractNo: '—',
  paymentTerms: '—',
  shipTo: '—',
  freightNote: '运费承担：供方承担',
  freightAmount: '—'
}
