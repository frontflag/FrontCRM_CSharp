export type WarrantyLetterReportLang = 'zh' | 'en'

/** 客户 / 供应商主数据质保书（无货表明细）共用 props */
export interface WarrantyLetterReportDocumentProps {
  /** V2：页内报告语言（V1 忽略） */
  lang?: WarrantyLetterReportLang
  /** V2：顶栏跨语言副标题（V1 忽略） */
  docSubtitle?: string
  issuerName: string
  docTitle: string
  docNo: string
  issueDate: string
  noLabel: string
  dateLabel: string
  toNameLabel: string
  codeLabel: string
  addrLabel: string
  /** 收件方名称（客户或供应商） */
  vendorName: string
  vendorCode: string
  vendorAddress: string
  paragraphs: string[]
  issuerSignLabel: string
  sealUrl: string | null
  logoUrl: string | null
  showSeal?: boolean
}

export const warrantyLetterReportDocumentPropDefaults = {
  showSeal: true
}
