<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { listAmountCurrencyDockClass } from '@/utils/moneyFormat'

export type AnalyticsKpiCurrencyItem = {
  currencyLabel: string
  /** 完整展示文案（如金额+币别）；split 布局可用 */
  originalText: string
  /** 仅金额数字；与 currency 搭配时 Tip 尾部币别用系统色 */
  amountText?: string
  /** 币别 short 编码，用于 dock-tier-ccy 色 */
  currency?: number
  usdText: string
  /** 原币行右侧「查看」（如财务分析已付款下钻） */
  showView?: boolean
  viewLabel?: string
}

export type AnalyticsKpiItem = {
  key: string
  label: string
  value: string
  /** 原币分档行（如 CNY 1,234.56），显示在折算 USD 下方 */
  currencyLines?: string[]
  /** 结构化原币分档（横向卡片，与 layout=split 搭配） */
  currencyItems?: AnalyticsKpiCurrencyItem[]
  /** 原币区小标题（如「原币」） */
  currencyCaption?: string
  /** 主金额上方小标题（如「折算 USD」） */
  valueCaption?: string
  /** 主金额右侧后缀（如「（折算美金）」），常规字重 */
  valueSuffix?: string
  /** stack：竖向（默认）；split：左侧总额 + 右侧原币横排 */
  layout?: 'stack' | 'split'
  /** 覆盖默认栅格跨度；split 默认 span 3，可设 2 以便两张金额卡并排 */
  gridColumnSpan?: number
  /** 强制从新行起排（如统计概览「销售客户数」后的金额卡） */
  forceNewRow?: boolean
  tone?: 'todo' | 'snapshot'
  /** 整卡可点（有 showDetail 时勿开，避免与按钮冲突） */
  drillable?: boolean
  /** money：缩小字号并禁止折行，适配 12 位整数金额 */
  valueFormat?: 'money' | 'text'
  showDefinition?: boolean
  definitionLabel?: string
  definitionChart?: string
  definitionDataSource?: string
  definitionText?: string
  showDetail?: boolean
  detailLabel?: string
  /** 标签行「查看本币」Tip：展示 currencyItems 原币分档 */
  showCurrencyTip?: boolean
  currencyTipLabel?: string
}

defineProps<{
  items: AnalyticsKpiItem[]
}>()

const emit = defineEmits<{
  'item-click': [key: string]
  detail: [key: string]
  'currency-view': [key: string, currency: number]
}>()

const { t } = useI18n()

function isCardDrillable(item: AnalyticsKpiItem): boolean {
  return !!item.drillable && !item.showDetail
}

function onCardClick(item: AnalyticsKpiItem) {
  if (isCardDrillable(item)) emit('item-click', item.key)
}

function onDetailClick(item: AnalyticsKpiItem, e: Event) {
  e.stopPropagation()
  emit('detail', item.key)
}

function onCurrencyViewClick(item: AnalyticsKpiItem, cur: AnalyticsKpiCurrencyItem, e: Event) {
  e.stopPropagation()
  if (cur.currency == null) return
  emit('currency-view', item.key, cur.currency)
}

function splitSpanClass(item: AnalyticsKpiItem): string | undefined {
  if (item.layout !== 'split' && !item.gridColumnSpan) return undefined
  const span = item.gridColumnSpan ?? 3
  if (span === 2) return 'kpi-card--span-2'
  if (span >= 3) return 'kpi-card--span-3'
  return undefined
}

function currencyDockClass(cur: AnalyticsKpiCurrencyItem): string {
  return listAmountCurrencyDockClass(cur.currency)
}
</script>

<template>
  <div class="kpi-grid">
    <div
      v-for="item in items"
      :key="item.key"
      class="kpi-card"
      :class="[
        {
          'kpi-card--todo': item.tone === 'todo',
          'kpi-card--clickable': isCardDrillable(item),
          'kpi-card--split': item.layout === 'split',
          'kpi-card--new-row': item.forceNewRow
        },
        splitSpanClass(item)
      ]"
      :role="isCardDrillable(item) ? 'button' : undefined"
      :tabindex="isCardDrillable(item) ? 0 : undefined"
      @click="onCardClick(item)"
      @keydown.enter="onCardClick(item)"
    >
      <div class="kpi-label-row">
        <span class="kpi-label">{{ item.label }}</span>
        <div
          v-if="item.showDefinition || item.showDetail || item.showCurrencyTip"
          class="kpi-actions"
          @click.stop
        >
          <el-popover
            v-if="item.showDefinition"
            placement="bottom-end"
            :width="360"
            trigger="click"
          >
            <template #reference>
              <el-button link type="primary" size="small" class="kpi-action-btn">
                {{ item.definitionLabel || t('salesAnalytics.definitionTip.button') }}
              </el-button>
            </template>
            <div class="definition-tip">
              <div v-if="item.definitionChart" class="definition-tip__row">
                <span class="definition-tip__label">{{ t('salesAnalytics.definitionTip.metric') }}</span>
                <span>{{ item.definitionChart }}</span>
              </div>
              <div v-if="item.definitionDataSource" class="definition-tip__row">
                <span class="definition-tip__label">{{ t('salesAnalytics.definitionTip.dataSource') }}</span>
                <span>{{ item.definitionDataSource }}</span>
              </div>
              <div v-if="item.definitionText" class="definition-tip__row">
                <span class="definition-tip__label">{{ t('salesAnalytics.definitionTip.definition') }}</span>
                <span class="definition-tip__text">{{ item.definitionText }}</span>
              </div>
            </div>
          </el-popover>
          <el-popover
            v-if="item.showCurrencyTip"
            placement="bottom-end"
            :width="280"
            trigger="click"
          >
            <template #reference>
              <el-button link type="primary" size="small" class="kpi-action-btn">
                {{ item.currencyTipLabel || t('salesAnalytics.kpi.viewLocalCurrency') }}
              </el-button>
            </template>
            <div class="currency-tip">
              <div class="currency-tip__title">
                {{ item.currencyCaption || t('salesAnalytics.kpi.originalCaption') }}
              </div>
              <template v-if="item.currencyItems?.length">
                <div
                  v-for="cur in item.currencyItems"
                  :key="`${item.key}-tip-${cur.currencyLabel}`"
                  class="currency-tip__row"
                >
                  <span class="currency-tip__label">{{ cur.currencyLabel }}</span>
                  <span class="currency-tip__value">
                    <template v-if="cur.amountText">
                      <span>{{ cur.amountText }}</span>
                      <span class="dock-tier-ccy-gap">&nbsp;</span>
                      <span :class="['dock-tier-ccy', currencyDockClass(cur)]">{{ cur.currencyLabel }}</span>
                    </template>
                    <template v-else>{{ cur.originalText }}</template>
                  </span>
                </div>
              </template>
              <div v-else class="currency-tip__empty">
                {{ t('salesAnalytics.kpi.viewLocalCurrencyEmpty') }}
              </div>
            </div>
          </el-popover>
          <el-button
            v-if="item.showDetail"
            link
            type="primary"
            size="small"
            class="kpi-action-btn"
            @click="onDetailClick(item, $event)"
          >
            {{ item.detailLabel || t('salesAnalytics.stockOutProgressDetail.detail') }}
          </el-button>
        </div>
      </div>

      <div v-if="item.layout === 'split'" class="kpi-split-body">
        <div class="kpi-split-col kpi-split-col--main">
          <span v-if="item.valueCaption" class="kpi-split-caption">{{ item.valueCaption }}</span>
          <span class="kpi-value kpi-value--split-money">{{ item.value }}</span>
        </div>
        <div
          v-for="cur in item.currencyItems ?? []"
          :key="`${item.key}-${cur.currencyLabel}`"
          class="kpi-split-col kpi-split-col--currency"
        >
          <span class="kpi-currency-item-label">{{ cur.currencyLabel }}</span>
          <div class="kpi-currency-card">
            <span class="kpi-currency-card-original">
              <template v-if="cur.amountText">
                <span>{{ cur.amountText }}</span>
                <span class="dock-tier-ccy-gap">&nbsp;</span>
                <span :class="['dock-tier-ccy', currencyDockClass(cur)]">{{ cur.currencyLabel }}</span>
              </template>
              <template v-else>{{ cur.originalText }}</template>
            </span>
            <el-button
              v-if="cur.showView && cur.currency != null"
              link
              type="primary"
              size="small"
              class="kpi-currency-view-btn"
              @click="onCurrencyViewClick(item, cur, $event)"
            >
              {{ cur.viewLabel || t('common.view') }}
            </el-button>
          </div>
        </div>
      </div>

      <div v-else class="kpi-amount-block">
        <span v-if="item.valueCaption" class="kpi-value-caption">{{ item.valueCaption }}</span>
        <div class="kpi-value-row">
          <span
            class="kpi-value"
            :class="{ 'kpi-value--money': item.valueFormat === 'money' }"
          >{{ item.value }}</span>
          <span v-if="item.valueSuffix" class="kpi-value-suffix">{{ item.valueSuffix }}</span>
        </div>
        <div v-if="item.currencyItems?.length || item.currencyLines?.length" class="kpi-currencies">
          <span v-if="item.currencyCaption" class="kpi-currency-caption">{{ item.currencyCaption }}</span>
          <template v-if="item.currencyItems?.length">
            <span
              v-for="cur in item.currencyItems"
              :key="`${item.key}-cur-item-${cur.currencyLabel}`"
              class="kpi-currency-line"
              :class="{ 'kpi-currency-line--with-view': cur.showView && cur.currency != null }"
            >
              <template v-if="cur.amountText">
                <span class="kpi-currency-line__amount">{{ cur.amountText }}</span>
                <span class="dock-tier-ccy-gap">&nbsp;</span>
                <span :class="['dock-tier-ccy', currencyDockClass(cur)]">{{ cur.currencyLabel }}</span>
              </template>
              <template v-else>{{ cur.originalText }}</template>
              <el-button
                v-if="cur.showView && cur.currency != null"
                link
                type="primary"
                size="small"
                class="kpi-currency-view-btn"
                @click="onCurrencyViewClick(item, cur, $event)"
              >
                {{ cur.viewLabel || t('common.view') }}
              </el-button>
            </span>
          </template>
          <template v-else>
            <span
              v-for="(line, idx) in item.currencyLines"
              :key="`${item.key}-cur-${idx}`"
              class="kpi-currency-line"
            >{{ line }}</span>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 12px;
}

.kpi-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 14px 16px;
}

.kpi-card--new-row {
  grid-column-start: 1;
}

.kpi-card--todo {
  border-color: var(--el-color-warning-light-5);
  background: var(--el-color-warning-light-9);
}

.kpi-card--clickable {
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;

  &:hover {
    border-color: var(--el-color-primary-light-5);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  }
}

.kpi-label-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 6px;
}

.kpi-label {
  display: block;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  line-height: 1.4;
  min-width: 0;
}

.kpi-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.kpi-action-btn {
  flex-shrink: 0;
}

.definition-tip {
  font-size: 13px;
  line-height: 1.5;
  color: var(--el-text-color-primary);
}

.definition-tip__row {
  display: grid;
  grid-template-columns: 64px 1fr;
  gap: 8px;
  margin-bottom: 8px;

  &:last-child {
    margin-bottom: 0;
  }
}

.definition-tip__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.definition-tip__text {
  white-space: pre-line;
}

.currency-tip {
  font-size: 13px;
  line-height: 1.5;
  color: var(--el-text-color-primary);
}

.currency-tip__title {
  margin-bottom: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.currency-tip__row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 6px;

  &:last-child {
    margin-bottom: 0;
  }
}

.currency-tip__label {
  flex-shrink: 0;
  color: var(--el-text-color-secondary);
}

.currency-tip__value {
  font-variant-numeric: tabular-nums;
  text-align: right;
  color: var(--el-text-color-primary);
}

.currency-tip__empty {
  color: var(--el-text-color-secondary);
}

.kpi-amount-block {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.kpi-value-caption {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  line-height: 1.2;
}

.kpi-value-row {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 4px;
}

.kpi-value {
  display: block;
  font-size: 22px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  line-height: 1.25;
}

.kpi-value-suffix {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  line-height: 1.25;
  white-space: nowrap;
}

.kpi-value--money {
  font-size: 13px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  letter-spacing: -0.02em;
}

.kpi-card--todo .kpi-value--money {
  font-size: 15px;
}

.kpi-currencies {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin-top: 4px;
  padding-top: 6px;
  border-top: 1px dashed var(--el-border-color-lighter);
}

.kpi-currency-caption {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-bottom: 2px;
}

.kpi-currency-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  font-weight: 500;
  font-variant-numeric: tabular-nums;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  letter-spacing: -0.01em;
}

.kpi-currency-line--with-view {
  width: 100%;
}

.kpi-currency-line__amount {
  color: inherit;
}

.kpi-card--todo .kpi-currency-line {
  color: var(--el-text-color-regular);
}

.kpi-card--split {
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.kpi-card--span-3 {
  grid-column: span 3;
}

.kpi-card--span-2 {
  grid-column: span 2;
}

@media (max-width: 1100px) {
  .kpi-card--span-3 {
    grid-column: span 2;
  }
}

@media (max-width: 900px) {
  .kpi-card--span-3,
  .kpi-card--span-2 {
    grid-column: span 1;
  }
}

.kpi-split-body {
  display: flex;
  align-items: stretch;
  gap: 0;
  flex: 1;
  min-height: 72px;
}

.kpi-split-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
  min-height: 0;
}

.kpi-split-col--main {
  flex: 0 0 auto;
  min-width: 180px;
  padding-right: 20px;
  border-right: 1px solid var(--el-border-color-lighter);
}

.kpi-split-col--main + .kpi-split-col--currency {
  margin-left: 20px;
}

.kpi-split-col--currency + .kpi-split-col--currency {
  margin-left: 50px;
}

.kpi-split-col--currency {
  flex: 0 0 auto;
  width: max-content;
  min-width: max-content;
}

.kpi-split-caption,
.kpi-currency-item-label {
  font-size: 11px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
  line-height: 14px;
  height: 14px;
  flex-shrink: 0;
  margin-top: 7px;
}

.kpi-value--split-money {
  font-size: 18px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  letter-spacing: -0.02em;
  line-height: 1.2;
  flex: 1;
  display: flex;
  align-items: center;
}

.kpi-currency-card {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1 0 auto;
  width: max-content;
  min-height: 0;
  padding: 0;
  box-sizing: border-box;
}

.kpi-currency-view-btn {
  flex-shrink: 0;
  margin-left: auto;
  padding: 0;
  height: auto;
  min-height: 0;
  font-size: 12px;
}

.kpi-currency-card-original {
  font-size: 14px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--el-text-color-primary);
  white-space: nowrap;
}
</style>
