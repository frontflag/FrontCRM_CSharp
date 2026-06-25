export type JsonRenderMode =
  | 'skip'
  | 'scalar'
  | 'url'
  | 'string-list'
  | 'breakdown-table'
  | 'price-tiers-table'
  | 'alternatives-table'
  | 'object-table'
  | 'object-list'
  | 'industry-news-list'
  | 'labeled-rows'
  | 'mixed-list'
  | 'object'

export type IndustryNewsItem = {
  title: string
  summary: string
  url: string
}

export function isPlainObject(v: unknown): v is Record<string, unknown> {
  return v !== null && typeof v === 'object' && !Array.isArray(v)
}

export function isEmptyValue(v: unknown): boolean {
  if (v == null) return true
  if (typeof v === 'string') return v.trim() === ''
  if (Array.isArray(v)) return v.length === 0
  if (isPlainObject(v)) return Object.keys(v).length === 0
  return false
}

export function isScalar(v: unknown): v is string | number | boolean {
  const t = typeof v
  return t === 'string' || t === 'number' || t === 'boolean'
}

export function snakeToCamel(key: string): string {
  return key.replace(/_([a-z0-9])/gi, (_, c: string) => c.toUpperCase())
}

export function humanizeKey(key: string): string {
  return key.replace(/_/g, ' ')
}

export function formatScalar(v: unknown): string {
  if (v == null) return ''
  if (typeof v === 'string') return v.trim()
  if (typeof v === 'number' || typeof v === 'boolean') return String(v)
  return ''
}

export function isUrlField(key: string, v: unknown): v is string {
  if (typeof v !== 'string' || !v.trim()) return false
  const k = key.toLowerCase()
  if (k.endsWith('_url') || k === 'url' || k === 'link' || k === 'datasheet_url' || k === 'image_url') {
    return /^https?:\/\//i.test(v.trim())
  }
  return false
}

export function isStringArray(v: unknown): v is string[] {
  return Array.isArray(v) && v.length > 0 && v.every((x) => typeof x === 'string')
}

export function isObjectArray(v: unknown): v is Record<string, unknown>[] {
  return Array.isArray(v) && v.length > 0 && v.every((x) => isPlainObject(x))
}

export function isBreakdownArray(v: unknown): boolean {
  if (!isObjectArray(v)) return false
  return v.every((row) => 'segment' in row || 'meaning' in row)
}

export function isPriceTiersArray(v: unknown): boolean {
  if (!isObjectArray(v)) return false
  return v.every(
    (row) =>
      'quantity' in row ||
      'qty' in row ||
      'min_qty' in row ||
      'unit_price' in row ||
      'unitPrice' in row ||
      'price' in row
  )
}

export function objectEntries(obj: Record<string, unknown>): { key: string; value: unknown }[] {
  return Object.entries(obj).map(([key, value]) => ({ key, value }))
}

export function visibleEntries(obj: Record<string, unknown>): { key: string; value: unknown }[] {
  return objectEntries(obj).filter(({ value }) => !isEmptyValue(value))
}

export function objectArrayColumns(rows: Record<string, unknown>[]): string[] {
  const keys = new Set<string>()
  for (const row of rows) {
    for (const k of Object.keys(row)) keys.add(k)
  }
  return Array.from(keys)
}

export function formatCellValue(v: unknown): string {
  if (v == null) return '—'
  if (isScalar(v)) return formatScalar(v) || '—'
  if (Array.isArray(v)) {
    const parts = v.map((x) => formatCellValue(x)).filter((x) => x !== '—')
    return parts.length ? parts.join('; ') : '—'
  }
  if (isPlainObject(v)) {
    try {
      return JSON.stringify(v)
    } catch {
      return '—'
    }
  }
  return String(v)
}

/** 混合数组项 → 可读单行文本 */
export function formatMixedListItem(v: unknown): string | null {
  if (v == null) return null
  if (typeof v === 'string') return v.trim() || null
  if (typeof v === 'number' || typeof v === 'boolean') return String(v)
  if (!isPlainObject(v)) return null

  const pn =
    formatScalar(v.part_number) ||
    formatScalar(v.pn) ||
    formatScalar(v.model) ||
    formatScalar(v.name)
  const brand = formatScalar(v.brand) || formatScalar(v.manufacturer) || formatScalar(v.distributor)
  const note =
    formatScalar(v.note) ||
    formatScalar(v.description) ||
    formatScalar(v.reason) ||
    formatScalar(v.summary) ||
    formatScalar(v.price_range) ||
    formatScalar(v.status)

  if (pn && brand && note) return `${pn}（${brand}）— ${note}`
  if (brand && note) return `${brand} — ${note}`
  if (pn && note) return `${pn} — ${note}`
  if (pn && brand) return `${pn}（${brand}）`
  if (pn) return pn

  const title = formatScalar(v.title) || formatScalar(v.headline)
  const url = formatScalar(v.url) || formatScalar(v.link)
  if (title && url) return `${title} — ${url}`
  if (title) return title

  const parts = Object.values(v)
    .map((val) => formatCellValue(val))
    .filter((x) => x && x !== '—')
  return parts.length ? parts.join('；') : null
}

export function breakdownRows(v: unknown): { segment: string; meaning: string }[] {
  if (!Array.isArray(v)) return []
  return v
    .map((item) => {
      if (!isPlainObject(item)) return null
      const segment = formatScalar(item.segment) || '—'
      const meaning = formatScalar(item.meaning) || '—'
      if (segment === '—' && meaning === '—') return null
      return { segment, meaning }
    })
    .filter(Boolean) as { segment: string; meaning: string }[]
}

export function alternativeRows(v: unknown): { partNumber: string; brand: string; note: string }[] {
  if (!isObjectArray(v)) return []
  return v
    .map((item) => {
      const partNumber =
        formatScalar(item.part_number) || formatScalar(item.pn) || formatScalar(item.model) || '—'
      const brand = formatScalar(item.brand) || formatScalar(item.manufacturer) || '—'
      const note =
        formatScalar(item.note) ||
        formatScalar(item.description) ||
        formatScalar(item.reason) ||
        '—'
      if (partNumber === '—' && brand === '—' && note === '—') return null
      return { partNumber, brand, note }
    })
    .filter(Boolean) as { partNumber: string; brand: string; note: string }[]
}

export function industryNewsItems(v: unknown): IndustryNewsItem[] {
  if (!isObjectArray(v)) return []
  return v
    .map((item) => ({
      title: formatScalar(item.title) || formatScalar(item.headline) || '',
      summary: formatScalar(item.summary) || formatScalar(item.description) || '',
      url: formatScalar(item.url) || formatScalar(item.link) || ''
    }))
    .filter((item) => item.title || item.summary || item.url)
}

export function priceTierRows(v: unknown): { quantity: string; unitPrice: string }[] {
  if (!Array.isArray(v)) return []
  return v
    .map((item) => {
      if (!isPlainObject(item)) return null
      const quantity =
        formatScalar(item.quantity) || formatScalar(item.qty) || formatScalar(item.min_qty) || '—'
      const unitPrice =
        formatScalar(item.unit_price) || formatScalar(item.unitPrice) || formatScalar(item.price) || '—'
      if (quantity === '—' && unitPrice === '—') return null
      return { quantity, unitPrice }
    })
    .filter(Boolean) as { quantity: string; unitPrice: string }[]
}

export function joinPath(base: string | undefined, key: string): string {
  return base ? `${base}.${key}` : key
}
