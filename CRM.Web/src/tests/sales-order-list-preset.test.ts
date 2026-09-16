import { describe, expect, it } from 'vitest'
import {
  isSoListQuickFilterPresetId,
  resolveSoListPresetApiParams
} from '@/utils/salesOrderListPreset'

describe('salesOrderListPreset 快捷检索', () => {
  it('有/未上传文档为 quickFilter', () => {
    expect(isSoListQuickFilterPresetId('has_sales_order_docs')).toBe(true)
    expect(isSoListQuickFilterPresetId('no_sales_order_docs')).toBe(true)
    expect(resolveSoListPresetApiParams('has_sales_order_docs')).toEqual({
      quickFilter: 'has_sales_order_docs'
    })
  })

  it('时间 preset 展开为 startDate/endDate', () => {
    const api = resolveSoListPresetApiParams('order_today')
    expect(api.quickFilter).toBeUndefined()
    expect(api.startDate).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(api.endDate).toBe(api.startDate)
  })
})
