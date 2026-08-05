export type SalesOrderWarrantyLang = 'zh' | 'en'

export interface SoWarrantyLineVm {
  pn: string
  brand: string
  qty: string
  dateCode: string
  customerPn: string
  customerSo: string
}

export interface SalesOrderWarrantyReportDocumentProps {
  lang: SalesOrderWarrantyLang
  /** 右上 / 页眉公司名 */
  companyName: string
  companyAddress: string
  docTitle: string
  docSubtitle: string
  partyALabel: string
  partyBLabel: string
  partyAName: string
  partyBName: string
  introText: string
  notesHeading: string
  notes: string[]
  /** 编号说明之后的补充段（可空） */
  notesAfter?: string
  /** 货表前引导句（可空；中文常放在 notes 末条） */
  goodsLead?: string
  colPn: string
  colBrand: string
  colQty: string
  colDc: string
  colCustomerPn: string
  colCustomerSo: string
  lines: SoWarrantyLineVm[]
  emptyLinesHint: string
  signRepLabel: string
  signPhoneLabel: string
  signAddrLabel: string
  partyARep: string
  partyAPhone: string
  partyAAddress: string
  partyBRep: string
  partyBPhone: string
  partyBAddress: string
  logoUrl: string | null
  sealUrl: string | null
  showSeal?: boolean
}

export const salesOrderWarrantyReportDocumentPropDefaults = {
  showSeal: true,
  notesAfter: '',
  goodsLead: ''
}
