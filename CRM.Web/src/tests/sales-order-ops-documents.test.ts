import { describe, expect, it } from 'vitest'
import { isInlinePreviewableUpload, previewMimeForUpload } from '@/utils/salesOrderOpsDocuments'

describe('salesOrderOpsDocuments', () => {
  it('图片与 PDF 可在线预览', () => {
    expect(isInlinePreviewableUpload({ mimeType: 'image/png' })).toBe(true)
    expect(isInlinePreviewableUpload({ mimeType: 'application/pdf' })).toBe(true)
    expect(isInlinePreviewableUpload({ fileExtension: '.PDF', originalFileName: 'a.pdf' })).toBe(true)
    expect(isInlinePreviewableUpload({ originalFileName: 'scan.JPG' })).toBe(true)
  })

  it('Word / Excel 不可在线预览', () => {
    expect(isInlinePreviewableUpload({ mimeType: 'application/vnd.ms-excel', originalFileName: 'a.xls' })).toBe(false)
    expect(isInlinePreviewableUpload({ fileExtension: '.docx', originalFileName: '合同.docx' })).toBe(false)
  })

  it('缺 mime 时按扩展名补预览类型', () => {
    expect(previewMimeForUpload({ originalFileName: 'a.pdf' })).toBe('application/pdf')
    expect(previewMimeForUpload({ fileExtension: '.png' })).toBe('image/png')
    expect(previewMimeForUpload({ mimeType: 'image/jpeg' })).toBe('image/jpeg')
  })
})
