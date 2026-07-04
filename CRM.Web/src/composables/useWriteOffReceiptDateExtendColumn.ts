import { computed, ref } from 'vue'
import {
  WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_MIN_WIDTH,
  WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_WIDTH,
  WRITE_OFF_RECEIPT_DATE_SUB_COL_DEFAULT_WIDTHS,
  WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH,
  expandedWriteOffReceiptDateOuterWidth,
  writeOffReceiptDateSubColWidthsToGridTemplate,
  type WriteOffReceiptDateFieldKey
} from '@/constants/writeOffReceiptDateExtendColumnSpec'

const expanded = ref(false)
const activeField = ref<WriteOffReceiptDateFieldKey>('earliest')
const subColWidths = ref<[number, number]>([...WRITE_OFF_RECEIPT_DATE_SUB_COL_DEFAULT_WIDTHS])

/** 收款核销页左栏「收款日期」扩展列（页内模块级共享状态） */
export function useWriteOffReceiptDateExtendColumn() {
  const subColGridTemplateColumns = computed(() => writeOffReceiptDateSubColWidthsToGridTemplate(subColWidths.value))

  const colWidth = computed(() =>
    expanded.value ? expandedWriteOffReceiptDateOuterWidth(subColWidths.value) : WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_WIDTH
  )
  const colMinWidth = computed(() =>
    expanded.value
      ? expandedWriteOffReceiptDateOuterWidth([
          WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH,
          WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH
        ])
      : WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_MIN_WIDTH
  )

  function toggleExpanded() {
    expanded.value = !expanded.value
  }

  function setActiveField(field: WriteOffReceiptDateFieldKey) {
    activeField.value = field
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
      if (left < WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH || right < WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH) return
      const next = [...subColWidths.value] as [number, number]
      next[boundaryIndex] = Math.round(left)
      next[boundaryIndex + 1] = Math.round(right)
      subColWidths.value = next
    }

    const onUp = () => {
      document.removeEventListener('mousemove', onMove)
      document.removeEventListener('mouseup', onUp)
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
    }

    document.addEventListener('mousemove', onMove)
    document.addEventListener('mouseup', onUp)
  }

  return {
    expanded,
    activeField,
    subColWidths,
    subColGridTemplateColumns,
    colWidth,
    colMinWidth,
    toggleExpanded,
    setActiveField,
    startSubColResize
  }
}
