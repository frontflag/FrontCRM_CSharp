export type QuoteProductOriginLabels = {
  us: string
  nonUs: string
  pending: string
  na: string
}

export type QuoteFreeShippingLabels = {
  yes: string
  no: string
  na: string
}

export function firstQuoteListItem(row: Record<string, unknown>): Record<string, unknown> | null {
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) return null
  return items[0] as Record<string, unknown>
}

export function parseQuoteProductOriginState(value: unknown): 0 | 1 | 2 | null {
  const n = Number(value)
  if (n === 0 || n === 1 || n === 2) return n
  if (value == null || value === '') return null
  return null
}

export type QuoteFreeShippingState = 'yes' | 'no'

export function parseQuoteFreeShippingState(value: unknown): QuoteFreeShippingState | null {
  if (value === true || value === 'true' || value === 1 || value === '1') return 'yes'
  if (value === false || value === 'false' || value === 0 || value === '0') return 'no'
  return null
}

export function formatQuoteProductOrigin(
  value: unknown,
  labels: QuoteProductOriginLabels
): string {
  const n = Number(value)
  if (n === 0) return labels.us
  if (n === 1) return labels.nonUs
  if (n === 2) return labels.pending
  if (value == null || value === '') return labels.na
  return labels.na
}

export function formatQuoteFreeShipping(
  value: unknown,
  labels: QuoteFreeShippingLabels
): string {
  if (value === true || value === 'true' || value === 1 || value === '1') return labels.yes
  if (value === false || value === 'false' || value === 0 || value === '0') return labels.no
  return labels.na
}

export function resolveQuoteWaferOrigin(row: Record<string, unknown>): unknown {
  const it = firstQuoteListItem(row)
  return it?.waferOrigin ?? it?.WaferOrigin ?? row.waferOrigin ?? row.WaferOrigin
}

export function resolveQuotePackageOrigin(row: Record<string, unknown>): unknown {
  const it = firstQuoteListItem(row)
  return it?.packageOrigin ?? it?.PackageOrigin ?? row.packageOrigin ?? row.PackageOrigin
}

export function resolveQuoteFreeShipping(row: Record<string, unknown>): unknown {
  const it = firstQuoteListItem(row)
  return it?.freeShipping ?? it?.FreeShipping ?? row.freeShipping ?? row.FreeShipping
}
