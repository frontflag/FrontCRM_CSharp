<template>
  <div class="so-item-line-performance-panel">
    <button
      type="button"
      class="so-item-line-performance-panel__head"
      :aria-expanded="!collapsed"
      @click="onToggleClick"
    >
      <span class="so-item-line-performance-panel__toggle-icon" :class="{ 'is-collapsed': collapsed }">▾</span>
      <span class="so-item-line-performance-panel__title">{{ t('salesOrderDetailView.performance.title') }}</span>
      <span class="so-item-line-performance-panel__code panel-hint__value">{{ sellOrderItemCode || '—' }}</span>
    </button>
    <div v-show="!collapsed" v-loading="loading" class="so-item-line-performance-panel__body">
      <template v-if="lineProfit">
        <div class="so-line-performance-vars">
          <div class="so-line-performance-vars__title">{{ t('salesOrderDetailView.performance.variables.title') }}</div>
          <div
            v-for="group in variableGroups"
            :key="group.key"
            class="so-line-performance-vars__group"
          >
            <div class="so-line-performance-vars__group-title">{{ group.title }}</div>
            <div class="so-line-performance-vars__grid">
              <div v-for="item in group.items" :key="item.key" class="so-line-performance-vars__item">
                <span class="so-line-performance-vars__label">{{ item.label }}</span>
                <span class="so-line-performance-vars__value">{{ item.value }}</span>
              </div>
            </div>
          </div>
        </div>
        <table class="so-line-performance">
          <colgroup>
            <col class="so-line-performance__col-equal" />
            <col class="so-line-performance__col-equal" />
            <col class="so-line-performance__col-equal" />
            <col class="so-line-performance__col-equal" />
          </colgroup>
          <thead>
            <tr>
              <th class="so-line-performance__col-head so-line-performance__col-head--blank" />
              <th class="so-line-performance__col-head">{{ t('salesOrderDetailView.performance.colProfit') }}</th>
              <th class="so-line-performance__col-head">{{ t('salesOrderDetailView.performance.colRate') }}</th>
              <th class="so-line-performance__col-head">{{ t('salesOrderDetailView.performance.colGrossMargin') }}</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="row in rows" :key="row.key">
              <tr>
                <td class="so-line-performance__cell so-line-performance__cell--layer">
                  <span class="so-line-performance__layer-label">{{ row.label }}</span>
                </td>
                <td class="so-line-performance__cell">
                  <span class="so-line-performance__metric amount-with-code">
                    <span>{{ formatUsdProfitAmount(row.profitUsd) }}</span>
                    <span
                      v-if="formatUsdProfitAmount(row.profitUsd) !== '—'"
                      class="dock-tier-ccy dock-tier-ccy--usd"
                    >USD</span>
                  </span>
                </td>
                <td class="so-line-performance__cell">
                  <span class="so-line-performance__metric">{{ row.rateText }}</span>
                </td>
                <td class="so-line-performance__cell">
                  <el-tooltip
                    v-if="row.grossMarginFormula"
                    :content="row.grossMarginFormula"
                    placement="top"
                  >
                    <span class="so-line-performance__metric">{{ row.grossMarginText }}</span>
                  </el-tooltip>
                  <span v-else class="so-line-performance__metric">{{ row.grossMarginText }}</span>
                </td>
              </tr>
              <tr class="so-line-performance__formula-row">
                <td colspan="4" class="so-line-performance__formula-cell">
                  <ul class="so-line-performance__formula-list">
                    <li v-for="line in row.formulas" :key="line.key" class="so-line-performance__formula-line">
                      {{ line.text }}
                    </li>
                  </ul>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
        <SellOrderOutboundCostDetailTable
          v-if="showOutboundCostDetails"
          :details="lineProfit.outboundCostDetails!"
          :total-cost-usd="lineProfit.outboundCostUsd"
        />
        <el-alert
          v-for="(hint, idx) in hints"
          :key="`${hint.level}-${idx}`"
          :type="hint.type"
          :closable="false"
          :title="hint.message"
          class="so-item-line-performance-panel__alert"
          show-icon
        />
      </template>
      <DetailListPanelEmpty v-else-if="!loading" size="low" :description="t('salesOrderDetailView.performance.empty')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { salesOrderApi, type SellOrderLineProfit } from '@/api/salesOrder'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import {
  computeGrossMarginPercent,
  formatGrossMarginDisplay,
  formatProfitRateMultiplierDisplay,
  formatUsdProfitAmount
} from '@/utils/sellOrderLineProfitDisplay'
import { buildSellOrderLineProfitHints } from '@/utils/sellOrderLineProfitHints'
import { buildSellOrderLineProfitLayerFormulas } from '@/utils/sellOrderLineProfitFormulas'
import { buildSellOrderLineProfitVariableGroups } from '@/utils/sellOrderLineProfitVariables'
import SellOrderOutboundCostDetailTable from '@/components/RFQ/SellOrderOutboundCostDetailTable.vue'

const props = defineProps<{
  salesOrderId?: string | null
  sellOrderItemId?: string | null
  sellOrderItemCode?: string | null
  /** 明细关联数据刷新时递增，用于在展开状态下重新拉取绩效 */
  refreshKey?: number
}>()

const { t } = useI18n()
const collapsed = ref(true)
const lineProfit = ref<SellOrderLineProfit | null>(null)
const loading = ref(false)
const loadedItemId = ref('')

const hints = computed(() => buildSellOrderLineProfitHints(lineProfit.value, t))

const showOutboundCostDetails = computed(() => {
  const p = lineProfit.value
  if (!p?.useActualOutboundCost) return false
  return (p.outboundCostDetails?.length ?? 0) > 0
})

const variableGroups = computed(() => {
  const p = lineProfit.value
  if (!p) return []
  return buildSellOrderLineProfitVariableGroups(p, t)
})

const layerFormulas = computed(() => {
  const p = lineProfit.value
  if (!p) return new Map<string, ReturnType<typeof buildSellOrderLineProfitLayerFormulas>[number]['lines']>()
  const layers = buildSellOrderLineProfitLayerFormulas(p, t)
  return new Map(layers.map((layer) => [layer.key, layer.lines]))
})

const rows = computed(() => {
  const p = lineProfit.value
  if (!p) return []
  const revenue = p.revenueUsd
  const formulaMap = layerFormulas.value

  const buildRow = (
    key: 'quote' | 'salesExpected' | 'outbound',
    label: string,
    profitUsd: number,
    profitRate: number | null | undefined,
    grossRevenue: number
  ) => {
    const grossMarginPct = formatGrossMargin(profitUsd, grossRevenue)
    const grossMarginFormula =
      grossMarginPct != null
        ? (formulaMap.get(key)?.find((line) => line.key === 'grossMargin')?.text ??
          t('salesOrderDetailView.performance.grossMarginTooltip', { pct: grossMarginPct }))
        : ''
    return {
      key,
      label,
      profitUsd,
      rateText: formatProfitRateMultiplierDisplay(profitUsd, profitRate),
      grossMarginText: formatGrossMarginDisplay(profitUsd, grossRevenue),
      grossMarginPct,
      grossMarginFormula,
      formulas: formulaMap.get(key) ?? []
    }
  }

  return [
    buildRow('quote', t('salesOrderDetailView.performance.layerQuote'), p.quote.profitUsd, p.quote.profitRate, revenue),
    buildRow(
      'salesExpected',
      t('salesOrderDetailView.performance.layerSalesExpected'),
      p.salesExpected.profitUsd,
      p.salesExpected.profitRate,
      revenue
    ),
    buildRow(
      'outbound',
      t('salesOrderDetailView.performance.layerOutbound'),
      p.outbound.profitUsd,
      p.outbound.profitRate,
      p.outboundRevenueUsd
    )
  ]
})

function resetLineProfitCache() {
  lineProfit.value = null
  loadedItemId.value = ''
}

async function ensureLineProfitLoaded(force = false) {
  const orderId = String(props.salesOrderId ?? '').trim()
  const itemId = String(props.sellOrderItemId ?? '').trim()
  if (!orderId || !itemId) {
    resetLineProfitCache()
    return
  }
  if (!force && loadedItemId.value === itemId && lineProfit.value) return

  loading.value = true
  try {
    lineProfit.value = (await salesOrderApi.getSellOrderItemLineProfit(orderId, itemId)) ?? null
    loadedItemId.value = itemId
  } catch {
    resetLineProfitCache()
  } finally {
    loading.value = false
  }
}

function onToggleClick() {
  const expanding = collapsed.value
  collapsed.value = !collapsed.value
  if (expanding) {
    void ensureLineProfitLoaded()
  }
}

watch(
  () => props.sellOrderItemId,
  (itemId, prevItemId) => {
    if (itemId === prevItemId) return
    resetLineProfitCache()
    if (!collapsed.value && itemId) {
      void ensureLineProfitLoaded()
    }
  }
)

watch(
  () => props.refreshKey,
  () => {
    resetLineProfitCache()
    if (!collapsed.value) {
      void ensureLineProfitLoaded(true)
    }
  }
)

function formatGrossMargin(profitUsd: number, revenueUsd: number): string | null {
  const pct = computeGrossMarginPercent(profitUsd, revenueUsd)
  if (pct == null) return null
  return pct.toFixed(2)
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-line-performance-panel {
  margin-top: 12px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: var(--crm-detail-panel-card-bg);
  overflow: hidden;
}

.so-item-line-performance-panel__head {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  width: 100%;
  padding: 12px 16px;
  border: none;
  border-bottom: 1px solid $border-panel;
  background: var(--crm-detail-panel-card-head-bg);
  cursor: pointer;
  text-align: left;
  color: inherit;
  font: inherit;

  &:hover {
    background: var(--crm-detail-section-header-bg);
  }
}

.so-item-line-performance-panel__toggle-icon {
  display: inline-block;
  font-size: 12px;
  line-height: 1;
  color: var(--crm-table-header-text);
  transition: transform 0.15s ease;
  &.is-collapsed {
    transform: rotate(-90deg);
  }
}

.so-item-line-performance-panel__title {
  font-size: 14px;
  font-weight: 600;
  color: var(--crm-text-primary);
}

.so-item-line-performance-panel__code {
  font-size: 14px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: $color-amber;
}

.so-item-line-performance-panel__body {
  padding: 12px 16px 16px;
  min-height: 48px;
}

.so-item-line-performance-panel__alert {
  margin-top: 10px;
}

.so-line-performance-vars {
  margin-bottom: 12px;
  padding: 10px 12px;
  border: 1px dashed rgba(37, 99, 235, 0.28);
  border-radius: $border-radius-sm;
  background: var(--crm-card-bg);
}

.so-line-performance-vars__title {
  font-size: 13px;
  font-weight: 600;
  color: var(--crm-text-primary);
  margin-bottom: 10px;
}

.so-line-performance-vars__group + .so-line-performance-vars__group {
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid var(--crm-table-cell-line);
}

.so-line-performance-vars__group-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--crm-table-header-text);
  margin-bottom: 6px;
}

.so-line-performance-vars__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 6px 16px;
}

.so-line-performance-vars__item {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  column-gap: 8px;
  row-gap: 2px;
  min-width: 0;
  font-size: 12px;
  line-height: 1.5;
}

.so-line-performance-vars__label {
  flex: 0 0 auto;
  color: var(--crm-table-header-text);
  white-space: nowrap;
}

.so-line-performance-vars__value {
  flex: 1 1 auto;
  min-width: max-content;
  font-variant-numeric: tabular-nums;
  color: var(--crm-table-text);
  font-weight: 500;
  white-space: nowrap;
}

.so-line-performance {
  width: 100%;
  table-layout: fixed;
  border-collapse: collapse;
  font-size: 14px;
  line-height: 1.45;
  color: var(--crm-table-text);

  th,
  td {
    padding: 10px 12px;
    vertical-align: middle;
    border-left: none;
    border-right: none;
    border-top: none;
    border-bottom: 1px solid var(--crm-table-cell-line);
  }
}

.so-line-performance__col-equal {
  width: 25%;
}

.so-line-performance__col-head {
  text-align: center;
  font-weight: 500;
  white-space: nowrap;
  background: var(--crm-detail-section-header-bg);
  color: var(--crm-table-header-text);
}

.so-line-performance__col-head--blank {
  background: var(--crm-detail-section-header-bg);
}

.so-line-performance__cell {
  text-align: center;
  color: var(--crm-table-text);
  font-weight: 500;
  background: var(--crm-card-bg);
  vertical-align: middle;
}

.so-line-performance__cell--layer {
  background: var(--crm-card-bg);
  text-align: left;
}

.so-line-performance__layer-label {
  font-weight: 600;
  color: var(--crm-table-text);
  display: inline-block;
}

.so-line-performance__metric {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-variant-numeric: tabular-nums;
  color: var(--crm-table-text);
}

.so-line-performance .amount-with-code {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
}

.so-line-performance .amount-with-code > span:first-child {
  color: var(--crm-table-text);
}

.so-line-performance__formula-row {
  .so-line-performance__formula-cell {
    padding: 8px 12px 12px;
    background: var(--crm-detail-section-header-bg);
    text-align: left;
  }
}

.so-line-performance__formula-list {
  margin: 0;
  padding: 0;
  list-style: none;
  text-align: left;
}

.so-line-performance__formula-line {
  font-size: 12px;
  line-height: 1.55;
  font-variant-numeric: tabular-nums;
  color: var(--crm-table-text);
  word-break: break-word;

  & + & {
    margin-top: 4px;
  }
}
</style>
