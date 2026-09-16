import { describe, expect, it } from 'vitest'
import {
  isPoItemQuickFilterPresetId,
  resolvePoItemPresetApiParams
} from '@/utils/purchaseOrderItemListPreset'

describe('purchaseOrderItemListPreset 文档快捷检索', () => {
  it('有/未上传文档为 quickFilter preset', () => {
    expect(isPoItemQuickFilterPresetId('has_purchase_order_docs')).toBe(true)
    expect(isPoItemQuickFilterPresetId('no_purchase_order_docs')).toBe(true)
    expect(resolvePoItemPresetApiParams('has_purchase_order_docs')).toEqual({
      quickFilter: 'has_purchase_order_docs'
    })
    expect(resolvePoItemPresetApiParams('no_purchase_order_docs')).toEqual({
      quickFilter: 'no_purchase_order_docs'
    })
  })
})
