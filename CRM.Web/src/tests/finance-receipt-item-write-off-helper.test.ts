import { describe, expect, it } from 'vitest'

// Mirror CRM.Core/Utilities/FinanceReceiptItemWriteOffHelper.cs for regression guard.
function effectiveConvertAmount(item: {
  receiptConvertAmount?: number
  receiptAmount?: number
}): number {
  const convert = Number(item.receiptConvertAmount) || 0
  if (convert > 0) return convert
  const amount = Number(item.receiptAmount) || 0
  return amount > 0 ? amount : 0
}

function getRemaining(item: {
  receiptConvertAmount?: number
  receiptAmount?: number
  verifiedAmount?: number
  advancePoolAmount?: number
}): number {
  return (
    effectiveConvertAmount(item)
    - (Number(item.verifiedAmount) || 0)
    - (Number(item.advancePoolAmount) || 0)
  )
}

describe('FinanceReceiptItemWriteOffHelper parity', () => {
  it('ReceiptConvertAmount 为 0 时回退 ReceiptAmount', () => {
    expect(getRemaining({
      receiptConvertAmount: 0,
      receiptAmount: 10800,
      verifiedAmount: 0,
      advancePoolAmount: 0
    })).toBe(10800)
  })

  it('优先使用 ReceiptConvertAmount', () => {
    expect(getRemaining({
      receiptConvertAmount: 5000,
      receiptAmount: 10800,
      verifiedAmount: 1000,
      advancePoolAmount: 0
    })).toBe(4000)
  })
})
