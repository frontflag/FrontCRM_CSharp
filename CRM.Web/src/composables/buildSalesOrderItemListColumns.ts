import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildSalesOrderItemListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  listCustomerColumnOk: boolean
  listShowAmountColumns: boolean
  opColWidth: number
  opColMinWidth: number
  /** 与 `/sales-order-items` 列表一致；拣货单嵌入表可关 */
  withSelection?: boolean
  withActions?: boolean
}

/**
 * 与 `SalesOrderItemList.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。
 */
export function buildSalesOrderItemListColumns(p: BuildSalesOrderItemListColumnsParams): CrmTableColumnDef[] {
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
      key: 'sellOrderItemCode',
      label: p.t('salesOrderItemList.columns.sellOrderItemCode'),
      prop: 'sellOrderItemCode',
      width: 180,
      minWidth: 168,
      showOverflowTooltip: true
    },
    {
      key: 'sellOrderCode',
      label: p.t('salesOrderItemList.columns.sellOrderCode'),
      prop: 'sellOrderCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'orderStatus', label: p.t('salesOrderItemList.columns.status'), prop: 'orderStatus', width: 160, align: 'center' },
    { key: 'orderCreateTime', label: p.t('salesOrderItemList.columns.orderCreateDate'), prop: 'orderCreateTime', width: 160 },
    { key: 'salesUserName', label: p.t('salesOrderItemList.columns.salesUser'), prop: 'salesUserName', width: 100, showOverflowTooltip: true },
    { key: 'pn', label: p.t('salesOrderItemList.columns.pn'), prop: 'pn', minWidth: 130, showOverflowTooltip: true },
    { key: 'brand', label: p.t('salesOrderItemList.columns.brand'), prop: 'brand', width: 110, showOverflowTooltip: true },
    { key: 'qty', label: p.t('salesOrderItemList.columns.qty'), prop: 'qty', width: 100, align: 'right' },
    { key: 'createTime', label: p.t('salesOrderItemList.columns.createTime'), width: 160 },
    { key: 'createUser', label: p.t('salesOrderItemList.columns.createUser'), width: 120, showOverflowTooltip: true }
  )
  if (p.listCustomerColumnOk) {
    const insertCustomerAt = cols.findIndex((c) => c.key === 'orderCreateTime')
    if (insertCustomerAt >= 0) {
      cols.splice(insertCustomerAt, 0, {
        key: 'customerName',
        label: p.t('salesOrderItemList.columns.customerName'),
        prop: 'customerName',
        minWidth: 200,
        showOverflowTooltip: true
      })
    }
  }
  const currencyColumn: CrmTableColumnDef = {
    key: 'currency',
    label: p.t('salesOrderItemList.columns.currency'),
    prop: 'currency',
    width: 72,
    minWidth: 72,
    align: 'center',
    hideable: false
  }

  if (p.listShowAmountColumns) {
    cols.splice(cols.length - 2, 0,
      currencyColumn,
      {
        key: 'price',
        label: p.t('salesOrderItemList.columns.unitPrice'),
        prop: 'price',
        width: 128,
        minWidth: 112,
        align: 'right',
        hideable: false
      },
      {
        key: 'lineTotal',
        label: p.t('salesOrderItemList.columns.lineTotal'),
        prop: 'lineTotal',
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'usdUnitPrice',
        label: p.t('salesOrderItemList.columns.usdUnitPrice'),
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'usdLineTotal',
        label: p.t('salesOrderItemList.columns.usdLineTotal'),
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'salesProfitExpected',
        label: p.t('salesOrderItemList.columns.salesProfitExpected'),
        prop: 'salesProfitExpected',
        width: 140,
        align: 'right'
      },
      {
        key: 'profitOutBizUsd',
        label: p.t('salesOrderItemList.columns.profitOutBizUsd'),
        prop: 'profitOutBizUsd',
        width: 120,
        align: 'right'
      },
      {
        key: 'profitOutRateBiz',
        label: p.t('salesOrderItemList.columns.profitOutRateBiz'),
        prop: 'profitOutRateBiz',
        width: 120,
        align: 'right'
      }
    )
  } else {
    cols.splice(cols.length - 2, 0, currencyColumn)
  }

  const progressSix: CrmTableColumnDef[] = [
    {
      key: 'purchaseProgressStatus',
      label: p.t('salesOrderItemList.columns.purchaseProgressStatus'),
      prop: 'purchaseProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'stockInProgressStatus',
      label: p.t('salesOrderItemList.columns.stockInProgressStatus'),
      prop: 'stockInProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'stockOutNotifyProgressStatus',
      label: p.t('salesOrderItemList.columns.stockOutNotifyProgressStatus'),
      prop: 'stockOutNotifyProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'stockOutProgressStatus',
      label: p.t('salesOrderItemList.columns.stockOutProgressStatus'),
      prop: 'stockOutProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'receiptProgressStatus',
      label: p.t('salesOrderItemList.columns.receiptProgressStatus'),
      prop: 'receiptProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'invoiceProgressStatus',
      label: p.t('salesOrderItemList.columns.invoiceProgressStatus'),
      prop: 'invoiceProgressStatus',
      width: 108,
      align: 'center'
    }
  ]
  const profitIdx = cols.findIndex((c) => c.key === 'profitOutRateBiz')
  const qtyIdx = cols.findIndex((c) => c.key === 'qty')
  if (profitIdx >= 0) {
    cols.splice(profitIdx + 1, 0, ...progressSix)
  } else if (qtyIdx >= 0) {
    cols.splice(qtyIdx + 1, 0, ...progressSix)
  }

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('salesOrderItemList.columns.actions'),
      width: p.opColWidth,
      minWidth: p.opColMinWidth,
      fixed: 'right',
      align: 'center',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    })
  }
  return cols
}
