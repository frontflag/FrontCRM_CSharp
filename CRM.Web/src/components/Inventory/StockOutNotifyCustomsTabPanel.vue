<template>
  <div class="so-item-ops-root so-item-ops-root--embedded" aria-label="stock-out-notify-customs-tab-panel">
    <div v-if="!notifyRow" class="so-item-ops-root__empty">
      {{ t('stockOutNotifyList.customsTab.pickRow') }}
    </div>

    <div v-else-if="noDeclaration" class="so-item-ops-root__empty">
      {{ t('stockOutNotifyList.customsTab.noDeclaration') }}
    </div>

    <div
      v-else
      v-loading="loading"
      class="so-item-ops-root__content so-item-ops-root__content--embedded"
    >
      <p v-if="loadError" class="so-item-ops-root__error">{{ loadError }}</p>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutNotifyList.customsTab.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-kv ops-kv--declaration-row">
            <div class="ops-kv__main">
              <span class="ops-kv__label">{{ t('stockOutNotifyList.customsTab.declarationCode') }}</span>
              <span class="ops-kv__value ops-kv__value--declaration-code">{{ declarationCode }}</span>
            </div>
            <router-link
              v-if="canAccessCustoms && declarationId"
              :to="{ name: 'CustomsDeclarationDetail', params: { id: declarationId } }"
              class="cell-link stock-out-notify-customs-tab__view-link"
            >
              {{ t('stockOutNotifyList.customsTab.viewDeclaration') }}
            </router-link>
          </div>
          <div class="ops-kv">
            <span class="ops-kv__label">{{ t('stockOutNotifyList.customsTab.customsBroker') }}</span>
            <span class="ops-kv__value">{{ brokerDisplay }}</span>
          </div>
          <div class="ops-kv">
            <span class="ops-kv__label">{{ t('stockOutNotifyList.customsTab.declareDate') }}</span>
            <span class="ops-kv__value">{{ declareDateText }}</span>
          </div>
          <div class="ops-overview-line ops-overview-line--route">{{ warehouseRoute }}</div>
        </div>
      </section>

      <section v-if="detail" class="ops-card ops-card--status-only">
        <div class="ops-card__body ops-card__body--status">
          <div class="ops-status-tags">
            <div class="ops-status-tags__row">
              <el-tag effect="dark" :type="internalTagType" size="small">
                {{ internalLabel }}
              </el-tag>
              <el-tag effect="dark" :type="clearanceTagType" size="small">
                {{ clearanceLabel }}
              </el-tag>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CustomsDeclarationDetailDto } from '@/api/customs'
import type { StockOutRequestDto } from '@/api/stockOut'

const props = defineProps<{
  notifyRow?: StockOutRequestDto | null
  detail?: CustomsDeclarationDetailDto | null
  loading?: boolean
  loadError?: string
  noDeclaration?: boolean
  canAccessCustoms?: boolean
}>()

const { t } = useI18n()

const declarationId = computed(() => {
  const fromDetail = props.detail?.id?.trim()
  if (fromDetail) return fromDetail
  return String(props.notifyRow?.customsDeclarationId ?? '').trim()
})

const declarationCode = computed(() => {
  const fromDetail = props.detail?.declarationCode?.trim()
  if (fromDetail) return fromDetail
  const fromRow = String(props.notifyRow?.customsDeclarationCode ?? '').trim()
  if (fromRow) return fromRow
  return declarationId.value || '—'
})

const brokerDisplay = computed(() => {
  const d = props.detail
  if (d) return d.customsBrokerName || d.customsBrokerCode || d.customsBrokerId || '—'
  return String(props.notifyRow?.customsBrokerName ?? '').trim() || '—'
})

const declareDateText = computed(() => {
  const raw = props.detail?.declareDate
  if (!raw) return '—'
  const s = String(raw)
  return s.includes('T') ? s.slice(0, 10) : s.slice(0, 10)
})

const warehouseRoute = computed(() => {
  const d = props.detail
  if (!d) return '—'
  const from = (d.fromWarehouseName ?? d.fromWarehouseCode ?? d.fromWarehouseId ?? '').trim()
  const to = (d.toWarehouseName ?? d.toWarehouseCode ?? d.toWarehouseId ?? '').trim()
  if (from && to) return `${from} → ${to}`
  return from || to || '—'
})

const internalStatus = computed(() => Number(props.detail?.internalStatus ?? 0))

const clearanceStatus = computed(() => Number(props.detail?.customsClearanceStatus ?? 0))

const internalLabel = computed(() => {
  const v = internalStatus.value
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
})

const clearanceLabel = computed(() => {
  const v = clearanceStatus.value
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
})

const internalTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = internalStatus.value
  if (s === 3) return 'success'
  if (s === 2) return 'warning'
  if (s === -1) return 'danger'
  return 'info'
})

const clearanceTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = clearanceStatus.value
  if (s === 100) return 'success'
  if (s === 10) return 'warning'
  return 'info'
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-ops-root--embedded {
  flex: 1;
  width: 100%;
  min-width: 0;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow: visible;
}

.so-item-ops-root--embedded .so-item-ops-root__empty,
.so-item-ops-root--embedded .so-item-ops-root__error {
  color: var(--crm-aux-placeholder);
}

.so-item-ops-root__content--embedded {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0;
  overflow: visible;
}

.so-item-ops-root--embedded .ops-card {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 12px;
  box-shadow: none;
}

.so-item-ops-root--embedded .ops-card__head,
.so-item-ops-root--embedded .ops-card__body {
  min-width: 0;
}

.so-item-ops-root--embedded .ops-card__title {
  color: var(--crm-aux-body-text);
}

.ops-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 14px 8px;
}

.ops-card__title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.ops-card__body--overview {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0 14px 14px;
}

.ops-kv {
  display: flex;
  align-items: flex-start;
  gap: 4px;
  font-size: 13px;
  line-height: 1.5;
  min-width: 0;
}

.ops-kv--declaration-row {
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.ops-kv__main {
  display: flex;
  align-items: flex-start;
  gap: 4px;
  min-width: 0;
  flex: 1;
}

.stock-out-notify-customs-tab__view-link {
  flex-shrink: 0;
  font-size: 13px;
  white-space: nowrap;
}

.ops-kv__label {
  flex-shrink: 0;
  color: var(--crm-aux-hint);
  white-space: nowrap;

  &::after {
    content: '：';
  }
}

.ops-kv__value {
  flex: 1;
  min-width: 0;
  color: var(--crm-aux-body-text);
  word-break: break-word;
}

.ops-kv__value--declaration-code {
  color: $color-amber;
  font-weight: 600;
  font-size: 13px;
}

.ops-overview-line {
  font-size: 13px;
  color: var(--crm-aux-body-text);
  word-break: break-word;
}

.ops-overview-line--route {
  margin-top: 2px;
}

.ops-card--status-only .ops-card__body--status {
  padding: 12px 14px;
}

.ops-status-tags__row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
