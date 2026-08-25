import { describe, expect, it } from 'vitest'
import {
  buildRfqItemListRouteQuery,
  isRfqItemListPresetId,
  isRfqItemQuickFilterPresetId,
  resolveRfqItemPresetApiParams
} from '@/utils/rfqItemListPreset'

describe('rfqItemListPreset', () => {
  it('recognizes has_deleted_quote as a quickFilter preset', () => {
    expect(isRfqItemListPresetId('has_deleted_quote')).toBe(true)
    expect(isRfqItemQuickFilterPresetId('has_deleted_quote')).toBe(true)
    expect(isRfqItemListPresetId('unknown')).toBe(false)
  })

  it('maps has_deleted_quote to quickFilter without time windows', () => {
    const api = resolveRfqItemPresetApiParams('has_deleted_quote')
    expect(api).toEqual({ quickFilter: 'has_deleted_quote' })
  })

  it('buildRfqItemListRouteQuery writes quickFilter for has_deleted_quote and keeps keywords', () => {
    const q = buildRfqItemListRouteQuery({
      preset: 'has_deleted_quote',
      keywords: { rfqCode: 'RFQ1' }
    })
    expect(q.preset).toBe('has_deleted_quote')
    expect(q.quickFilter).toBe('has_deleted_quote')
    expect(q.rfqCode).toBe('RFQ1')
    expect(q.itemCreateStart).toBeUndefined()
    expect(q.startDate).toBeUndefined()
  })
})
