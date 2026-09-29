import type { DictionaryItemDto } from '@/api/dictionary'
import { listAmountCurrencyIso } from '@/utils/moneyFormat'
import { copyTextToClipboard } from '@/utils/clipboard'
import { productionDateDisplayLabel } from '@/composables/useMaterialProductionDateDict'

export type QuoteSummaryCopyBuildOptions = {
  naLabel: string
  materialPdOptions: readonly DictionaryItemDto[]
}

/** 方案 C：多行「标题: 值」 */
export type QuoteSummaryCopyFieldLabels = {
  mpn: string
  brand: string
  quantity: string
  unitPrice: string
  productionDate: string
  leadTime: string
  remark: string
}

export type QuoteSummaryCopyFields = {
  mpn: string
  brand: string
  quantity: string
  priceCurrency: string
  productionDate: string
  leadTime: string
  remark: string
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

/** 交期：明细 leadTime 去重顿号拼接；无明细则用头字段 */
function displayQuoteLeadTime(row: Record<string, unknown>, naLabel: string): string {
  const items = row.items ?? row.Items
  if (Array.isArray(items) && items.length > 0) {
    const set = new Set<string>()
    for (const raw of items) {
      const o = raw as Record<string, unknown>
      const lt = o.leadTime ?? o.LeadTime
      if (lt != null && String(lt).trim() !== '') set.add(String(lt).trim())
    }
    if (set.size > 0) return [...set].join('、')
  }
  const hdr = row.leadTime ?? row.LeadTime
  const s = hdr != null ? String(hdr).trim() : ''
  return s || naLabel
}

/** 报价头备注（非需求明细、非阶梯行备注） */
function displayQuoteHeaderRemark(row: Record<string, unknown>, naLabel: string): string {
  const raw = row.remark ?? row.Remark
  const s = raw != null ? String(raw).trim() : ''
  return s || naLabel
}

function formatCopyUnitPrice(value: number): string {
  if (!Number.isFinite(value)) return '—'
  const fixed = value.toFixed(6).replace(/\.?0+$/, '')
  return fixed || '0'
}

function dashIfNa(value: string, naLabel: string): string {
  return value === naLabel ? '—' : value
}

/** 解析复制用字段值（无标题 / 带标题共用） */
export function resolveQuoteSummaryCopyFields(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): QuoteSummaryCopyFields {
  const { naLabel } = opts
  const mpn = firstQuoteItemMpn(row) || '—'

  const brandRaw = displayFirstItemBrand(row, naLabel)
  const brand = dashIfNa(brandRaw, naLabel)

  const qtyRaw = displayFirstItemQuantity(row, naLabel)
  const quantity = qtyRaw === naLabel ? '—' : `${qtyRaw}PCS`

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
  const productionDate = dashIfNa(pdRaw, naLabel)

  const leadRaw = displayQuoteLeadTime(row, naLabel)
  const leadTime = dashIfNa(leadRaw, naLabel)

  const remarkRaw = displayQuoteHeaderRemark(row, naLabel)
  const remark = dashIfNa(remarkRaw, naLabel)

  return { mpn, brand, quantity, priceCurrency, productionDate, leadTime, remark }
}

/** 复制摘要：物料型号、品牌、数量PCS、单价+币别、生产日期、交期、报价头备注（空格分隔一行） */
export function buildQuoteSummaryCopyText(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): string {
  const f = resolveQuoteSummaryCopyFields(row, opts)
  return [f.mpn, f.brand, f.quantity, f.priceCurrency, f.productionDate, f.leadTime, f.remark].join(
    '    '
  )
}

/** 方案 C：多行「标题: 值」 */
export function buildQuoteSummaryCopyTextWithTitles(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions & { labels: QuoteSummaryCopyFieldLabels }
): string {
  const f = resolveQuoteSummaryCopyFields(row, opts)
  const { labels } = opts
  return [
    `${labels.mpn}: ${f.mpn}`,
    `${labels.brand}: ${f.brand}`,
    `${labels.quantity}: ${f.quantity}`,
    `${labels.unitPrice}: ${f.priceCurrency}`,
    `${labels.productionDate}: ${f.productionDate}`,
    `${labels.leadTime}: ${f.leadTime}`,
    `${labels.remark}: ${f.remark}`
  ].join('\n')
}

async function writeClipboardText(text: string): Promise<boolean> {
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

export async function copyQuoteSummaryToClipboard(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions
): Promise<boolean> {
  return writeClipboardText(buildQuoteSummaryCopyText(row, opts))
}

export async function copyQuoteSummaryWithTitlesToClipboard(
  row: Record<string, unknown>,
  opts: QuoteSummaryCopyBuildOptions & { labels: QuoteSummaryCopyFieldLabels }
): Promise<boolean> {
  return writeClipboardText(buildQuoteSummaryCopyTextWithTitles(row, opts))
}
