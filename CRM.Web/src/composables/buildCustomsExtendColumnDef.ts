import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export function buildCustomsExtendColumnDef(
  t: (key: string, ...args: unknown[]) => string,
  width: number,
  minWidth: number
): CrmTableColumnDef {
  return {
    key: 'customs',
    label: t('common.customsExtendCol.columnTitle'),
    prop: 'customs',
    minWidth,
    width,
    className: 'customs-extend-col',
    labelClassName: 'customs-extend-col'
  }
}
