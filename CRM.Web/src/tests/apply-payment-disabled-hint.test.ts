import { describe, expect, it } from 'vitest'
import {
  applyPaymentButtonDisabled,
  buildApplyPaymentDisabledHintContent,
  listLinkedFinancePaymentDocs
} from '@/utils/applyPaymentDisabledHint'

const t = (key: string) => key

function confirmedRow(extra: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    itemStatus: 30,
    orderStatus: 30,
    financePaymentStatus: 0,
    lineTotal: 65600,
    paymentRequestedAmount: 0,
    canApplyPayment: true,
    ...extra
  }
}

describe('applyPaymentDisabledHint', () => {
  it('returns null when the line can request payment', () => {
    const row = confirmedRow()
    expect(applyPaymentButtonDisabled(row)).toBe(false)
        expect(buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })).toBeNull()
  })

  it('allows another request when finance is partial and remaining requestable > 0', () => {
    const row = confirmedRow({
      financePaymentStatus: 2,
      paymentProgressStatus: 1,
      lineTotal: 21375,
      paymentRequestedAmount: 6412.5,
      canApplyPayment: true
    })
    expect(applyPaymentButtonDisabled(row, { canInitiatePayment: true })).toBe(false)
    expect(buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })).toBeNull()
  })

  it('permission is the first reason when the account cannot initiate payment', () => {
    const row = confirmedRow({ itemStatus: -2, canApplyPayment: false })
    const hint = buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: false })
    expect(hint?.summary).toBe('purchaseOrderItemList.opsPanel.paymentNoPermission')
    expect(hint?.nextStep).toBe('purchaseOrderItemList.opsPanel.paymentNextNoPermission')
  })

  it('cancelled / audit-failed is the reason when the account has permission', () => {
    const row = confirmedRow({ itemStatus: -2, canApplyPayment: false })
    const hint = buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })
    expect(hint?.summary).toBe('purchaseOrderItemList.opsPanel.paymentCancelled')
    expect(hint?.nextStep).toBe('purchaseOrderItemList.opsPanel.paymentNextCancelled')
  })

  it('not confirmed is the reason before finance or remaining', () => {
    const row = confirmedRow({
      itemStatus: 20,
      orderStatus: 20,
      financePaymentStatus: 2,
      canApplyPayment: false
    })
    const hint = buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })
    expect(hint?.summary).toBe('purchaseOrderItemList.opsPanel.paymentNeedConfirmed')
  })

  it('finance fully paid lists payment order codes from aggregates', () => {
    const row = confirmedRow({
      financePaymentStatus: 2,
      paymentProgressStatus: 2,
      canApplyPayment: false
    })
    const opts = {
      canInitiatePayment: true,
      aggregates: {
        payments: [
          { id: 'p2', financePaymentCode: 'PAY0022N', status: 100 },
          { id: 'p1', financePaymentCode: 'PAY0001A', status: 10 },
          { id: 'c1', financePaymentCode: 'PAY0099X', status: -2 }
        ]
      }
    }
    expect(applyPaymentButtonDisabled(row, opts)).toBe(true)
    const hint = buildApplyPaymentDisabledHintContent(row, t, opts)
    expect(hint?.summary).toBe('purchaseOrderItemList.opsPanel.paymentFinanceDone')
    expect(hint?.paymentDocs?.map((d) => d.code)).toEqual(['PAY0001A', 'PAY0022N'])
    expect(hint?.nextStep).toBe('purchaseOrderItemList.opsPanel.paymentNextFinanceDoneWithDocs')
  })

  it('stale fully-paid status with no live payments does not block', () => {
    const row = confirmedRow({
      financePaymentStatus: 2,
      paymentProgressStatus: 2,
      canApplyPayment: true
    })
    const opts = { canInitiatePayment: true, aggregates: { payments: [] as { id: string }[] } }
    expect(applyPaymentButtonDisabled(row, opts)).toBe(false)
    expect(buildApplyPaymentDisabledHintContent(row, t, opts)).toBeNull()
  })

  it('deleted-only payments do not block apply', () => {
    const row = confirmedRow({
      financePaymentStatus: 2,
      paymentProgressStatus: 2,
      canApplyPayment: true
    })
    const opts = {
      canInitiatePayment: true,
      aggregates: {
        payments: [{ id: 'p2', financePaymentCode: 'PAY0022N', status: 100, isDeleted: true }]
      }
    }
    expect(applyPaymentButtonDisabled(row, opts)).toBe(false)
    expect(buildApplyPaymentDisabledHintContent(row, t, opts)).toBeNull()
  })

  it('linked payment docs omit deleted and cancelled rows', () => {
    const docs = listLinkedFinancePaymentDocs({
      payments: [
        { id: 'p2', financePaymentCode: 'PAY0022N', status: 100, isDeleted: true },
        { id: 'p1', financePaymentCode: 'PAY0001A', status: 10 },
        { id: 'c1', financePaymentCode: 'PAY0099X', status: -2 }
      ]
    })
    expect(docs?.map((d) => d.code)).toEqual(['PAY0001A'])
  })

  it('remaining amount is the reason when business status still allows apply', () => {
    const row = confirmedRow({
      paymentRequestedAmount: 65600,
      canApplyPayment: true
    })
    const hint = buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })
    expect(hint?.summary).toBe('purchaseOrderItemList.opsPanel.paymentNoRemaining')
  })

  it('does not mention in-transit payment request as a fallback', () => {
    const row = confirmedRow({
      financePaymentStatus: 2,
      paymentProgressStatus: 2,
      canApplyPayment: false
    })
    const hint = buildApplyPaymentDisabledHintContent(row, t, { canInitiatePayment: true })
    expect(hint?.summary).not.toContain('paymentNotEligible')
    expect(hint?.nextStep).not.toContain('paymentNextNotEligible')
  })
})
