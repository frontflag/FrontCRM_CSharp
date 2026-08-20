import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

/** 列界热区宽度（px），全部落在左列右缘，避免盖住下一列左侧的展开钮 */
export const CRM_COL_RESIZE_HIT_PX = 16

export const CRM_COL_RESIZE_MIN_PX = 30
export const CRM_COL_RESIZE_MAX_PX = 4000

const SUB_COL_RESIZER_SELECTOR =
  '.customer-extend-sub-col-resizer, .vendor-extend-sub-col-resizer'

const HEADER_CONTROL_SELECTOR = [
  SUB_COL_RESIZER_SELECTOR,
  '.customer-extend-col-toggle-btn',
  '.vendor-extend-col-toggle-btn',
  '.op-col-toggle-btn',
  '.op-col-header'
].join(', ')

export interface HeaderResizeBoundary {
  key: string
  property?: string
  minWidth: number
  startWidth: number
  /** 列右缘的 viewport X */
  right: number
  top: number
  height: number
}

export function isHeaderColumnResizable(col: CrmTableColumnDef): boolean {
  if (col.type === 'selection' || col.type === 'index' || col.type === 'expand') return false
  if (col.resizable === false) return false
  if (col.pinned === 'end' || col.fixed === 'right') return false
  const cn = typeof col.className === 'string' ? col.className : ''
  if (cn.split(/\s+/).includes('op-col')) return false
  return true
}

export function isHeaderResizeControlTarget(target: EventTarget | null): boolean {
  return target instanceof Element && !!target.closest(HEADER_CONTROL_SELECTOR)
}

export function parseColumnWidthPx(value: number | string | undefined, fallback: number): number {
  if (typeof value === 'number' && Number.isFinite(value) && value > 0) return value
  if (typeof value === 'string') {
    const n = Number.parseFloat(value)
    if (Number.isFinite(n) && n > 0) return n
  }
  return fallback
}

export function resolveHeaderResizeMinWidth(
  def: CrmTableColumnDef | undefined,
  startWidth: number
): number {
  const cn = typeof def?.className === 'string' ? def.className : ''
  // 扩展列把 minWidth 绑成当前 width，防止表格把列挤窄；拖宽时不能把这个值当收缩下限
  if (cn.includes('extend-col')) return CRM_COL_RESIZE_MIN_PX
  const declared = parseColumnWidthPx(def?.minWidth, CRM_COL_RESIZE_MIN_PX)
  if (declared >= startWidth - 1) return CRM_COL_RESIZE_MIN_PX
  return declared
}

export function clampColumnResizeWidth(px: number, minWidth: number): number {
  const min = Math.max(CRM_COL_RESIZE_MIN_PX, minWidth)
  if (!Number.isFinite(px)) return min
  return Math.min(CRM_COL_RESIZE_MAX_PX, Math.max(min, Math.round(px)))
}

export function pickNearestBoundaryIndex(rights: number[], clientX: number): number {
  if (rights.length === 0) return -1
  let best = 0
  let bestDist = Math.abs(rights[0]! - clientX)
  for (let i = 1; i < rights.length; i++) {
    const d = Math.abs(rights[i]! - clientX)
    if (d < bestDist) {
      best = i
      bestDist = d
    }
  }
  return best
}

export function isPointerOverTableHeader(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false
  if (target.closest('.crm-col-resize-hit')) return true
  return !!target.closest(
    '.el-table__header-wrapper, .el-table__fixed-header-wrapper, .el-table__fixed-right-header-wrapper'
  )
}
