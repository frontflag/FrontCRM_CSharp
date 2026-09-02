import { describe, expect, it } from 'vitest'
import { buildPackingItemFlowStations, formatFlowCardDate } from '@/utils/packingItemFlowPanel'

const t = (key: string) => key

const row = {
  packingId: 'pk-1',
  packingCode: 'PK001',
  packingStatus: 10,
  packingItemId: 'pi-1',
  packingItemQty: 5,
  stockOutNotifyId: 'n-1',
  sellOrderItemId: 'soi-1',
  sellOrderId: 'so-1',
  sellOrderItemCode: 'SO-1-01',
  customerName: 'Cust',
  salesUserName: 'Sales',
  createUserName: 'Packer',
  createTime: '2026-08-01T00:00:00Z',
  stockOutType: 10,
  customsDeclarationId: null,
  customsDeclarationCode: null
}

describe('buildPackingItemFlowStations stock-out type', () => {
  it('puts stock-out type on notify and packing cards from extras/header', () => {
    const stations = buildPackingItemFlowStations(row, null, t, {
      extras: {
        stockOutNotifies: [
          {
            id: 'n-1',
            requestCode: 'STOR00001',
            status: 10,
            salesOrderItemId: 'soi-1',
            outQuantity: 5,
            requestDate: '2026-08-01T00:00:00Z',
            createTime: '2026-08-01T00:00:00Z',
            stockOutType: 20,
            customsDeclarationId: 'dec-1',
            customsDeclarationCode: 'CD001',
            customerName: 'Cust',
            salesUserName: 'Sales'
          }
        ]
      }
    })

    const notify = stations.find((s) => s.key === 'stockOutNotify')?.cards[0]
    expect(notify?.stockOutType).toBe(20)
    expect(notify?.customsDeclarationId).toBe('dec-1')
    expect(notify?.customsDeclarationCode).toBe('CD001')

    const packing = stations.find((s) => s.key === 'packing')?.cards[0]
    expect(packing?.stockOutType).toBe(10)
    expect(packing?.customsDeclarationId).toBeNull()
  })

  it('shows customs declaration on packing when type is customs and id is linked', () => {
    const stations = buildPackingItemFlowStations(
      {
        ...row,
        stockOutType: 20,
        customsDeclarationId: 'dec-p',
        customsDeclarationCode: 'CD-P'
      },
      null,
      t
    )
    const packing = stations.find((s) => s.key === 'packing')?.cards[0]
    expect(packing?.stockOutType).toBe(20)
    expect(packing?.customsDeclarationId).toBe('dec-p')
    expect(packing?.customsDeclarationCode).toBe('CD-P')
  })

  it('adds stock-out type on stock-out cards while keeping creator', () => {
    const stations = buildPackingItemFlowStations(row, null, t, {
      extras: {
        stockOutLines: [
          {
            stockOutId: 'out-1',
            stockOutCode: 'STO1',
            stockOutItemId: 'outi-1',
            stockOutItemCode: 'STO1-01',
            qty: 5,
            status: 2,
            createTime: '2026-08-03T00:00:00Z',
            customerName: 'Cust',
            createUserName: 'Alice',
            stockOutType: 20,
            customsDeclarationId: 'dec-1',
            customsDeclarationCode: 'CD001'
          }
        ]
      }
    })
    const out = stations.find((s) => s.key === 'stockOut')?.cards[0]
    expect(out?.stockOutType).toBe(20)
    expect(out?.customsDeclarationId).toBe('dec-1')
    expect(out?.personName).toBe('Alice')
    expect(out?.createdAt).toBe('2026-08-03T00:00:00Z')
  })

  it('falls back to packing header stock-out type when flow lines omit type', () => {
    const stations = buildPackingItemFlowStations(
      {
        ...row,
        stockOutType: 20,
        customsDeclarationId: 'dec-p',
        customsDeclarationCode: 'CD-P'
      },
      null,
      t,
      {
        extras: {
          stockOutLines: [
            {
              stockOutId: 'out-1',
              stockOutCode: 'STO0021X',
              stockOutItemId: 'outi-1',
              stockOutItemCode: 'STO0021X-1',
              qty: 1000,
              status: 2,
              createTime: '2026-07-17T19:44:00Z',
              customerName: 'Cust',
              createUserName: 'Jolin'
            }
          ]
        }
      }
    )
    const out = stations.find((s) => s.key === 'stockOut')?.cards[0]
    expect(out?.stockOutType).toBe(20)
    expect(out?.customsDeclarationId).toBe('dec-p')
    expect(out?.customsDeclarationCode).toBe('CD-P')
    expect(out?.personName).toBe('Jolin')
  })

  it('does not attach customs ids when stock-out type is sales', () => {
    const stations = buildPackingItemFlowStations(row, null, t, {
      extras: {
        stockOutNotifies: [
          {
            id: 'n-1',
            requestCode: 'STOR00001',
            status: 10,
            salesOrderItemId: 'soi-1',
            outQuantity: 5,
            requestDate: '2026-08-01T00:00:00Z',
            createTime: '2026-08-01T00:00:00Z',
            stockOutType: 10,
            customsDeclarationId: null,
            customerName: 'Cust'
          }
        ]
      }
    })
    const notify = stations.find((s) => s.key === 'stockOutNotify')?.cards[0]
    expect(notify?.stockOutType).toBe(10)
    expect(notify?.customsDeclarationId).toBeNull()
  })
})

describe('formatFlowCardDate', () => {
  it('renders YY-MM-DD without time', () => {
    expect(formatFlowCardDate('2026-08-01T00:00:00Z')).toBe('26-08-01')
    expect(formatFlowCardDate(null)).toBe('—')
    expect(formatFlowCardDate('')).toBe('—')
  })
})
