/** 出库利润率展示：成本为 0 时后端 rate=0，统一显示 — */
export function formatProfitOutRateBizDisplay(
  profitOutBizUsd?: unknown,
  profitOutRateBiz?: unknown
): string {
  if (profitOutRateBiz == null) return '—'
  const rate = Number(profitOutRateBiz)
  const profit = Number(profitOutBizUsd)
  if (!Number.isFinite(rate)) return '—'
  if (rate === 0 && (!Number.isFinite(profit) || profit >= 0)) return '—'
  return rate.toFixed(6)
}
