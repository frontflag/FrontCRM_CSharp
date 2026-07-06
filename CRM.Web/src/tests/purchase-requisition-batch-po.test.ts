import { describe, it, expect, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { usePurchaseRequisitionPoBasketStore } from '@/stores/purchaseRequisitionPoBasket'
import {
  PR_PO_BATCH_MIN,
  PR_PO_BATCH_MAX,
  buildPoLineItemFromPr,
  isPrBasketEligibleStatus,
  messageKeyForPrBatchValidateError,
  normalizePrListRowToBasketItem,
  resolveLatestDeliveryDate,
  resolvePurchaserFromPr,
  validatePrBatchForPoGeneration
} from '@/utils/purchaseRequisitionBatchPo'

const basePr = (overrides: Record<string, unknown> = {}) => ({
  id: 'pr-1',
  billCode: 'PR001',
  status: 0,
  quoteVendorId: '11111111-1111-1111-1111-111111111111',
  prefillPurchaseOrderType: 1,
  quoteCurrency: 1,
  pn: 'PN-A',
  brand: 'TI',
  qty: 10,
  quoteCost: 1.5,
  deliveryDate: '2026-07-01',
  ...overrides
})

describe('purchaseRequisitionBatchPo - 校验与预填', () => {
  it('batchMinCount：少于 2 条失败', () => {
    expect(validatePrBatchForPoGeneration([basePr()])).toBe('batchMinCount')
    expect(PR_PO_BATCH_MIN).toBe(2)
  })

  it('batchMaxCount：超过上限失败', () => {
    const prs = Array.from({ length: PR_PO_BATCH_MAX + 1 }, (_, i) =>
      basePr({ id: `pr-${i}`, billCode: `PR${i}` })
    )
    expect(validatePrBatchForPoGeneration(prs)).toBe('batchMaxCount')
  })

  it('statusNotAllowed：状态 2/3 不可批量', () => {
    expect(isPrBasketEligibleStatus(0)).toBe(true)
    expect(isPrBasketEligibleStatus(1)).toBe(true)
    expect(isPrBasketEligibleStatus(2)).toBe(false)
    expect(validatePrBatchForPoGeneration([basePr(), basePr({ id: 'pr-2', status: 2 })])).toBe(
      'statusNotAllowed'
    )
  })

  it('vendorMissing / vendorMismatch', () => {
    expect(
      validatePrBatchForPoGeneration([basePr(), basePr({ id: 'pr-2', quoteVendorId: '' })])
    ).toBe('vendorMissing')
    expect(
      validatePrBatchForPoGeneration([
        basePr(),
        basePr({ id: 'pr-2', quoteVendorId: '22222222-2222-2222-2222-222222222222' })
      ])
    ).toBe('vendorMismatch')
  })

  it('poTypeMismatch / currencyMismatch', () => {
    expect(
      validatePrBatchForPoGeneration([basePr(), basePr({ id: 'pr-2', prefillPurchaseOrderType: 2 })])
    ).toBe('poTypeMismatch')
    expect(validatePrBatchForPoGeneration([basePr(), basePr({ id: 'pr-2', quoteCurrency: 2 })])).toBe(
      'currencyMismatch'
    )
  })

  it('全部一致时校验通过', () => {
    expect(
      validatePrBatchForPoGeneration([
        basePr(),
        basePr({ id: 'pr-2', billCode: 'PR002', pn: 'PN-B', deliveryDate: '2026-07-10' })
      ])
    ).toBeNull()
  })

  it('resolveLatestDeliveryDate 取最晚交期', () => {
    expect(
      resolveLatestDeliveryDate([
        basePr({ deliveryDate: '2026-07-01' }),
        basePr({ id: 'pr-2', deliveryDate: '2026-07-15' }),
        basePr({ id: 'pr-3', expectedPurchaseTime: '2026-07-08T00:00:00Z' })
      ])
    ).toBe('2026-07-15')
  })

  it('resolvePurchaserFromPr 优先级：报价 → RFQ → 申请', () => {
    expect(resolvePurchaserFromPr({ prefillPurchaseUserId: 'u1', prefillPurchaseUserName: '报价员' })).toEqual({
      id: 'u1',
      name: '报价员'
    })
    expect(
      resolvePurchaserFromPr({
        prefillRfqPurchaserUserId: 'u2',
        prefillRfqPurchaserUserName: '询价员',
        purchaseUserId: 'u3',
        purchaseUserName: '申请员'
      })
    ).toEqual({ id: 'u2', name: '询价员' })
    expect(resolvePurchaserFromPr({ purchaseUserId: 'u3', purchaseUserName: '申请员' })).toEqual({
      id: 'u3',
      name: '申请员'
    })
  })

  it('buildPoLineItemFromPr 生成明细草稿', () => {
    const line = buildPoLineItemFromPr(basePr(), {
      manualVendorId: 'manual-vendor',
      coercePd: (v) => v || 'PD-NA',
      headerDeliveryDate: '2026-07-20'
    })
    expect(line.purchaseRequisitionId).toBe('pr-1')
    expect(line.pn).toBe('PN-A')
    expect(line.qty).toBe(10)
    expect(line.cost).toBe(1.5)
    expect(line.vendorId).toBe('11111111-1111-1111-1111-111111111111')
    expect(line.deliveryDate).toBe('2026-07-01')
  })

  it('messageKeyForPrBatchValidateError 映射 i18n key', () => {
    expect(messageKeyForPrBatchValidateError('vendorMismatch')).toBe(
      'purchaseRequisitionList.basket.validateVendorMismatch'
    )
  })
})

describe('purchaseRequisitionPoBasket store - 跨页篮子', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('upsert / remove / clear', () => {
    const store = usePurchaseRequisitionPoBasketStore()
    const row = { id: 'a1', billCode: 'PR-A', status: 0, qty: 1 }
    expect(store.upsertFromListRow(row)).not.toBeNull()
    expect(store.count).toBe(1)
    expect(store.has('a1')).toBe(true)
    store.remove('a1')
    expect(store.count).toBe(0)
    store.upsertFromListRow(row)
    store.clear()
    expect(store.count).toBe(0)
  })

  it('状态 2 不可加入篮子', () => {
    const store = usePurchaseRequisitionPoBasketStore()
    expect(store.upsertFromListRow({ id: 'x', billCode: 'PR-X', status: 2, qty: 1 })).toBeNull()
    expect(store.count).toBe(0)
  })

  it('mergePageSelection 仅同步当前页勾选', () => {
    const store = usePurchaseRequisitionPoBasketStore()
    const pageRows = [
      { id: 'p1', billCode: 'PR1', status: 0, qty: 1 },
      { id: 'p2', billCode: 'PR2', status: 0, qty: 2 }
    ]
    store.upsertFromListRow({ id: 'old', billCode: 'OLD', status: 0, qty: 1 })
    store.mergePageSelection(pageRows, [pageRows[1]!])
    expect(store.count).toBe(2)
    expect(store.has('p2')).toBe(true)
    expect(store.has('old')).toBe(true)
    expect(store.has('p1')).toBe(false)

    store.mergePageSelection(pageRows, [])
    expect(store.has('p2')).toBe(false)
    expect(store.has('old')).toBe(true)
  })

  it('normalizePrListRowToBasketItem 字段映射', () => {
    const item = normalizePrListRowToBasketItem({
      Id: 'id-1',
      BillCode: 'BC1',
      PN: 'PN1',
      Brand: 'ST',
      Qty: 3,
      Status: 1,
      QuoteVendorId: 'vendor-1'
    })
    expect(item).toEqual({
      id: 'id-1',
      billCode: 'BC1',
      pn: 'PN1',
      brand: 'ST',
      qty: 3,
      status: 1,
      sellOrderCode: undefined,
      quoteVendorId: 'vendor-1'
    })
  })
})
