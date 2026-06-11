import { computed, ref } from 'vue'
import {
  VENDOR_EXTEND_COL_STORAGE_KEY,
  VENDOR_EXTEND_FIELD_KEYS,
  VENDOR_EXTEND_SUB_COL_DEFAULT_WIDTHS,
  VENDOR_EXTEND_SUB_COL_GAP_PX,
  VENDOR_EXTEND_SUB_COL_MIN_WIDTH,
  VENDOR_EXTEND_COL_PADDING_PX,
  VENDOR_EXTEND_TOGGLE_RESERVE_PX,
  expandedVendorExtendOuterWidth,
  LIST_VENDOR_EXTEND_COL_COLLAPSED_MIN_WIDTH,
  LIST_VENDOR_EXTEND_COL_COLLAPSED_WIDTH,
  subColWidthsToGridTemplate,
  sumVendorExtendSubColWidths,
  type VendorExtendFieldKey,
  type VendorExtendRowSlice
} from '@/constants/listVendorExtendColumnSpec'

interface VendorExtendColPrefs {
  expanded: boolean
  activeField: VendorExtendFieldKey
  subColWidths?: number[]
  outerWidthExpanded?: number
  outerWidthCollapsed?: number
}

function isFieldKey(v: unknown): v is VendorExtendFieldKey {
  return typeof v === 'string' && (VENDOR_EXTEND_FIELD_KEYS as string[]).includes(v)
}

function normalizeSubColWidths(raw: unknown): [number, number, number] {
  if (!Array.isArray(raw) || raw.length !== 3) return [...VENDOR_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  const parsed = raw.map((x) => Math.round(Number(x)))
  if (parsed.some((w) => !Number.isFinite(w) || w < VENDOR_EXTEND_SUB_COL_MIN_WIDTH)) {
    return [...VENDOR_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  return parsed as [number, number, number]
}

function normalizeOuterWidth(raw: unknown, fallback: number, min: number): number {
  const n = Math.round(Number(raw))
  if (!Number.isFinite(n) || n < min) return fallback
  return n
}

function loadPrefs(): Required<VendorExtendColPrefs> {
  const subColWidths = [...VENDOR_EXTEND_SUB_COL_DEFAULT_WIDTHS] as [number, number, number]
  const defaults: Required<VendorExtendColPrefs> = {
    expanded: false,
    activeField: 'nameZh',
    subColWidths,
    outerWidthExpanded: expandedVendorExtendOuterWidth(subColWidths),
    outerWidthCollapsed: LIST_VENDOR_EXTEND_COL_COLLAPSED_WIDTH
  }
  try {
    const raw = localStorage.getItem(VENDOR_EXTEND_COL_STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<VendorExtendColPrefs>
    const loadedSub = normalizeSubColWidths(parsed.subColWidths)
    return {
      expanded: parsed.expanded === true,
      activeField: isFieldKey(parsed.activeField) ? parsed.activeField : 'nameZh',
      subColWidths: loadedSub,
      outerWidthExpanded: normalizeOuterWidth(
        parsed.outerWidthExpanded,
        expandedVendorExtendOuterWidth(loadedSub),
        expandedVendorExtendOuterWidth([
          VENDOR_EXTEND_SUB_COL_MIN_WIDTH,
          VENDOR_EXTEND_SUB_COL_MIN_WIDTH,
          VENDOR_EXTEND_SUB_COL_MIN_WIDTH
        ] as [number, number, number])
      ),
      outerWidthCollapsed: normalizeOuterWidth(
        parsed.outerWidthCollapsed,
        LIST_VENDOR_EXTEND_COL_COLLAPSED_WIDTH,
        LIST_VENDOR_EXTEND_COL_COLLAPSED_MIN_WIDTH
      )
    }
  } catch {
    return defaults
  }
}

function savePrefs(prefs: Required<VendorExtendColPrefs>) {
  try {
    localStorage.setItem(VENDOR_EXTEND_COL_STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    /* private mode */
  }
}

/** 模块级共享状态：全局供应商列偏好跨组件实例一致 */
const initialPrefs = loadPrefs()
const expanded = ref(initialPrefs.expanded)
const activeField = ref<VendorExtendFieldKey>(initialPrefs.activeField)
const subColWidths = ref<[number, number, number]>([...initialPrefs.subColWidths] as [number, number, number])
const outerWidthExpanded = ref(initialPrefs.outerWidthExpanded)
const outerWidthCollapsed = ref(initialPrefs.outerWidthCollapsed)

function contentBudgetForOuter(outer: number): number {
  const gaps = VENDOR_EXTEND_SUB_COL_GAP_PX * (subColWidths.value.length - 1)
  const fixed = VENDOR_EXTEND_TOGGLE_RESERVE_PX + VENDOR_EXTEND_COL_PADDING_PX + gaps
  return Math.max(
    VENDOR_EXTEND_SUB_COL_MIN_WIDTH * subColWidths.value.length,
    outer - fixed
  )
}

/** 按当前子列比例，将内部三列宽度适配到目标外层宽度 */
function scaleSubColWidthsToOuter(targetOuter: number) {
  const budget = contentBudgetForOuter(targetOuter)
  const sum = sumVendorExtendSubColWidths(subColWidths.value)
  if (sum <= 0) return
  const scaled = subColWidths.value.map((w) =>
    Math.max(VENDOR_EXTEND_SUB_COL_MIN_WIDTH, Math.round((w / sum) * budget))
  ) as [number, number, number]
  const drift = budget - sumVendorExtendSubColWidths(scaled)
  if (drift !== 0) {
    scaled[2] = Math.max(VENDOR_EXTEND_SUB_COL_MIN_WIDTH, scaled[2] + drift)
  }
  subColWidths.value = scaled
  outerWidthExpanded.value = targetOuter
}

function syncOuterWidthExpandedFromSubCols() {
  outerWidthExpanded.value = expandedVendorExtendOuterWidth(subColWidths.value)
}

export function pickVendorExtendFieldValue(
  row: VendorExtendRowSlice,
  field: VendorExtendFieldKey
): string {
  switch (field) {
    case 'nameZh':
      return String(row.vendorName ?? '').trim()
    case 'nameEn':
      return String(row.vendorEnglishName ?? '').trim()
    case 'code':
      return String(row.vendorCode ?? '').trim()
    default:
      return ''
  }
}

/** 全局「供应商列」扩展偏好（跨列表共用） */
export function useVendorExtendColumn() {
  const subColGridTemplateColumns = computed(() => subColWidthsToGridTemplate(subColWidths.value))

  const colWidth = computed(() => (expanded.value ? outerWidthExpanded.value : outerWidthCollapsed.value))
  const colMinWidth = computed(() => colWidth.value)

  function persist() {
    savePrefs({
      expanded: expanded.value,
      activeField: activeField.value,
      subColWidths: [...subColWidths.value],
      outerWidthExpanded: outerWidthExpanded.value,
      outerWidthCollapsed: outerWidthCollapsed.value
    })
  }

  function toggleExpanded() {
    expanded.value = !expanded.value
    persist()
  }

  function setActiveField(field: VendorExtendFieldKey) {
    activeField.value = field
    persist()
  }

  /** el-table 表头拖拽调宽（整体列宽） */
  function applyOuterWidthFromTable(newWidth: number) {
    const w = Math.round(newWidth)
    if (!Number.isFinite(w) || w <= 0) return
    if (expanded.value) {
      scaleSubColWidthsToOuter(w)
    } else {
      outerWidthCollapsed.value = Math.max(LIST_VENDOR_EXTEND_COL_COLLAPSED_MIN_WIDTH, w)
    }
    persist()
  }

  /** boundaryIndex：0 = 中文|英文 之间，1 = 英文|编号 之间 */
  function startSubColResize(boundaryIndex: number, event: MouseEvent) {
    if (boundaryIndex < 0 || boundaryIndex >= subColWidths.value.length - 1) return
    event.preventDefault()
    event.stopPropagation()

    const startX = event.clientX
    const startWidths = [...subColWidths.value] as [number, number, number]
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'

    const onMove = (ev: MouseEvent) => {
      const dx = ev.clientX - startX
      const left = startWidths[boundaryIndex] + dx
      const right = startWidths[boundaryIndex + 1] - dx
      if (left < VENDOR_EXTEND_SUB_COL_MIN_WIDTH || right < VENDOR_EXTEND_SUB_COL_MIN_WIDTH) return
      const next = [...subColWidths.value] as [number, number, number]
      next[boundaryIndex] = Math.round(left)
      next[boundaryIndex + 1] = Math.round(right)
      subColWidths.value = next
      syncOuterWidthExpandedFromSubCols()
    }

    const onUp = () => {
      document.removeEventListener('mousemove', onMove)
      document.removeEventListener('mouseup', onUp)
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
      persist()
    }

    document.addEventListener('mousemove', onMove)
    document.addEventListener('mouseup', onUp)
  }

  return {
    expanded,
    activeField,
    subColWidths,
    outerWidthExpanded,
    outerWidthCollapsed,
    subColGridTemplateColumns,
    colWidth,
    colMinWidth,
    toggleExpanded,
    setActiveField,
    startSubColResize,
    applyOuterWidthFromTable
  }
}

/** 判断 el-table header-dragend 是否为供应商扩展列 */
export function isVendorExtendTableColumn(column: { property?: string; label?: string } | undefined): boolean {
  if (!column) return false
  return column.property === 'vendor'
}
