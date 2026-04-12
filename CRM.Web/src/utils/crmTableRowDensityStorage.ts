/** localStorage 键前缀：与列布局 `crm-table-columns:v1:` 分离 */
const STORAGE_PREFIX = 'crm-table-row-density:v1:'

export type CrmTableRowDensity = 'compact' | 'medium'

export function readPersistedRowDensity(storageKey: string): CrmTableRowDensity {
  const k = storageKey.trim()
  if (!k) return 'compact'
  try {
    const v = localStorage.getItem(STORAGE_PREFIX + k)
    if (v === 'medium' || v === 'compact') return v
  } catch {
    /* ignore */
  }
  return 'compact'
}

export function writePersistedRowDensity(storageKey: string, density: CrmTableRowDensity): void {
  const k = storageKey.trim()
  if (!k) return
  try {
    localStorage.setItem(STORAGE_PREFIX + k, density)
  } catch {
    /* ignore */
  }
}
