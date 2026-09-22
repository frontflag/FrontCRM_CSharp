export const UPLOAD_DOC_CATEGORY = {
  ShipPhoto: 'SHIP_PHOTO',
  Pod: 'POD',
  Contract: 'CONTRACT',
  Other: 'OTHER'
} as const

export type UploadDocCategory = (typeof UPLOAD_DOC_CATEGORY)[keyof typeof UPLOAD_DOC_CATEGORY]

/** 出库单类型下拉顺序。 */
export const UPLOAD_DOC_CATEGORY_ORDER: UploadDocCategory[] = [
  UPLOAD_DOC_CATEGORY.ShipPhoto,
  UPLOAD_DOC_CATEGORY.Pod,
  UPLOAD_DOC_CATEGORY.Other
]

/** 报关单类型：合同、其他。 */
export const CUSTOMS_DECLARATION_DOC_CATEGORIES: UploadDocCategory[] = [
  UPLOAD_DOC_CATEGORY.Contract,
  UPLOAD_DOC_CATEGORY.Other
]

export const CUSTOMS_DECLARATION_DOC_BIZ = 'CUSTOMS_DECLARATION'

export function normalizeUploadDocCategory(value?: string | null): UploadDocCategory {
  const v = String(value ?? '').trim().toUpperCase()
  if (v === UPLOAD_DOC_CATEGORY.ShipPhoto) return UPLOAD_DOC_CATEGORY.ShipPhoto
  if (v === UPLOAD_DOC_CATEGORY.Pod) return UPLOAD_DOC_CATEGORY.Pod
  if (v === UPLOAD_DOC_CATEGORY.Contract) return UPLOAD_DOC_CATEGORY.Contract
  return UPLOAD_DOC_CATEGORY.Other
}

export function uploadDocCategoryI18nKey(code: UploadDocCategory): string {
  if (code === UPLOAD_DOC_CATEGORY.ShipPhoto) return 'documentUpload.category.shipPhoto'
  if (code === UPLOAD_DOC_CATEGORY.Pod) return 'documentUpload.category.pod'
  if (code === UPLOAD_DOC_CATEGORY.Contract) return 'documentUpload.category.contract'
  return 'documentUpload.category.other'
}
