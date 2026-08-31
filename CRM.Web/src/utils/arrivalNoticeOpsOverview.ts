import { resolveStockInTypeLabelKey } from '@/constants/stockInType'

/** 报关入库概况行：类型标签右侧展示报关公司名称；非报关或无名则空串（不渲染占位）。 */
export function resolveArrivalNoticeCustomsBrokerName(
  row: Record<string, unknown> | null | undefined,
  stockInType?: number | string | null
): string {
  if (resolveStockInTypeLabelKey(stockInType) !== 'customs') return ''
  return String(row?.customsBrokerName ?? row?.CustomsBrokerName ?? '').trim()
}
