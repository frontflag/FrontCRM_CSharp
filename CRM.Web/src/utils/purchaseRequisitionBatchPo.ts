import type { PurchaseRequisitionBasketItem } from '@/types/purchaseRequisitionBasket'

export const PR_PO_BATCH_MIN = 2
export const PR_PO_BATCH_MAX = 50

export type PrBatchValidateErrorCode =
  | 'batchMinCount'
  | 'batchMaxCount'
  | 'statusNotAllowed'
  | 'vendorMissing'
  | 'vendorMismatch'
  | 'poTypeMismatch'
  | 'currencyMismatch'

export function isPrBasketEligibleStatus(status: number): boolean {
  return status === 0 || status === 1
}

function pickStr(v: unknown): string | undefined {
  const s = v == null ? '' : String(v).trim()
  return s || undefined
}

export function normalizePrListRowToBasketItem(row: Record<string, unknown>): PurchaseRequisitionBasketItem | null {
  const id = String(row.id ?? row.Id ?? '').trim()
  if (!id) return null
  return {
    id,
    billCode: String(row.billCode ?? row.BillCode ?? '').trim(),
    pn: pickStr(row.pn ?? row.PN),
    brand: pickStr(row.brand ?? row.Brand),
    qty: Number(row.qty ?? row.Qty ?? 0),
    status: Number(row.status ?? row.Status ?? -1),
    sellOrderCode: pickStr(row.sellOrderCode ?? row.SellOrderCode),
    quoteVendorId: pickStr(row.quoteVendorId ?? row.QuoteVendorId)
  }
}

export function normalizePrDetailToBasketItem(pr: Record<string, unknown>): PurchaseRequisitionBasketItem | null {
  return normalizePrListRowToBasketItem(pr)
}

export function getPrQuoteVendorId(pr: Record<string, unknown>): string {
  return String(pr.quoteVendorId ?? pr.QuoteVendorId ?? '').trim()
}

export function getPrQuoteCurrency(pr: Record<string, unknown>): number {
  const n = Number(pr.quoteCurrency ?? pr.QuoteCurrency ?? pr.currency ?? pr.Currency ?? 1)
  return Number.isFinite(n) && n > 0 ? n : 1
}

export function getPrPrefillPoType(pr: Record<string, unknown>): number {
  const n = Number(pr.prefillPurchaseOrderType ?? pr.PrefillPurchaseOrderType ?? 0)
  return n >= 1 && n <= 3 ? n : 1
}

export function messageKeyForPrBatchValidateError(code: PrBatchValidateErrorCode): string {
  const map: Record<PrBatchValidateErrorCode, string> = {
    batchMinCount: 'purchaseRequisitionList.basket.batchMinTip',
    batchMaxCount: 'purchaseRequisitionList.basket.batchMaxTip',
    statusNotAllowed: 'purchaseRequisitionList.basket.validateStatusNotAllowed',
    vendorMissing: 'purchaseRequisitionList.basket.validateVendorMissing',
    vendorMismatch: 'purchaseRequisitionList.basket.validateVendorMismatch',
    poTypeMismatch: 'purchaseRequisitionList.basket.validatePoTypeMismatch',
    currencyMismatch: 'purchaseRequisitionList.basket.validateCurrencyMismatch'
  }
  return map[code]
}

export function prBatchValidateMessageParams(code: PrBatchValidateErrorCode): Record<string, number> {
  return code === 'batchMaxCount' ? { max: PR_PO_BATCH_MAX } : {}
}

export function validatePrBatchForPoGeneration(
  prs: Record<string, unknown>[]
): PrBatchValidateErrorCode | null {
  if (prs.length < PR_PO_BATCH_MIN) return 'batchMinCount'
  if (prs.length > PR_PO_BATCH_MAX) return 'batchMaxCount'

  let vendorKey: string | null = null
  let poType: number | null = null
  let currency: number | null = null

  for (const pr of prs) {
    const status = Number(pr.status ?? pr.Status ?? -1)
    if (!isPrBasketEligibleStatus(status)) return 'statusNotAllowed'

    const vid = getPrQuoteVendorId(pr)
    if (!vid) return 'vendorMissing'

    if (vendorKey == null) vendorKey = vid.toLowerCase()
    else if (vendorKey !== vid.toLowerCase()) return 'vendorMismatch'

    const pt = getPrPrefillPoType(pr)
    if (poType == null) poType = pt
    else if (poType !== pt) return 'poTypeMismatch'

    const cur = getPrQuoteCurrency(pr)
    if (currency == null) currency = cur
    else if (currency !== cur) return 'currencyMismatch'
  }

  return null
}

export function resolveLatestDeliveryDate(prs: Record<string, unknown>[]): string {
  let latest = ''
  for (const pr of prs) {
    const raw = pr.deliveryDate ?? pr.DeliveryDate ?? pr.expectedPurchaseTime ?? pr.ExpectedPurchaseTime
    const s = raw == null ? '' : String(raw).split('T')[0]!
    if (s && (!latest || s > latest)) latest = s
  }
  return latest
}

export function resolvePurchaserFromPr(pr: Record<string, unknown>): { id: string; name: string } {
  const quoteUid = String(pr.prefillPurchaseUserId ?? pr.PrefillPurchaseUserId ?? '').trim()
  const quoteName = String(pr.prefillPurchaseUserName ?? pr.PrefillPurchaseUserName ?? '').trim()
  const rfqUid = String(pr.prefillRfqPurchaserUserId ?? pr.PrefillRfqPurchaserUserId ?? '').trim()
  const rfqName = String(pr.prefillRfqPurchaserUserName ?? pr.PrefillRfqPurchaserUserName ?? '').trim()
  const prUid = String(pr.purchaseUserId ?? pr.PurchaseUserId ?? '').trim()
  const prName = String(pr.purchaseUserName ?? pr.PurchaseUserName ?? '').trim()
  if (quoteUid) return { id: quoteUid, name: quoteName }
  if (rfqUid) return { id: rfqUid, name: rfqName }
  return { id: prUid, name: prName }
}

export type PoLineItemDraft = {
  purchaseRequisitionId?: string
  sellOrderItemId?: string
  vendorId: string
  pn: string
  brand: string
  brandId?: number
  customerMaterialModel: string
  targetPrice: number
  qty: number
  cost: number
  currency: number
  quoteCurrency: number
  dateCode: string
  deliveryDate: string
  comment: string
  innerComment: string
}

export function buildPoLineItemFromPr(
  pr: Record<string, unknown>,
  opts: {
    manualVendorId: string
    coercePd: (v: string) => string
    headerDeliveryDate: string
  }
): PoLineItemDraft {
  const quoteCostNum = Number(pr.quoteCost ?? pr.QuoteCost ?? 0) || 0
  const quoteCurNum = getPrQuoteCurrency(pr)
  const deliveryDateStr = pr.deliveryDate
    ? String(pr.deliveryDate).split('T')[0]!
    : pr.expectedPurchaseTime
      ? String(pr.expectedPurchaseTime).split('T')[0]!
      : ''

  return {
    purchaseRequisitionId: String(pr.id ?? pr.Id ?? '').trim() || undefined,
    sellOrderItemId: pr.sellOrderItemId ? String(pr.sellOrderItemId).trim() : undefined,
    vendorId: getPrQuoteVendorId(pr) || opts.manualVendorId,
    pn: String(pr.pn ?? pr.PN ?? ''),
    brand: String(pr.brand ?? pr.Brand ?? ''),
    brandId: undefined,
    customerMaterialModel: String(pr.customerMaterialModel ?? pr.CustomerMaterialModel ?? ''),
    targetPrice: quoteCostNum,
    qty: Number(pr.qty ?? pr.Qty ?? 1) || 1,
    cost: quoteCostNum,
    currency: quoteCurNum,
    quoteCurrency: quoteCurNum,
    dateCode: opts.coercePd(String(pr.dateCode ?? pr.DateCode ?? '').trim()),
    deliveryDate: deliveryDateStr || opts.headerDeliveryDate || '',
    comment: String(pr.itemRemark ?? pr.ItemRemark ?? pr.remark ?? pr.Remark ?? ''),
    innerComment: ''
  }
}
