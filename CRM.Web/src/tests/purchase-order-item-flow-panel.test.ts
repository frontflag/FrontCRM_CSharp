import { describe, expect, it } from 'vitest'
import { buildPurchaseOrderItemFlowStations } from '@/utils/purchaseOrderItemFlowPanel'
import { formatFlowVendorNameWithCode } from '@/utils/sellOrderItemFlowPanel'

describe('formatFlowVendorNameWithCode', () => {
  it('joins name and code', () => {
    expect(formatFlowVendorNameWithCode('Acme Semi', 'V001')).toBe('Acme Semi (V001)')
  })
})

describe('buildPurchaseOrderItemFlowStations vendorId', () => {
  const t = (key: string) => key

  it('copies vendorId onto the purchase-order-item card', () => {
    const stations = buildPurchaseOrderItemFlowStations(
      {
        purchaseOrderItemId: 'poi-1',
        purchaseOrderId: 'po-1',
        purchaseOrderItemCode: 'PO-1-01',
        itemStatus: 10,
        vendorId: 'vend-1',
        vendorName: 'Acme Semi',
        vendorCode: 'V001',
        qty: 1,
        cost: 1,
        currency: 1
      },
      null,
      t
    )
    const card = stations.find((s) => s.key === 'purchaseOrderItem')?.cards[0]
    expect(card?.vendorId).toBe('vend-1')
    expect(card?.vendorName).toBe('Acme Semi')
    expect(card?.vendorCode).toBe('V001')
  })

  it('clears vendorId when masked', () => {
    const stations = buildPurchaseOrderItemFlowStations(
      {
        purchaseOrderItemId: 'poi-1',
        purchaseOrderId: 'po-1',
        vendorId: 'vend-1',
        vendorName: 'Acme Semi',
        qty: 1
      },
      null,
      t,
      { maskSensitive: true }
    )
    const card = stations.find((s) => s.key === 'purchaseOrderItem')?.cards[0]
    expect(card?.vendorId).toBeNull()
    expect(card?.vendorName).toBe('—')
  })
})
