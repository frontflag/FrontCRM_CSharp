/** 物料型号 / PN 规范化（全局 AI 物料情报缓存 key） */
export function normalizeMaterialPn(pn: string | null | undefined): string {
  return String(pn ?? '').trim()
}

/** 从需求明细行解析物料型号 */
export function resolveRfqItemMaterialPn(row: { materialModel?: string; mpn?: string } | null | undefined): string {
  if (!row) return ''
  const r = row as { materialModel?: string; mpn?: string }
  return normalizeMaterialPn(r.materialModel || r.mpn)
}
