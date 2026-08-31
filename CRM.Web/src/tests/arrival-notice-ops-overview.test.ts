import { describe, expect, it } from 'vitest'
import { resolveArrivalNoticeCustomsBrokerName } from '@/utils/arrivalNoticeOpsOverview'
import { StockInTypeCode } from '@/constants/stockInType'

describe('resolveArrivalNoticeCustomsBrokerName', () => {
  it('returns broker name for customs stock-in', () => {
    expect(
      resolveArrivalNoticeCustomsBrokerName(
        { customsBrokerName: '  港通报关  ' },
        StockInTypeCode.Customs
      )
    ).toBe('港通报关')
  })

  it('reads PascalCase field', () => {
    expect(
      resolveArrivalNoticeCustomsBrokerName(
        { CustomsBrokerName: 'ABC Broker' },
        StockInTypeCode.Customs
      )
    ).toBe('ABC Broker')
  })

  it('hides name for non-customs types even if field is filled', () => {
    expect(
      resolveArrivalNoticeCustomsBrokerName(
        { customsBrokerName: '港通报关' },
        StockInTypeCode.Purchase
      )
    ).toBe('')
  })

  it('returns empty when customs but name missing', () => {
    expect(resolveArrivalNoticeCustomsBrokerName({}, StockInTypeCode.Customs)).toBe('')
    expect(resolveArrivalNoticeCustomsBrokerName(null, StockInTypeCode.Customs)).toBe('')
  })
})
