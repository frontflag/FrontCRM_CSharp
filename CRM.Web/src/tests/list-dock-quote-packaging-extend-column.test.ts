import { describe, expect, it } from 'vitest'
import { pickDockQuotePackagingField } from '@/constants/listDockQuotePackagingExtendColumnSpec'

describe('pickDockQuotePackagingField', () => {
  it('从报价明细读取库存、最小包装、起订量', () => {
    const row = {
      items: [{ stockQty: 120, minPackageQty: 10, moq: 50 }]
    }
    expect(pickDockQuotePackagingField(row, 'stock')).toBe('120')
    expect(pickDockQuotePackagingField(row, 'minPackage')).toBe('10')
    expect(pickDockQuotePackagingField(row, 'moq')).toBe('50')
  })

  it('0 原样显示；缺省为空串', () => {
    expect(pickDockQuotePackagingField({ items: [{ stockQty: 0 }] }, 'stock')).toBe('0')
    expect(pickDockQuotePackagingField({ items: [{}] }, 'stock')).toBe('')
    expect(pickDockQuotePackagingField({}, 'moq')).toBe('')
  })

  it('多条明细不同值用顿号拼接；PascalCase 与行级回退', () => {
    expect(
      pickDockQuotePackagingField(
        { items: [{ StockQty: 8 }, { stockQty: 12 }] },
        'stock'
      )
    ).toBe('8、12')
    expect(pickDockQuotePackagingField({ moq: 100 }, 'moq')).toBe('100')
    expect(pickDockQuotePackagingField({ MinPackageQty: 25 }, 'minPackage')).toBe('25')
  })
})
