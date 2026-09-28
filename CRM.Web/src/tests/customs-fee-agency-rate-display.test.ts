import { describe, expect, it } from 'vitest'
import { CurrencyCode } from '@/constants/currency'
import {
  resolveDisplayedBrokerAgencyRate,
  resolveDisplayedBrokerAgencyRateDetail
} from '@/utils/customsFeeAgencyRateDisplay'
import { buildCustomsFeeLineDemo } from '@/utils/customsFeeFormulaDemo'

describe('resolveDisplayedBrokerAgencyRate', () => {
  it('shows the broker master rate before fees are calculated', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: null,
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: 1.003
      })
    ).toBe(1.003)
  })

  it('treats a blank calculated-at as not calculated', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: '  ',
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: 1.003
      })
    ).toBe(1.003)
  })

  it('shows the declaration snapshot after fees are calculated', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: '2026-09-28T08:17:33.752Z',
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: 1.003
      })
    ).toBe(1.025)
  })

  it('keeps 1.000000 as a valid zero-percent rate', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: '2026-09-28T08:17:33.752Z',
        brokerAgencyRate: 1,
        brokerMasterAgencyRate: 1.003
      })
    ).toBe(1)
  })

  it('falls back to the master rate when a calculated snapshot is invalid', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: '2026-09-28T08:17:33.752Z',
        brokerAgencyRate: 0,
        brokerMasterAgencyRate: 1.003
      })
    ).toBe(1.003)
  })

  it('falls back to the snapshot when fees are not calculated and the master rate is invalid', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: null,
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: Number.NaN
      })
    ).toBe(1.025)
  })

  it('uses 1 when neither rate is a positive finite number', () => {
    expect(
      resolveDisplayedBrokerAgencyRate({
        feesCalculatedAt: null,
        brokerAgencyRate: -1,
        brokerMasterAgencyRate: 0
      })
    ).toBe(1)
  })

  it('labels the live broker rate as the current parameter', () => {
    expect(
      resolveDisplayedBrokerAgencyRateDetail({
        feesCalculatedAt: null,
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: 1.003
      }).source
    ).toBe('current')
  })

  it('labels the saved declaration rate as the historical parameter', () => {
    expect(
      resolveDisplayedBrokerAgencyRateDetail({
        feesCalculatedAt: '2026-09-28T08:17:33.752Z',
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: 1.003
      }).source
    ).toBe('history')
  })

  it('labels a calculated fallback to the broker master as the current parameter', () => {
    expect(
      resolveDisplayedBrokerAgencyRateDetail({
        feesCalculatedAt: '2026-09-28T08:17:33.752Z',
        brokerAgencyRate: 0,
        brokerMasterAgencyRate: 1.003
      }).source
    ).toBe('current')
  })

  it('labels an uncalculated fallback to the saved rate as the historical parameter', () => {
    expect(
      resolveDisplayedBrokerAgencyRateDetail({
        feesCalculatedAt: null,
        brokerAgencyRate: 1.025,
        brokerMasterAgencyRate: Number.NaN
      }).source
    ).toBe('history')
  })

  it('omits a source label when neither rate is usable', () => {
    expect(
      resolveDisplayedBrokerAgencyRateDetail({
        feesCalculatedAt: null,
        brokerAgencyRate: -1,
        brokerMasterAgencyRate: 0
      }).source
    ).toBe('none')
  })
})

describe('CDS00001 agency fee demo uses the saved snapshot rate', () => {
  it('matches the saved agency fee 351.47 when the header shows 1.025', () => {
    const agencyRate = resolveDisplayedBrokerAgencyRate({
      feesCalculatedAt: '2026-09-28T08:17:33.752Z',
      brokerAgencyRate: 1.025,
      brokerMasterAgencyRate: 1.003
    })
    const steps = buildCustomsFeeLineDemo({
      manualCostUsd: false,
      displayedCostUsd: 5.91475,
      originalPurchasePrice: 5.9,
      purchaseCurrency: CurrencyCode.USD,
      purchaseRatio: 1.0025,
      exchangeRate: 6.7853,
      declareQty: 310,
      dutyRate: 0,
      vatRate: 0.13,
      agencyRate,
      otherFee: 0,
      financeFx: { usdToCny: 6.7853, usdToHkd: 7.8, usdToEur: 0.92 },
      table: {
        customsPaymentGoods: 12441.34,
        dutyAmount: 0,
        vatAmount: 1617.37,
        customsAgencyFee: 351.47,
        totalValueTax: 14410.18,
        taxIncludedUnitPrice: 46.484452
      }
    })
    const agency = steps.find((s) => s.id === 'agency')
    expect(agency?.expr?.result).toBe(351.47)
    expect(agency?.mismatch).toBe(false)
    const total = steps.find((s) => s.id === 'total')
    expect(total?.expr?.result).toBe(14410.18)
    expect(total?.mismatch).toBe(false)
  })
})
