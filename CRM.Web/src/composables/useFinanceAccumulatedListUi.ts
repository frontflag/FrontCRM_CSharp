import { ref } from 'vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { formatTotalAmountNumber } from '@/utils/moneyFormat'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'

export const ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS = [
  'prvStockQty',
  'stockInQty',
  'stockOutQty',
  'balanceStockQty',
  'prvAmountTotal',
  'currentStockInAmountTotal',
  'currentStockOutAmountTotal',
  'balanceAmountTotal'
] as const

export type AccumulatedSummaryMetricColumnKey = (typeof ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS)[number]

/** 滚存汇总表 8 个指标列统一最小宽度，便于 table-layout:fixed 下均分剩余空间 */
export function resolveAccumulatedSummaryMetricMinWidth(
  labelForKey: (key: AccumulatedSummaryMetricColumnKey) => string
): number {
  return Math.max(
    ...ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS.map((key) =>
      estimateListColumnHeaderMinWidth(labelForKey(key), { align: 'right' })
    )
  )
}

export function buildAccumulatedSummaryMetricColumn(
  key: AccumulatedSummaryMetricColumnKey,
  label: string,
  prop: string,
  minWidth: number
): CrmTableColumnDef {
  return {
    key,
    label,
    prop,
    minWidth,
    align: 'right',
    className: 'accumulated-metric-col',
    labelClassName: 'accumulated-metric-col'
  }
}

export function useFinanceAccumulatedTableFooter() {
  const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
  const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
  return { dataTableRef, rowDensityToggleAnchorEl }
}

export function formatAccumulatedUsd(maskAmounts: boolean, value: number | null | undefined): string {
  if (maskAmounts || value == null) return '—'
  return formatTotalAmountNumber(value)
}

/** 《业务列表规范》§3.2：数量千分位、tabular-nums */
export function formatAccumulatedQty(value: unknown): string {
  if (value == null || value === '') return '—'
  const n = Number(value)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

export function formatAccumulatedDateTimeParts(v: string | null | undefined) {
  return formatDisplayDateTime2DigitYearParts(v)
}

export function isAccumulatedTimeMidnightOnly(time: string) {
  const s = String(time ?? '').trim()
  return !s || s === '00:00' || s === '00:00:00'
}

export function isAccumulatedNegative(value: unknown): boolean {
  if (value == null || value === '') return false
  const n = Number(value)
  return Number.isFinite(n) && n < 0
}
