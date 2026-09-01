import { describe, expect, it } from 'vitest'
import {
  buildStockInAmountCurrencyDrillRoute,
  buildStockOutAmountCurrencyDrillRoute,
  canShowStockInCurrencyView,
  canShowStockOutCurrencyView,
  LOGISTICS_ANALYTICS_STOCK_IN_DRILL,
  LOGISTICS_ANALYTICS_STOCK_OUT_DRILL,
  parseStockInAmountCurrencyDrillQuery,
  parseStockOutAmountCurrencyDrillQuery,
  STOCK_IN_POSTED_STATUS,
  STOCK_IN_PURCHASE_TYPE,
  STOCK_OUT_FINISHED_STATUS,
  STOCK_OUT_SALES_TYPE
} from '@/utils/logisticsAnalyticsDrill'

describe('canShowStockInCurrencyView', () => {
  const ok = {
    viewLevel: 'company',
    accessMode: 'logistics',
    inventoryType: 'all' as const,
    maskAmounts: false,
    hasInventoryRead: true
  }

  it('allows logistics company view with inventory type all, unmasked, and inventory.read', () => {
    expect(canShowStockInCurrencyView(ok)).toBe(true)
  })

  it('hides in department or personal view', () => {
    expect(canShowStockInCurrencyView({ ...ok, viewLevel: 'department' })).toBe(false)
    expect(canShowStockInCurrencyView({ ...ok, viewLevel: 'personal' })).toBe(false)
  })

  it('hides for sales-purchase-only access', () => {
    expect(canShowStockInCurrencyView({ ...ok, accessMode: 'salesPurchaseOnly' })).toBe(false)
  })

  it('hides when inventory type is customer-order or purchase stock', () => {
    expect(canShowStockInCurrencyView({ ...ok, inventoryType: 'customerOrder' })).toBe(false)
    expect(canShowStockInCurrencyView({ ...ok, inventoryType: 'purchaseStock' })).toBe(false)
  })

  it('hides when amounts are masked or inventory.read is missing', () => {
    expect(canShowStockInCurrencyView({ ...ok, maskAmounts: true })).toBe(false)
    expect(canShowStockInCurrencyView({ ...ok, hasInventoryRead: false })).toBe(false)
  })
})

describe('buildStockInAmountCurrencyDrillRoute', () => {
  it('uses trend window, posted status, purchase type, and line currency', () => {
    const route = buildStockInAmountCurrencyDrillRoute(
      { dateFrom: '2026-01-01', dateTo: '2026-09-01' },
      1
    )
    expect(route).toEqual({
      path: '/inventory/stock-in',
      query: {
        drill: LOGISTICS_ANALYTICS_STOCK_IN_DRILL,
        stockInType: String(STOCK_IN_PURCHASE_TYPE),
        status: String(STOCK_IN_POSTED_STATUS),
        itemCurrency: '1',
        stockInDateStart: '2026-01-01',
        stockInDateEnd: '2026-09-01'
      }
    })
  })

  it('rejects non-finite currency', () => {
    expect(buildStockInAmountCurrencyDrillRoute({}, Number.NaN)).toBeNull()
  })
})

describe('parseStockInAmountCurrencyDrillQuery', () => {
  it('reads drill query including array-shaped vue-router values', () => {
    const parsed = parseStockInAmountCurrencyDrillQuery({
      drill: LOGISTICS_ANALYTICS_STOCK_IN_DRILL,
      status: ['2'],
      itemCurrency: '2',
      stockInDateStart: '2026-03-01',
      stockInDateEnd: '2026-08-31'
    })
    expect(parsed).toEqual({
      isDrill: true,
      itemCurrency: 2,
      status: 2,
      stockInDateStart: '2026-03-01',
      stockInDateEnd: '2026-08-31'
    })
  })

  it('is not a drill when drill key is absent', () => {
    expect(parseStockInAmountCurrencyDrillQuery({ status: '2', itemCurrency: '1' }).isDrill).toBe(
      false
    )
  })
})

describe('canShowStockOutCurrencyView', () => {
  const ok = {
    viewLevel: 'company',
    accessMode: 'logistics',
    inventoryType: 'all' as const,
    maskSalesAmounts: false,
    hasInventoryRead: true
  }

  it('allows logistics company view with inventory type all, unmasked, and inventory.read', () => {
    expect(canShowStockOutCurrencyView(ok)).toBe(true)
  })

  it('hides in department or personal view', () => {
    expect(canShowStockOutCurrencyView({ ...ok, viewLevel: 'department' })).toBe(false)
    expect(canShowStockOutCurrencyView({ ...ok, viewLevel: 'personal' })).toBe(false)
  })

  it('hides for sales-purchase-only access', () => {
    expect(canShowStockOutCurrencyView({ ...ok, accessMode: 'salesPurchaseOnly' })).toBe(false)
  })

  it('hides when inventory type is customer-order or purchase stock', () => {
    expect(canShowStockOutCurrencyView({ ...ok, inventoryType: 'customerOrder' })).toBe(false)
    expect(canShowStockOutCurrencyView({ ...ok, inventoryType: 'purchaseStock' })).toBe(false)
  })

  it('hides when sales amounts are masked or inventory.read is missing', () => {
    expect(canShowStockOutCurrencyView({ ...ok, maskSalesAmounts: true })).toBe(false)
    expect(canShowStockOutCurrencyView({ ...ok, hasInventoryRead: false })).toBe(false)
  })
})

describe('buildStockOutAmountCurrencyDrillRoute', () => {
  it('uses trend window, finished status, sales type, and sales currency', () => {
    const route = buildStockOutAmountCurrencyDrillRoute(
      { dateFrom: '2026-01-01', dateTo: '2026-09-01' },
      2
    )
    expect(route).toEqual({
      path: '/inventory/stock-out/items',
      query: {
        drill: LOGISTICS_ANALYTICS_STOCK_OUT_DRILL,
        stockOutType: String(STOCK_OUT_SALES_TYPE),
        status: String(STOCK_OUT_FINISHED_STATUS),
        salesCurrency: '2',
        stockOutDateFrom: '2026-01-01',
        stockOutDateTo: '2026-09-01'
      }
    })
  })

  it('rejects non-finite currency', () => {
    expect(buildStockOutAmountCurrencyDrillRoute({}, Number.NaN)).toBeNull()
  })
})

describe('parseStockOutAmountCurrencyDrillQuery', () => {
  it('reads drill query including array-shaped vue-router values', () => {
    const parsed = parseStockOutAmountCurrencyDrillQuery({
      drill: LOGISTICS_ANALYTICS_STOCK_OUT_DRILL,
      status: ['4'],
      salesCurrency: '1',
      stockOutDateFrom: '2026-03-01',
      stockOutDateTo: '2026-08-31'
    })
    expect(parsed).toEqual({
      isDrill: true,
      salesCurrency: 1,
      status: 4,
      stockOutDateFrom: '2026-03-01',
      stockOutDateTo: '2026-08-31'
    })
  })

  it('is not a drill when drill key is absent', () => {
    expect(parseStockOutAmountCurrencyDrillQuery({ status: '4', salesCurrency: '1' }).isDrill).toBe(
      false
    )
  })
})
