import { describe, expect, it } from 'vitest'
import {
  SALES_ORDER_WARRANTY_REPORT_V2_TENANT_ID,
  usesSalesOrderWarrantyReportV2
} from '@/components/SalesOrder/salesOrderWarrantyReport/resolveSalesOrderWarrantyReportSkin'

describe('usesSalesOrderWarrantyReportV2', () => {
  it('applies V2 only to the semicore tenant', () => {
    expect(usesSalesOrderWarrantyReportV2('semicore', 'V2')).toBe(true)
    expect(usesSalesOrderWarrantyReportV2('SEMICORE', 'V2')).toBe(true)
    expect(SALES_ORDER_WARRANTY_REPORT_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 layout when the global param is V2', () => {
    expect(usesSalesOrderWarrantyReportV2('idesemi', 'V2')).toBe(false)
    expect(usesSalesOrderWarrantyReportV2('ecoinf', 'V2')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesSalesOrderWarrantyReportV2('semicore', 'V1')).toBe(false)
    expect(usesSalesOrderWarrantyReportV2('idesemi', 'V1')).toBe(false)
  })
})
