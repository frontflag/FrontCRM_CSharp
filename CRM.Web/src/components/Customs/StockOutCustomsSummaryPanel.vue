<script setup lang="ts">
import type { StockOutCustomsSummaryDto } from '@/api/stockOut'

defineProps<{
  summary?: StockOutCustomsSummaryDto | null
  /** 嵌入其它面板（如拣货单装箱信息）时不渲染外层 info-section 与标题 */
  embedded?: boolean
}>()

function brokerText(name?: string | null) {
  const s = (name ?? '').trim()
  return s || '—'
}
</script>

<template>
  <template v-if="summary?.declarationId">
    <div v-if="!embedded" class="info-section stock-out-customs-summary-section">
      <div class="section-header">
        <div class="section-header__main">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">{{ $t('stockOutDetail.customsSection') }}</span>
        </div>
        <div class="section-header__meta">
          <router-link
            :to="{ name: 'CustomsDeclarationDetail', params: { id: summary.declarationId } }"
            class="cell-link stock-out-customs-summary__hub-link"
          >
            {{ $t('stockOutDetail.viewFullCustomsInfo') }}
          </router-link>
        </div>
      </div>
      <div class="info-grid info-grid--inline-labels info-grid--basic stock-out-customs-summary__grid">
        <div class="info-item">
          <span class="info-label">{{ $t('stockOutDetail.customsDeclaration') }}</span>
          <span class="info-value">
            <router-link
              :to="{ name: 'CustomsDeclarationDetail', params: { id: summary.declarationId } }"
              class="cell-link"
            >
              {{ summary.declarationCode || summary.declarationId }}
            </router-link>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ $t('stockOutDetail.customsBroker') }}</span>
          <span class="info-value">{{ brokerText(summary.customsBrokerName) }}</span>
        </div>
        <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
      </div>
    </div>

    <div v-else class="stock-out-customs-summary-embedded">
      <div class="stock-out-customs-summary-embedded__header">
        <span class="stock-out-customs-summary-embedded__title">{{ $t('stockOutDetail.customsSection') }}</span>
        <router-link
          :to="{ name: 'CustomsDeclarationDetail', params: { id: summary.declarationId } }"
          class="cell-link stock-out-customs-summary__hub-link"
        >
          {{ $t('stockOutDetail.viewFullCustomsInfo') }}
        </router-link>
      </div>
      <div class="info-grid info-grid--inline-labels info-grid--basic stock-out-customs-summary__grid">
        <div class="info-item">
          <span class="info-label">{{ $t('stockOutDetail.customsDeclaration') }}</span>
          <span class="info-value">
            <router-link
              :to="{ name: 'CustomsDeclarationDetail', params: { id: summary.declarationId } }"
              class="cell-link"
            >
              {{ summary.declarationCode || summary.declarationId }}
            </router-link>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ $t('stockOutDetail.customsBroker') }}</span>
          <span class="info-value">{{ brokerText(summary.customsBrokerName) }}</span>
        </div>
        <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
      </div>
    </div>
  </template>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/business-detail-info-grid.scss';

.stock-out-customs-summary-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.stock-out-customs-summary__hub-link {
  font-size: 13px;
  white-space: nowrap;
}

.stock-out-customs-summary-embedded {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.stock-out-customs-summary-embedded__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 0;
  padding: 0 20px 8px;
}

.stock-out-customs-summary-embedded__title {
  font-size: 13px;
  font-weight: 500;
  color: $text-secondary;
}

.stock-out-customs-summary__grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.stock-out-customs-summary__grid .info-item {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.stock-out-customs-summary__grid .info-item--basic-spacer {
  border-right: none;
}

.stock-out-customs-summary__grid .info-label {
  flex-shrink: 0;
  white-space: nowrap;
  font-size: 12px;
  color: $text-secondary;
  text-transform: none;
  letter-spacing: 0;

  &::after {
    content: '：';
  }
}

.stock-out-customs-summary__grid .info-value {
  flex: 1;
  min-width: 0;
  word-break: break-word;
  font-size: 13px;
  color: $text-primary;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  flex-shrink: 0;
  margin-left: auto;
}

.section-title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}
</style>
