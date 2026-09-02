import { describe, expect, it } from 'vitest'
import {
  buildSellOrderItemFlowStations,
  formatFlowCustomerNameWithCode,
  resolveFlowPartyId
} from '@/utils/sellOrderItemFlowPanel'

describe('formatFlowCustomerNameWithCode', () => {
  it('joins name and code', () => {
    expect(formatFlowCustomerNameWithCode('Acme', 'C001')).toBe('Acme (C001)')
  })

  it('returns em dash when masked or empty', () => {
    expect(formatFlowCustomerNameWithCode('—', '—')).toBe('—')
    expect(formatFlowCustomerNameWithCode(null, null)).toBe('—')
    expect(formatFlowCustomerNameWithCode('', '')).toBe('—')
  })

  it('omits parentheses when code is missing', () => {
    expect(formatFlowCustomerNameWithCode('Acme', null)).toBe('Acme')
    expect(formatFlowCustomerNameWithCode('Acme', '—')).toBe('Acme')
  })
})

describe('buildSellOrderItemFlowStations customerId', () => {
  const t = (key: string) => key

  it('copies customerId onto the sales-order-item card', () => {
    const stations = buildSellOrderItemFlowStations(
      {
        sellOrderItemId: 'soi-1',
        sellOrderId: 'so-1',
        sellOrderItemCode: 'SO-1-01',
        orderStatus: 10,
        customerId: 'cust-1',
        customerName: 'Acme',
        customerCode: 'C001',
        qty: 1,
        price: 1,
        currency: 1
      },
      null,
      t
    )
    const card = stations.find((s) => s.key === 'sellOrderItem')?.cards[0]
    expect(card?.customerId).toBe('cust-1')
  })

  it('clears customerId when masked', () => {
    const stations = buildSellOrderItemFlowStations(
      {
        sellOrderItemId: 'soi-1',
        sellOrderId: 'so-1',
        customerId: 'cust-1',
        customerName: 'Acme',
        qty: 1
      },
      null,
      t,
      { maskSensitive: true }
    )
    const card = stations.find((s) => s.key === 'sellOrderItem')?.cards[0]
    expect(card?.customerId).toBeNull()
    expect(card?.customerName).toBe('—')
  })
})

describe('resolveFlowPartyId', () => {
  it('returns first non-empty id', () => {
    expect(resolveFlowPartyId(false, '', '  ', 'abc')).toBe('abc')
  })

  it('returns null when masked', () => {
    expect(resolveFlowPartyId(true, 'abc')).toBeNull()
  })
})
