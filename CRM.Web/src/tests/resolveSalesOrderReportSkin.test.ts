import { describe, expect, it } from 'vitest'
import {
  SALES_ORDER_REPORT_V2_TENANT_ID,
  usesSalesOrderReportV2
} from '@/components/SalesOrder/salesOrderReport/resolveSalesOrderReportSkin'

describe('usesSalesOrderReportV2', () => {
  it('applies V2 only to the semicore tenant', () => {
    expect(usesSalesOrderReportV2('semicore', 'V2')).toBe(true)
    expect(usesSalesOrderReportV2('SEMICORE', 'V2')).toBe(true)
    expect(SALES_ORDER_REPORT_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 layout when the global param is V2', () => {
    expect(usesSalesOrderReportV2('idesemi', 'V2')).toBe(false)
    expect(usesSalesOrderReportV2('ecoinf', 'V2')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesSalesOrderReportV2('semicore', 'V1')).toBe(false)
    expect(usesSalesOrderReportV2('idesemi', 'V1')).toBe(false)
  })
})
