import { computed, ref } from 'vue'
import {
  DOCK_QUOTE_EXTEND_COL_STORAGE_KEY,
  DOCK_QUOTE_EXTEND_FIELD_KEYS,
  DOCK_QUOTE_EXTEND_SUB_COL_DEFAULT_WIDTHS,
  DOCK_QUOTE_EXTEND_SUB_COL_GAP_PX,
  DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH,
  DOCK_QUOTE_EXTEND_COL_PADDING_PX,
  DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX,
  expandedDockQuoteExtendOuterWidth,
  LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_MIN_WIDTH,
  LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_WIDTH,
  subColWidthsToGridTemplate,
  sumDockQuoteExtendSubColWidths,
  type DockQuoteExtendFieldKey
} from '@/constants/listDockQuoteExtendColumnSpec'

interface DockQuoteExtendColPrefs {
  expanded: boolean
  activeField: DockQuoteExtendFieldKey
  subColWidths?: number[]
  outerWidthExpanded?: number
  outerWidthCollapsed?: number
}

function isFieldKey(v: unknown): v is DockQuoteExtendFieldKey {
  return typeof v === 'string' && (DOCK_QUOTE_EXTEND_FIELD_KEYS as string[]).includes(v)
}

function normalizeSubColWidths(raw: unknown): [number, number, number] {
  if (!Array.isArray(raw) || raw.length !== 3) return [...DOCK_QUOTE_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  const parsed = raw.map((x) => Math.round(Number(x)))
  if (parsed.some((w) => !Number.isFinite(w) || w < DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH)) {
    return [...DOCK_QUOTE_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  return parsed as [number, number, number]
}

function normalizeOuterWidth(raw: unknown, fallback: number, min: number): number {
  const n = Math.round(Number(raw))
  if (!Number.isFinite(n) || n < min) return fallback
  return n
}

function loadPrefs(): Required<DockQuoteExtendColPrefs> {
  const subColWidths = [...DOCK_QUOTE_EXTEND_SUB_COL_DEFAULT_WIDTHS] as [number, number, number]
  const defaults: Required<DockQuoteExtendColPrefs> = {
    expanded: false,
    activeField: 'waferOrigin',
    subColWidths,
    outerWidthExpanded: expandedDockQuoteExtendOuterWidth(subColWidths),
    outerWidthCollapsed: LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_WIDTH
  }
  try {
    const raw = localStorage.getItem(DOCK_QUOTE_EXTEND_COL_STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<DockQuoteExtendColPrefs>
    const loadedSub = normalizeSubColWidths(parsed.subColWidths)
    return {
      expanded: parsed.expanded === true,
      activeField: isFieldKey(parsed.activeField) ? parsed.activeField : 'waferOrigin',
      subColWidths: loadedSub,
      outerWidthExpanded: normalizeOuterWidth(
        parsed.outerWidthExpanded,
        expandedDockQuoteExtendOuterWidth(loadedSub),
        expandedDockQuoteExtendOuterWidth([
          DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH,
          DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH,
          DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH
        ] as [number, number, number])
      ),
      outerWidthCollapsed: normalizeOuterWidth(
        parsed.outerWidthCollapsed,
        LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_WIDTH,
        LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_MIN_WIDTH
      )
    }
  } catch {
    return defaults
  }
}

function savePrefs(prefs: Required<DockQuoteExtendColPrefs>) {
  try {
    localStorage.setItem(DOCK_QUOTE_EXTEND_COL_STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    /* private mode */
  }
}

const initialPrefs = loadPrefs()
const expanded = ref(initialPrefs.expanded)
const activeField = ref<DockQuoteExtendFieldKey>(initialPrefs.activeField)
const subColWidths = ref<[number, number, number]>([...initialPrefs.subColWidths] as [number, number, number])
const outerWidthExpanded = ref(initialPrefs.outerWidthExpanded)
const outerWidthCollapsed = ref(initialPrefs.outerWidthCollapsed)

function contentBudgetForOuter(outer: number): number {
  const gaps = DOCK_QUOTE_EXTEND_SUB_COL_GAP_PX * (subColWidths.value.length - 1)
  const fixed = DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX + DOCK_QUOTE_EXTEND_COL_PADDING_PX + gaps
  return Math.max(
    DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH * subColWidths.value.length,
    outer - fixed
  )
}

function scaleSubColWidthsToOuter(targetOuter: number) {
  const budget = contentBudgetForOuter(targetOuter)
  const sum = sumDockQuoteExtendSubColWidths(subColWidths.value)
  if (sum <= 0) return
  const scaled = subColWidths.value.map((w) =>
    Math.max(DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH, Math.round((w / sum) * budget))
  ) as [number, number, number]
  const drift = budget - sumDockQuoteExtendSubColWidths(scaled)
  if (drift !== 0) {
    scaled[2] = Math.max(DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH, scaled[2] + drift)
  }
  subColWidths.value = scaled
  outerWidthExpanded.value = targetOuter
}

function syncOuterWidthExpandedFromSubCols() {
  outerWidthExpanded.value = expandedDockQuoteExtendOuterWidth(subColWidths.value)
}

export function useDockQuoteExtendColumn() {
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

  function setActiveField(field: DockQuoteExtendFieldKey) {
    activeField.value = field
    persist()
  }

  function applyOuterWidthFromTable(newWidth: number) {
    const w = Math.round(newWidth)
    if (!Number.isFinite(w) || w <= 0) return
    if (expanded.value) {
      scaleSubColWidthsToOuter(w)
    } else {
      outerWidthCollapsed.value = Math.max(LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_MIN_WIDTH, w)
    }
    persist()
  }

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
      if (left < DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH || right < DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH) return
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

export function isDockQuoteExtendTableColumn(
  column: { property?: string; label?: string } | undefined
): boolean {
  if (!column) return false
  return column.property === 'dockQuoteExtend'
}
