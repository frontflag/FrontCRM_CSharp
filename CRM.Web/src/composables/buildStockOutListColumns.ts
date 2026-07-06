import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildStockOutListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  opColWidth: number
  opColMinWidth: number
  withSelection?: boolean
  withActions?: boolean
  withCustomerExtend?: boolean
  customerExtendColWidth?: number
  customerExtendColMinWidth?: number
}

/** 与 `StockOutList.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。 */
export function buildStockOutListColumns(p: BuildStockOutListColumnsParams): CrmTableColumnDef[] {
  const withSelection = p.withSelection !== false
  const withActions = p.withActions !== false
  const withCustomerExtend = p.withCustomerExtend !== false
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
    {
      key: 'stockOutCode',
      label: p.t('stockOutList.columns.stockOutCode'),
      prop: 'stockOutCode',
      width: 190,
      minWidth: 170,
      showOverflowTooltip: true
    },
    { key: 'status', label: p.t('stockOutList.columns.status'), prop: 'status', width: 110, align: 'center' },
    {
      key: 'stockOutType',
      label: p.t('stockOutList.columns.stockOutType'),
      prop: 'stockOutType',
      width: 110,
      align: 'center'
    },
    {
      key: 'expectedStockOutDate',
      label: p.t('stockOutList.columns.expectedStockOutDate'),
      prop: 'expectedStockOutDate',
      width: 130
    },
    { key: 'stockOutDate', label: p.t('stockOutList.columns.stockOutDate'), prop: 'stockOutDate', width: 170 },
    {
      key: 'shipmentMethod',
      label: p.t('stockOutList.columns.shipmentMethod'),
      prop: 'shipmentMethod',
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'expressCompany',
      label: p.t('stockOutList.columns.expressCompany'),
      prop: 'expressCompany',
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'courierTrackingNo',
      label: p.t('stockOutList.columns.courierTrackingNo'),
      prop: 'courierTrackingNo',
      width: 140,
      minWidth: 120,
      showOverflowTooltip: true
    }
  )

  if (withCustomerExtend) {
    cols.push({
      key: 'customer',
      label: p.t('common.customerExtendCol.columnTitle'),
      prop: 'customer',
      minWidth: p.customerExtendColMinWidth ?? 140,
      width: p.customerExtendColWidth ?? 140,
      showOverflowTooltip: true,
      className: 'customer-extend-col',
      labelClassName: 'customer-extend-col'
    })
  } else {
    cols.push({
      key: 'customerName',
      label: p.t('stockOutList.columns.customerName'),
      prop: 'customerName',
      minWidth: 140,
      width: 140,
      showOverflowTooltip: true
    })
  }

  cols.push(
    {
      key: 'salesUserName',
      label: p.t('stockOutList.columns.salesUserName'),
      prop: 'salesUserName',
      width: 110,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'packingCodes',
      label: p.t('stockOutList.columns.packingCodes'),
      prop: 'packingCodes',
      width: 160,
      minWidth: 140,
      showOverflowTooltip: true
    },
    {
      key: 'freightForwarderOrderNo',
      label: p.t('common.freightForwarderOrderNo'),
      prop: 'freightForwarderOrderNo',
      width: 160,
      minWidth: 140,
      showOverflowTooltip: true
    },
    {
      key: 'packingCount',
      label: p.t('stockOutList.columns.packingCount'),
      prop: 'packingCount',
      width: 120,
      minWidth: 112,
      align: 'right'
    },
    { key: 'remark', label: p.t('stockOutList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
    { key: 'createTime', label: p.t('stockOutList.columns.createTime'), width: 170 },
    { key: 'createUser', label: p.t('stockOutList.columns.createUser'), width: 120, showOverflowTooltip: true }
  )

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('stockOutList.columns.actions'),
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
