import { describe, expect, it } from 'vitest'
import {
  applyCustomsInvoiceUsdPrices,
  formatInvoiceMoney,
  resolveCustomsInvoiceStockOutType,
  resolveInvoiceLineAmounts,
  resolveInvoiceTotalCurrency,
  sumInvoiceLineAmounts,
  STOCK_OUT_TYPE_CUSTOMS
} from '@/utils/invoiceReportLines'
import { CurrencyCode } from '@/constants/currency'

describe('invoice report line amounts', () => {
  it('uses packing line sales price instead of header total', () => {
    const lines = resolveInvoiceLineAmounts(
      [{ qty: 389, price: 6.75 }],
      0
    )
    expect(lines).toEqual([{ qty: 389, unit: 6.75, amount: 6.75 * 389 }])
    expect(sumInvoiceLineAmounts(lines)).toBe(6.75 * 389)
  })

  it('keeps per-line prices when header total is also present', () => {
    const lines = resolveInvoiceLineAmounts(
      [
        { qty: 10, price: 2 },
        { qty: 5, price: 4 }
      ],
      999
    )
    expect(lines).toEqual([
      { qty: 10, unit: 2, amount: 20 },
      { qty: 5, unit: 4, amount: 20 }
    ])
  })

  it('allocates header total by qty when no line price', () => {
    const lines = resolveInvoiceLineAmounts([{ qty: 2 }, { qty: 2 }], 100)
    expect(lines[0].unit).toBe(25)
    expect(lines[0].amount).toBe(50)
    expect(sumInvoiceLineAmounts(lines)).toBe(100)
  })

  it('prints unit and amount with line currency', () => {
    expect(formatInvoiceMoney(6.75, CurrencyCode.RMB, 'unit')).toBe('6.75 RMB')
    expect(formatInvoiceMoney(6.75 * 389, CurrencyCode.RMB, 'total')).toBe('2,625.75 RMB')
  })

  it('uses a single currency on the total when all lines match', () => {
    expect(
      resolveInvoiceTotalCurrency([
        { priceCurrency: CurrencyCode.RMB },
        { priceCurrency: CurrencyCode.RMB }
      ])
    ).toBe(CurrencyCode.RMB)
    expect(
      resolveInvoiceTotalCurrency([
        { priceCurrency: CurrencyCode.RMB },
        { priceCurrency: CurrencyCode.USD }
      ])
    ).toBeNull()
  })

  it('customs packing invoice forces USD and convert price', () => {
    const lines = [
      { packingItemId: 'a', price: 72, priceCurrency: CurrencyCode.RMB }
    ]
    applyCustomsInvoiceUsdPrices(
      20,
      lines,
      [{ packingItemId: 'a', priceConvertPrice: 10.4 }]
    )
    expect(lines[0].price).toBe(10.4)
    expect(lines[0].priceCurrency).toBe(CurrencyCode.USD)
  })

  it('sales packing invoice keeps order currency', () => {
    const lines = [
      { packingItemId: 'a', price: 72, priceCurrency: CurrencyCode.RMB }
    ]
    applyCustomsInvoiceUsdPrices(10, lines, [{ packingItemId: 'a', priceConvertPrice: 10.4 }])
    expect(lines[0].price).toBe(72)
    expect(lines[0].priceCurrency).toBe(CurrencyCode.RMB)
  })

  it('customs invoice uses packing type not linked sales stock-out type', () => {
    expect(
      resolveCustomsInvoiceStockOutType({
        packingStockOutType: STOCK_OUT_TYPE_CUSTOMS,
        stockOutType: 10,
        customsBrokerConsignee: false
      })
    ).toBe(STOCK_OUT_TYPE_CUSTOMS)
  })

  it('customs invoice falls back to broker consignee flag', () => {
    expect(
      resolveCustomsInvoiceStockOutType({
        stockOutType: 10,
        customsBrokerConsignee: true
      })
    ).toBe(STOCK_OUT_TYPE_CUSTOMS)
  })

  it('customs invoice without convert price still forces USD currency', () => {
    const lines = [{ packingItemId: 'a', price: 72, priceCurrency: CurrencyCode.RMB }]
    applyCustomsInvoiceUsdPrices(20, lines, [])
    expect(lines[0].price).toBe(72)
    expect(lines[0].priceCurrency).toBe(CurrencyCode.USD)
  })
})
