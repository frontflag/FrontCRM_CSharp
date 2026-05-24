import type { InvoiceReportLabels } from '@/components/stockOut/packingReportLabels'
import { PACKING_REPORT_LABELS } from '@/components/stockOut/packingReportLabels'

/** Bill To / Ship To 四行地址（Attn / Tel 标签随报表语言） */
export function normalizePackingAddrLines(
  lines: string[] | undefined | null,
  customerName?: string,
  labels?: Pick<InvoiceReportLabels, 'attn' | 'tel'>
): string[] {
  const L = labels ?? PACKING_REPORT_LABELS
  const dash = '—'
  const customer = (customerName ?? '').trim() || dash
  const src = (lines ?? []).map((x) => String(x ?? '').trim() || dash)

  let rows: string[]
  if (src.length >= 4) {
    rows = [customer, src[1] || dash, src[2] || dash, src[3] || dash]
  } else if (src.length === 3) {
    rows = [customer, src[0], src[1], src[2]]
  } else {
    const [addr = dash, attn = dash, tel = dash] = src
    rows = [customer, addr, attn, tel]
  }

  return [
    rows[0],
    rows[1],
    `${L.attn}${rows[2]}`,
    `${L.tel}${rows[3]}`
  ]
}
