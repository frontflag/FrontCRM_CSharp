import { describe, expect, it } from 'vitest'
import {
  bizBrandCountryToSelect,
  filterBizBrandCountryOptions,
  resolveBizBrandCountryCode,
  shouldPreserveBizBrandCountryCode
} from '@/constants/bizBrandCountry'

describe('resolveBizBrandCountryCode', () => {
  it('中文名 → 国家代码', () => {
    expect(resolveBizBrandCountryCode('美国')).toBe('US')
    expect(resolveBizBrandCountryCode('中国')).toBe('CN')
    expect(resolveBizBrandCountryCode('中国台湾')).toBe('TAIWAN,CHINA')
  })

  it('英文名 / ISO 代码', () => {
    expect(resolveBizBrandCountryCode('United States')).toBe('US')
    expect(resolveBizBrandCountryCode('US')).toBe('US')
    expect(resolveBizBrandCountryCode('usa')).toBe('US')
    expect(resolveBizBrandCountryCode('TW')).toBe('TAIWAN,CHINA')
    expect(resolveBizBrandCountryCode('TAIWAN,CHINA')).toBe('TAIWAN,CHINA')
  })

  it('别名', () => {
    expect(resolveBizBrandCountryCode('台湾')).toBe('TAIWAN,CHINA')
  })

  it('未知国家返回 null', () => {
    expect(resolveBizBrandCountryCode('未知国')).toBeNull()
  })
})

describe('bizBrandCountryToSelect', () => {
  it('预设国家回填下拉', () => {
    expect(bizBrandCountryToSelect('美国')).toEqual({ select: '美国', other: '' })
  })

  it('非预设国家走其他', () => {
    expect(bizBrandCountryToSelect('未知小国')).toEqual({
      select: '__OTHER__',
      other: '未知小国'
    })
  })
})

describe('filterBizBrandCountryOptions', () => {
  it('按中文过滤', () => {
    const hits = filterBizBrandCountryOptions('美')
    expect(hits.some((h) => h.code === 'US')).toBe(true)
    expect(hits.length).toBeLessThan(20)
  })

  it('按 ISO 代码过滤', () => {
    const hits = filterBizBrandCountryOptions('US')
    expect(hits.some((h) => h.code === 'US')).toBe(true)
  })
})

describe('shouldPreserveBizBrandCountryCode', () => {
  it('与自动解析一致时不保留手工标记', () => {
    expect(shouldPreserveBizBrandCountryCode('美国', 'US')).toBe(false)
    expect(shouldPreserveBizBrandCountryCode('中国台湾', 'TAIWAN,CHINA')).toBe(false)
  })

  it('与自动解析不一致时保留', () => {
    expect(shouldPreserveBizBrandCountryCode('中国台湾', 'TW')).toBe(true)
  })
})
