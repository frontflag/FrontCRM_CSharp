import { describe, expect, it } from 'vitest'
import { displayOpsCheckRelatedDocCode } from '@/utils/opsCheckRelatedDocCode'

describe('displayOpsCheckRelatedDocCode', () => {
  it('业务单号原样返回', () => {
    expect(displayOpsCheckRelatedDocCode('STI0000E', 'stockIn')).toBe('STI0000E')
  })

  it('Debug 占位不显示为单号', () => {
    expect(displayOpsCheckRelatedDocCode('Debug', 'debug')).toBe('')
    expect(displayOpsCheckRelatedDocCode('Debug')).toBe('')
    expect(displayOpsCheckRelatedDocCode('debug', 'DEBUG')).toBe('')
  })

  it('空值返回空串', () => {
    expect(displayOpsCheckRelatedDocCode(null, 'stockIn')).toBe('')
    expect(displayOpsCheckRelatedDocCode('  ')).toBe('')
  })
})
