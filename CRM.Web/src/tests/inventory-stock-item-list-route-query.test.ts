import { describe, expect, it } from 'vitest'
import {
  applyStockItemListRouteQuery,
  buildRankingStockItemDrillRoute
} from '@/utils/inventoryOnHandBoardDrill'
import { INVENTORY_BOARD_CONVERTED_USD_KEY } from '@/utils/inventoryBoardConvertedUsd'

function emptyTargets() {
  return {
    filters: {
      stockInCode: '',
      stockItemCode: '',
      freightForwarderOrderNo: '',
      purchasePn: '',
      purchaseBrand: '',
      warehouseId: '',
      outboundStatus: undefined as number | undefined,
      stockPresence: '' as '' | 'has' | 'none',
      customerName: '',
      vendorName: '',
      salespersonUserId: undefined as string | undefined,
      purchaserUserId: undefined as string | undefined,
      stockType: undefined as number | undefined,
      stockInType: undefined as number | undefined,
      stagnantOnly: false,
      rankDimension: '',
      rankKey: '',
      rankCurrency: undefined as number | undefined
    },
    dateFrom: { value: null as string | null },
    dateTo: { value: null as string | null },
    drillMode: { value: '' as '' | 'stagnant' | 'ranking' },
    drillRankLabel: { value: '' },
    drillRankPanel: { value: '' },
    drillRankCurrencyKey: { value: '' }
  }
}

describe('applyStockItemListRouteQuery', () => {
  it('applies stockType=2 from URL', () => {
    const targets = emptyTargets()
    applyStockItemListRouteQuery({ stockType: '2' }, targets)
    expect(targets.filters.stockType).toBe(2)
  })

  it('does not treat converted USD as original-currency rank filter', () => {
    const targets = emptyTargets()
    applyStockItemListRouteQuery(
      {
        drill: 'ranking',
        rankDimension: 'customer',
        rankKey: 'c1',
        rankCurrency: INVENTORY_BOARD_CONVERTED_USD_KEY
      },
      targets
    )
    expect(targets.filters.rankCurrency).toBeUndefined()
    expect(targets.drillRankCurrencyKey.value).toBe(INVENTORY_BOARD_CONVERTED_USD_KEY)
  })
})

describe('buildRankingStockItemDrillRoute', () => {
  it('omits rankCurrency for converted USD', () => {
    const to = buildRankingStockItemDrillRoute(
      {},
      'customer',
      { id: 'c1', name: 'Acme' },
      'amount',
      INVENTORY_BOARD_CONVERTED_USD_KEY
    )
    expect(to.query.rankCurrency).toBeUndefined()
  })

  it('keeps rankCurrency for original USD', () => {
    const to = buildRankingStockItemDrillRoute(
      {},
      'customer',
      { id: 'c1', name: 'Acme' },
      'amount',
      '2'
    )
    expect(to.query.rankCurrency).toBe('2')
  })
})
