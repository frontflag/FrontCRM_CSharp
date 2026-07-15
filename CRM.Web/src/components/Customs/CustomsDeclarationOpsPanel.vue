<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="customs-declaration-ops-panel"
  >
    <header v-if="!embedded" class="so-item-ops-panel__head">
      <h2 class="so-item-ops-panel__title">{{ t('customsPages.declarations.opsPanel.title') }}</h2>
      <button type="button" class="so-item-ops-panel__close" @click="emit('close')">
        {{ t('customsPages.declarations.opsPanel.close') }}
      </button>
    </header>

    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('customsPages.declarations.opsPanel.pickRow') }}
    </div>

    <div
      v-else
      v-loading="loading"
      class="so-item-ops-root__content"
      :class="embedded ? 'so-item-ops-root__content--embedded' : 'so-item-ops-panel__body'"
    >
      <p v-if="loadError" class="so-item-ops-root__error">{{ loadError }}</p>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('customsPages.declarations.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">{{ declarationCode }}</div>
          <div class="ops-overview-line">{{ brokerDisplay }}</div>
          <div class="ops-overview-line">{{ declareDateText }}</div>
          <div class="ops-overview-line">{{ warehouseRoute }}</div>
          <div class="ops-overview-line">{{ sorDisplay }}</div>
        </div>
      </section>

      <section class="ops-card ops-card--status-only">
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

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('customsPages.declarations.opsPanel.clearanceTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <div class="ops-kv">
            <span class="ops-kv__label">{{ t('customsPages.declarations.colClearance') }}</span>
            <span class="ops-kv__value ops-kv__value--accent">{{ clearanceLabel }}</span>
          </div>
          <p v-if="isVoided" class="ops-status ops-status--warn">{{ t('customsPages.declarations.opsPanel.voidedHint') }}</p>
          <button
            v-if="canWriteLogistics && !isVoided"
            type="button"
            class="ops-action-btn ops-action-btn--primary"
            @click="emit('set-clearance')"
          >
            {{ t('customsPages.declarations.setClearance') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('customsPages.declarations.opsPanel.arrivalTitle') }}</h3>
          <span v-if="arrivalCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('customsPages.declarations.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <div class="ops-metrics">
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('customsPages.declarations.opsPanel.existingArrivalCount') }}</span>
              <span class="ops-metrics__value">{{ formatQty(existingArrivalCount) }}</span>
            </div>
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('customsPages.declarations.opsPanel.pendingArrivalCount') }}</span>
              <span class="ops-metrics__value">{{ formatQty(pendingArrivalCount) }}</span>
            </div>
          </div>
          <p v-if="arrivalDisabledHint && !arrivalCompleted" class="ops-status ops-status--warn">
            {{ arrivalDisabledHint.summary }}
          </p>
          <div class="ops-progress">
            <div class="ops-progress__track">
              <div class="ops-progress__bar ops-progress__bar--arrival" :style="{ width: `${arrivalProgressPct}%` }" />
            </div>
          </div>
          <ul v-if="arrivalDisabledHint?.details.length && !arrivalCompleted" class="ops-hint-list">
            <li v-for="(line, idx) in arrivalDisabledHint.details" :key="`a-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="arrivalDisabledHint && !arrivalCompleted" class="ops-next-step">{{ arrivalDisabledHint.nextStep }}</p>
          <p
            v-else-if="existingArrivalCodes.length && !arrivalCompleted"
            class="ops-status ops-status--info"
          >
            {{ t('customsPages.declarations.existingArrivalNotifies', { codes: existingArrivalCodes.join('、') }) }}
          </p>
          <button
            v-if="canWriteLogistics && !arrivalCompleted"
            type="button"
            class="ops-action-btn"
            :class="arrivalBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="arrivalBtnDisabled || actionLoading"
            @click="emit('create-arrival')"
          >
            {{ t('customsPages.declarations.createArrivalNotifies') }}
          </button>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type { CustomsDeclarationDetailDto } from '@/api/customs'
import {
  buildCustomsArrivalDisabledHintContent,
  isCustomsArrivalOpsCompleted
} from '@/utils/customsDeclarationArrivalDisabledHint'

const props = defineProps<{
  row: Record<string, unknown> | null
  detail: CustomsDeclarationDetailDto | null
  loading?: boolean
  loadError?: string
  actionLoading?: boolean
  canWriteLogistics?: boolean
  embedded?: boolean
}>()

const emit = defineEmits<{
  close: []
  clear: []
  'set-clearance': []
  'create-arrival': []
}>()

const { t } = useI18n()

const declarationCode = computed(() => {
  const fromDetail = props.detail?.declarationCode?.trim()
  if (fromDetail) return fromDetail
  return String(props.row?.declarationCode ?? '—') || '—'
})

const brokerDisplay = computed(() => {
  const d = props.detail
  if (d) return d.customsBrokerName || d.customsBrokerCode || d.customsBrokerId || '—'
  return String(props.row?.customsBrokerName ?? '—') || '—'
})

const declareDateText = computed(() => {
  const raw = props.detail?.declareDate ?? props.row?.declareDate
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

const sorDisplay = computed(() => {
  const d = props.detail
  if (d?.stockOutRequestCode) return d.stockOutRequestCode
  if (d?.stockOutRequestId) return d.stockOutRequestId
  return String(props.row?.stockOutRequestCode ?? props.row?.stockOutRequestId ?? '—') || '—'
})

const internalStatus = computed(() =>
  Number(props.detail?.internalStatus ?? props.row?.internalStatus ?? 0)
)

const clearanceStatus = computed(() =>
  Number(props.detail?.customsClearanceStatus ?? props.row?.customsClearanceStatus ?? 0)
)

const isVoided = computed(() => internalStatus.value === -1)

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

const itemCount = computed(() => props.detail?.items?.length ?? 0)

const existingArrivalCount = computed(() => props.detail?.existingArrivalNotifyCount ?? 0)

const pendingArrivalCount = computed(() => props.detail?.pendingArrivalNotifyCount ?? 0)

const existingArrivalCodes = computed(() => props.detail?.existingArrivalNotifyCodes ?? [])

const arrivalCompleted = computed(() => isCustomsArrivalOpsCompleted(props.detail))

const arrivalProgressPct = computed(() => {
  const total = itemCount.value
  if (total <= 0) return 0
  return Math.min(100, Math.round((existingArrivalCount.value / total) * 100))
})

const arrivalDisabledHint = computed(() =>
  props.detail ? buildCustomsArrivalDisabledHintContent(props.detail, t) : null
)

const arrivalBtnDisabled = computed(() => !props.detail?.canCreateArrivalNotifies || isVoided.value)

function formatQty(v: number) {
  if (!Number.isFinite(v)) return '—'
  return v.toLocaleString()
}
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

.so-item-ops-root--embedded .ops-card__title,
.so-item-ops-root--embedded .ops-kv__value,
.so-item-ops-root--embedded .ops-metrics__value {
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-kv__label,
.so-item-ops-root--embedded .ops-metrics__label,
.so-item-ops-root--embedded .ops-hint-list,
.so-item-ops-root--embedded .ops-status--info {
  color: var(--crm-aux-hint);
}

.so-item-ops-root--embedded .ops-kv__label,
.so-item-ops-root--embedded .ops-kv__value {
  min-width: 0;
  word-break: break-word;
}

.so-item-ops-root--embedded .ops-kv__value {
  flex: 0 1 46%;
  max-width: 46%;
}

.so-item-ops-root--embedded .ops-metrics {
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
}

.so-item-ops-root--embedded .ops-progress {
  flex-direction: column;
  align-items: stretch;
  gap: 6px;
}

.so-item-ops-root--embedded .ops-progress__track {
  background: var(--crm-aux-tab-bg);
}

.so-item-ops-root--embedded .ops-next-step,
.so-item-ops-root--embedded .ops-hint-list,
.so-item-ops-root--embedded .ops-status {
  min-width: 0;
  word-break: break-word;
}

.so-item-ops-root--embedded .ops-next-step {
  background: var(--crm-accent-005);
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-action-btn {
  max-width: 100%;
  box-sizing: border-box;
}

.so-item-ops-panel {
  flex: 0 0 min(400px, 36vw);
  width: min(400px, 36vw);
  min-width: 320px;
  max-height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
  background: #f3f5f8;
  border: 1px solid $border-panel;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
}

.so-item-ops-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  background: #fff;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
}

.so-item-ops-panel__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.so-item-ops-panel__close {
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 13px;
}

.so-item-ops-root__empty,
.so-item-ops-root__error {
  padding: 0;
  font-size: 13px;
}

.so-item-ops-root__error {
  color: $danger-color;
}

.so-item-ops-panel__body {
  flex: 1;
  overflow: auto;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.ops-card {
  width: 100%;
  min-width: 0;
  box-sizing: border-box;
  background: #fff;
  border-radius: 12px;
  border: 1px solid rgba(15, 23, 42, 0.06);
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.04);
}

.ops-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 14px 0;
}

.ops-card__done {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
  font-size: 12px;
  font-weight: 600;
  color: #67c23a;
}

.ops-card__done-icon {
  font-size: 14px;
}

.ops-card__title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.ops-card__body {
  padding: 10px 14px 14px;
}

.ops-card__body--overview {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.ops-card--status-only .ops-card__body--status {
  padding-top: 10px;
}

.ops-status-tags__row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.ops-overview-line {
  width: 100%;
  text-align: left;
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  line-height: 1.5;
  word-break: break-word;
}

.ops-overview-line--hero {
  font-size: 18px;
  font-weight: 700;
  color: $color-amber;
}

.so-item-ops-root--embedded .ops-overview-line {
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-overview-line--hero {
  color: $color-amber;
}

.ops-kv {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 6px 0 10px;
  font-size: 13px;
}

.ops-kv__label {
  color: $text-secondary;
  flex: 1;
}

.ops-kv__value {
  color: $text-primary;
  text-align: right;
  font-weight: 500;
}

.ops-kv__value--accent {
  color: $cyan-primary;
  font-weight: 700;
}

.ops-metrics {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 10px;
  margin-bottom: 8px;
}

.ops-metrics__item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.ops-metrics__label {
  font-size: 12px;
  color: $text-secondary;
}

.ops-metrics__value {
  font-size: 20px;
  font-weight: 700;
  color: $text-primary;
}

.ops-status {
  margin: 8px 0 0;
  font-size: 12px;
  line-height: 1.5;
}

.ops-status--warn {
  color: $danger-color;
}

.ops-status--info {
  color: $cyan-primary;
}

.ops-progress {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin: 10px 0;
}

.ops-progress__track {
  flex: 1 1 100%;
  min-width: 0;
  width: 100%;
  height: 10px;
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.08);
  overflow: hidden;
}

.ops-progress__bar {
  height: 100%;
  border-radius: inherit;
  transition: width 0.2s ease;
}

.ops-progress__bar--arrival {
  background: linear-gradient(90deg, #34d399, #059669);
}

.ops-hint-list {
  margin: 0 0 8px;
  padding-left: 18px;
  font-size: 12px;
  color: $text-secondary;
  line-height: 1.5;
}

.ops-next-step {
  margin: 0 0 12px;
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(56, 189, 248, 0.08);
  color: $text-primary;
  font-size: 12px;
  line-height: 1.55;
}

.ops-action-btn {
  width: 100%;
  border: none;
  border-radius: 10px;
  padding: 11px 14px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.ops-action-btn--primary {
  background: #0f4c81;
  color: #fff;
}

.ops-action-btn--disabled,
.ops-action-btn:disabled {
  cursor: not-allowed;
  background: #e5e7eb;
  color: #9ca3af;
  opacity: 1;
}

.so-item-ops-root--embedded :deep(.el-loading-parent--relative) {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  overflow-x: hidden;
}
</style>
