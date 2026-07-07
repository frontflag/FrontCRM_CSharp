import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildPackingListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  opColWidth: number
  opColMinWidth: number
  withSelection?: boolean
  withActions?: boolean
}

/** 与 `PackingListPage.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。 */
export function buildPackingListColumns(p: BuildPackingListColumnsParams): CrmTableColumnDef[] {
  const withSelection = p.withSelection !== false
  const withActions = p.withActions !== false
  const cols: CrmTableColumnDef[] = []

  if (withSelection) {
    cols.push({
      key: 'selection',
      type: 'selection',
      width: 44,
      fixed: 'left',
      hideable: false,
      reorderable: false,
      reserveSelection: true
    })
  }

  cols.push(
    { key: 'packingCode', label: p.t('packingList.columns.packingCode'), width: 160, minWidth: 160, showOverflowTooltip: true },
    { key: 'status', label: p.t('packingList.columns.status'), width: 110, minWidth: 110, align: 'center' },
    { key: 'stockOutType', label: p.t('packingList.columns.stockOutType'), width: 140, minWidth: 140, align: 'center', className: 'stock-out-type-col', labelClassName: 'stock-out-type-col' },
    { key: 'materialType', label: p.t('packingList.columns.materialType'), width: 140, minWidth: 140, align: 'center' },
    { key: 'customerName', label: p.t('packingList.columns.customerName'), width: 140, minWidth: 140, showOverflowTooltip: true },
    { key: 'salesUserName', label: p.t('packingList.columns.salesUserName'), width: 130, minWidth: 130, showOverflowTooltip: true },
    { key: 'warehouseName', label: p.t('packingList.columns.warehouseName'), width: 120, minWidth: 120, showOverflowTooltip: true },
    { key: 'requestDate', label: p.t('packingList.columns.expectedShipDate'), width: 150, minWidth: 150 },
    { key: 'shipmentMethod', label: p.t('packingList.columns.shipmentMethod'), width: 120, minWidth: 100, showOverflowTooltip: true },
    { key: 'expressCompany', label: p.t('packingList.columns.expressCompany'), width: 120, minWidth: 100, showOverflowTooltip: true },
    { key: 'itemRows', label: p.t('packingList.columns.itemRows'), width: 120, minWidth: 120, align: 'right' },
    { key: 'remark', label: p.t('packingList.columns.remark'), minWidth: 160, showOverflowTooltip: true },
    { key: 'createTime', label: p.t('packingList.columns.createTime'), width: 170, minWidth: 170 },
    { key: 'createUserName', label: p.t('packingList.columns.createUserName'), width: 140, minWidth: 140, showOverflowTooltip: true }
  )

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('packingList.columns.actions'),
      width: p.opColWidth,
      minWidth: p.opColMinWidth,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    })
  }

  return cols
}
