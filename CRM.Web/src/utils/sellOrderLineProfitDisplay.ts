/** 利润率倍数展示：成本为 0 且利润 ≥ 0 时显示 — */
export function formatProfitRateMultiplierDisplay(
  profitUsd?: unknown,
  profitRate?: unknown | null
): string {
  if (profitRate == null) return '—'
  const rate = Number(profitRate)
  const profit = Number(profitUsd)
  if (!Number.isFinite(rate)) return '—'
  if (rate === 0 && (!Number.isFinite(profit) || profit >= 0)) return '—'
  return rate.toFixed(6)
}

/** 毛利率 % = (利润 ÷ 收入) × 100；收入 ≤ 0 时返回 null */
export function computeGrossMarginPercent(profitUsd?: unknown, revenueUsd?: unknown): number | null {
  const profit = Number(profitUsd)
  const revenue = Number(revenueUsd)
  if (!Number.isFinite(revenue) || revenue <= 0) return null
  if (!Number.isFinite(profit)) return null
  return Math.round((profit / revenue) * 10000) / 100
}

export function formatUsdProfitAmount(value?: unknown | null): string {
  if (value == null) return '—'
  const n = Number(value)
  if (!Number.isFinite(n)) return '—'
  return n.toFixed(2)
}

/** 毛利率 % 展示；收入 ≤ 0 时显示 — */
export function formatGrossMarginDisplay(profitUsd?: unknown, revenueUsd?: unknown): string {
  if (profitUsd == null) return '—'
  const pct = computeGrossMarginPercent(profitUsd, revenueUsd)
  if (pct == null) return '—'
  return `${pct.toFixed(2)}%`
}
