import { describe, expect, it } from 'vitest'
import { buildStockInFlowStations } from '@/utils/stockInFlowPanel'

const t = (key: string) => key

const row = {
  id: 'sti-1',
  stockInCode: 'STI0001',
  vendorId: 'v-1',
  vendorName: 'Vendor A',
  totalQuantity: 100
}

describe('buildStockInFlowStations', () => {
  it('always renders seven stations in business order', () => {
    const stations = buildStockInFlowStations(row, null, t)
    expect(stations.map((s) => s.key)).toEqual([
      'purchaseOrderItem',
      'qc',
      'stockIn',
      'stockItem',
      'stockOutNotify',
      'packing',
      'stockOut'
    ])
    expect(stations.every((s) => s.stationStatus === 'empty')).toBe(true)
  })

  it('marks stockIn station active when aggregates include header', () => {
    const stations = buildStockInFlowStations(row, {
      stockInId: 'sti-1',
      stockIn: {
        id: 'sti-1',
        docCode: 'STI0001',
        status: 2,
        stockInType: 10,
        qty: 100,
        createTime: '2026-08-01T00:00:00Z'
      },
      stockItems: []
    }, t)
    const stockIn = stations.find((s) => s.key === 'stockIn')
    expect(stockIn?.cards).toHaveLength(1)
    expect(stockIn?.stationStatus).toBe('done')
    expect(stockIn?.cards[0].showVendor).toBe(true)
  })

  it('includes manual transfer source rows in stockItem station', () => {
    const stations = buildStockInFlowStations(row, {
      stockInId: 'sti-1',
      stockIn: {
        id: 'sti-1',
        docCode: 'STI0001',
        status: 2,
        stockInType: 10,
        qty: 100,
        createTime: '2026-08-01T00:00:00Z'
      },
      stockItems: [
        {
          id: 'si-src',
          docCode: 'STK-SRC',
          status: 1,
          qty: 50,
          transferType: 10,
          createTime: '2026-08-02T00:00:00Z'
        }
      ]
    }, t)
    const stockItem = stations.find((s) => s.key === 'stockItem')
    expect(stockItem?.cards).toHaveLength(1)
    expect(stockItem?.cards[0].docNo).toBe('STK-SRC')
  })
})
