import { describe, expect, it } from 'vitest'
import { resolveStockInTypeLabelKey, stockInTypeLabel } from '@/constants/stockInType'
import { resolveStockOutTypeLabelKey } from '@/constants/stockOutType'

describe('resolveStockInTypeLabelKey', () => {
  it('仅 1 与 10 为采购入库', () => {
    expect(resolveStockInTypeLabelKey(1)).toBe('purchase')
    expect(resolveStockInTypeLabelKey(10)).toBe('purchase')
    expect(stockInTypeLabel(1)).toBe('采购入库')
    expect(stockInTypeLabel(10)).toBe('采购入库')
  })

  it('3 为移库', () => {
    expect(resolveStockInTypeLabelKey(3)).toBe('transfer')
    expect(stockInTypeLabel(3)).toBe('移库')
  })

  it('0、空、未识别为未知，禁止兜成采购入库', () => {
    expect(resolveStockInTypeLabelKey(0)).toBe('unknown')
    expect(resolveStockInTypeLabelKey(null)).toBe('unknown')
    expect(resolveStockInTypeLabelKey(undefined)).toBe('unknown')
    expect(resolveStockInTypeLabelKey('')).toBe('unknown')
    expect(resolveStockInTypeLabelKey('  ')).toBe('unknown')
    expect(resolveStockInTypeLabelKey(99)).toBe('unknown')
    expect(resolveStockInTypeLabelKey(Number.NaN)).toBe('unknown')
    expect(stockInTypeLabel(0)).toBe('未知')
    expect(stockInTypeLabel(99)).toBe('未知')
  })

  it('现行与旧值报关/退货/报废仍按原义', () => {
    expect(resolveStockInTypeLabelKey(20)).toBe('customs')
    expect(resolveStockInTypeLabelKey(2)).toBe('return')
    expect(resolveStockInTypeLabelKey(30)).toBe('return')
    expect(resolveStockInTypeLabelKey(4)).toBe('scrap')
    expect(resolveStockInTypeLabelKey(40)).toBe('scrap')
  })
})

describe('resolveStockOutTypeLabelKey', () => {
  it('仅 1 与 10 为销售出库', () => {
    expect(resolveStockOutTypeLabelKey(1)).toBe('sales')
    expect(resolveStockOutTypeLabelKey(10)).toBe('sales')
  })

  it('3 为移库；0/空/未识别为未知', () => {
    expect(resolveStockOutTypeLabelKey(3)).toBe('transfer')
    expect(resolveStockOutTypeLabelKey(0)).toBe('unknown')
    expect(resolveStockOutTypeLabelKey(null)).toBe('unknown')
    expect(resolveStockOutTypeLabelKey(99)).toBe('unknown')
  })
})
