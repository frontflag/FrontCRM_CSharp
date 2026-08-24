import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

/** 业务列表中「物料型号」「品牌」「货代单号」等列 key / prop（统一走可复制悬停提示） */
export const CRM_LIST_COPYABLE_FIELD_KEYS = new Set([
  'pn',
  'materialModel',
  'materialBrand',
  'brand',
  'mpn',
  'customerPn',
  'model',
  'purchasePn',
  'purchaseBrand',
  'freightForwarderOrderNo'
])

export function isCrmListCopyableFieldKey(key: string): boolean {
  return CRM_LIST_COPYABLE_FIELD_KEYS.has(key)
}

/** 列定义 key 或 prop 任一命中可复制字段时返回 true */
export function isCrmListCopyableColumn(col: { key?: string; prop?: string }): boolean {
  const key = String(col.key ?? '').trim()
  if (key && isCrmListCopyableFieldKey(key)) return true
  const prop = String(col.prop ?? '').trim()
  return !!prop && isCrmListCopyableFieldKey(prop)
}

function pickRowField(row: Record<string, unknown>, prop: string): string {
  const pascal = prop.charAt(0).toUpperCase() + prop.slice(1)
  const v = row[prop] ?? row[pascal]
  if (v == null || v === '') return ''
  return String(v).trim()
}

function resolveByFieldKey(row: Record<string, unknown>, field: string): string {
  switch (field) {
    case 'pn':
      return (
        pickRowField(row, 'pn')
        || pickRowField(row, 'materialModel')
        || pickRowField(row, 'purchasePn')
        || pickRowField(row, 'mpn')
      )
    case 'materialModel':
      return pickRowField(row, 'materialModel') || pickRowField(row, 'mpn') || pickRowField(row, 'pn')
    case 'brand':
      return pickRowField(row, 'brand') || pickRowField(row, 'materialBrand') || pickRowField(row, 'purchaseBrand')
    case 'customerPn':
      return pickRowField(row, 'customerPn')
    case 'materialBrand':
      return pickRowField(row, 'materialBrand') || pickRowField(row, 'brand') || pickRowField(row, 'purchaseBrand')
    case 'mpn':
      return pickRowField(row, 'mpn') || pickRowField(row, 'materialModel') || pickRowField(row, 'pn')
    case 'model':
      return pickRowField(row, 'model') || pickRowField(row, 'materialModel') || pickRowField(row, 'pn')
    case 'purchasePn':
      return pickRowField(row, 'purchasePn') || pickRowField(row, 'pn') || pickRowField(row, 'materialModel')
    case 'purchaseBrand':
      return pickRowField(row, 'purchaseBrand') || pickRowField(row, 'brand') || pickRowField(row, 'materialBrand')
    case 'freightForwarderOrderNo':
      return pickRowField(row, 'freightForwarderOrderNo')
    default:
      return pickRowField(row, field)
  }
}

/** 从行对象按字段名解析可复制原文（表格自定义列 slot 用） */
export function pickCrmCopyableRowField(row: Record<string, unknown>, field: string): string {
  const f = field.trim()
  if (!f) return ''
  return resolveByFieldKey(row, f)
}

/** 解析列对应的可复制原文（不含占位符「—」） */
export function resolveCrmListCopyableCellValue(
  row: Record<string, unknown>,
  col: Pick<CrmTableColumnDef, 'key' | 'prop'>
): string {
  const key = String(col.key ?? '').trim()
  if (key && isCrmListCopyableFieldKey(key)) {
    const v = resolveByFieldKey(row, key)
    if (v) return v
  }
  const prop = String(col.prop ?? '').trim()
  if (prop && isCrmListCopyableFieldKey(prop)) {
    return resolveByFieldKey(row, prop)
  }
  return ''
}
