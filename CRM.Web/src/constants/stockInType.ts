/** 与后端 StockInTypeCode / stock_in.StockInType 一致 */
export const StockInTypeCode = {
  Purchase: 10,
  Customs: 20,
  Return: 30,
  Scrap: 40,
  /** 移库虚拟入库，购销列表默认排除 */
  Transfer: 3
} as const

export const STOCK_IN_TYPE_FILTER_VALUES = [
  StockInTypeCode.Purchase,
  StockInTypeCode.Customs,
  StockInTypeCode.Return,
  StockInTypeCode.Scrap
] as const

export type StockInTypeLabelKey =
  | 'purchase'
  | 'customs'
  | 'return'
  | 'scrap'
  | 'transfer'
  | 'unknown'

/**
 * 入库类型界面文案键。禁止未识别值兜成采购入库：仅 1/10 为采购入库。
 */
export function resolveStockInTypeLabelKey(
  type: number | string | null | undefined
): StockInTypeLabelKey {
  if (type === null || type === undefined) return 'unknown'
  if (typeof type === 'string' && type.trim() === '') return 'unknown'
  const n = Number(type)
  if (!Number.isFinite(n) || n === 0) return 'unknown'
  if (n === 1 || n === StockInTypeCode.Purchase) return 'purchase'
  if (n === StockInTypeCode.Customs) return 'customs'
  if (n === 2 || n === StockInTypeCode.Return) return 'return'
  if (n === 4 || n === StockInTypeCode.Scrap) return 'scrap'
  if (n === StockInTypeCode.Transfer) return 'transfer'
  return 'unknown'
}

const STOCK_IN_TYPE_LABELS: Record<StockInTypeLabelKey, string> = {
  purchase: '采购入库',
  customs: '报关入库',
  return: '退货入库',
  scrap: '报废入库',
  transfer: '移库',
  unknown: '未知'
}

export function stockInTypeLabel(type: number | string | null | undefined): string {
  return STOCK_IN_TYPE_LABELS[resolveStockInTypeLabelKey(type)]
}

/** 列表筛选下拉/URL：仅购销四档；历史采购 1 归一为 10；移库与非法值忽略。 */
export function parseStockInTypeFilterValue(
  raw: number | string | null | undefined
): (typeof STOCK_IN_TYPE_FILTER_VALUES)[number] | undefined {
  if (raw === null || raw === undefined) return undefined
  if (typeof raw === 'string' && raw.trim() === '') return undefined
  const n = Number(raw)
  if (!Number.isFinite(n)) return undefined
  if (n === 1 || n === StockInTypeCode.Purchase) return StockInTypeCode.Purchase
  if (n === StockInTypeCode.Customs) return StockInTypeCode.Customs
  if (n === StockInTypeCode.Return) return StockInTypeCode.Return
  if (n === StockInTypeCode.Scrap) return StockInTypeCode.Scrap
  return undefined
}
