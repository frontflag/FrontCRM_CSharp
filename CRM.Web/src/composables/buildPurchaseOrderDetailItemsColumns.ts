import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

export type BuildPurchaseOrderDetailItemsColumnsParams = {
  canViewPurchaseAmount: boolean
  /** 操作列宽（随列头展开/收起切换，见《列表操作列规范》） */
  opColWidth: number
  opColMinWidth: number
}

/** 采购订单详情页「订单明细」主表列（与 CrmDataTable + purchase-order-detail-items 布局键配合） */
export function buildPurchaseOrderDetailItemsColumns(
  p: BuildPurchaseOrderDetailItemsColumnsParams
): CrmTableColumnDef[] {
  const cols: CrmTableColumnDef[] = [
    { key: 'rowIndex', type: 'index', width: 50, label: '#', hideable: false, reorderable: false, pinned: 'start' },
    {
      key: 'purchaseOrderItemCode',
      label: '明细编号',
      prop: 'purchaseOrderItemCode',
      minWidth: 168,
      showOverflowTooltip: true
    },
    { key: 'pn', label: '物料型号', prop: 'pn', minWidth: 160, showOverflowTooltip: true },
    { key: 'brand', label: '品牌', prop: 'brand', width: 120, showOverflowTooltip: true },
    { key: 'qty', label: '数量', prop: 'qty', width: 100, align: 'right' }
  ]
  if (p.canViewPurchaseAmount) {
    cols.push(
      { key: 'cost', label: '单价', prop: 'cost', width: 120, align: 'right' },
      { key: 'lineAmount', label: '金额', width: 130, align: 'right' }
    )
  }
  cols.push(
    { key: 'purchaseProgressStatus', label: '采购状态', width: 100, align: 'center' },
    { key: 'stockInProgressStatus', label: '入库状态', width: 100, align: 'center' },
    { key: 'paymentProgressStatus', label: '付款状态', width: 100, align: 'center' },
    { key: 'invoiceProgressStatus', label: '开票状态', width: 100, align: 'center' },
    {
      key: 'purchaseProgressQty',
      label: '采购数量',
      width: 108,
      align: 'right',
      className: 'po-item-progress-qty-col'
    },
    { key: 'stockInProgressQty', label: '入库数量', width: 96, align: 'right' }
  )
  if (p.canViewPurchaseAmount) {
    cols.push(
      { key: 'paymentProgressAmount', label: '已付款', width: 100, align: 'right' },
      { key: 'invoiceProgressAmount', label: '已开票额', width: 100, align: 'right' }
    )
  }
  cols.push(
    { key: 'comment', label: '备注', prop: 'comment', minWidth: 120, showOverflowTooltip: true },
    { key: 'innerComment', label: '内部备注', prop: 'innerComment', minWidth: 160, showOverflowTooltip: true },
    {
      key: 'sellOrderItemCode',
      label: '销售订单明细编号',
      prop: 'sellOrderItemCode',
      minWidth: 168,
      showOverflowTooltip: true
    },
    {
      key: 'actions',
      /** 列设置抽屉仍用「操作」；表头由页面 slot 渲染为仅图标切换，不展示该文案 */
      label: '操作',
      width: p.opColWidth,
      minWidth: p.opColMinWidth,
      fixed: 'right',
      align: 'center',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    }
  )
  return cols
}
