import type { InvoiceReportLabels, InvoiceReportLang } from '../packingReportLabels'

export interface StockOutInvoiceLineVm {
  index: number
  pn: string
  customerPn: string
  brand: string
  customerBrand: string
  qty: string
  unitPrice: string
  amount: string
  remark: string
}

/** Commercial Invoice 打印文档三皮肤共用 props */
export interface InvoiceReportDocumentProps {
  labels: InvoiceReportLabels
  headerCompanyName: string
  invoiceTitle: string
  invoiceSubtitle?: string
  invoiceNo: string
  invoiceDate: string
  headerWarehouseAddress?: string
  billToLines?: string[]
  shipToLines?: string[]
  lines: StockOutInvoiceLineVm[]
  totalQty: string
  totalAmount: string
  bankLines: string[]
  sealUrl: string | null
  logoUrl: string | null
  showAmounts: boolean
  showSeal?: boolean
  signDate: string
  /** V2 明细表头随工具栏中/英切换（chrome 仍固定双语） */
  reportLang?: InvoiceReportLang
}

export const invoiceReportDocumentPropDefaults = {
  invoiceSubtitle: '',
  headerWarehouseAddress: '',
  billToLines: () => [] as string[],
  shipToLines: () => [] as string[],
  showSeal: true,
  signDate: '',
  reportLang: 'en' as InvoiceReportLang
}

/** 明细不足时补空行 */
export function invoiceReportFillerRowCount(lineCount: number, target = 5): number {
  if (lineCount === 0) return 0
  return Math.max(0, target - lineCount)
}
