import { describe, expect, it, vi, afterEach } from 'vitest'
import {
  buildCustomerListRouteQuery,
  isCustomerListPresetId,
  isCustomerQuickFilterPresetId,
  resolveCustomerTimePresetDateRange
} from '@/utils/customerListPreset'

describe('customerListPreset', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('recognizes all preset ids', () => {
    expect(isCustomerListPresetId('created_last_7_days')).toBe(true)
    expect(isCustomerListPresetId('pending_audit')).toBe(true)
    expect(isCustomerListPresetId('has_demand')).toBe(true)
    expect(isCustomerListPresetId('has_receivable')).toBe(true)
    expect(isCustomerListPresetId('unknown')).toBe(false)
  })

  it('recognizes quickFilter preset ids', () => {
    expect(isCustomerQuickFilterPresetId('deal_last_7_days')).toBe(true)
    expect(isCustomerQuickFilterPresetId('created_today')).toBe(false)
  })

  it('resolves created_last_7_days as 7 inclusive calendar days', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 6, 19, 12, 0, 0))

    const [from, to] = resolveCustomerTimePresetDateRange('created_last_7_days')
    expect(from).toBe('2026-07-13')
    expect(to).toBe('2026-07-19')
  })

  it('buildCustomerListRouteQuery writes preset and expanded fields', () => {
    const q = buildCustomerListRouteQuery({
      preset: 'pending_submit',
      keywords: { searchTerm: 'acme' }
    })
    expect(q.preset).toBe('pending_submit')
    expect(q.status).toBe('1')
    expect(q.searchTerm).toBe('acme')
    expect(q.quickFilter).toBeUndefined()
  })

  it('buildCustomerListRouteQuery writes quickFilter for business presets', () => {
    const q = buildCustomerListRouteQuery({
      preset: 'has_demand',
      keywords: { searchTerm: 'acme' }
    })
    expect(q.preset).toBe('has_demand')
    expect(q.quickFilter).toBe('has_demand')
    expect(q.searchTerm).toBe('acme')
    expect(q.status).toBeUndefined()
    expect(q.createdFrom).toBeUndefined()
  })

  it('buildCustomerListRouteQuery without preset clears preset fields', () => {
    const q = buildCustomerListRouteQuery({
      keywords: { searchTerm: 'x' },
      advanced: { status: 2 }
    })
    expect(q.preset).toBeUndefined()
    expect(q.status).toBe('2')
    expect(q.searchTerm).toBe('x')
  })
})
