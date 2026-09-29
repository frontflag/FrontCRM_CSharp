/** Trim + 忽略大小写后的型号键，用于报价 vs 需求原型号比对。 */
export function normalizeQuoteMpnKey(value: unknown): string {
  return String(value ?? '').trim().toLowerCase()
}

/**
 * 报价型号与需求明细原型号是否不一致。
 * 无需求原型号时不提示（无可对照基线）。
 */
export function isQuoteMpnMismatch(quoteMpn: unknown, rfqItemMpn: unknown): boolean {
  const rfqKey = normalizeQuoteMpnKey(rfqItemMpn)
  if (!rfqKey) return false
  return normalizeQuoteMpnKey(quoteMpn) !== rfqKey
}

/** 从报价行读取需求原型号（列表/Dock 接口 hydrate 的 rfqItemMpn）。 */
export function quoteRowRfqItemMpn(row: Record<string, unknown>): string {
  const v = row.rfqItemMpn ?? row.RfqItemMpn
  return v != null && String(v).trim() !== '' ? String(v).trim() : ''
}
