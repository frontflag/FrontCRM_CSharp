import { describe, expect, it } from 'vitest'
import { quoteVendorTradeCountsDisplay } from '@/utils/quoteVendorDisplay'

describe('quoteVendorTradeCountsDisplay', () => {
  it('无供应商为 —', () => {
    expect(quoteVendorTradeCountsDisplay({ items: [{ vendorName: 'A' }] })).toBe('—')
    expect(quoteVendorTradeCountsDisplay({})).toBe('—')
  })

  it('有供应商无次数按 0', () => {
    expect(
      quoteVendorTradeCountsDisplay({ items: [{ vendorId: 'v1', vendorTradeCount: undefined }] })
    ).toBe('0')
  })

  it('同一供应商多行只显示一次次数', () => {
    expect(
      quoteVendorTradeCountsDisplay({
        items: [
          { vendorId: 'v1', vendorTradeCount: 4 },
          { vendorId: 'V1', VendorTradeCount: 4 }
        ]
      })
    ).toBe('4')
  })

  it('多供应商顿号拼接', () => {
    expect(
      quoteVendorTradeCountsDisplay({
        items: [
          { vendorId: 'a', vendorTradeCount: 1 },
          { vendorId: 'b', vendorTradeCount: 8 }
        ]
      })
    ).toBe('1、8')
  })
})
