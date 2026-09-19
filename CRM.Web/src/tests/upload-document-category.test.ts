import { describe, expect, it } from 'vitest'
import {
  UPLOAD_DOC_CATEGORY,
  normalizeUploadDocCategory,
  uploadDocCategoryI18nKey
} from '@/constants/uploadDocumentCategory'

describe('uploadDocumentCategory', () => {
  it('空值与未知码归其他', () => {
    expect(normalizeUploadDocCategory(null)).toBe(UPLOAD_DOC_CATEGORY.Other)
    expect(normalizeUploadDocCategory('')).toBe(UPLOAD_DOC_CATEGORY.Other)
    expect(normalizeUploadDocCategory('foo')).toBe(UPLOAD_DOC_CATEGORY.Other)
  })

  it('识别出货照片与签收单（忽略大小写）', () => {
    expect(normalizeUploadDocCategory('SHIP_PHOTO')).toBe(UPLOAD_DOC_CATEGORY.ShipPhoto)
    expect(normalizeUploadDocCategory('ship_photo')).toBe(UPLOAD_DOC_CATEGORY.ShipPhoto)
    expect(normalizeUploadDocCategory('POD')).toBe(UPLOAD_DOC_CATEGORY.Pod)
  })

  it('i18n key 与分类码对应', () => {
    expect(uploadDocCategoryI18nKey(UPLOAD_DOC_CATEGORY.ShipPhoto)).toBe('documentUpload.category.shipPhoto')
    expect(uploadDocCategoryI18nKey(UPLOAD_DOC_CATEGORY.Pod)).toBe('documentUpload.category.pod')
    expect(uploadDocCategoryI18nKey(UPLOAD_DOC_CATEGORY.Other)).toBe('documentUpload.category.other')
  })
})
