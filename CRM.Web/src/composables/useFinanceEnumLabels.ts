import { useI18n } from 'vue-i18n'
import {
  PAYMENT_STATUS_MAP,
  RECEIPT_STATUS_MAP,
  INVOICE_STATUS_MAP,
  PAYMENT_DONE_STATUS_MAP,
  RECEIVE_STATUS_MAP,
  PAYMENT_MODE_MAP,
  INVOICE_TYPE_MAP,
  PURCHASE_INVOICE_TYPE_MAP,
  SELL_INVOICE_TYPE_MAP,
} from '@/api/finance'

const P = 'financeEnums'

function labelFromMap(
  t: (k: string) => string,
  te: (k: string) => boolean,
  sub: string,
  n: number,
  fallback: Record<number, { label: string; type: string } | undefined>
): string {
  const key = `${P}.${sub}.${n}`
  if (te(key)) return t(key)
  return fallback[n]?.label ?? String(n)
}

function tagFromMap(
  n: number,
  fallback: Record<number, { label: string; type: string } | undefined>
): string {
  return fallback[n]?.type ?? 'info'
}

function strFromMap(
  t: (k: string) => string,
  te: (k: string) => boolean,
  sub: string,
  n: number,
  fallback: Record<number, string>
): string {
  const key = `${P}.${sub}.${n}`
  if (te(key)) return t(key)
  return fallback[n] ?? String(n)
}

function coerceInvoiceStatusKey(n: unknown): number {
  const x = Number(n)
  if (!Number.isFinite(x) || x === 0) return 1
  return x
}

function coercePaymentDoneKey(n: unknown): number {
  const x = Number(n)
  return Number.isFinite(x) ? x : 0
}

function coercePurchaseInvoiceTypeKey(n: unknown): number {
  const x = Number(n)
  return Number.isFinite(x) && x > 0 ? x : 100
}

export function useFinanceEnumLabels() {
  const { t, te } = useI18n()

  return {
    paymentStatusLabel: (n: number) => labelFromMap(t, te, 'paymentStatus', n, PAYMENT_STATUS_MAP),
    paymentStatusTag: (n: number) => tagFromMap(n, PAYMENT_STATUS_MAP),
    receiptStatusLabel: (n: number) => labelFromMap(t, te, 'receiptStatus', n, RECEIPT_STATUS_MAP),
    receiptStatusTag: (n: number) => tagFromMap(n, RECEIPT_STATUS_MAP),
    invoiceStatusLabel: (n: unknown) =>
      labelFromMap(t, te, 'invoiceStatus', coerceInvoiceStatusKey(n), INVOICE_STATUS_MAP),
    invoiceStatusTag: (n: unknown) => tagFromMap(coerceInvoiceStatusKey(n), INVOICE_STATUS_MAP),
    paymentDoneStatusLabel: (n: unknown) =>
      labelFromMap(t, te, 'paymentDoneStatus', coercePaymentDoneKey(n), PAYMENT_DONE_STATUS_MAP),
    paymentDoneStatusTag: (n: unknown) => tagFromMap(coercePaymentDoneKey(n), PAYMENT_DONE_STATUS_MAP),
    receiveStatusLabel: (n: number) => labelFromMap(t, te, 'receiveStatus', n, RECEIVE_STATUS_MAP),
    receiveStatusTag: (n: number) => tagFromMap(n, RECEIVE_STATUS_MAP),
    paymentModeLabel: (n: number) => strFromMap(t, te, 'paymentMode', n, PAYMENT_MODE_MAP),
    invoiceTypeLabel: (n: number) => strFromMap(t, te, 'invoiceType', n, INVOICE_TYPE_MAP),
    purchaseInvoiceTypeLabel: (n: unknown) =>
      strFromMap(t, te, 'purchaseInvoiceType', coercePurchaseInvoiceTypeKey(n), PURCHASE_INVOICE_TYPE_MAP),
    sellInvoiceTypeLabel: (n: number) => strFromMap(t, te, 'sellInvoiceType', n, SELL_INVOICE_TYPE_MAP),
    verificationStatusLabel: (n: number) =>
      strFromMap(t, te, 'verificationStatus', n, {
        0: '未核销',
        1: '部分核销',
        2: '核销完成',
      }),
  }
}
