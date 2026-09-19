import { describe, expect, it } from 'vitest'
import zhCN from '@/locales/zh-CN'
import enUS from '@/locales/en-US'
import {
  formatMonthSpan,
  openWindowMonthSpan,
  resolveIncentiveTermPeriod,
  resolveIncentiveYearPeriod
} from '@/utils/incentiveTargetPeriod'

describe('incentiveTargetPeriod', () => {
  it('uses API monthSpan and year periodKey', () => {
    const mine = {
      term: { monthSpan: '8–9月', from: '2026-08-01', to: '2026-09-30' },
      year: { periodKey: '2026' }
    }
    expect(resolveIncentiveTermPeriod(mine)).toBe('8–9月')
    expect(resolveIncentiveYearPeriod(mine)).toBe('2026')
  })

  it('falls back to from/to, then PascalCase', () => {
    expect(
      resolveIncentiveTermPeriod({
        term: { from: '2026-08-01', to: '2026-09-30' }
      })
    ).toBe('8–9月')
    expect(
      resolveIncentiveTermPeriod({
        Term: { MonthSpan: '12–1月' }
      })
    ).toBe('12–1月')
    expect(
      resolveIncentiveYearPeriod({
        Year: { PeriodKey: '2026' }
      })
    ).toBe('2026')
  })

  it('ports GetOpenWindow for labels when API fields are missing', () => {
    expect(openWindowMonthSpan(new Date('2026-02-01T04:00:00Z'))).toBe('12–1月')
    expect(openWindowMonthSpan(new Date('2026-02-02T04:00:00Z'))).toBe('2–3月')
    expect(openWindowMonthSpan(new Date('2026-09-18T04:00:00Z'))).toBe('8–9月')
    expect(openWindowMonthSpan(new Date('2026-12-02T04:00:00Z'))).toBe('12–1月')
    expect(resolveIncentiveTermPeriod(null, new Date('2026-09-18T04:00:00Z'))).toBe('8–9月')
    expect(resolveIncentiveYearPeriod(null, new Date('2026-09-18T04:00:00Z'))).toBe('2026')
  })

  it('formatMonthSpan reads yyyy-MM-dd', () => {
    expect(formatMonthSpan('2026-08-01', '2026-09-30')).toBe('8–9月')
    expect(formatMonthSpan('2025-12-01', '2026-01-31')).toBe('12–1月')
  })

  it('locale strings keep a period placeholder', () => {
    expect(zhCN.dashboard.incentive.termField).toBe('本期计划提成（{period}）')
    expect(zhCN.dashboard.incentive.yearField).toBe('年度计划提成（{period}）')
    expect(enUS.dashboard.incentive.termField).toBe('Term target ({period})')
    expect(enUS.dashboard.incentive.yearField).toBe('Year target ({period})')
  })
})
