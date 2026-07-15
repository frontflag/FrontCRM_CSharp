<template>
  <div class="po-aggregate-table-wrap purchase-order-item-stock-tab-table">
    <el-table
      v-if="items.length > 0"
      :data="items"
      size="small"
      stripe
      @row-dblclick="onRowDblclick"
    >
      <el-table-column type="index" width="50" label="#" />
      <el-table-column :label="t('inventoryStockItemList.columns.outboundStatus')" width="110" align="center">
        <template #default="{ row }">
          <span class="outbound-status-chip" :class="`outbound-status-chip--${outboundStatusKind(row.outboundStatus)}`">
            <span>{{ outboundLabel(row.outboundStatus) }}</span>
          </span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.stockItemCode')"
        prop="stockItemCode"
        width="168"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <span class="stock-item-code-with-badge">
            <span>{{ row.stockItemCode || '—' }}</span>
            <el-tooltip
              v-if="isStockingStockItem(row)"
              :content="t('inventoryList.stockTypes.stocking')"
              placement="top"
              :hide-after="0"
            >
              <span class="inv-stock-item-code-stocking-hit" role="img" :aria-label="t('inventoryList.stockTypes.stocking')">
                <el-icon class="inv-stock-item-code-stocking-icon" aria-hidden="true">
                  <Box />
                </el-icon>
              </span>
            </el-tooltip>
          </span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.stockInCode')"
        prop="stockInCode"
        width="150"
        show-overflow-tooltip
      />
      <el-table-column :label="t('inventoryStockItemList.columns.stockInDate')" width="118">
        <template #default="{ row }">
          <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.stockInDate)]" :key="'sid-' + row.stockItemId">
            <span v-if="!p" class="inv-list-dash">—</span>
            <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            </span>
            <span v-else class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
          </template>
        </template>
      </el-table-column>
      <el-table-column :label="t('inventoryStockItemList.columns.warehouse')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ warehouseCell(row) }}</template>
      </el-table-column>
      <el-table-column :label="t('inventoryStockItemList.columns.regionType')" width="100" align="center">
        <template #default="{ row }">
          <span class="region-type-chip" :class="`region-type-chip--${regionTypeKind(row)}`">
            <span>{{ stockItemRegionLabel(row) }}</span>
          </span>
        </template>
      </el-table-column>
      <CrmCopyableTableColumn
        prop="purchasePn"
        :label="t('inventoryStockItemList.columns.purchasePn')"
        min-width="130"
      />
      <CrmCopyableTableColumn
        prop="purchaseBrand"
        :label="t('inventoryStockItemList.columns.purchaseBrand')"
        min-width="100"
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.qtyInbound')"
        prop="qtyInbound"
        min-width="124"
        align="right"
        class-name="inv-stock-item-qty-col"
        label-class-name="inv-stock-item-qty-col"
      >
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(row.qtyInbound) }}</span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.qtyStockOut')"
        prop="qtyStockOut"
        min-width="124"
        align="right"
        class-name="inv-stock-item-qty-col"
        label-class-name="inv-stock-item-qty-col"
      >
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(row.qtyStockOut) }}</span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.qtyRepertory')"
        prop="qtyRepertory"
        min-width="124"
        align="right"
        class-name="inv-stock-item-qty-col"
        label-class-name="inv-stock-item-qty-col"
      >
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(row.qtyRepertory) }}</span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('common.vendorExtendCol.columnTitle')"
        :min-width="vendorExtendColMinWidth"
        :width="vendorExtendColWidth"
        show-overflow-tooltip
        class-name="vendor-extend-col"
        label-class-name="vendor-extend-col"
      >
        <template #header>
          <VendorExtendColumnHeader
            :active-field="vendorExtendActiveField"
            @set-active-field="setVendorExtendActiveField"
          />
        </template>
        <template #default="{ row }">
          <VendorExtendCell
            :row="row"
            :active-field="vendorExtendActiveField"
            :masked="maskPurchaseSensitiveFields"
            :empty-text="t('quoteList.na')"
          />
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.purchaserName')"
        prop="purchaserName"
        width="112"
        min-width="112"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.purchaseOrderItemCode')"
        prop="purchaseOrderItemCode"
        width="168"
        min-width="168"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('common.freightForwarderOrderNo')"
        prop="freightForwarderOrderNo"
        width="160"
        min-width="140"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.customerName')"
        prop="customerName"
        min-width="120"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() ? row.customerName : '—') }}</span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.salespersonName')"
        prop="salespersonName"
        width="112"
        min-width="112"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <span>{{ maskSaleSensitiveFields ? '—' : (row.salespersonName?.trim() ? row.salespersonName : '—') }}</span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryStockItemList.columns.sellOrderItemCode')"
        prop="sellOrderItemCode"
        width="120"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.batchNo')"
        prop="batchNo"
        width="100"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.locationId')"
        prop="locationId"
        min-width="100"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('inventoryStockItemList.columns.profitOutBizUsd')"
        prop="profitOutBizUsd"
        width="200"
        min-width="200"
        align="right"
      >
        <template #default="{ row }">
          <span v-if="maskPurchaseSensitiveFields || maskSaleSensitiveFields" class="inv-list-dash">—</span>
          <template v-else-if="row.profitOutBizUsd == null">
            <span class="inv-list-dash">—</span>
          </template>
          <div v-else class="inv-list-amount-cell dock-tier-price-line">
            <template v-for="amt in [splitUsdMoneyParts(Number(row.profitOutBizUsd))]" :key="'p-' + row.stockItemId">
              <span class="inv-list-amt">
                <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
              </span>
            </template>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
          </div>
        </template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('purchaseOrderItemStockTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Box } from '@element-plus/icons-vue'
import type { StockItemListRow } from '@/api/inventoryCenter'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useVendorExtendColumn } from '@/composables/useVendorExtendColumn'

withDefaults(
  defineProps<{
    items: StockItemListRow[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField
} = useVendorExtendColumn()

function isTimeMidnightOnly(time: string) {
  const t0 = (time || '').trim()
  return t0 === '00:00' || t0.startsWith('00:00:')
}

const stockItemRegionLabel = (row: StockItemListRow) => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const regionTypeKind = (row: StockItemListRow): 'domestic' | 'overseas' => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

const formatQtyCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

const splitUsdMoneyParts = (n: number): { intPart: string; fracPart: string } => {
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) fracPart = '.00'
  return { intPart, fracPart }
}

function isStockingStockItem(row: StockItemListRow): boolean {
  return Number(row.stockType ?? 0) === 2
}

const outboundLabel = (s: number) => {
  if (s === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (s === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (s === 3) return t('inventoryStockItemList.filters.outboundDone')
  return '—'
}

const outboundStatusKind = (s: number): 'none' | 'partial' | 'done' | 'unknown' => {
  if (s === 1) return 'none'
  if (s === 2) return 'partial'
  if (s === 3) return 'done'
  return 'unknown'
}

const warehouseCell = (row: StockItemListRow) => {
  const code = row.warehouseCode?.trim()
  if (code) return code
  return t('quoteList.na')
}

const onRowDblclick = (row: StockItemListRow) => {
  const sid = (row.stockAggregateId || '').trim()
  if (!sid) {
    ElMessage.warning(t('inventoryStockItemList.messages.missingAggregateId'))
    return
  }
  router.push({
    path: `/inventory/stocks/${encodeURIComponent(sid)}`,
    query: {
      materialId: row.materialId || undefined,
      materialModel: row.purchasePn || undefined,
      materialBrand: row.purchaseBrand || undefined,
      warehouseId: row.warehouseId || undefined
    }
  })
}
</script>

<style scoped lang="scss">
.inv-list-qty {
  font-variant-numeric: tabular-nums;
}

.inv-list-dash {
  color: var(--crm-text-muted, #909399);
}

.outbound-status-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 56px;
  padding: 3px 10px;
  border-radius: 5px;
  font-size: 12px;
  line-height: 1.1;
  font-weight: 400;
  color: #fff;
  border: none;
  white-space: nowrap;
}

.outbound-status-chip--none,
.outbound-status-chip--unknown {
  background: #9ca3af;
}

.outbound-status-chip--partial {
  background: #e6a23c;
}

.outbound-status-chip--done {
  background: #67c23a;
}

.region-type-chip {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.region-type-chip--domestic {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.region-type-chip--overseas {
  color: #409eff;
  background: rgba(64, 158, 255, 0.14);
}

.stock-item-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.inv-stock-item-code-stocking-icon {
  color: #e6a23c;
  font-size: 14px;
}
</style>
