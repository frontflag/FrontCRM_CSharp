/** 库存看板金额下拉：各原币折成美元后再加总（非数字 key，避免当下原币过滤）。 */
export const INVENTORY_BOARD_CONVERTED_USD_KEY = 'usdConverted'

export function isInventoryBoardConvertedUsd(key: string | undefined | null): boolean {
  return key === INVENTORY_BOARD_CONVERTED_USD_KEY
}

/** 金额下拉顺序：RMB → USD → 折算USD → 其余原币。 */
export function appendInventoryBoardConvertedUsdOption(
  original: { key: string; label: string }[],
  convertedLabel: string
): { key: string; label: string }[] {
  const converted = { key: INVENTORY_BOARD_CONVERTED_USD_KEY, label: convertedLabel }
  const usdIndex = original.findIndex((o) => o.key === '2')
  if (usdIndex >= 0) {
    return [...original.slice(0, usdIndex + 1), converted, ...original.slice(usdIndex + 1)]
  }
  const rmbIndex = original.findIndex((o) => o.key === '1')
  if (rmbIndex >= 0) {
    return [...original.slice(0, rmbIndex + 1), converted, ...original.slice(rmbIndex + 1)]
  }
  return [...original, converted]
}

export function formatInventoryBoardConvertedUsd(amount: number | null | undefined): string {
  if (amount == null) return '—'
  return `$\u00a0${amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}
