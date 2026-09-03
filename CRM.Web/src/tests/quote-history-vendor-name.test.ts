import { describe, expect, it } from 'vitest'
import {
  canShowQuoteHistoryVendorName,
  quoteHistoryVendorNameDisplay,
  quoteHistoryVendorNameParts
} from '@/utils/quoteVendorDisplay'

describe('quoteHistoryVendorNameDisplay', () => {
  it('formats zh/en without spaces around slash', () => {
    expect(
      quoteHistoryVendorNameDisplay({
        items: [
          {
            vendorName: '利物通发展（香港）有限公司',
            vendorEnglishName: 'LIVERTONE DEVELOPMENT (HK) CO., LIMITED'
          }
        ]
      })
    ).toBe('利物通发展（香港）有限公司/LIVERTONE DEVELOPMENT (HK) CO., LIMITED')
  })

  it('shows one side when the other is empty', () => {
    expect(quoteHistoryVendorNameDisplay({ items: [{ vendorName: '仅中文' }] })).toBe('仅中文')
    expect(quoteHistoryVendorNameDisplay({ items: [{ vendorEnglishName: 'EN ONLY' }] })).toBe(
      'EN ONLY'
    )
  })

  it('joins distinct vendors with ideographic semicolon', () => {
    expect(
      quoteHistoryVendorNameDisplay({
        items: [
          { vendorName: '甲', vendorEnglishName: 'A' },
          { vendorName: '乙', vendorEnglishName: 'B' }
        ]
      })
    ).toBe('甲/A；乙/B')
  })

  it('returns dash when no vendor names', () => {
    expect(quoteHistoryVendorNameDisplay({ items: [] })).toBe('—')
    expect(quoteHistoryVendorNameDisplay({})).toBe('—')
  })
})

describe('quoteHistoryVendorNameParts', () => {
  it('keeps vendorId for detail navigation', () => {
    expect(
      quoteHistoryVendorNameParts({
        items: [
          {
            vendorId: 'v-1',
            vendorName: '甲',
            vendorEnglishName: 'A'
          },
          {
            vendorId: 'v-1',
            vendorName: '甲',
            vendorEnglishName: 'A'
          },
          {
            vendorId: 'v-2',
            vendorName: '乙',
            vendorEnglishName: 'B'
          }
        ]
      })
    ).toEqual([
      { label: '甲/A', vendorId: 'v-1' },
      { label: '乙/B', vendorId: 'v-2' }
    ])
  })

  it('allows name without vendorId (plain text, no link)', () => {
    expect(quoteHistoryVendorNameParts({ items: [{ vendorName: '仅中文' }] })).toEqual([
      { label: '仅中文', vendorId: null }
    ])
  })
})

describe('canShowQuoteHistoryVendorName', () => {
  it('shows for purchase identity and purchase dept', () => {
    expect(canShowQuoteHistoryVendorName({ identityType: 2 })).toBe(true)
    expect(canShowQuoteHistoryVendorName({ identityType: 3 })).toBe(true)
    expect(canShowQuoteHistoryVendorName({ belongsToPurchaseDept: true })).toBe(true)
  })

  it('shows for SYS_ADMIN / SYS_MANAGER / SYS_BIZ_MANAGER', () => {
    expect(canShowQuoteHistoryVendorName({ isSysAdmin: true })).toBe(true)
    expect(canShowQuoteHistoryVendorName({ isSysManager: true })).toBe(true)
    expect(canShowQuoteHistoryVendorName({ isBizManager: true })).toBe(true)
    expect(canShowQuoteHistoryVendorName({ roleCodes: ['SYS_BIZ_MANAGER'] })).toBe(true)
  })

  it('hides for sales and other identities', () => {
    expect(canShowQuoteHistoryVendorName(null)).toBe(false)
    expect(canShowQuoteHistoryVendorName({ identityType: 1 })).toBe(false)
    expect(canShowQuoteHistoryVendorName({ identityType: 5 })).toBe(false)
  })
})
