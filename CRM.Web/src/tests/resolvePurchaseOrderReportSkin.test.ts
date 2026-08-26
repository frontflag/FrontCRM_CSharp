import { describe, expect, it } from 'vitest'
import {
  PURCHASE_ORDER_V2_TENANT_ID,
  usesPurchaseOrderReportV2
} from '@/components/purchaseOrder/purchaseOrderReport/resolvePurchaseOrderReportSkin'

describe('usesPurchaseOrderReportV2', () => {
  it('applies V2 only to the semicore tenant', () => {
    expect(usesPurchaseOrderReportV2('semicore', 'V2')).toBe(true)
    expect(usesPurchaseOrderReportV2('SEMICORE', 'V2')).toBe(true)
    expect(PURCHASE_ORDER_V2_TENANT_ID).toBe('semicore')
  })

  it('keeps other tenants on V1 layout when the global param is V2', () => {
    expect(usesPurchaseOrderReportV2('idesemi', 'V2')).toBe(false)
    expect(usesPurchaseOrderReportV2('ecoinf', 'V2')).toBe(false)
  })

  it('never applies V2 when the global param is V1', () => {
    expect(usesPurchaseOrderReportV2('semicore', 'V1')).toBe(false)
    expect(usesPurchaseOrderReportV2('idesemi', 'V1')).toBe(false)
  })
})
