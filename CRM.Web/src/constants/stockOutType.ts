/** 与后端 <see cref="StockOutTypeCode"/> / packing.StockOutType 一致 */
export const StockOutTypeCode = {
  Sales: 10,
  Customs: 20,
  Return: 30,
  Scrap: 40,
  /** 移库虚拟出库，装箱单业务不使用 */
  Transfer: 3
} as const

export const STOCK_OUT_TYPE_FILTER_VALUES = [
  StockOutTypeCode.Sales,
  StockOutTypeCode.Customs,
  StockOutTypeCode.Return,
  StockOutTypeCode.Scrap
] as const

export type StockOutTypeLabelKey =
  | 'sales'
  | 'customs'
  | 'return'
  | 'scrap'
  | 'transfer'
  | 'unknown'

/**
 * 出库类型界面文案键。禁止未识别值兜成销售出库。
 */
export function resolveStockOutTypeLabelKey(
  type: number | string | null | undefined
): StockOutTypeLabelKey {
  if (type === null || type === undefined) return 'unknown'
  if (typeof type === 'string' && type.trim() === '') return 'unknown'
  const n = Number(type)
  if (!Number.isFinite(n) || n === 0) return 'unknown'
  if (n === 1 || n === StockOutTypeCode.Sales) return 'sales'
  if (n === StockOutTypeCode.Customs) return 'customs'
  if (n === StockOutTypeCode.Return) return 'return'
  if (n === StockOutTypeCode.Scrap) return 'scrap'
  if (n === StockOutTypeCode.Transfer) return 'transfer'
  return 'unknown'
}

const STOCK_OUT_TYPE_LABELS: Record<StockOutTypeLabelKey, string> = {
  sales: '销售出库',
  customs: '报关出库',
  return: '退货出库',
  scrap: '报废出库',
  transfer: '移库',
  unknown: '未知'
}

export function stockOutTypeLabel(type: number | string | null | undefined): string {
  return STOCK_OUT_TYPE_LABELS[resolveStockOutTypeLabelKey(type)]
}
