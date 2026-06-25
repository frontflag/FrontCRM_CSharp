import { computed, ref } from 'vue'
import {
  VENDOR_RECEIVING_BANK_EXTEND_COL_STORAGE_KEY,
  VENDOR_RECEIVING_BANK_EXTEND_FIELD_KEYS,
  VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_DEFAULT_WIDTHS,
  VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_GAP_PX,
  VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH,
  VENDOR_RECEIVING_BANK_EXTEND_COL_PADDING_PX,
  VENDOR_RECEIVING_BANK_EXTEND_TOGGLE_RESERVE_PX,
  expandedVendorReceivingBankExtendOuterWidth,
  LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_MIN_WIDTH,
  LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_WIDTH,
  subColWidthsToGridTemplate,
  sumVendorReceivingBankExtendSubColWidths,
  type VendorReceivingBankExtendFieldKey,
  type VendorReceivingBankExtendRowSlice
} from '@/constants/listVendorReceivingBankExtendColumnSpec'

interface VendorReceivingBankExtendColPrefs {
  expanded: boolean
  activeField: VendorReceivingBankExtendFieldKey
  subColWidths?: number[]
  outerWidthExpanded?: number
  outerWidthCollapsed?: number
}

function isFieldKey(v: unknown): v is VendorReceivingBankExtendFieldKey {
  return typeof v === 'string' && (VENDOR_RECEIVING_BANK_EXTEND_FIELD_KEYS as string[]).includes(v)
}

function normalizeSubColWidths(raw: unknown): [number, number] {
  if (!Array.isArray(raw) || raw.length !== 2) {
    return [...VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  const parsed = raw.map((x) => Math.round(Number(x)))
  if (parsed.some((w) => !Number.isFinite(w) || w < VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH)) {
    return [...VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_DEFAULT_WIDTHS]
  }
  return parsed as [number, number]
}

function normalizeOuterWidth(raw: unknown, fallback: number, min: number): number {
  const n = Math.round(Number(raw))
  if (!Number.isFinite(n) || n < min) return fallback
  return n
}

function loadPrefs(): Required<VendorReceivingBankExtendColPrefs> {
  const subColWidths = [...VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_DEFAULT_WIDTHS] as [number, number]
  const defaults: Required<VendorReceivingBankExtendColPrefs> = {
    expanded: false,
    activeField: 'accountName',
    subColWidths,
    outerWidthExpanded: expandedVendorReceivingBankExtendOuterWidth(subColWidths),
    outerWidthCollapsed: LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_WIDTH
  }
  try {
    const raw = localStorage.getItem(VENDOR_RECEIVING_BANK_EXTEND_COL_STORAGE_KEY)
    if (!raw) return defaults
    const parsed = JSON.parse(raw) as Partial<VendorReceivingBankExtendColPrefs>
    const loadedSub = normalizeSubColWidths(parsed.subColWidths)
    return {
      expanded: parsed.expanded === true,
      activeField: isFieldKey(parsed.activeField) ? parsed.activeField : 'accountName',
      subColWidths: loadedSub,
      outerWidthExpanded: normalizeOuterWidth(
        parsed.outerWidthExpanded,
        expandedVendorReceivingBankExtendOuterWidth(loadedSub),
        expandedVendorReceivingBankExtendOuterWidth([
          VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH,
          VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH
        ] as [number, number])
      ),
      outerWidthCollapsed: normalizeOuterWidth(
        parsed.outerWidthCollapsed,
        LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_WIDTH,
        LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_MIN_WIDTH
      )
    }
  } catch {
    return defaults
  }
}

function savePrefs(prefs: Required<VendorReceivingBankExtendColPrefs>) {
  try {
    localStorage.setItem(VENDOR_RECEIVING_BANK_EXTEND_COL_STORAGE_KEY, JSON.stringify(prefs))
  } catch {
    /* private mode */
  }
}

const initialPrefs = loadPrefs()
const expanded = ref(initialPrefs.expanded)
const activeField = ref<VendorReceivingBankExtendFieldKey>(initialPrefs.activeField)
const subColWidths = ref<[number, number]>([...initialPrefs.subColWidths] as [number, number])
const outerWidthExpanded = ref(initialPrefs.outerWidthExpanded)
const outerWidthCollapsed = ref(initialPrefs.outerWidthCollapsed)

function contentBudgetForOuter(outer: number): number {
  const gaps = VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_GAP_PX * (subColWidths.value.length - 1)
  const fixed =
    VENDOR_RECEIVING_BANK_EXTEND_TOGGLE_RESERVE_PX +
    VENDOR_RECEIVING_BANK_EXTEND_COL_PADDING_PX +
    gaps
  return Math.max(
    VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH * subColWidths.value.length,
    outer - fixed
  )
}

function scaleSubColWidthsToOuter(targetOuter: number) {
  const budget = contentBudgetForOuter(targetOuter)
  const sum = sumVendorReceivingBankExtendSubColWidths(subColWidths.value)
  if (sum <= 0) return
  const scaled = subColWidths.value.map((w) =>
    Math.max(VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH, Math.round((w / sum) * budget))
  ) as [number, number]
  const drift = budget - sumVendorReceivingBankExtendSubColWidths(scaled)
  if (drift !== 0) {
    scaled[1] = Math.max(VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH, scaled[1] + drift)
  }
  subColWidths.value = scaled
  outerWidthExpanded.value = targetOuter
}

function syncOuterWidthExpandedFromSubCols() {
  outerWidthExpanded.value = expandedVendorReceivingBankExtendOuterWidth(subColWidths.value)
}

export function pickVendorReceivingBankExtendFieldValue(
  row: VendorReceivingBankExtendRowSlice,
  field: VendorReceivingBankExtendFieldKey
): string {
  const ext = row as Record<string, unknown>
  switch (field) {
    case 'accountName':
      return String(row.vendorBankAccountName ?? ext.VendorBankAccountName ?? '').trim()
    case 'openingBank':
      return String(
        row.vendorBankOpeningBank ??
          ext.VendorBankOpeningBank ??
          row.vendorBankName ??
          ext.VendorBankName ??
          ''
      ).trim()
    default:
      return ''
  }
}

export function useVendorReceivingBankExtendColumn() {
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

  function setActiveField(field: VendorReceivingBankExtendFieldKey) {
    activeField.value = field
    persist()
  }

  function applyOuterWidthFromTable(newWidth: number) {
    const w = Math.round(newWidth)
    if (!Number.isFinite(w) || w <= 0) return
    if (expanded.value) {
      scaleSubColWidthsToOuter(w)
    } else {
      outerWidthCollapsed.value = Math.max(LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_MIN_WIDTH, w)
    }
    persist()
  }

  function startSubColResize(boundaryIndex: number, event: MouseEvent) {
    if (boundaryIndex < 0 || boundaryIndex >= subColWidths.value.length - 1) return
    event.preventDefault()
    event.stopPropagation()

    const startX = event.clientX
    const startWidths = [...subColWidths.value] as [number, number]
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'

    const onMove = (ev: MouseEvent) => {
      const dx = ev.clientX - startX
      const left = startWidths[boundaryIndex] + dx
      const right = startWidths[boundaryIndex + 1] - dx
      if (left < VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH || right < VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH) {
        return
      }
      const next = [...subColWidths.value] as [number, number]
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

export function isVendorReceivingBankExtendTableColumn(
  column: { property?: string; label?: string } | undefined
): boolean {
  if (!column) return false
  return column.property === 'vendorReceivingBank'
}
