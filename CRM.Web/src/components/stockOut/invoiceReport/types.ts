import type { InvoiceReportLabels } from '../packingReportLabels'

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
}

export const invoiceReportDocumentPropDefaults = {
  invoiceSubtitle: '',
  headerWarehouseAddress: '',
  billToLines: () => [] as string[],
  shipToLines: () => [] as string[],
  showSeal: true,
  signDate: ''
}

/** 明细不足时补空行 */
export function invoiceReportFillerRowCount(lineCount: number, target = 5): number {
  if (lineCount === 0) return 0
  return Math.max(0, target - lineCount)
}
