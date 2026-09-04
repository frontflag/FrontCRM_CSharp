import { describe, expect, it } from 'vitest'
import { buildStockOutItemFlowStations } from '@/utils/stockOutItemFlowPanel'

const t = (key: string) => key

const row = {
  stockOutItemId: 'outi-1',
  stockOutId: 'out-1',
  stockOutCode: 'STO0021X',
  stockOutItemCode: 'STO0021X-1',
  status: 2,
  stockOutDate: '2026-07-17T19:44:00Z',
  customerName: 'Cust',
  salesUserName: 'Jolin',
  outQuantity: 1000,
  stockOutType: 20
}

describe('buildStockOutItemFlowStations', () => {
  it('renders seven stations in business order with stock-out as current', () => {
    const stations = buildStockOutItemFlowStations(row, null, t)
    expect(stations.map((s) => s.key)).toEqual([
      'sellOrderItem',
      'stockOutNotify',
      'stockItem',
      'packing',
      'stockOut',
      'receivable',
      'receiptWriteOff'
    ])
    expect(stations.find((s) => s.key === 'stockOut')?.stationStatus).toBe('done')
    expect(stations.filter((s) => s.key !== 'stockOut').every((s) => s.stationStatus === 'empty')).toBe(true)
  })

  it('scopes packing and stock-out cards to this line payload only', () => {
    const stations = buildStockOutItemFlowStations(row, {
      stockOutItemId: 'outi-1',
      packings: [{ id: 'pk-1', docCode: 'PAK0020U', status: 100, qty: 1000, stockOutType: 20 }],
      stockOuts: [
        {
          id: 'out-1',
          docCode: 'STO0021X',
          lineDocCode: 'STO0021X-1',
          status: 2,
          qty: 1000,
          stockOutType: 20,
          personName: 'Jolin'
        }
      ]
    }, t)
    expect(stations.find((s) => s.key === 'packing')?.cards).toHaveLength(1)
    expect(stations.find((s) => s.key === 'stockOut')?.cards).toHaveLength(1)
    expect(stations.find((s) => s.key === 'stockOut')?.cards[0].lineDocNo).toBe('STO0021X-1')
    expect(stations.find((s) => s.key === 'stockOut')?.cards[0].personName).toBe('Jolin')
  })

  it('falls back to packing stock-out type when the flow line omits type', () => {
    const stations = buildStockOutItemFlowStations(
      { ...row, stockOutType: undefined },
      {
        stockOutItemId: 'outi-1',
        packings: [{ id: 'pk-1', docCode: 'PAK0020U', status: 100, qty: 1000, stockOutType: 20 }],
        stockOuts: [
          {
            id: 'out-1',
            docCode: 'STO0021X',
            lineDocCode: 'STO0021X-1',
            status: 2,
            qty: 1000
          }
        ]
      },
      t
    )
    expect(stations.find((s) => s.key === 'stockOut')?.cards[0].stockOutType).toBe(20)
    expect(stations.find((s) => s.key === 'packing')?.cards[0].stockOutType).toBe(20)
  })

  it('falls back to list-row stock-out type when flow line omits type', () => {
    const stations = buildStockOutItemFlowStations(
      { ...row, stockOutType: 20 },
      {
        stockOutItemId: 'outi-1',
        stockOuts: [
          {
            id: 'out-1',
            docCode: 'STO0021X',
            lineDocCode: 'STO0021X-1',
            status: 2,
            qty: 1000,
            personName: 'Jolin'
          }
        ]
      },
      t
    )
    const out = stations.find((s) => s.key === 'stockOut')?.cards[0]
    expect(out?.stockOutType).toBe(20)
    expect(out?.showPerson).toBe(true)
  })

  it('does not expand stock items beyond this line payload and keeps qty=0 layers', () => {
    const stations = buildStockOutItemFlowStations(row, {
      stockOutItemId: 'outi-1',
      stockItems: [
        { id: 'si-1', docCode: 'STK-1', status: 3, qty: 10, qty2: 10 },
        { id: 'si-2', docCode: 'STK-2', status: 1, qty: 0, qty2: 0 }
      ]
    }, t)
    const stock = stations.find((s) => s.key === 'stockItem')
    expect(stock?.cards).toHaveLength(2)
    expect(stock?.cards.map((c) => c.docNo)).toEqual(['STK-1', 'STK-2'])
    expect(stock?.cards[1].qtyText).toBe('0 pcs')
    expect(stations.map((s) => s.key)).not.toContain('stockingStockItem')
    expect(stations.map((s) => s.key)).not.toContain('picking')
  })

  it('shows receivable scope note and linked stock-out line codes when shared', () => {
    const stations = buildStockOutItemFlowStations(row, {
      stockOutItemId: 'outi-1',
      receivables: [
        {
          id: 'ar-1',
          receivableCode: 'AR0001',
          verificationStatus: 1,
          amount: 1000,
          verifiedToBe: 400,
          currency: 2,
          stockOutDate: '2026-07-17T19:44:00Z',
          customerName: 'Cust',
          stockOutItemLineCount: 2,
          stockOutItemCodes: ['STO0021X-1', 'STO0021X-2']
        }
      ],
      receiptWriteOffs: [
        {
          id: 'wo-1',
          amount: 600,
          currency: 2,
          createTime: '2026-07-18T10:00:00Z',
          financeReceiptId: 'rc-1',
          financeReceiptCode: 'RC0001',
          customerName: 'Cust',
          operatorUserName: 'FinUser'
        }
      ]
    }, t)
    const receivable = stations.find((s) => s.key === 'receivable')?.cards[0]
    expect(receivable?.receivableScopeNote).toBe('stockOutItemList.flowPanel.receivableScopeNote')
    expect(receivable?.linkedStockOutItemCodes).toEqual(['STO0021X-1', 'STO0021X-2'])
    expect(receivable?.verifiedToBeText).toBeTruthy()
    expect(stations.find((s) => s.key === 'receiptWriteOff')?.cards).toHaveLength(1)
  })
})
