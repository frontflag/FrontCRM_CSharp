import { describe, expect, it } from 'vitest'
import {
  pickCustomsBrokerName,
  pickCustomsDeclarationCode,
  pickCustomsDeclarationId,
  pickCustomsExtendFieldValue,
  pickCustomsExtendStatusCode,
  pickCustomsExtendStatusKind
} from '@/constants/listCustomsExtendColumnSpec'

describe('listCustomsExtendColumnSpec', () => {
  it('读取 camelCase / PascalCase 报关字段', () => {
    expect(
      pickCustomsDeclarationId({ customsDeclarationId: ' dec-1 ' })
    ).toBe('dec-1')
    expect(
      pickCustomsDeclarationCode({ CustomsDeclarationCode: ' CDS00001 ' })
    ).toBe('CDS00001')
    expect(pickCustomsBrokerName({ customsBrokerName: '港通报关' })).toBe('港通报关')
  })

  it('按子字段取值：图标用报关单 Id，单号与公司各自独立', () => {
    const row = {
      customsDeclarationId: 'id-1',
      customsDeclarationCode: 'CDS00001',
      customsBrokerName: '港通报关'
    }
    expect(pickCustomsExtendFieldValue(row, 'icon')).toBe('id-1')
    expect(pickCustomsExtendFieldValue(row, 'declarationCode')).toBe('CDS00001')
    expect(pickCustomsExtendFieldValue(row, 'broker')).toBe('港通报关')
  })

  it('出库通知优先读业务报关状态；入库/到货读海关状态', () => {
    expect(pickCustomsExtendStatusKind({ customsStatus: 30 })).toBe('notify')
    expect(pickCustomsExtendStatusCode({ customsStatus: 30 })).toBe(30)
    expect(pickCustomsExtendFieldValue({ customsStatus: 20 }, 'status')).toBe('20')

    expect(pickCustomsExtendStatusKind({ customsClearanceStatus: 100 })).toBe('clearance')
    expect(pickCustomsExtendStatusCode({ CustomsClearanceStatus: 10 })).toBe(10)
    expect(pickCustomsExtendStatusKind({})).toBeNull()
    expect(pickCustomsExtendFieldValue({}, 'status')).toBe('')
  })
})
