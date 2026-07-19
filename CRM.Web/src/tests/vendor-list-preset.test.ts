import { describe, expect, it, vi, afterEach } from 'vitest'
import {
  buildVendorListRouteQuery,
  isVendorListPresetId,
  isVendorQuickFilterPresetId,
  resolveVendorTimePresetDateRange
} from '@/utils/vendorListPreset'

describe('vendorListPreset', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('recognizes all preset ids', () => {
    expect(isVendorListPresetId('created_last_7_days')).toBe(true)
    expect(isVendorListPresetId('favorite')).toBe(true)
    expect(isVendorListPresetId('has_quote')).toBe(true)
    expect(isVendorListPresetId('has_payable')).toBe(true)
    expect(isVendorListPresetId('unknown')).toBe(false)
  })

  it('recognizes quickFilter preset ids', () => {
    expect(isVendorQuickFilterPresetId('purchase_last_7_days')).toBe(true)
    expect(isVendorQuickFilterPresetId('pending_submit')).toBe(false)
  })

  it('resolves created_last_7_days as 7 inclusive calendar days', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 6, 19, 12, 0, 0))

    const [from, to] = resolveVendorTimePresetDateRange('created_last_7_days')
    expect(from).toBe('2026-07-13')
    expect(to).toBe('2026-07-19')
  })

  it('buildVendorListRouteQuery writes preset and expanded fields', () => {
    const q = buildVendorListRouteQuery({
      preset: 'pending_submit',
      keywords: { searchTerm: 'acme' }
    })
    expect(q.preset).toBe('pending_submit')
    expect(q.status).toBe('1')
    expect(q.searchTerm).toBe('acme')
    expect(q.quickFilter).toBeUndefined()
  })

  it('buildVendorListRouteQuery writes quickFilter for quote presets', () => {
    const q = buildVendorListRouteQuery({
      preset: 'has_quote',
      keywords: { searchTerm: 'x' }
    })
    expect(q.preset).toBe('has_quote')
    expect(q.quickFilter).toBe('has_quote')
    expect(q.searchTerm).toBe('x')
    expect(q.status).toBeUndefined()
  })

  it('buildVendorListRouteQuery without preset uses advanced fields', () => {
    const q = buildVendorListRouteQuery({
      keywords: { searchTerm: 'x' },
      advanced: { status: 2, credit: 1 }
    })
    expect(q.preset).toBeUndefined()
    expect(q.status).toBe('2')
    expect(q.credit).toBe('1')
  })
})
