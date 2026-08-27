import { describe, expect, it } from 'vitest'
import {
  INVOICE_REPORT_V2_TENANT_ID,
  usesInvoiceReportV2
} from '@/components/stockOut/invoiceReport/resolveInvoiceReportSkin'

describe('usesInvoiceReportV2', () => {
  it('applies V2 only to the semicore tenant', () => {
    expect(usesInvoiceReportV2('semicore', 'V2')).toBe(true)
    expect(usesInvoiceReportV2('SEMICORE', 'V2')).toBe(true)
    expect(INVOICE_REPORT_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 layout when the global param is V2', () => {
    expect(usesInvoiceReportV2('idesemi', 'V2')).toBe(false)
    expect(usesInvoiceReportV2('ecoinf', 'V2')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesInvoiceReportV2('semicore', 'V1')).toBe(false)
    expect(usesInvoiceReportV2('idesemi', 'V1')).toBe(false)
  })
})
