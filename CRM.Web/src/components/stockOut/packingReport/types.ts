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

/** V2 竖版：发货人 / 收货人 */
export type PackingReportV2Party = {
  name: string
  address: string
  contact: string
  phone: string
  email: string
}

export interface PackingReportV2LineVm {
  index: number
  carton: string
  mpn: string
  brand: string
  lotNo: string
  description: string
  qty: string
  nw: string
  gw: string
  dimensions: string
}

/** V2 横版包装明细（表头对齐图1 14 列英文） */
export interface PackingReportV2LandscapeLineVm {
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
}

/** V2 文档（仅 semicore + 参数 V2） */
export interface PackingReportV2DocumentProps {
  headerCompanyName: string
  packingNo: string
  docDate: string
  invoicePoNo: string
  incoterms: string
  transportMode: string
  shipper: PackingReportV2Party
  consignee: PackingReportV2Party
  lines: PackingReportV2LineVm[]
  landscapeLines?: PackingReportV2LandscapeLineVm[]
  orientation?: PackingReportOrientation
  shipMarks: string
  departure: string
  destination: string
  carrierAwb: string
  remarks: string[]
  totalCartons: string
  totalQty: string
  totalNw: string
  totalGw: string
  totalVolume: string
  withShipmentInspection: boolean
  qcItems: readonly string[]
  sealUrl: string | null
  logoUrl: string | null
  showSeal?: boolean
  shipperSignDate: string
}

export const packingReportV2DocumentPropDefaults = {
  invoicePoNo: '—',
  incoterms: '—',
  transportMode: '—',
  shipMarks: '—',
  departure: '—',
  destination: '—',
  carrierAwb: '—',
  remarks: () => [] as string[],
  landscapeLines: () => [] as PackingReportV2LandscapeLineVm[],
  orientation: 'portrait' as PackingReportOrientation,
  totalCartons: '—',
  totalNw: '—',
  totalGw: '—',
  totalVolume: '—',
  withShipmentInspection: false,
  qcItems: () => [] as string[],
  showSeal: true,
  shipperSignDate: ''
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

/** 空箱号按行序补 01 / 02 …（V2 竖版第一列；不另开序号列） */
export function formatPackingV2Carton(carton: string | null | undefined, index1: number): string {
  const s = (carton ?? '').trim()
  if (s) return s
  return String(index1).padStart(2, '0')
}

export function writePackingReportOrientation(v: PackingReportOrientation) {
  try {
    localStorage.setItem(PACKING_REPORT_ORIENTATION_STORAGE_KEY, v)
  } catch {
    /* ignore */
  }
}
