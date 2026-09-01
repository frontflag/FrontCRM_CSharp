/** 报价单行明细（兼容 PascalCase JSON）。 */
export function quoteLineItemsRaw(quoteRow: Record<string, unknown>): Record<string, unknown>[] {
  const rawItems = (quoteRow.items ?? quoteRow.Items) as unknown[] | undefined
  if (!rawItems?.length) return []
  return rawItems.map((it) => it as Record<string, unknown>)
}

/** 供应商名称：多行去重后顿号拼接；采购脱敏为 —。 */
export function quoteVendorNamesDisplay(quoteRow: Record<string, unknown>, masked: boolean): string {
  if (masked) return '—'
  const set = new Set<string>()
  for (const o of quoteLineItemsRaw(quoteRow)) {
    const n = o.vendorName ?? o.VendorName
    if (n != null && String(n).trim() !== '') set.add(String(n).trim())
  }
  return set.size > 0 ? [...set].join('、') : '—'
}

/** 供应商等级：现读码转文案后去重顿号拼接。无等级为 —。 */
export function quoteVendorLevelsDisplay(
  quoteRow: Record<string, unknown>,
  levelLabel: (level: number | null | undefined) => string
): string {
  const set = new Set<string>()
  for (const o of quoteLineItemsRaw(quoteRow)) {
    const raw = o.vendorLevel ?? o.VendorLevel
    if (raw == null || raw === '') continue
    const n = typeof raw === 'number' ? raw : Number(raw)
    const label = Number.isFinite(n) ? levelLabel(n) : String(raw).trim()
    if (label && label !== '--' && label !== '—') set.add(label)
  }
  return set.size > 0 ? [...set].join('、') : '—'
}

/** 供应商交易次数：按供应商去重后顿号拼接。无供应商 ID 时仍显示次数（511 / 需求参考只打码名称）。 */
export function quoteVendorTradeCountsDisplay(quoteRow: Record<string, unknown>): string {
  const seen = new Set<string>()
  const parts: string[] = []
  for (const o of quoteLineItemsRaw(quoteRow)) {
    const id = String(o.vendorId ?? o.VendorId ?? '').trim()
    const raw = o.vendorTradeCount ?? o.VendorTradeCount
    const hasCount = raw != null && raw !== ''
    if (!id && !hasCount) continue
    const n = !hasCount ? 0 : Number(raw)
    const display = Number.isFinite(n) && n >= 0 ? String(Math.trunc(n)) : '0'
    const key = id ? id.toLowerCase() : `n:${display}`
    if (seen.has(key)) continue
    seen.add(key)
    parts.push(display)
  }
  return parts.length > 0 ? parts.join('、') : '—'
}
