<template>
  <div class="so-aggregate-table-wrap sell-order-item-stock-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe>
      <el-table-column type="index" width="50" label="#" />
      <el-table-column min-width="170" :label="t('sellOrderItemStockTab.colStockItemCode')">
        <template #default="{ row }">
          <router-link class="so-tab-link" :to="`/inventory/stocks/${row.stockAggregateId}`">{{
            row.stockItemCode || row.id
          }}</router-link>
        </template>
      </el-table-column>
      <el-table-column :label="t('sellOrderItemStockTab.colStockType')" width="88" align="center">
        <template #default="{ row }">
          <span class="stock-type-chip" :class="`stock-type-chip--${stockItemTypeKind(row)}`">{{
            stockItemTypeLabel(row)
          }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="stockInCode" :label="t('sellOrderItemStockTab.colStockInCode')" min-width="150" show-overflow-tooltip />
      <el-table-column :label="t('sellOrderItemStockTab.colStockInDate')" width="160" prop="stockInDate">
        <template #default="{ row }">{{ row?.stockInDate ? formatDateTime(row.stockInDate) : '—' }}</template>
      </el-table-column>
      <el-table-column prop="warehouseName" :label="t('sellOrderItemStockTab.colWarehouse')" min-width="130" show-overflow-tooltip />
      <el-table-column :label="t('sellOrderItemStockTab.colRegion')" width="90" align="center">
        <template #default="{ row }">
          <span class="region-type-chip" :class="`region-type-chip--${stockRegionTypeKind(row?.regionType)}`">
            <span>{{ stockRegionTypeLabel(row?.regionType) }}</span>
          </span>
        </template>
      </el-table-column>
      <el-table-column :label="t('sellOrderItemStockTab.colStockOutStatus')" width="100" align="center">
        <template #default="{ row }">
          <span
            class="outbound-status-chip"
            :class="`outbound-status-chip--${stockOutboundStatusKind(row?.stockOutStatus)}`"
          >
            <span>{{ stockOutboundStatusLabel(row?.stockOutStatus) }}</span>
          </span>
        </template>
      </el-table-column>
      <el-table-column prop="purchasePn" label="PN" min-width="140" show-overflow-tooltip />
      <el-table-column prop="purchaseBrand" :label="t('sellOrderItemStockTab.colBrand')" width="120" show-overflow-tooltip />
      <el-table-column :label="t('sellOrderItemStockTab.colQtyInbound')" width="110" align="right" prop="qtyInbound" />
      <el-table-column :label="t('sellOrderItemStockTab.colQtyStockOut')" width="110" align="right" prop="qtyStockOut" />
      <el-table-column :label="t('sellOrderItemStockTab.colQtyRepertory')" width="100" align="right" prop="qtyRepertory" />
      <el-table-column
        prop="purchaseOrderItemCode"
        :label="t('sellOrderItemStockTab.colPoLine')"
        min-width="130"
        show-overflow-tooltip
      />
      <el-table-column
        prop="sellOrderItemCode"
        :label="t('sellOrderItemStockTab.colSoLine')"
        min-width="140"
        show-overflow-tooltip
      />
      <el-table-column prop="batchNo" :label="t('sellOrderItemStockTab.colBatchNo')" min-width="100" show-overflow-tooltip />
      <el-table-column prop="locationId" :label="t('sellOrderItemStockTab.colLocation')" min-width="110" show-overflow-tooltip />
      <el-table-column :label="t('sellOrderItemStockTab.colAvailable')" width="100" align="right" prop="qtyRepertoryAvailable" />
    </el-table>
    <el-empty v-else :description="emptyText ?? t('sellOrderItemStockTab.empty')" :image-size="64" />
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { SellOrderItemStockTabRow } from '@/api/salesOrder'

withDefaults(
  defineProps<{
    items: SellOrderItemStockTabRow[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()

function formatDateTime(v?: string | null | number) {
  return v != null && String(v).length > 0 ? formatDisplayDateTime(String(v)) : '—'
}

function stockRegionTypeLabel(regionType: unknown): string {
  const n = normalizeRegionType(regionType)
  return n === REGION_TYPE_OVERSEAS ? t('sellOrderItemStockTab.regionOverseas') : t('sellOrderItemStockTab.regionDomestic')
}

function stockRegionTypeKind(regionType: unknown): 'domestic' | 'overseas' {
  const n = normalizeRegionType(regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

function stockOutboundStatusLabel(status: unknown): string {
  const n = Number(status)
  if (n === 1) return t('sellOrderItemStockTab.outboundNone')
  if (n === 2) return t('sellOrderItemStockTab.outboundPartial')
  if (n === 3) return t('sellOrderItemStockTab.outboundDone')
  return '—'
}

function stockOutboundStatusKind(status: unknown): 'none' | 'partial' | 'done' | 'unknown' {
  const n = Number(status)
  if (n === 1) return 'none'
  if (n === 2) return 'partial'
  if (n === 3) return 'done'
  return 'unknown'
}

function stockItemTypeNum(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): number {
  const n = Number(row.stockType ?? 1)
  if (n >= 1 && n <= 3) return n
  return row.isStockingPoolMatch ? 2 : 1
}

function stockItemTypeLabel(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): string {
  const n = stockItemTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  return t('inventoryList.stockTypes.customer')
}

function stockItemTypeKind(row: { stockType?: unknown; isStockingPoolMatch?: unknown }): 'customer' | 'stocking' | 'sample' {
  const n = stockItemTypeNum(row)
  if (n === 2) return 'stocking'
  if (n === 3) return 'sample'
  return 'customer'
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-tab-link {
  color: $cyan-primary;
  text-decoration: none;
  font-weight: 500;
  &:hover {
    text-decoration: underline;
  }
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

.stock-type-chip {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.stock-type-chip--customer {
  color: #67c23a;
  background: rgba(103, 194, 58, 0.14);
}

.stock-type-chip--stocking {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.stock-type-chip--sample {
  color: #909399;
  background: rgba(144, 147, 153, 0.14);
}
</style>
