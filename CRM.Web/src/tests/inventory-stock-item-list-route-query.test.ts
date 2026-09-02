import { describe, expect, it } from 'vitest'
import { applyStockItemListRouteQuery } from '@/utils/inventoryOnHandBoardDrill'

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
})
