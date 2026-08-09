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

/** 横版 Packing List 物料行 */
export interface StockOutPackingLandscapeLineVm {
  index: number
  customerPo: string
  partNumber: string
  customerPn: string
  brand: string
  qty: string
  dc: string
  co: string
  cod: string
  size: string
  nw: string
  gw: string
  carton: string
  remark: string
  /** 数值合计用 */
  qtyNum: number
  nwNum: number | null
  gwNum: number | null
  cartonNum: number | null
}

export type PackingReportOrientation = 'portrait' | 'landscape'
export type PackingReportTheme = 'semicore' | 'idesemi' | 'ecoinf'

/** Packing List 打印文档三皮肤共用 props（竖版） */
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

/** 横版文档 props */
export interface PackingReportLandscapeDocumentProps {
  theme: PackingReportTheme
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
  lines: StockOutPackingLandscapeLineVm[]
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

export const PACKING_REPORT_ORIENTATION_STORAGE_KEY = 'frontcrm.packingReport.orientation'

export function readPackingReportOrientation(): PackingReportOrientation {
  try {
    const v = localStorage.getItem(PACKING_REPORT_ORIENTATION_STORAGE_KEY)
    if (v === 'portrait' || v === 'landscape') return v
  } catch {
    /* ignore */
  }
  return 'landscape'
}

export function writePackingReportOrientation(v: PackingReportOrientation) {
  try {
    localStorage.setItem(PACKING_REPORT_ORIENTATION_STORAGE_KEY, v)
  } catch {
    /* ignore */
  }
}
