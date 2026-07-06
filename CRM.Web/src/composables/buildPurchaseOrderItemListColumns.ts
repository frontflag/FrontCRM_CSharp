import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildPurchaseOrderItemListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  canViewVendor: boolean
  canViewPurchaseUser: boolean
  canViewAmount: boolean
  opColWidth: number
  opColMinWidth: number
  withSelection?: boolean
  withActions?: boolean
}

/**
 * 与 `PurchaseOrderItemList.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。
 */
export function buildPurchaseOrderItemListColumns(
  p: BuildPurchaseOrderItemListColumnsParams
): CrmTableColumnDef[] {
  const withSelection = p.withSelection !== false
  const withActions = p.withActions !== false
  const cols: CrmTableColumnDef[] = []

  if (withSelection) {
    cols.push({
      key: 'selection',
      type: 'selection',
      width: 48,
      reserveSelection: true,
      fixed: 'left',
      hideable: false,
      reorderable: false
    })
  }

  cols.push(
    {
      key: 'purchaseOrderItemCode',
      label: p.t('purchaseOrderItemList.columns.purchaseOrderItemCode'),
      prop: 'purchaseOrderItemCode',
      width: 180,
      minWidth: 168,
      fixed: withSelection ? undefined : 'left',
      showOverflowTooltip: true
    },
    {
      key: 'purchaseOrderCode',
      label: p.t('purchaseOrderItemList.columns.purchaseOrderCode'),
      prop: 'purchaseOrderCode',
      width: 160,
      minWidth: 160,
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
      key: 'itemStatus',
      label: p.t('purchaseOrderItemList.columns.itemStatus'),
      prop: 'itemStatus',
      width: 160,
      align: 'center'
    }
  )

  if (p.canViewVendor) {
    cols.push({
      key: 'vendorName',
      label: p.t('purchaseOrderItemList.columns.vendorName'),
      prop: 'vendorName',
      minWidth: 200,
      showOverflowTooltip: true
    })
  }

  if (p.canViewPurchaseUser) {
    cols.push({
      key: 'purchaseUserName',
      label: p.t('purchaseOrderItemList.columns.purchaseUserName'),
      prop: 'purchaseUserName',
      width: 100,
      showOverflowTooltip: true
    })
  }

  cols.push(
    { key: 'pn', label: p.t('purchaseOrderItemList.columns.pn'), prop: 'pn', minWidth: 130, showOverflowTooltip: true },
    { key: 'brand', label: p.t('purchaseOrderItemList.columns.brand'), prop: 'brand', width: 110, showOverflowTooltip: true },
    { key: 'qty', label: p.t('purchaseOrderItemList.columns.qty'), prop: 'qty', width: 100, align: 'right' }
  )

  if (p.canViewAmount) {
    cols.push(
      { key: 'cost', label: p.t('purchaseOrderItemList.columns.cost'), prop: 'cost', width: 160, align: 'right' },
      { key: 'lineTotal', label: p.t('purchaseOrderItemList.columns.lineTotal'), prop: 'lineTotal', width: 160, align: 'right' }
    )
  }

  cols.push(
    { key: 'createTime', label: p.t('purchaseOrderItemList.columns.createTime'), width: 160 },
    { key: 'createUser', label: p.t('purchaseOrderItemList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'paymentRequestProgressStatus',
      label: p.t('purchaseOrderItemList.columns.paymentRequestProgressStatus'),
      prop: 'paymentRequestProgressStatus',
      width: 130,
      align: 'center'
    },
    {
      key: 'paymentProgressStatus',
      label: p.t('purchaseOrderItemList.columns.paymentProgressStatus'),
      prop: 'paymentProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'purchaseProgressStatus',
      label: p.t('purchaseOrderItemList.columns.purchaseProgressStatus'),
      prop: 'purchaseProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'stockInProgressStatus',
      label: p.t('purchaseOrderItemList.columns.stockInProgressStatus'),
      prop: 'stockInProgressStatus',
      width: 120,
      align: 'center'
    },
    {
      key: 'invoiceProgressStatus',
      label: p.t('purchaseOrderItemList.columns.invoiceProgressStatus'),
      prop: 'invoiceProgressStatus',
      width: 120,
      align: 'center'
    }
  )

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('purchaseOrderItemList.columns.actions'),
      width: p.opColWidth,
      minWidth: p.opColMinWidth,
      fixed: 'right',
      align: 'center',
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
