/**
 * 单元测试：customer API 辅助函数
 * 测试 mapCustomerLevelToInt 和 mapCustomerTypeToLabel
 */
import { describe, it, expect } from 'vitest'
import { mapCustomerLevelToInt, mapCustomerTypeToLabel } from '@/api/customer'

describe('mapCustomerLevelToInt - P2 修复验证', () => {
  it('后端枚举字符串: D -> 1', () => {
    expect(mapCustomerLevelToInt('D')).toBe(1)
  })
  it('后端枚举字符串: C -> 2', () => {
    expect(mapCustomerLevelToInt('C')).toBe(2)
  })
  it('后端枚举字符串: B -> 3', () => {
    expect(mapCustomerLevelToInt('B')).toBe(3)
  })
  it('后端枚举字符串: BPO -> 4', () => {
    expect(mapCustomerLevelToInt('BPO')).toBe(4)
  })
  it('后端枚举字符串: VIP -> 5', () => {
    expect(mapCustomerLevelToInt('VIP')).toBe(5)
  })
  it('后端枚举字符串: VPO -> 6', () => {
    expect(mapCustomerLevelToInt('VPO')).toBe(6)
  })
  it('数字类型直接返回: 5 -> 5', () => {
    expect(mapCustomerLevelToInt(5)).toBe(5)
  })
  it('数字 0 返回默认值 3', () => {
    expect(mapCustomerLevelToInt(0)).toBe(3)
  })
  it('undefined 返回默认值 3', () => {
    expect(mapCustomerLevelToInt(undefined)).toBe(3)
  })
  it('未知字符串返回默认值 3', () => {
    expect(mapCustomerLevelToInt('Unknown')).toBe(3)
  })
})

describe('mapCustomerTypeToLabel - P3 修复验证', () => {
  it('1 -> OEM', () => {
    expect(mapCustomerTypeToLabel(1)).toBe('OEM')
  })
  it('2 -> ODM', () => {
    expect(mapCustomerTypeToLabel(2)).toBe('ODM')
  })
  it('3 -> 终端', () => {
    expect(mapCustomerTypeToLabel(3)).toBe('终端')
  })
  it('4 -> IDH', () => {
    expect(mapCustomerTypeToLabel(4)).toBe('IDH')
  })
  it('5 -> 贸易商', () => {
    expect(mapCustomerTypeToLabel(5)).toBe('贸易商')
  })
  it('6 -> 代理商', () => {
    expect(mapCustomerTypeToLabel(6)).toBe('代理商')
  })
  it('7 -> EMS', () => {
    expect(mapCustomerTypeToLabel(7)).toBe('EMS')
  })
  it('11 -> 原厂', () => {
    expect(mapCustomerTypeToLabel(11)).toBe('原厂')
  })
  it('0 -> 未知', () => {
    expect(mapCustomerTypeToLabel(0)).toBe('未知')
  })
  it('undefined -> 未知', () => {
    expect(mapCustomerTypeToLabel(undefined)).toBe('未知')
  })
})
