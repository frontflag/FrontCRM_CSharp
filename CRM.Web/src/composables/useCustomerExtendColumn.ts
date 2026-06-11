import { computed, ref } from 'vue'
import {
  CUSTOMER_EXTEND_COL_STORAGE_KEY,
  CUSTOMER_EXTEND_FIELD_KEYS,
  CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS,
  CUSTOMER_EXTEND_SUB_COL_GAP_PX,
  CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH,
  CUSTOMER_EXTEND_COL_PADDING_PX,
  CUSTOMER_EXTEND_TOGGLE_RESERVE_PX,
  expandedCustomerExtendOuterWidth,
  LIST_CUSTOMER_EXTEND_COL_COLLAPSED_MIN_WIDTH,
  LIST_CUSTOMER_EXTEND_COL_COLLAPSED_WIDTH,
  subColWidthsToGridTemplate,
  sumCustomerExtendSubColWidths,
  type CustomerExtendFieldKey,
  type CustomerExtendRowSlice
} from '@/constants/listCustomerExtendColumnSpec'

interface CustomerExtendColPrefs {
  expanded: boolean
  activeField: CustomerExtendFieldKey
  subColWidths?: number[]
  outerWidthExpanded?: number
  outerWidthCollapsed?: number
}

function isFieldKey(v: unknown): v is CustomerExtendFieldKey {
  return typeof v === 'string' && (CUSTOMER_EXTEND_FIELD_KEYS as string[]).includes(v)
}

function normalizeSubColWidths(raw: unknown): [number, number, number] {
  if (!Array.isArray(raw) || raw.length !== 3) return [...CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  const parsed = raw.map((x) => Math.round(Number(x)))
  if (parsed.some((w) => !Number.isFinite(w) || w < CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH)) {
    return [...CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  return parsed as [number, number, number]
}

function normalizeOuterWidth(raw: unknown, fallback: number, min: number): number {
  const n = Math.round(Number(raw))
  if (!Number.isFinite(n) || n < min) return fallback
  return n
}

function loadPrefs(): Required<CustomerExtendColPrefs> {
  const subColWidths = [...CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS] as [number, number, number]
  const defaults: Required<CustomerExtendColPrefs> = {
    expanded: false,
    activeField: 'nameZh',
    subColWidths,
    outerWidthExpanded: expandedCustomerExtendOuterWidth(subColWidths),
    outerWidthCollapsed: LIST_CUSTOMER_EXTEND_COL_COLLAPSED_WIDTH
  }
  try {
    const raw = localStorage.getItem(CUSTOMER_EXTEND_COL_STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<CustomerExtendColPrefs>
    const loadedSub = normalizeSubColWidths(parsed.subColWidths)
    return {
      expanded: parsed.expanded === true,
      activeField: isFieldKey(parsed.activeField) ? parsed.activeField : 'nameZh',
      subColWidths: loadedSub,
      outerWidthExpanded: normalizeOuterWidth(
        parsed.outerWidthExpanded,
        expandedCustomerExtendOuterWidth(loadedSub),
        expandedCustomerExtendOuterWidth([
          CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH,
          CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH,
          CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH
        ] as [number, number, number])
      ),
      outerWidthCollapsed: normalizeOuterWidth(
        parsed.outerWidthCollapsed,
        LIST_CUSTOMER_EXTEND_COL_COLLAPSED_WIDTH,
        LIST_CUSTOMER_EXTEND_COL_COLLAPSED_MIN_WIDTH
      )
    }
  } catch {
    return defaults
  }
}

function savePrefs(prefs: Required<CustomerExtendColPrefs>) {
  try {
    localStorage.setItem(CUSTOMER_EXTEND_COL_STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    /* private mode */
  }
}

/** 模块级共享状态：全局客户列偏好跨组件实例一致 */
const initialPrefs = loadPrefs()
const expanded = ref(initialPrefs.expanded)
const activeField = ref<CustomerExtendFieldKey>(initialPrefs.activeField)
const subColWidths = ref<[number, number, number]>([...initialPrefs.subColWidths] as [number, number, number])
const outerWidthExpanded = ref(initialPrefs.outerWidthExpanded)
const outerWidthCollapsed = ref(initialPrefs.outerWidthCollapsed)

function contentBudgetForOuter(outer: number): number {
  const gaps = CUSTOMER_EXTEND_SUB_COL_GAP_PX * (subColWidths.value.length - 1)
  const fixed = CUSTOMER_EXTEND_TOGGLE_RESERVE_PX + CUSTOMER_EXTEND_COL_PADDING_PX + gaps
  return Math.max(
    CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH * subColWidths.value.length,
    outer - fixed
  )
}

/** 按当前子列比例，将内部三列宽度适配到目标外层宽度 */
function scaleSubColWidthsToOuter(targetOuter: number) {
  const budget = contentBudgetForOuter(targetOuter)
  const sum = sumCustomerExtendSubColWidths(subColWidths.value)
  if (sum <= 0) return
  const scaled = subColWidths.value.map((w) =>
    Math.max(CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH, Math.round((w / sum) * budget))
  ) as [number, number, number]
  const drift = budget - sumCustomerExtendSubColWidths(scaled)
  if (drift !== 0) {
    scaled[2] = Math.max(CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH, scaled[2] + drift)
  }
  subColWidths.value = scaled
  outerWidthExpanded.value = targetOuter
}

function syncOuterWidthExpandedFromSubCols() {
  outerWidthExpanded.value = expandedCustomerExtendOuterWidth(subColWidths.value)
}

export function pickCustomerExtendFieldValue(
  row: CustomerExtendRowSlice,
  field: CustomerExtendFieldKey
): string {
  switch (field) {
    case 'nameZh':
      return String(row.customerName ?? '').trim()
    case 'nameEn':
      return String(row.customerEnglishName ?? '').trim()
    case 'code':
      return String(row.customerCode ?? '').trim()
    default:
      return ''
  }
}

/** 全局「客户列」扩展偏好（跨列表共用） */
export function useCustomerExtendColumn() {
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

  function setActiveField(field: CustomerExtendFieldKey) {
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
      outerWidthCollapsed.value = Math.max(LIST_CUSTOMER_EXTEND_COL_COLLAPSED_MIN_WIDTH, w)
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
      if (left < CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH || right < CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH) return
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

/** 判断 el-table header-dragend 是否为客户扩展列 */
export function isCustomerExtendTableColumn(column: { property?: string; label?: string } | undefined): boolean {
  if (!column) return false
  return column.property === 'customer'
}
