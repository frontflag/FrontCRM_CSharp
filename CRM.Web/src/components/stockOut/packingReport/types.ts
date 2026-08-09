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

/** 不再补空白行；保留函数以免皮肤调用处改动过大 */
export function packingReportFillerRowCount(_lineCount: number, _target = 5): number {
  return 0
}
