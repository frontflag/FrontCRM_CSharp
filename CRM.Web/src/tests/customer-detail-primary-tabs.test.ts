import { describe, expect, it } from 'vitest'
import {
  CUSTOMER_DETAIL_PRIMARY_TAB_DEFAULT,
  CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS,
  CUSTOMER_PORTRAIT_SUB_TAB_DEFAULT,
  parseCustomerDetailPrimaryTab,
  parseCustomerPortraitSubTab
} from '@/utils/customerDetailPrimaryTabs'

describe('parseCustomerDetailPrimaryTab', () => {
  it('defaults when missing or unknown', () => {
    expect(parseCustomerDetailPrimaryTab(undefined)).toBe(CUSTOMER_DETAIL_PRIMARY_TAB_DEFAULT)
    expect(parseCustomerDetailPrimaryTab('')).toBe('profile')
    expect(parseCustomerDetailPrimaryTab('unknown')).toBe('profile')
  })

  it('accepts known keys', () => {
    expect(parseCustomerDetailPrimaryTab('portrait')).toBe('portrait')
    expect(parseCustomerDetailPrimaryTab('statement')).toBe('statement')
    expect(parseCustomerDetailPrimaryTab('opportunity')).toBe('opportunity')
    expect(parseCustomerDetailPrimaryTab('followUp')).toBe('followUp')
    expect(parseCustomerDetailPrimaryTab('timeline')).toBe('timeline')
    expect(parseCustomerDetailPrimaryTab('news')).toBe('news')
  })

  it('reads first value from array query', () => {
    expect(parseCustomerDetailPrimaryTab(['followUp', 'news'])).toBe('followUp')
  })

  it('excludes profile, statement, portrait, news and followUp from placeholders', () => {
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).not.toContain('profile')
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).not.toContain('statement')
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).not.toContain('portrait')
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).not.toContain('news')
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).not.toContain('followUp')
    expect(CUSTOMER_DETAIL_PRIMARY_TAB_PLACEHOLDERS).toHaveLength(2)
  })
})

describe('parseCustomerPortraitSubTab', () => {
  it('defaults when missing or unknown', () => {
    expect(parseCustomerPortraitSubTab(undefined)).toBe(CUSTOMER_PORTRAIT_SUB_TAB_DEFAULT)
    expect(parseCustomerPortraitSubTab('')).toBe('rfq')
    expect(parseCustomerPortraitSubTab('unknown')).toBe('rfq')
  })

  it('accepts rfq and order', () => {
    expect(parseCustomerPortraitSubTab('rfq')).toBe('rfq')
    expect(parseCustomerPortraitSubTab('order')).toBe('order')
  })
})
