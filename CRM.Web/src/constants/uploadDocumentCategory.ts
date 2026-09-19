export const UPLOAD_DOC_CATEGORY = {
  ShipPhoto: 'SHIP_PHOTO',
  Pod: 'POD',
  Other: 'OTHER'
} as const

export type UploadDocCategory = (typeof UPLOAD_DOC_CATEGORY)[keyof typeof UPLOAD_DOC_CATEGORY]

export const UPLOAD_DOC_CATEGORY_ORDER: UploadDocCategory[] = [
  UPLOAD_DOC_CATEGORY.ShipPhoto,
  UPLOAD_DOC_CATEGORY.Pod,
  UPLOAD_DOC_CATEGORY.Other
]

export function normalizeUploadDocCategory(value?: string | null): UploadDocCategory {
  const v = String(value ?? '').trim().toUpperCase()
  if (v === UPLOAD_DOC_CATEGORY.ShipPhoto) return UPLOAD_DOC_CATEGORY.ShipPhoto
  if (v === UPLOAD_DOC_CATEGORY.Pod) return UPLOAD_DOC_CATEGORY.Pod
  return UPLOAD_DOC_CATEGORY.Other
}

export function uploadDocCategoryI18nKey(code: UploadDocCategory): string {
  if (code === UPLOAD_DOC_CATEGORY.ShipPhoto) return 'documentUpload.category.shipPhoto'
  if (code === UPLOAD_DOC_CATEGORY.Pod) return 'documentUpload.category.pod'
  return 'documentUpload.category.other'
}
