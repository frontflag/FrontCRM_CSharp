<template>
  <el-collapse v-model="activeNames" class="so-outbound-cost-details">
    <el-collapse-item name="details">
      <template #title>
        <span class="so-outbound-cost-details__title">
          {{ t('salesOrderDetailView.performance.outboundCostDetails.title') }}
        </span>
        <span v-if="summaryText" class="so-outbound-cost-details__summary">{{ summaryText }}</span>
      </template>
      <el-table
        :data="details"
        size="small"
        border
        class="so-outbound-cost-details__table"
        :empty-text="t('salesOrderDetailView.performance.outboundCostDetails.empty')"
      >
        <el-table-column
          :label="t('salesOrderDetailView.performance.outboundCostDetails.colStockOut')"
          min-width="108"
        >
          <template #default="{ row }">
            <router-link
              v-if="row.stockOutId"
              class="so-outbound-cost-details__link"
              :to="`/inventory/stock-out/${encodeURIComponent(row.stockOutId)}`"
            >
              {{ row.stockOutCode || row.stockOutId }}
            </router-link>
            <span v-else>{{ row.stockOutCode || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column
          :label="t('salesOrderDetailView.performance.outboundCostDetails.colPoItem')"
          min-width="108"
        >
          <template #default="{ row }">
            {{ row.purchaseOrderItemCode || row.purchaseOrderItemId || '—' }}
          </template>
        </el-table-column>
        <el-table-column
          :label="t('salesOrderDetailView.performance.outboundCostDetails.colPurchasePrice')"
          min-width="120"
          align="right"
        >
          <template #default="{ row }">{{ formatUnitUsd(row.purchasePriceUsd) }}</template>
        </el-table-column>
        <el-table-column
          :label="t('salesOrderDetailView.performance.outboundCostDetails.colQty')"
          min-width="88"
          align="right"
        >
          <template #default="{ row }">{{ formatQty(row.qty) }}</template>
        </el-table-column>
        <el-table-column
          :label="t('salesOrderDetailView.performance.outboundCostDetails.colCost')"
          min-width="112"
          align="right"
        >
          <template #default="{ row }">{{ formatUsd2(row.costUsd) }}</template>
        </el-table-column>
      </el-table>
      <div v-if="details.length" class="so-outbound-cost-details__footer">
        <span class="so-outbound-cost-details__footer-label">
          {{ t('salesOrderDetailView.performance.outboundCostDetails.totalCost') }}
        </span>
        <span class="so-outbound-cost-details__footer-value">{{ formatUsd2(totalCostUsd) }}</span>
      </div>
    </el-collapse-item>
  </el-collapse>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SellOrderLineOutboundCostDetail } from '@/api/salesOrder'

const props = defineProps<{
  details: SellOrderLineOutboundCostDetail[]
  totalCostUsd: number
}>()

const { t } = useI18n()
const activeNames = ref<string[]>([])

const summaryText = computed(() => {
  const rows = props.details.length
  if (rows <= 0) return ''
  const stockOuts = new Set(
    props.details.map((d) => (d.stockOutId || d.stockOutCode || '').trim()).filter(Boolean)
  ).size
  return t('salesOrderDetailView.performance.outboundCostDetails.summary', {
    stockOuts,
    rows
  })
})

function formatQty(value: number): string {
  if (!Number.isFinite(value)) return '—'
  if (Number.isInteger(value)) return String(value)
  return value.toFixed(4).replace(/\.?0+$/, '')
}

function formatUnitUsd(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return `${value.toFixed(6)} USD`
}

function formatUsd2(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return `${value.toFixed(2)} USD`
}
</script>

<style scoped lang="scss">
.so-outbound-cost-details {
  margin-top: 12px;
  border: 1px dashed rgba(37, 99, 235, 0.22);
  border-radius: 4px;
  background: var(--crm-card-bg);

  :deep(.el-collapse-item__header) {
    height: auto;
    min-height: 36px;
    line-height: 1.45;
    padding: 6px 12px;
    font-size: 12px;
    background: transparent;
    border-bottom: none;
  }

  :deep(.el-collapse-item__wrap) {
    border-bottom: none;
  }

  :deep(.el-collapse-item__content) {
    padding: 0 12px 10px;
  }
}

.so-outbound-cost-details__title {
  font-weight: 600;
  color: var(--crm-text-primary);
}

.so-outbound-cost-details__summary {
  margin-left: 8px;
  font-weight: 400;
  color: var(--crm-table-header-text);
}

.so-outbound-cost-details__table {
  width: 100%;
  font-size: 12px;
}

.so-outbound-cost-details__link {
  color: var(--el-color-primary);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.so-outbound-cost-details__footer {
  display: flex;
  justify-content: flex-end;
  align-items: baseline;
  gap: 8px;
  margin-top: 8px;
  padding-top: 6px;
  border-top: 1px solid var(--crm-table-cell-line);
  font-size: 12px;
}

.so-outbound-cost-details__footer-label {
  color: var(--crm-table-header-text);
}

.so-outbound-cost-details__footer-value {
  font-variant-numeric: tabular-nums;
  font-weight: 600;
  color: var(--crm-table-text);
}
</style>
