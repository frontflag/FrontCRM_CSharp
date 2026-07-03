import type { DictionaryItemDto } from '@/api/dictionary'
import { listAmountCurrencyIso } from '@/utils/moneyFormat'
import { copyTextToClipboard } from '@/utils/clipboard'
import { productionDateDisplayLabel } from '@/composables/useMaterialProductionDateDict'

export type QuoteSummaryCopyBuildOptions = {
  naLabel: string
  materialPdOptions: readonly DictionaryItemDto[]
}

function firstQuoteItem(row: Record<string, unknown>): Record<string, unknown> | null {
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) return null
  return items[0] as Record<string, unknown>
}

function firstQuoteItemMpn(row: Record<string, unknown>): string {
  const hdr = row.mpn ?? row.Mpn ?? row.MPN
  if (hdr != null && String(hdr).trim() !== '') return String(hdr).trim()
  const it = firstQuoteItem(row)
  if (!it) return ''
  const m = it.mpn ?? it.Mpn ?? it.MPN
  return m != null && String(m).trim() !== '' ? String(m).trim() : ''
}

function displayFirstItemBrand(row: Record<string, unknown>, naLabel: string): string {
  const it = firstQuoteItem(row)
  if (!it) return naLabel
  const b = it.brand ?? it.Brand
  if (b != null && String(b).trim() !== '') return String(b)
  return naLabel
}

function displayFirstItemQuantity(row: Record<string, unknown>, naLabel: string): string {
  const it = firstQuoteItem(row)
  if (!it) return naLabel
  const q = it.quantity ?? it.Quantity
  if (q == null || q === '') return naLabel
  const n = Number(q)
  if (Number.isNaN(n)) return naLabel
  return String(n)
}

function displayQuoteProductionDateDc(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): string {
  const { naLabel, materialPdOptions } = opts
  const mapOne = (code: string) => {
    const label = productionDateDisplayLabel(code, materialPdOptions)
    return (label && label.trim()) || code
  }
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) {
    const hdr = row.dateCode ?? row.DateCode
    const s = hdr != null ? String(hdr).trim() : ''
    if (!s) return naLabel
    return mapOne(s) || naLabel
  }
  const labels = new Set<string>()
  for (const raw of items) {
    const o = raw as Record<string, unknown>
    const dcRaw = o.dateCode ?? o.DateCode
    if (dcRaw == null || String(dcRaw).trim() === '') continue
    const code = String(dcRaw).trim()
    const text = mapOne(code)
    if (text) labels.add(text)
  }
  if (labels.size === 0) return naLabel
  return [...labels].join('、')
}

function formatCopyUnitPrice(value: number): string {
  if (!Number.isFinite(value)) return '—'
  const fixed = value.toFixed(6).replace(/\.?0+$/, '')
  return fixed || '0'
}

/** 复制摘要：物料型号、品牌、数量PCS、单价+币别、生产日期 */
export function buildQuoteSummaryCopyText(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): string {
  const { naLabel } = opts
  const mpn = firstQuoteItemMpn(row) || '—'

  const brandRaw = displayFirstItemBrand(row, naLabel)
  const brand = brandRaw === naLabel ? '—' : brandRaw

  const qtyRaw = displayFirstItemQuantity(row, naLabel)
  const qty = qtyRaw === naLabel ? '—' : `${qtyRaw}PCS`

  let priceCurrency = '—'
  const it = firstQuoteItem(row)
  if (it) {
    const p = it.unitPrice ?? it.UnitPrice
    const n = Number(p)
    if (Number.isFinite(n)) {
      const ccy = listAmountCurrencyIso(Number(it.currency ?? it.Currency ?? 1))
      priceCurrency = `${formatCopyUnitPrice(n)}${ccy}`
    }
  }

  const pdRaw = displayQuoteProductionDateDc(row, opts)
  const pd = pdRaw === naLabel ? '—' : pdRaw

  return [mpn, brand, qty, priceCurrency, pd].join('    ')
}

export async function copyQuoteSummaryToClipboard(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): Promise<boolean> {
  const text = buildQuoteSummaryCopyText(row, opts)
  const ok = copyTextToClipboard(text)
  if (ok) return true
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      /* fall through */
    }
  }
  return false
}
