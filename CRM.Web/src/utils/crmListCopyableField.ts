import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

/** 业务列表中「物料型号」「品牌」「货代单号」等列 key（统一走可复制悬停提示） */
export const CRM_LIST_COPYABLE_FIELD_KEYS = new Set([
  'pn',
  'materialModel',
  'materialBrand',
  'brand',
  'mpn',
  'model',
  'freightForwarderOrderNo'
])

export function isCrmListCopyableFieldKey(key: string): boolean {
  return CRM_LIST_COPYABLE_FIELD_KEYS.has(key)
}

function pickRowField(row: Record<string, unknown>, prop: string): string {
  const pascal = prop.charAt(0).toUpperCase() + prop.slice(1)
  const v = row[prop] ?? row[pascal]
  if (v == null || v === '') return ''
  return String(v).trim()
}

/** 解析列对应的可复制原文（不含占位符「—」） */
export function resolveCrmListCopyableCellValue(
  row: Record<string, unknown>,
  col: Pick<CrmTableColumnDef, 'key' | 'prop'>
): string {
  if (col.prop) {
    return pickRowField(row, col.prop)
  }
  switch (col.key) {
    case 'pn':
      return pickRowField(row, 'pn') || pickRowField(row, 'materialModel')
    case 'materialModel':
      return pickRowField(row, 'materialModel') || pickRowField(row, 'mpn')
    case 'brand':
      return pickRowField(row, 'brand')
    case 'materialBrand':
      return pickRowField(row, 'materialBrand') || pickRowField(row, 'brand')
    case 'mpn':
      return pickRowField(row, 'mpn')
    case 'model':
      return pickRowField(row, 'model')
    case 'freightForwarderOrderNo':
      return pickRowField(row, 'freightForwarderOrderNo')
    default:
      return ''
  }
}
