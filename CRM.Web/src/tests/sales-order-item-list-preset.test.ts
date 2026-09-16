import { describe, expect, it } from 'vitest'
import {
  isSoItemQuickFilterPresetId,
  resolveSoItemPresetApiParams
} from '@/utils/salesOrderItemListPreset'

describe('salesOrderItemListPreset 文档快捷检索', () => {
  it('有/未上传文档为 quickFilter preset', () => {
    expect(isSoItemQuickFilterPresetId('has_sales_order_docs')).toBe(true)
    expect(isSoItemQuickFilterPresetId('no_sales_order_docs')).toBe(true)
    expect(resolveSoItemPresetApiParams('has_sales_order_docs')).toEqual({
      quickFilter: 'has_sales_order_docs'
    })
    expect(resolveSoItemPresetApiParams('no_sales_order_docs')).toEqual({
      quickFilter: 'no_sales_order_docs'
    })
  })
})
