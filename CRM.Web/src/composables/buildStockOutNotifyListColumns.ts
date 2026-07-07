import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildStockOutNotifyListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  opColWidth: number
  opColMinWidth: number
  withSelection?: boolean
  withActions?: boolean
}

/** 与 `StockOutNotifyList.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。 */
export function buildStockOutNotifyListColumns(p: BuildStockOutNotifyListColumnsParams): CrmTableColumnDef[] {
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
    { key: 'status', label: p.t('stockOutNotifyList.columns.status'), prop: 'status', width: 110, align: 'center' },
    {
      key: 'customsStatus',
      label: p.t('stockOutNotifyList.columns.customsStatus'),
      width: 120,
      minWidth: 110,
      align: 'center'
    },
    {
      key: 'stockOutType',
      label: p.t('stockOutNotifyList.columns.stockOutType'),
      width: 140,
      minWidth: 130,
      align: 'center',
      className: 'stock-out-type-col',
      labelClassName: 'stock-out-type-col'
    },
    {
      key: 'materialModel',
      label: p.t('stockOutNotifyList.columns.materialModel'),
      prop: 'materialModel',
      width: 180,
      showOverflowTooltip: true
    },
    { key: 'brand', label: p.t('stockOutNotifyList.columns.brand'), prop: 'brand', width: 140, showOverflowTooltip: true },
    {
      key: 'outQuantity',
      label: p.t('stockOutNotifyList.columns.outQuantity'),
      prop: 'outQuantity',
      width: 110,
      align: 'right'
    },
    {
      key: 'regionType',
      label: p.t('stockOutNotifyList.columns.regionType'),
      width: 100,
      minWidth: 100,
      align: 'center'
    },
    {
      key: 'shipmentMethod',
      label: p.t('stockOutNotifyList.columns.shipmentMethod'),
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'expressCompany',
      label: p.t('stockOutNotifyList.columns.expressCompany'),
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'packingCode',
      label: p.t('stockOutNotifyList.columns.packingCode'),
      prop: 'packingCode',
      width: 150,
      minWidth: 130,
      showOverflowTooltip: true
    },
    { key: 'requestDate', label: p.t('stockOutNotifyList.columns.requestDate'), prop: 'requestDate', width: 170 },
    {
      key: 'salesUserName',
      label: p.t('stockOutNotifyList.columns.salesUserName'),
      prop: 'salesUserName',
      width: 130,
      showOverflowTooltip: true
    },
    {
      key: 'customerName',
      label: p.t('stockOutNotifyList.columns.customer'),
      prop: 'customerName',
      minWidth: 180,
      showOverflowTooltip: true
    },
    { key: 'remark', label: p.t('stockOutNotifyList.columns.remark'), prop: 'remark', minWidth: 180, showOverflowTooltip: true },
    {
      key: 'requestCode',
      label: p.t('stockOutNotifyList.columns.requestCode'),
      prop: 'requestCode',
      width: 190,
      minWidth: 170
    },
    {
      key: 'salesOrderCode',
      label: p.t('stockOutNotifyList.columns.salesOrderCode'),
      prop: 'salesOrderCode',
      width: 160,
      minWidth: 160
    },
    { key: 'createTime', label: p.t('stockOutNotifyList.columns.createTime'), prop: 'createTime', width: 170 },
    { key: 'createUser', label: p.t('stockOutNotifyList.columns.createUser'), width: 140, showOverflowTooltip: true }
  )

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('stockOutNotifyList.columns.actions'),
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
