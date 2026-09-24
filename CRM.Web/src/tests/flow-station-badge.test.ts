import { describe, expect, it } from 'vitest'
import {
  foldFlowCards,
  foldFlowStationBadge,
  paymentOutcome,
  poItemOutcome,
  purchaseInvoiceOutcome,
  qcOutcome,
  salesInvoiceOutcome,
  sellLineOutcome,
  stockOutOutcome
} from '@/utils/flowStationBadge'

describe('foldFlowStationBadge', () => {
  it('keeps in-progress ahead of failures and cancellations', () => {
    expect(foldFlowStationBadge(['done', 'active', 'reviewFailed'])).toBe('active')
    expect(foldFlowStationBadge(['cancelled', 'invoiceFailed'])).toBe('invoiceFailed')
  })

  it('lets failure beat done and cancelled when nothing is in progress', () => {
    expect(foldFlowStationBadge(['done', 'reviewFailed'])).toBe('reviewFailed')
    expect(foldFlowStationBadge(['done', 'invoiceFailed', 'cancelled'])).toBe('invoiceFailed')
    expect(foldFlowStationBadge(['done', 'failed'])).toBe('failed')
    expect(foldFlowStationBadge(['done', 'redFlushed'])).toBe('redFlushed')
    expect(foldFlowStationBadge(['reviewFailed', 'invoiceFailed', 'failed', 'redFlushed'])).toBe('reviewFailed')
  })

  it('marks a mix of done and cancelled as done', () => {
    expect(foldFlowStationBadge(['done', 'cancelled'])).toBe('done')
    expect(foldFlowStationBadge(['cancelled', 'cancelled'])).toBe('cancelled')
    expect(foldFlowStationBadge([])).toBe('empty')
  })
})

describe('foldFlowCards', () => {
  it('ignores deleted documents and treats an all-deleted station as cancelled', () => {
    expect(
      foldFlowCards([
        { outcome: 'done', isDeleted: true },
        { outcome: 'active', isDeleted: false }
      ])
    ).toBe('active')
    expect(foldFlowCards([{ outcome: 'done', isDeleted: true }])).toBe('cancelled')
    expect(foldFlowCards([])).toBe('empty')
  })
})

describe('document outcomes', () => {
  it('maps stock-out, sales line, purchase, invoice and qc', () => {
    expect(stockOutOutcome(4)).toBe('done')
    expect(stockOutOutcome(2)).toBe('active')
    expect(stockOutOutcome(3)).toBe('cancelled')
    expect(sellLineOutcome(1, 2, 2)).toBe('cancelled')
    expect(sellLineOutcome(0, 2, 2)).toBe('done')
    expect(sellLineOutcome(0, 2, 1)).toBe('active')
    expect(poItemOutcome(100)).toBe('done')
    expect(poItemOutcome(-1)).toBe('reviewFailed')
    expect(poItemOutcome(-2)).toBe('cancelled')
    expect(paymentOutcome(-1)).toBe('reviewFailed')
    expect(salesInvoiceOutcome(101)).toBe('invoiceFailed')
    expect(salesInvoiceOutcome(-1)).toBe('cancelled')
    expect(purchaseInvoiceOutcome(1, 1)).toBe('redFlushed')
    expect(purchaseInvoiceOutcome(1, 0)).toBe('done')
    expect(qcOutcome(-1)).toBe('failed')
    expect(qcOutcome(10)).toBe('active')
  })
})
