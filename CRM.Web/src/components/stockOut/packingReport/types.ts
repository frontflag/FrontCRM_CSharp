import type { PackingReportLabels } from '../packingReportLabels'

export interface StockOutPackingLineVm {
  index: number
  pn: string
  customerPn: string
  brand: string
  customerBrand: string
  qty: string
  carton: string
  remark: string
}

/** Packing List 打印文档三皮肤共用 props */
export interface PackingReportDocumentProps {
  labels: PackingReportLabels
  headerCompanyName: string
  headerWarehouseAddress?: string
  docTitle: string
  docSubtitle?: string
  docNo: string
  docDate: string
  shipmentMethodDisplay?: string
  billToLines?: string[]
  shipToLines?: string[]
  lines: StockOutPackingLineVm[]
  totalQty: string
  notes: string[]
  withShipmentInspection: boolean
  sealUrl: string | null
  logoUrl: string | null
  showSeal?: boolean
  signDate: string
}

export const packingReportDocumentPropDefaults = {
  docSubtitle: '',
  headerWarehouseAddress: '',
  shipmentMethodDisplay: '—',
  billToLines: () => [] as string[],
  shipToLines: () => [] as string[],
  showSeal: true,
  signDate: ''
}

/** 明细不足时补空行，使表格视觉高度更稳 */
export function packingReportFillerRowCount(lineCount: number, target = 5): number {
  if (lineCount === 0) return 0
  return Math.max(0, target - lineCount)
}
