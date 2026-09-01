import { describe, expect, it } from 'vitest'
import {
  CANCELLED_ORDER_HEADER_STATUS,
  LIST_ROW_CANCELLED_CLASS,
  cancelledOrderListRowClass,
  isCancelledOrderHeaderStatus,
  joinRowClassNames
} from '@/utils/listCancelledRow'

describe('listCancelledRow', () => {
  it('仅主单取消(-2)为真，审核失败不是', () => {
    expect(isCancelledOrderHeaderStatus(-2)).toBe(true)
    expect(isCancelledOrderHeaderStatus(CANCELLED_ORDER_HEADER_STATUS)).toBe(true)
    expect(isCancelledOrderHeaderStatus(-1)).toBe(false)
    expect(isCancelledOrderHeaderStatus(30)).toBe(false)
    expect(isCancelledOrderHeaderStatus(undefined)).toBe(false)
  })

  it('主单列表读 status，明细列表读 orderStatus', () => {
    expect(cancelledOrderListRowClass({ status: -2 })).toBe(LIST_ROW_CANCELLED_CLASS)
    expect(cancelledOrderListRowClass({ status: 30 })).toBe('')
    expect(cancelledOrderListRowClass({ orderStatus: -2, itemStatus: 30 }, 'orderStatus')).toBe(
      LIST_ROW_CANCELLED_CLASS
    )
    expect(cancelledOrderListRowClass({ orderStatus: 30, itemStatus: -2 }, 'orderStatus')).toBe('')
  })

  it('joinRowClassNames 去掉空段', () => {
    expect(joinRowClassNames(LIST_ROW_CANCELLED_CLASS, false, 'so-item-row--active', '')).toBe(
      `${LIST_ROW_CANCELLED_CLASS} so-item-row--active`
    )
  })
})
