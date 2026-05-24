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

const STOCK_IN_TYPE_LABELS: Record<number, string> = {
  [StockInTypeCode.Purchase]: '采购入库',
  [StockInTypeCode.Customs]: '报关入库',
  [StockInTypeCode.Return]: '退货入库',
  [StockInTypeCode.Scrap]: '报废入库',
  [StockInTypeCode.Transfer]: '调拨入库'
}

/** 兼容迁移前库内 1/2/4 旧值展示 */
const LEGACY_STOCK_IN_TYPE: Record<number, number> = {
  1: StockInTypeCode.Purchase,
  2: StockInTypeCode.Return,
  4: StockInTypeCode.Scrap
}

export function stockInTypeLabel(type: number): string {
  const resolved = LEGACY_STOCK_IN_TYPE[type] ?? type
  return STOCK_IN_TYPE_LABELS[resolved] ?? String(type)
}
