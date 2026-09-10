import { computed, ref } from 'vue'
import {
  CUSTOMS_EXTEND_COL_STORAGE_KEY,
  CUSTOMS_EXTEND_FIELD_KEYS,
  CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS,
  CUSTOMS_EXTEND_SUB_COL_GAP_PX,
  CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH,
  CUSTOMS_EXTEND_COL_PADDING_PX,
  CUSTOMS_EXTEND_TOGGLE_RESERVE_PX,
  expandedCustomsExtendOuterWidth,
  LIST_CUSTOMS_EXTEND_COL_COLLAPSED_MIN_WIDTH,
  LIST_CUSTOMS_EXTEND_COL_COLLAPSED_WIDTH,
  customsSubColWidthsToGridTemplate,
  sumCustomsExtendSubColWidths,
  type CustomsExtendFieldKey,
  type CustomsExtendSubColWidths
} from '@/constants/listCustomsExtendColumnSpec'

interface CustomsExtendColPrefs {
  expanded: boolean
  activeField: CustomsExtendFieldKey
  subColWidths?: number[]
  outerWidthExpanded?: number
  outerWidthCollapsed?: number
}

const SUB_COL_COUNT = CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS.length

function isFieldKey(v: unknown): v is CustomsExtendFieldKey {
  return typeof v === 'string' && (CUSTOMS_EXTEND_FIELD_KEYS as string[]).includes(v)
}

function defaultMinExpandedOuter(): number {
  return expandedCustomsExtendOuterWidth(
    Array.from({ length: SUB_COL_COUNT }, () => CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH)
  )
}

function normalizeSubColWidths(raw: unknown): CustomsExtendSubColWidths {
  if (!Array.isArray(raw) || raw.length !== SUB_COL_COUNT) return [...CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  const parsed = raw.map((x) => Math.round(Number(x)))
  if (parsed.some((w) => !Number.isFinite(w) || w < CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH)) {
    return [...CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  return parsed as CustomsExtendSubColWidths
}

function normalizeOuterWidth(raw: unknown, fallback: number, min: number): number {
  const n = Math.round(Number(raw))
  if (!Number.isFinite(n) || n < min) return fallback
  return n
}

function loadPrefs(): Required<CustomsExtendColPrefs> {
  const subColWidths = [...CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS] as CustomsExtendSubColWidths
  const defaults: Required<CustomsExtendColPrefs> = {
    expanded: false,
    activeField: 'icon',
    subColWidths,
    outerWidthExpanded: expandedCustomsExtendOuterWidth(subColWidths),
    outerWidthCollapsed: LIST_CUSTOMS_EXTEND_COL_COLLAPSED_WIDTH
  }
  try {
    const raw = localStorage.getItem(CUSTOMS_EXTEND_COL_STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<CustomsExtendColPrefs>
    const loadedSub = normalizeSubColWidths(parsed.subColWidths)
    return {
      expanded: parsed.expanded === true,
      activeField: isFieldKey(parsed.activeField) ? parsed.activeField : 'icon',
      subColWidths: loadedSub,
      outerWidthExpanded: normalizeOuterWidth(
        parsed.outerWidthExpanded,
        expandedCustomsExtendOuterWidth(loadedSub),
        defaultMinExpandedOuter()
      ),
      outerWidthCollapsed: normalizeOuterWidth(
        parsed.outerWidthCollapsed,
        LIST_CUSTOMS_EXTEND_COL_COLLAPSED_WIDTH,
        LIST_CUSTOMS_EXTEND_COL_COLLAPSED_MIN_WIDTH
      )
    }
  } catch {
    return defaults
  }
}

function savePrefs(prefs: Required<CustomsExtendColPrefs>) {
  try {
    localStorage.setItem(CUSTOMS_EXTEND_COL_STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    /* private mode */
  }
}

/** 模块级共享状态：全局报关列偏好跨组件实例一致 */
const initialPrefs = loadPrefs()
const expanded = ref(initialPrefs.expanded)
const activeField = ref<CustomsExtendFieldKey>(initialPrefs.activeField)
const subColWidths = ref<CustomsExtendSubColWidths>([
  ...initialPrefs.subColWidths
] as CustomsExtendSubColWidths)
const outerWidthExpanded = ref(initialPrefs.outerWidthExpanded)
const outerWidthCollapsed = ref(initialPrefs.outerWidthCollapsed)

function contentBudgetForOuter(outer: number): number {
  const gaps = CUSTOMS_EXTEND_SUB_COL_GAP_PX * (subColWidths.value.length - 1)
  const fixed = CUSTOMS_EXTEND_TOGGLE_RESERVE_PX + CUSTOMS_EXTEND_COL_PADDING_PX + gaps
  return Math.max(
    CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH * subColWidths.value.length,
    outer - fixed
  )
}

/** 按当前子列比例，将内部子列宽度适配到目标外层宽度 */
function scaleSubColWidthsToOuter(targetOuter: number) {
  const budget = contentBudgetForOuter(targetOuter)
  const sum = sumCustomsExtendSubColWidths(subColWidths.value)
  if (sum <= 0) return
  const scaled = subColWidths.value.map((w) =>
    Math.max(CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH, Math.round((w / sum) * budget))
  ) as CustomsExtendSubColWidths
  const drift = budget - sumCustomsExtendSubColWidths(scaled)
  if (drift !== 0) {
    const last = scaled.length - 1
    scaled[last] = Math.max(CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH, scaled[last] + drift)
  }
  subColWidths.value = scaled
  outerWidthExpanded.value = targetOuter
}

function syncOuterWidthExpandedFromSubCols() {
  outerWidthExpanded.value = expandedCustomsExtendOuterWidth(subColWidths.value)
}

/** 全局「报关列」扩展偏好（跨列表共用） */
export function useCustomsExtendColumn() {
  const subColGridTemplateColumns = computed(() => customsSubColWidthsToGridTemplate(subColWidths.value))

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

  function setActiveField(field: CustomsExtendFieldKey) {
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
      outerWidthCollapsed.value = Math.max(LIST_CUSTOMS_EXTEND_COL_COLLAPSED_MIN_WIDTH, w)
    }
    persist()
  }

  /** boundaryIndex：相邻子列之间的分隔（0=图标|单号，1=单号|公司，2=公司|状态） */
  function startSubColResize(boundaryIndex: number, event: MouseEvent) {
    if (boundaryIndex < 0 || boundaryIndex >= subColWidths.value.length - 1) return
    event.preventDefault()
    event.stopPropagation()

    const startX = event.clientX
    const startWidths = [...subColWidths.value] as CustomsExtendSubColWidths
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'

    const onMove = (ev: MouseEvent) => {
      const dx = ev.clientX - startX
      const left = startWidths[boundaryIndex] + dx
      const right = startWidths[boundaryIndex + 1] - dx
      if (left < CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH || right < CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH) return
      const next = [...subColWidths.value] as CustomsExtendSubColWidths
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

/** 判断 el-table header-dragend 是否为报关扩展列 */
export function isCustomsExtendTableColumn(column: { property?: string; label?: string } | undefined): boolean {
  if (!column) return false
  return column.property === 'customs'
}
