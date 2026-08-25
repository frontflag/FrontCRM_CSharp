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
