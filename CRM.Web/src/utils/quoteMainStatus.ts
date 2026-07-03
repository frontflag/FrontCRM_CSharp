/** 报价主表状态：0 新建 · 1 成单 · 2 关闭 */
export const QuoteMainStatus = {
  New: 0,
  Won: 1,
  Closed: 2
} as const

export type QuoteMainStatusValue = (typeof QuoteMainStatus)[keyof typeof QuoteMainStatus]

export function normalizeQuoteMainStatus(raw: unknown): QuoteMainStatusValue | null {
  const n = Number(raw)
  if (n === QuoteMainStatus.New || n === QuoteMainStatus.Won || n === QuoteMainStatus.Closed) return n
  return null
}

export function quoteMainStatusTagType(status: unknown): string {
  const n = normalizeQuoteMainStatus(status)
  if (n === QuoteMainStatus.Won) return 'success'
  if (n === QuoteMainStatus.Closed) return 'info'
  return 'primary'
}

export function quoteMainStatusI18nKey(status: unknown): string {
  const n = normalizeQuoteMainStatus(status)
  if (n === QuoteMainStatus.Won) return 'quoteList.status.won'
  if (n === QuoteMainStatus.Closed) return 'quoteList.status.closed'
  if (n === QuoteMainStatus.New) return 'quoteList.status.new'
  return 'quoteList.status.unknown'
}

export function isQuoteReadOnly(status: unknown): boolean {
  const n = normalizeQuoteMainStatus(status)
  return n === QuoteMainStatus.Won || n === QuoteMainStatus.Closed
}

export function isQuoteDeleteForbidden(status: unknown): boolean {
  return normalizeQuoteMainStatus(status) === QuoteMainStatus.Won
}
