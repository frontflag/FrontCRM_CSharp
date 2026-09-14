import { describe, expect, it } from 'vitest'
import {
  logisticsDictLabel,
  qcDeliveryFromArrivalNotice
} from '@/utils/qcDeliveryFromArrivalNotice'

const arrivalOptions = [
  { label: '送货', value: '1' },
  { label: '自提', value: '2' },
  { label: '快递', value: '3' }
]
const expressOptions = [
  { label: '顺丰', value: '4' },
  { label: 'DHL', value: '3' }
]

describe('qcDeliveryFromArrivalNotice', () => {
  it('maps express notice to QC delivery fields with dict labels', () => {
    expect(
      qcDeliveryFromArrivalNotice(
        { shipmentMethod: '3', expressCompany: '4', courierTrackingNo: 'SF1579605041152' },
        arrivalOptions,
        expressOptions
      )
    ).toEqual({
      deliveryMethod: '快递',
      expressMethod: '顺丰',
      expressNo: 'SF1579605041152'
    })
  })

  it('leaves blanks when notice fields are empty', () => {
    expect(qcDeliveryFromArrivalNotice({}, arrivalOptions, expressOptions)).toEqual({
      deliveryMethod: '',
      expressMethod: '',
      expressNo: ''
    })
  })

  it('falls back to raw code when dict miss', () => {
    expect(logisticsDictLabel(arrivalOptions, '99')).toBe('99')
  })
})
