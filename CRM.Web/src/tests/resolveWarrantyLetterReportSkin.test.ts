import { describe, expect, it } from 'vitest'
import {
  WARRANTY_LETTER_REPORT_V2_TENANT_ID,
  usesWarrantyLetterReportV2
} from '@/components/warrantyLetterReport/resolveWarrantyLetterReportSkin'

describe('usesWarrantyLetterReportV2', () => {
  it('applies V2 only to the semicore tenant', () => {
    expect(usesWarrantyLetterReportV2('semicore', 'V2')).toBe(true)
    expect(usesWarrantyLetterReportV2('SEMICORE', 'V2')).toBe(true)
    expect(WARRANTY_LETTER_REPORT_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 layout when the global param is V2', () => {
    expect(usesWarrantyLetterReportV2('idesemi', 'V2')).toBe(false)
    expect(usesWarrantyLetterReportV2('ecoinf', 'V2')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesWarrantyLetterReportV2('semicore', 'V1')).toBe(false)
    expect(usesWarrantyLetterReportV2('idesemi', 'V1')).toBe(false)
  })
})
