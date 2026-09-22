import { describe, expect, it } from 'vitest'
import {
  countNewVendorContacts,
  failureRowsForRequestError,
  failureRowsFromBatch,
  formatVendorImportFailureText,
  isVendorImportCanceled,
  vendorImportRequestError
} from '@/utils/vendorImportResult'

describe('vendorImportResult', () => {
  it('formats failure lines as Excel row, name, and reason', () => {
    const text = formatVendorImportFailureText([
      { excelRow: 28, vendorName: '另一家公司', error: '请求超时，请重新导入；已成功的供应商会自动跳过' },
      { excelRow: 15, vendorName: ' 某某科技有限公司 ', error: '公司邮箱后缀已被其他供应商使用' }
    ])
    expect(text).toBe(
      [
        'Excel表第15行\t某某科技有限公司\t公司邮箱后缀已被其他供应商使用',
        'Excel表第28行\t另一家公司\t请求超时，请重新导入；已成功的供应商会自动跳过'
      ].join('\n')
    )
  })

  it('maps timeout text and keeps other errors', () => {
    expect(vendorImportRequestError('timeout of 120000ms exceeded')).toBe(
      '请求超时，请重新导入；已成功的供应商会自动跳过'
    )
    expect(vendorImportRequestError('统一社会信用代码超过 50 个字符')).toBe(
      '统一社会信用代码超过 50 个字符'
    )
  })

  it('treats abort as canceled and timeout as a real failure', () => {
    expect(isVendorImportCanceled(new Error('canceled'))).toBe(true)
    expect(isVendorImportCanceled({ code: 'ERR_CANCELED', message: 'canceled' })).toBe(true)
    expect(isVendorImportCanceled(new Error('timeout of 120000ms exceeded'))).toBe(false)
  })

  it('lists only failed item rows and fills the name from the parsed sheet', () => {
    const names = new Map<number, string>([[8, '本地名称']])
    const rows = failureRowsFromBatch(
      [
        { excelRow: 2, vendorName: '已成功', success: true },
        { excelRow: 3, vendorName: '已存在', skipped: true },
        { excelRow: 8, success: false, error: '供应商名称超过 64 个字符' }
      ],
      names
    )
    expect(rows).toEqual([
      { excelRow: 8, vendorName: '本地名称', error: '供应商名称超过 64 个字符' }
    ])
  })

  it('uses the customer label in timeout text', () => {
    expect(vendorImportRequestError('timeout of 120000ms exceeded', '客户')).toBe(
      '请求超时，请重新导入；已成功的客户会自动跳过'
    )
    const rows = failureRowsFromBatch(
      [{ excelRow: 4, customerName: '示例科技', success: false, error: '备注超过 500 个字符' }],
      new Map()
    )
    expect(rows).toEqual([{ excelRow: 4, vendorName: '示例科技', error: '备注超过 500 个字符' }])
  })

  it('turns a failed request into one failure line per excel row', () => {
    const rows = failureRowsForRequestError(
      [
        { excelRow: 2, vendorName: '甲公司' },
        { excelRow: 3, vendorName: '乙公司' }
      ],
      'timeout of 120000ms exceeded'
    )
    expect(rows.map((row) => row.error)).toEqual([
      '请求超时，请重新导入；已成功的供应商会自动跳过',
      '请求超时，请重新导入；已成功的供应商会自动跳过'
    ])
    expect(formatVendorImportFailureText(rows)).toContain('Excel表第2行\t甲公司')
  })

  it('counts contacts only for vendors that will be inserted', () => {
    const n = countNewVendorContacts(
      [
        { excelRow: 2, contacts: [{}, {}] },
        { excelRow: 3, contacts: [{}] },
        { excelRow: 4, contacts: [] }
      ],
      [3]
    )
    expect(n).toBe(2)
  })
})
