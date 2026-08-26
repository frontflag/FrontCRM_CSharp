import { describe, expect, it } from 'vitest'
import { formatPackingV2Carton } from '@/components/stockOut/packingReport/types'
import {
  PACKING_REPORT_V2_TENANT_ID,
  usesPackingReportV2
} from '@/components/stockOut/packingReport/resolvePackingReportSkin'

describe('usesPackingReportV2', () => {
  it('applies V2 to semicore portrait and landscape', () => {
    expect(usesPackingReportV2('semicore', 'V2', 'portrait')).toBe(true)
    expect(usesPackingReportV2('semicore', 'V2', 'landscape')).toBe(true)
    expect(PACKING_REPORT_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 when the global param is V2', () => {
    expect(usesPackingReportV2('idesemi', 'V2', 'portrait')).toBe(false)
    expect(usesPackingReportV2('ecoinf', 'V2', 'landscape')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesPackingReportV2('semicore', 'V1', 'portrait')).toBe(false)
    expect(usesPackingReportV2('semicore', 'V1', 'landscape')).toBe(false)
  })

  it('treats tenant id case-insensitively', () => {
    expect(usesPackingReportV2('SEMICORE', 'V2', 'landscape')).toBe(true)
  })
})

describe('formatPackingV2Carton', () => {
  it('keeps a non-empty carton as-is', () => {
    expect(formatPackingV2Carton('A-1', 3)).toBe('A-1')
  })

  it('uses a 2-digit row index when carton is empty', () => {
    expect(formatPackingV2Carton('', 1)).toBe('01')
    expect(formatPackingV2Carton('  ', 2)).toBe('02')
    expect(formatPackingV2Carton(null, 10)).toBe('10')
  })
})
