import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildArrivalNoticeListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
  opColWidth: number
  opColMinWidth: number
  withActions?: boolean
}

/** 与 `ArrivalNoticeList.vue` 中 CrmDataTable 列配置一致，供列表页与嵌入场景复用。 */
export function buildArrivalNoticeListColumns(p: BuildArrivalNoticeListColumnsParams): CrmTableColumnDef[] {
  const withActions = p.withActions !== false
  const cols: CrmTableColumnDef[] = [
    { key: 'status', label: p.t('arrivalNoticeList.columns.status'), prop: 'status', width: 110, align: 'center' },
    {
      key: 'stockInType',
      label: p.t('arrivalNoticeList.columns.arrivalType'),
      prop: 'stockInType',
      width: 140,
      minWidth: 130,
      align: 'center',
      className: 'stock-in-type-col',
      labelClassName: 'stock-in-type-col'
    },
    { key: 'pn', label: p.t('arrivalNoticeList.columns.pn'), minWidth: 120, showOverflowTooltip: true },
    { key: 'brand', label: p.t('arrivalNoticeList.columns.brand'), width: 100, showOverflowTooltip: true },
    {
      key: 'expectedArrivalDate',
      label: p.t('arrivalNoticeList.columns.expectedArrivalDate'),
      width: 130,
      align: 'center'
    },
    {
      key: 'actualArrivalDate',
      label: p.t('arrivalNoticeList.columns.actualArrivalDate'),
      width: 120,
      align: 'center'
    },
    {
      key: 'shipmentMethod',
      label: p.t('arrivalNoticeList.columns.expectedArrivalMethod'),
      width: 136,
      minWidth: 136,
      align: 'center',
      showOverflowTooltip: true
    },
    {
      key: 'courierTrackingNo',
      label: p.t('arrivalNoticeList.columns.expectedArrivalExpressNo'),
      width: 184,
      minWidth: 184,
      showOverflowTooltip: true
    },
    { key: 'vendorName', label: p.t('arrivalNoticeList.columns.vendorName'), prop: 'vendorName', minWidth: 160 },
    {
      key: 'purchaseUserName',
      label: p.t('arrivalNoticeList.columns.purchaseUserName'),
      prop: 'purchaseUserName',
      width: 120
    },
    { key: 'expectQty', label: p.t('arrivalNoticeList.columns.expectQty'), width: 120, minWidth: 120, align: 'right' },
    { key: 'receiveQty', label: p.t('arrivalNoticeList.columns.receiveQty'), width: 120, minWidth: 120, align: 'right' },
    { key: 'passedQty', label: p.t('arrivalNoticeList.columns.passedQty'), width: 120, minWidth: 120, align: 'right' },
    {
      key: 'regionType',
      label: p.t('arrivalNoticeList.columns.arrivalRegion'),
      width: 100,
      align: 'center'
    },
    { key: 'noticeCode', label: p.t('arrivalNoticeList.columns.noticeCode'), prop: 'noticeCode', width: 170 },
    {
      key: 'purchaseOrderCode',
      label: p.t('arrivalNoticeList.columns.purchaseOrderCode'),
      prop: 'purchaseOrderCode',
      width: 160
    },
    {
      key: 'freightForwarderOrderNo',
      label: p.t('common.freightForwarderOrderNo'),
      prop: 'freightForwarderOrderNo',
      width: 160,
      minWidth: 140,
      showOverflowTooltip: true
    },
    { key: 'createTime', label: p.t('arrivalNoticeList.columns.createTime'), prop: 'createTime', width: 170 },
    { key: 'createUser', label: p.t('arrivalNoticeList.columns.createUser'), width: 120, showOverflowTooltip: true }
  ]

  if (withActions) {
    cols.push({
      key: 'actions',
      label: p.t('arrivalNoticeList.columns.actions'),
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
