import { describe, expect, it } from 'vitest'
import { buildStockOutNotifyFlowStations } from '@/utils/stockOutNotifyFlowPanel'

const t = (key: string) => key

const row = {
  id: 'n-1',
  requestCode: 'STOR00001',
  salesOrderId: 'so-1',
  salesOrderItemId: 'soi-1',
  status: 10,
  stockOutType: 10,
  outQuantity: 5,
  customerName: 'Cust',
  salesUserName: 'Sales'
}

describe('buildStockOutNotifyFlowStations', () => {
  it('renders core stations in business order and omits stocking when empty', () => {
    const stations = buildStockOutNotifyFlowStations(row, null, t)
    expect(stations.map((s) => s.key)).toEqual([
      'sellOrderItem',
      'stockOutNotify',
      'stockItem',
      'packing',
      'stockOut'
    ])
    expect(stations.find((s) => s.key === 'stockOutNotify')?.stationStatus).toBe('active')
    expect(stations.filter((s) => s.key !== 'stockOutNotify').every((s) => s.stationStatus === 'empty')).toBe(
      true
    )
  })

  it('inserts stocking station only when there are on-hand stocking cards', () => {
    const without = buildStockOutNotifyFlowStations(row, {
      stockOutNotifyId: 'n-1',
      stockOutNotify: { id: 'n-1', docCode: 'STOR00001', status: 10, qty: 5 },
      stockItems: [{ id: 'si-0', docCode: 'STK-0', status: 3, qty: 10, qty2: 10 }],
      stockingStockItems: []
    }, t)
    expect(without.map((s) => s.key)).not.toContain('stockingStockItem')

    const withStocking = buildStockOutNotifyFlowStations(row, {
      stockOutNotifyId: 'n-1',
      stockOutNotify: { id: 'n-1', docCode: 'STOR00001', status: 10, qty: 5 },
      stockItems: [{ id: 'si-0', docCode: 'STK-0', status: 3, qty: 10, qty2: 10 }],
      stockingStockItems: [{ id: 'si-s', docCode: 'STK-S', status: 1, qty: 8, qty2: 0 }]
    }, t)
    expect(withStocking.map((s) => s.key)).toEqual([
      'sellOrderItem',
      'stockOutNotify',
      'stockItem',
      'stockingStockItem',
      'packing',
      'stockOut'
    ])
  })

  it('keeps bound stock items even when inbound qty is fully shipped', () => {
    const stations = buildStockOutNotifyFlowStations(row, {
      stockOutNotifyId: 'n-1',
      stockOutNotify: { id: 'n-1', docCode: 'STOR00001', status: 100, qty: 5 },
      stockItems: [
        { id: 'si-1', docCode: 'STK-1', status: 3, qty: 10, qty2: 10 },
        { id: 'si-2', docCode: 'STK-2', status: 1, qty: 0, qty2: 0 }
      ]
    }, t)
    const stock = stations.find((s) => s.key === 'stockItem')
    expect(stock?.cards).toHaveLength(2)
    expect(stock?.cards.map((c) => c.docNo)).toEqual(['STK-1', 'STK-2'])
    expect(stock?.cards[1].qtyText).toBe('0 pcs')
  })

  it('scopes packing and stock-out cards to this notify payload only', () => {
    const stations = buildStockOutNotifyFlowStations(row, {
      stockOutNotifyId: 'n-1',
      stockOutNotify: { id: 'n-1', docCode: 'STOR00001', status: 20, qty: 5 },
      packings: [{ id: 'pk-1', docCode: 'Pak1', status: 10, qty: 5, createTime: '2026-08-02T00:00:00Z' }],
      stockOuts: [{ id: 'out-1', docCode: 'STO1', status: 2, qty: 5, createTime: '2026-08-03T00:00:00Z' }]
    }, t)
    expect(stations.find((s) => s.key === 'packing')?.cards).toHaveLength(1)
    expect(stations.find((s) => s.key === 'packing')?.cards[0].qtyText).toBe('5 pcs')
    expect(stations.find((s) => s.key === 'stockOut')?.cards).toHaveLength(1)
    expect(stations.find((s) => s.key === 'stockOut')?.cards[0].qtyText).toBe('5 pcs')
  })
})
