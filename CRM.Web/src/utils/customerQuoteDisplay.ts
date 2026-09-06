/** 对外编号：v1 仅编码；v2 起为 {编码}-{版本}。 */
export function formatCustomerQuoteDisplayCode(
  customerQuoteCode?: string | null,
  versionNo?: number | null
): string {
  const code = (customerQuoteCode || '').trim()
  if (!code) return ''
  const v = Number(versionNo)
  if (!Number.isFinite(v) || v <= 1) return code
  return `${code}-${v}`
}
