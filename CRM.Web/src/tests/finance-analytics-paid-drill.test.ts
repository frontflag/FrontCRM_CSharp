import { describe, expect, it } from 'vitest'
import {
  buildCompletedDrillRoute,
  buildPaidCurrencyDrillRoute,
  buildReceivedCurrencyDrillRoute,
  canShowPaidCurrencyView,
  canShowReceivedCurrencyView,
  FINANCE_ANALYTICS_PAID_DRILL,
  FINANCE_ANALYTICS_RECEIVED_DRILL,
  FINANCE_PAYMENT_COMPLETE_STATUS,
  FINANCE_RECEIPT_CONFIRMED_STATUS,
  isCompletedDrillable,
  parsePaidCurrencyDrillQuery,
  parseReceivedCurrencyDrillQuery
} from '@/utils/financeAnalyticsDrill'

describe('canShowPaidCurrencyView', () => {
  const ok = {
    viewLevel: 'company',
    accessMode: 'finance',
    maskAmounts: false,
    hasPaymentRead: true
  }

  it('allows finance company view with payment read and unmasked amounts', () => {
    expect(canShowPaidCurrencyView(ok)).toBe(true)
  })

  it('hides in department or personal view', () => {
    expect(canShowPaidCurrencyView({ ...ok, viewLevel: 'department' })).toBe(false)
    expect(canShowPaidCurrencyView({ ...ok, viewLevel: 'personal' })).toBe(false)
  })

  it('hides for sales-purchase-only access', () => {
    expect(canShowPaidCurrencyView({ ...ok, accessMode: 'salesPurchaseOnly' })).toBe(false)
  })

  it('hides when amounts are masked or payment read is missing', () => {
    expect(canShowPaidCurrencyView({ ...ok, maskAmounts: true })).toBe(false)
    expect(canShowPaidCurrencyView({ ...ok, hasPaymentRead: false })).toBe(false)
  })
})

describe('buildPaidCurrencyDrillRoute', () => {
  it('uses KPI window, completed status, and payment currency', () => {
    const route = buildPaidCurrencyDrillRoute(
      { dateFrom: '2026-01-01', dateTo: '2026-09-01' },
      1
    )
    expect(route).toEqual({
      name: 'FinancePaymentList',
      query: {
        drill: FINANCE_ANALYTICS_PAID_DRILL,
        status: String(FINANCE_PAYMENT_COMPLETE_STATUS),
        paymentCurrency: '1',
        startDate: '2026-01-01',
        endDate: '2026-09-01'
      }
    })
  })

  it('rejects non-finite currency', () => {
    expect(buildPaidCurrencyDrillRoute({}, Number.NaN)).toBeNull()
  })
})

describe('parsePaidCurrencyDrillQuery', () => {
  it('reads drill query including array-shaped vue-router values', () => {
    const parsed = parsePaidCurrencyDrillQuery({
      drill: FINANCE_ANALYTICS_PAID_DRILL,
      status: ['100'],
      paymentCurrency: '2',
      startDate: '2026-03-01',
      endDate: '2026-08-31'
    })
    expect(parsed).toEqual({
      isDrill: true,
      paymentCurrency: 2,
      status: 100,
      startDate: '2026-03-01',
      endDate: '2026-08-31'
    })
  })

  it('is not a drill when the flag is absent', () => {
    const parsed = parsePaidCurrencyDrillQuery({ paymentCurrency: '1' })
    expect(parsed.isDrill).toBe(false)
    expect(parsed.paymentCurrency).toBe(1)
  })
})

describe('completed paid card is not whole-card drillable', () => {
  it('buildCompletedDrillRoute paid returns null', () => {
    expect(
      buildCompletedDrillRoute('paid', { dateFrom: '2026-01-01', dateTo: '2026-09-01' })
    ).toBeNull()
  })

  it('isCompletedDrillable paid is always false', () => {
    expect(isCompletedDrillable('paid', false)).toBe(false)
    expect(isCompletedDrillable('received', false)).toBe(false)
    expect(isCompletedDrillable('issuedPurchaseInvoice', false)).toBe(true)
  })
})

describe('canShowReceivedCurrencyView', () => {
  const ok = {
    viewLevel: 'company',
    accessMode: 'finance',
    maskAmounts: false,
    hasReceiptRead: true
  }

  it('allows finance company view with receipt read', () => {
    expect(canShowReceivedCurrencyView(ok)).toBe(true)
  })

  it('hides outside finance company view', () => {
    expect(canShowReceivedCurrencyView({ ...ok, viewLevel: 'department' })).toBe(false)
    expect(canShowReceivedCurrencyView({ ...ok, accessMode: 'salesPurchaseOnly' })).toBe(false)
    expect(canShowReceivedCurrencyView({ ...ok, hasReceiptRead: false })).toBe(false)
  })
})

describe('buildReceivedCurrencyDrillRoute', () => {
  it('uses receipt date params, not create-time startDate/endDate', () => {
    const route = buildReceivedCurrencyDrillRoute(
      { dateFrom: '2026-01-01', dateTo: '2026-09-01' },
      1
    )
    expect(route).toEqual({
      name: 'FinanceReceiptList',
      query: {
        drill: FINANCE_ANALYTICS_RECEIVED_DRILL,
        status: String(FINANCE_RECEIPT_CONFIRMED_STATUS),
        receiptCurrency: '1',
        receiptDateFrom: '2026-01-01',
        receiptDateTo: '2026-09-01'
      }
    })
  })
})

describe('parseReceivedCurrencyDrillQuery', () => {
  it('reads hidden receipt date and currency', () => {
    expect(
      parseReceivedCurrencyDrillQuery({
        drill: FINANCE_ANALYTICS_RECEIVED_DRILL,
        status: '3',
        receiptCurrency: '2',
        receiptDateFrom: '2026-03-01',
        receiptDateTo: '2026-08-31',
        startDate: '2020-01-01'
      })
    ).toEqual({
      isDrill: true,
      receiptCurrency: 2,
      status: 3,
      receiptDateFrom: '2026-03-01',
      receiptDateTo: '2026-08-31'
    })
  })
})

describe('completed received card is not whole-card drillable', () => {
  it('buildCompletedDrillRoute received returns null', () => {
    expect(
      buildCompletedDrillRoute('received', { dateFrom: '2026-01-01', dateTo: '2026-09-01' })
    ).toBeNull()
  })
})
