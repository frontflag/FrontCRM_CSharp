import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'

export type BuildStockOutItemListColumnsParams = {
  t: (key: string, ...args: unknown[]) => string
}

/** 与 `StockOutItemList.vue` 主表列一致；表头宽按《列表字段宽度规范》§3.3 估算。 */
export function buildStockOutItemListColumns(p: BuildStockOutItemListColumnsParams): CrmTableColumnDef[] {
  const headerMin = (i18nKey: string, align?: 'left' | 'center' | 'right') =>
    estimateListColumnHeaderMinWidth(p.t(i18nKey), align ? { align } : undefined)

  const statusMin = headerMin('stockOutItemList.columns.status', 'center')
  const typeMin = headerMin('stockOutItemList.columns.stockOutType', 'center')
  const qtyMin = headerMin('stockOutItemList.columns.outQuantity', 'right')
  const itemCodeMin = headerMin('stockOutItemList.columns.stockOutItemCode')
  const sellLineMin = headerMin('stockOutItemList.columns.sellOrderItemCode')
  const ffMin = headerMin('common.freightForwarderOrderNo')

  return [
    {
      key: 'status',
      label: p.t('stockOutItemList.columns.status'),
      prop: 'status',
      width: Math.max(110, statusMin),
      minWidth: statusMin,
      align: 'center'
    },
    {
      key: 'stockOutCode',
      label: p.t('stockOutItemList.columns.stockOutCode'),
      prop: 'stockOutCode',
      width: 150,
      minWidth: headerMin('stockOutItemList.columns.stockOutCode'),
      showOverflowTooltip: true
    },
    {
      key: 'stockOutItemCode',
      label: p.t('stockOutItemList.columns.stockOutItemCode'),
      prop: 'stockOutItemCode',
      width: Math.max(160, itemCodeMin),
      minWidth: itemCodeMin,
      showOverflowTooltip: true
    },
    {
      key: 'stockInCode',
      label: p.t('stockOutItemList.columns.stockInCode'),
      prop: 'stockInCode',
      width: 140,
      minWidth: headerMin('stockOutItemList.columns.stockInCode'),
      showOverflowTooltip: true
    },
    {
      key: 'packingCode',
      label: p.t('stockOutItemList.columns.packingCode'),
      prop: 'packingCode',
      width: 150,
      minWidth: headerMin('stockOutItemList.columns.packingCode'),
      showOverflowTooltip: true
    },
    {
      key: 'freightForwarderOrderNo',
      label: p.t('common.freightForwarderOrderNo'),
      prop: 'freightForwarderOrderNo',
      width: Math.max(160, ffMin),
      minWidth: ffMin
    },
    {
      key: 'stockOutDate',
      label: p.t('stockOutItemList.columns.stockOutDate'),
      prop: 'stockOutDate',
      width: Math.max(118, headerMin('stockOutItemList.columns.stockOutDate')),
      minWidth: headerMin('stockOutItemList.columns.stockOutDate')
    },
    {
      key: 'customerName',
      label: p.t('stockOutItemList.columns.customerName'),
      prop: 'customerName',
      minWidth: Math.max(120, headerMin('stockOutItemList.columns.customerName')),
      showOverflowTooltip: true
    },
    {
      key: 'salesUserName',
      label: p.t('stockOutItemList.columns.salesUserName'),
      prop: 'salesUserName',
      width: 110,
      minWidth: headerMin('stockOutItemList.columns.salesUserName'),
      showOverflowTooltip: true
    },
    {
      key: 'purchasePn',
      label: p.t('stockOutItemList.columns.purchasePn'),
      prop: 'purchasePn',
      minWidth: Math.max(130, headerMin('stockOutItemList.columns.purchasePn'))
    },
    {
      key: 'purchaseBrand',
      label: p.t('stockOutItemList.columns.purchaseBrand'),
      prop: 'purchaseBrand',
      minWidth: Math.max(100, headerMin('stockOutItemList.columns.purchaseBrand'))
    },
    {
      key: 'outQuantity',
      label: p.t('stockOutItemList.columns.outQuantity'),
      prop: 'outQuantity',
      minWidth: Math.max(120, qtyMin),
      width: Math.max(120, qtyMin),
      align: 'right',
      className: 'so-item-qty-col',
      labelClassName: 'so-item-qty-col'
    },
    {
      key: 'stockOutType',
      label: p.t('stockOutItemList.columns.stockOutType'),
      prop: 'stockOutType',
      width: Math.max(140, typeMin),
      minWidth: typeMin,
      align: 'center',
      className: 'stock-out-type-col',
      labelClassName: 'stock-out-type-col'
    },
    {
      key: 'shipmentMethod',
      label: p.t('stockOutItemList.columns.shipmentMethod'),
      prop: 'shipmentMethod',
      width: 110,
      minWidth: headerMin('stockOutItemList.columns.shipmentMethod'),
      showOverflowTooltip: true
    },
    {
      key: 'courierTrackingNo',
      label: p.t('stockOutItemList.columns.courierTrackingNo'),
      prop: 'courierTrackingNo',
      width: 130,
      minWidth: headerMin('stockOutItemList.columns.courierTrackingNo'),
      showOverflowTooltip: true
    },
    {
      key: 'sellOrderItemCode',
      label: p.t('stockOutItemList.columns.sellOrderItemCode'),
      prop: 'sellOrderItemCode',
      width: Math.max(160, sellLineMin),
      minWidth: sellLineMin,
      showOverflowTooltip: true
    }
  ]
}
