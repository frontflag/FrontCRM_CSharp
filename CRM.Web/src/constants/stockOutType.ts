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

const STOCK_OUT_TYPE_LABELS: Record<number, string> = {
  [StockOutTypeCode.Sales]: '销售出库',
  [StockOutTypeCode.Customs]: '报关出库',
  [StockOutTypeCode.Return]: '退货出库',
  [StockOutTypeCode.Scrap]: '报废出库'
}

export function stockOutTypeLabel(type: number): string {
  return STOCK_OUT_TYPE_LABELS[type] ?? String(type)
}
