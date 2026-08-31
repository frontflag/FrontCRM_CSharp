import { describe, expect, it } from 'vitest'
import {
  resolveStockInCustomsBrokerName,
  resolveStockInOverviewUnitPrice,
  resolveStockInPurchaseOrderTypeKey
} from '@/utils/stockInOpsOverview'
import { StockInTypeCode } from '@/constants/stockInType'

describe('resolveStockInPurchaseOrderTypeKey', () => {
  it('maps 1/2/3', () => {
    expect(resolveStockInPurchaseOrderTypeKey(1)).toBe('customer')
    expect(resolveStockInPurchaseOrderTypeKey(2)).toBe('stocking')
    expect(resolveStockInPurchaseOrderTypeKey(3)).toBe('sample')
    expect(resolveStockInPurchaseOrderTypeKey(0)).toBe('unknown')
  })
})

describe('resolveStockInOverviewUnitPrice', () => {
  it('masks as dash', () => {
    expect(resolveStockInOverviewUnitPrice({ maskSensitive: true, aggregateUnitPrice: 1.2, aggregateCurrency: 2 })).toBe(
      '—'
    )
  })

  it('prefers aggregate unit price', () => {
    const text = resolveStockInOverviewUnitPrice({
      aggregateUnitPrice: 1.5,
      aggregateCurrency: 2,
      listSummary: '9',
      listCurrency: 1
    })
    expect(text).toContain('1.50')
    expect(text).toContain('USD')
  })

  it('falls back to a single list summary number', () => {
    const text = resolveStockInOverviewUnitPrice({
      listSummary: '3.2',
      listCurrency: 1
    })
    expect(text).toContain('3.20')
    expect(text).toContain('RMB')
  })
})

describe('resolveStockInCustomsBrokerName', () => {
  it('shows name only for customs stock-in', () => {
    expect(
      resolveStockInCustomsBrokerName({ customsBrokerName: '港通报关' }, StockInTypeCode.Customs)
    ).toBe('港通报关')
    expect(
      resolveStockInCustomsBrokerName({ customsBrokerName: '港通报关' }, StockInTypeCode.Purchase)
    ).toBe('')
  })
})
