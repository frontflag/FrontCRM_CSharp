import { describe, expect, it } from 'vitest'
import {
  isCustomsPackingReport,
  resolvePackingReportConsigneeName
} from '@/utils/packingReportCustomsConsignee'
import { isCustomsBrokerConsigneeReady } from '@/api/customs'
import { StockOutTypeCode } from '@/constants/stockOutType'

describe('packing report customs consignee', () => {
  it('detects customs packing type', () => {
    expect(isCustomsPackingReport(StockOutTypeCode.Customs)).toBe(true)
    expect(isCustomsPackingReport(StockOutTypeCode.Sales)).toBe(false)
  })

  it('uses broker ship-to line and skips 521 mask for customs', () => {
    expect(
      resolvePackingReportConsigneeName({
        stockOutType: StockOutTypeCode.Customs,
        customerName: 'Sales Customer',
        shipToFirstLine: 'SZ Broker Ltd',
        maskSaleSensitive: true
      })
    ).toBe('SZ Broker Ltd')
  })

  it('uses overlay flag even if stock-out type is missing', () => {
    expect(
      resolvePackingReportConsigneeName({
        stockOutType: 0,
        customerName: 'Sales Customer',
        shipToFirstLine: 'SZ Broker Ltd',
        maskSaleSensitive: true,
        customsBrokerConsignee: true
      })
    ).toBe('SZ Broker Ltd')
  })

  it('masks sales customer when 521 applies', () => {
    expect(
      resolvePackingReportConsigneeName({
        stockOutType: StockOutTypeCode.Sales,
        customerName: 'Sales Customer',
        shipToFirstLine: 'SZ Broker Ltd',
        maskSaleSensitive: true
      })
    ).toBe('—')
  })

  it('requires contact tel address for print-ready broker', () => {
    expect(
      isCustomsBrokerConsigneeReady({
        cname: '行',
        contactName: '张三',
        tel: '123',
        address: '地址'
      })
    ).toBe(true)
    expect(
      isCustomsBrokerConsigneeReady({
        cname: '行',
        contactName: '',
        tel: '123',
        address: '地址'
      })
    ).toBe(false)
  })
})
