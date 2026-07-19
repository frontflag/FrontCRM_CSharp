import { describe, expect, it } from 'vitest'
import {
  buildQcListRouteQuery,
  isQcListPresetId,
  pickQcKeywordQuery
} from '@/utils/qcListPreset'

describe('qcListPreset', () => {
  it('recognizes all preset ids', () => {
    expect(isQcListPresetId('qc_today')).toBe(true)
    expect(isQcListPresetId('status_partial')).toBe(true)
    expect(isQcListPresetId('has_qc_images')).toBe(true)
    expect(isQcListPresetId('unknown')).toBe(false)
  })

  it('builds route query with preset and keywords', () => {
    expect(
      buildQcListRouteQuery({
        preset: 'qc_within_7_days',
        keywords: { model: 'ABC', stockInType: '20' }
      })
    ).toEqual({
      preset: 'qc_within_7_days',
      model: 'ABC',
      stockInType: '20'
    })
  })

  it('pickQcKeywordQuery keeps only known keyword keys', () => {
    expect(
      pickQcKeywordQuery({
        preset: 'qc_today',
        model: ' PN-1 ',
        vendorName: 'Acme',
        foo: 'bar'
      })
    ).toEqual({
      model: 'PN-1',
      vendorName: 'Acme'
    })
  })
})
