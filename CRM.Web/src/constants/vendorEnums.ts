/** 与后端 VendorLevelCode 一致（vendorinfo.Level：1=S 2=A 3=B 4=C） */
export const VENDOR_LEVEL_DEFAULT = 4

export const VENDOR_LEVEL_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: 'S' },
  { value: 2, label: 'A' },
  { value: 3, label: 'B' },
  { value: 4, label: 'C' }
]

/** 与后端 VendorIdentityCode 一致（vendorinfo.Credit） */
export const VENDOR_IDENTITY_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: '目录商' },
  { value: 2, label: '货代' },
  { value: 3, label: '原厂' },
  { value: 4, label: 'EMS工厂' },
  { value: 5, label: '代理' },
  { value: 6, label: 'IDH' },
  { value: 7, label: '渠道商' },
  { value: 8, label: '现货贸易商' },
  { value: 9, label: '电商' },
  { value: 10, label: '制造商' }
]

export function getVendorLevelLabel(level?: number | null): string {
  if (level == null || level === 0) return '--'
  const o = VENDOR_LEVEL_OPTIONS.find((x) => x.value === level)
  return o?.label ?? String(level)
}

export function getVendorIdentityLabel(identity?: number | null): string {
  if (identity == null || identity === 0) return '--'
  const o = VENDOR_IDENTITY_OPTIONS.find((x) => x.value === identity)
  return o?.label ?? String(identity)
}
