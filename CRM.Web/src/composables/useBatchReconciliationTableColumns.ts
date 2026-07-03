import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

/** 批次核销 / 入库单详情「入库批次」面板共用列定义 */
export function useBatchReconciliationTableColumns() {
  const { t } = useI18n()

  const tableColumns = computed<CrmTableColumnDef[]>(() => [
    { key: 'globalBatchNo', label: t('batchReconciliation.columns.globalBatchNo'), prop: 'globalBatchNo', width: 130, showOverflowTooltip: true },
    { key: 'warehouseName', label: t('batchReconciliation.columns.warehouseName'), prop: 'warehouseName', width: 100, showOverflowTooltip: true },
    { key: 'stockInDate', label: t('batchReconciliation.columns.stockInDate'), prop: 'stockInDate', width: 110 },
    { key: 'stockInCode', label: t('batchReconciliation.columns.stockInCode'), prop: 'stockInCode', width: 130, showOverflowTooltip: true },
    { key: 'purchaseOrderCode', label: t('batchReconciliation.columns.purchaseOrderCode'), prop: 'purchaseOrderCode', width: 130, showOverflowTooltip: true },
    { key: 'freightForwarderOrderNo', label: t('batchReconciliation.columns.freightForwarderOrderNo'), prop: 'freightForwarderOrderNo', minWidth: 120, showOverflowTooltip: true },
    { key: 'vendorName', label: t('batchReconciliation.columns.vendorName'), prop: 'vendorName', minWidth: 120, showOverflowTooltip: true },
    { key: 'materialModel', label: t('batchReconciliation.columns.materialModel'), prop: 'materialModel', minWidth: 120, showOverflowTooltip: true },
    { key: 'materialBrand', label: t('batchReconciliation.columns.materialBrand'), prop: 'materialBrand', width: 112, showOverflowTooltip: true },
    { key: 'stockInItemQuantity', label: t('batchReconciliation.columns.stockInItemQuantity'), prop: 'stockInItemQuantity', width: 112, align: 'right' },
    { key: 'batchDimension', label: t('batchReconciliation.columns.batchDimension'), prop: 'batchDimension', width: 112, showOverflowTooltip: true },
    { key: 'batchUnit', label: t('batchReconciliation.columns.batchUnit'), prop: 'batchUnit', width: 148, showOverflowTooltip: true },
    { key: 'unitNo', label: t('batchReconciliation.columns.unitNo'), prop: 'unitNo', width: 112, showOverflowTooltip: true },
    { key: 'batchQty', label: t('batchReconciliation.columns.batchQty'), prop: 'batchQty', width: 112, align: 'right' },
    { key: 'dc', label: t('batchReconciliation.columns.dc'), prop: 'dc', width: 100, showOverflowTooltip: true },
    { key: 'packageOrigin', label: t('batchReconciliation.columns.packageOrigin'), prop: 'packageOrigin', width: 112, showOverflowTooltip: true },
    { key: 'waferOrigin', label: t('batchReconciliation.columns.waferOrigin'), prop: 'waferOrigin', width: 112, showOverflowTooltip: true },
    { key: 'lot', label: t('batchReconciliation.columns.lot'), prop: 'lot', width: 90, showOverflowTooltip: true },
    { key: 'serialNumber', label: t('batchReconciliation.columns.serialNumber'), prop: 'serialNumber', minWidth: 100, showOverflowTooltip: true },
    { key: 'partCode', label: t('batchReconciliation.columns.partCode'), prop: 'partCode', width: 120, minWidth: 112, showOverflowTooltip: true },
    { key: 'packingCode', label: t('batchReconciliation.columns.packingCode'), prop: 'packingCode', width: 130, showOverflowTooltip: true },
    { key: 'customerName', label: t('batchReconciliation.columns.customerName'), prop: 'customerName', minWidth: 120, showOverflowTooltip: true },
    { key: 'stockOutDate', label: t('batchReconciliation.columns.stockOutDate'), prop: 'stockOutDate', width: 110 },
    { key: 'outQty', label: t('batchReconciliation.columns.outQty'), prop: 'outQty', width: 112, minWidth: 108, align: 'right' },
    { key: 'totalOutQty', label: t('batchReconciliation.columns.totalOutQty'), prop: 'totalOutQty', width: 130, minWidth: 120, align: 'right' },
    { key: 'remainingQty', label: t('batchReconciliation.columns.remainingQty'), prop: 'remainingQty', width: 112, minWidth: 108, align: 'right' }
  ])

  return { tableColumns }
}
