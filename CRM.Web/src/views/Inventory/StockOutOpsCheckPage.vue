<template>
  <div class="stock-out-ops-check-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutOpsCheck.title') }}</h1>
        </div>
        <div v-if="result" class="summary-metrics">
          <div class="summary-metric">
            <span class="summary-metric__label">{{ t('stockOutOpsCheck.severity.error') }}</span>
            <span class="summary-metric__value is-error">{{ result.errorCount }}</span>
          </div>
          <span class="summary-metric__divider" aria-hidden="true" />
          <div class="summary-metric">
            <span class="summary-metric__label">{{ t('stockOutOpsCheck.severity.warning') }}</span>
            <span class="summary-metric__value is-warning">{{ result.warningCount }}</span>
          </div>
          <template v-if="isFilterActive">
            <span class="summary-metric__divider" aria-hidden="true" />
            <div class="summary-metric">
              <span class="summary-metric__label">{{ t('stockOutOpsCheck.filters.shown') }}</span>
              <span class="summary-metric__value is-info">
                {{ visibleFindings.length }} / {{ result.findings.length }}
              </span>
            </div>
          </template>
        </div>
      </div>
      <div class="header-right">
        <p class="hint">{{ t('stockOutOpsCheck.hint') }}</p>
        <button type="button" class="btn-primary" :disabled="loading" @click="runCheck">
          {{ loading ? t('stockOutOpsCheck.running') : t('stockOutOpsCheck.run') }}
        </button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="draft.severity"
          :placeholder="t('stockOutOpsCheck.filters.severity')"
          clearable
          class="filter-select"
          :teleported="false"
          @change="applyFilters"
        >
          <el-option :label="t('stockOutOpsCheck.severity.error')" value="error" />
          <el-option :label="t('stockOutOpsCheck.severity.warning')" value="warning" />
        </el-select>
        <el-select
          v-model="draft.category"
          :placeholder="t('stockOutOpsCheck.filters.category')"
          clearable
          class="filter-select filter-select--wide"
          :teleported="false"
          @change="applyFilters"
        >
          <el-option
            v-for="opt in categoryOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-model="draft.docType"
          :placeholder="t('stockOutOpsCheck.filters.docType')"
          clearable
          class="filter-select filter-select--wide"
          :teleported="false"
          @change="applyFilters"
        >
          <el-option
            v-for="opt in docTypeOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <input
          v-model="draft.keyword"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('stockOutOpsCheck.filters.docCode')"
          @keyup.enter="applyFilters"
        />
        <button type="button" class="btn-primary btn-sm" @click="applyFilters">
          {{ t('stockOutOpsCheck.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">
          {{ t('stockOutOpsCheck.filters.reset') }}
        </button>
      </div>
    </div>

    <p v-if="loadError" class="alert alert--err">{{ loadError }}</p>
    <p v-if="result?.truncated" class="alert alert--warn">{{ t('stockOutOpsCheck.truncated') }}</p>

    <div class="result-panel" v-loading="loading">
      <table v-if="result && visibleFindings.length > 0" class="ops-table">
        <thead>
          <tr>
            <th class="col-severity">{{ t('stockOutOpsCheck.col.severity') }}</th>
            <th class="col-code">{{ t('stockOutOpsCheck.col.docCode') }}</th>
            <th class="col-code">{{ t('stockOutOpsCheck.col.related') }}</th>
            <th>{{ t('stockOutOpsCheck.col.reason') }}</th>
            <th>{{ t('stockOutOpsCheck.col.suggestion') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, idx) in visibleFindings" :key="`${row.docType}-${row.docId}-${row.category}-${idx}`">
            <td>
              <span class="tag" :class="row.severity === 'error' ? 'tag--err' : 'tag--warn'">
                {{ row.severity === 'error' ? t('stockOutOpsCheck.severity.error') : t('stockOutOpsCheck.severity.warning') }}
              </span>
            </td>
            <td class="col-code">
              <el-tooltip
                :disabled="!docTypeTip(row.docType) || dash(row.docCode) === '—'"
                :content="docTypeTip(row.docType)"
                placement="top"
                :hide-after="0"
              >
                <span class="code-wrap">
                  <button
                    v-if="canJump(row.routeName)"
                    type="button"
                    class="link"
                    @click="jump(row.routeName, row.routeParams, row.routeQuery)"
                  >
                    {{ dash(row.docCode) }}
                  </button>
                  <span v-else class="code-text">{{ dash(row.docCode) }}</span>
                </span>
              </el-tooltip>
            </td>
            <td class="col-code">
              <el-tooltip
                :disabled="!docTypeTip(row.relatedDocType) || dash(row.relatedDocCode) === '—'"
                :content="docTypeTip(row.relatedDocType)"
                placement="top"
                :hide-after="0"
              >
                <span class="code-wrap">
                  <button
                    v-if="canJump(row.relatedRouteName)"
                    type="button"
                    class="link"
                    @click="jump(row.relatedRouteName, row.relatedRouteParams, row.relatedRouteQuery)"
                  >
                    {{ dash(row.relatedDocCode) }}
                  </button>
                  <span v-else class="code-text">{{ dash(row.relatedDocCode) }}</span>
                </span>
              </el-tooltip>
            </td>
            <td>{{ row.reason }}</td>
            <td>{{ row.suggestion }}</td>
          </tr>
        </tbody>
      </table>
      <div v-else-if="result && !loading" class="empty">
        {{ isFilterActive ? t('stockOutOpsCheck.emptyFiltered') : t('stockOutOpsCheck.empty') }}
      </div>
      <div v-else-if="!loading && !result" class="empty">
        {{ t('stockOutOpsCheck.idle') }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { stockOutApi, type StockOutOpsCheckFinding, type StockOutOpsCheckResult } from '@/api/stockOut'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()
const loading = ref(false)
const loadError = ref('')
const result = ref<StockOutOpsCheckResult | null>(null)

const emptyFilters = () => ({
  keyword: '',
  severity: '',
  category: '',
  docType: ''
})

const draft = reactive(emptyFilters())
const applied = reactive(emptyFilters())

const categoryOptions = computed(() => [
  { value: 'chain', label: t('stockOutOpsCheck.category.chain') },
  { value: 'status', label: t('stockOutOpsCheck.category.status') },
  { value: 'duplicate', label: t('stockOutOpsCheck.category.duplicate') },
  { value: 'amount', label: t('stockOutOpsCheck.category.amount') },
  { value: 'customer', label: t('stockOutOpsCheck.category.customer') }
])

const docTypeOptions = computed(() => [
  { value: 'packing', label: t('stockOutOpsCheck.docType.packing') },
  { value: 'packingItem', label: t('stockOutOpsCheck.docType.packingItem') },
  { value: 'stockOut', label: t('stockOutOpsCheck.docType.stockOut') },
  { value: 'receivable', label: t('stockOutOpsCheck.docType.receivable') }
])

const isFilterActive = computed(() =>
  [applied.keyword, applied.severity, applied.category, applied.docType].some(
    (v) => String(v ?? '').trim() !== ''
  )
)

function startsWithCode(value: string | null | undefined, keyword: string) {
  const code = String(value ?? '').trim().toUpperCase()
  return code.length > 0 && code.startsWith(keyword)
}

function matchFinding(row: StockOutOpsCheckFinding) {
  if (applied.severity && row.severity !== applied.severity) return false
  if (applied.category && row.category !== applied.category) return false
  if (applied.docType && row.docType !== applied.docType) return false
  const keyword = applied.keyword.trim().toUpperCase()
  if (!keyword) return true
  return startsWithCode(row.docCode, keyword) || startsWithCode(row.relatedDocCode, keyword)
}

const visibleFindings = computed(() => {
  const rows = result.value?.findings ?? []
  if (!isFilterActive.value) return rows
  return rows.filter(matchFinding)
})

function applyFilters() {
  applied.keyword = String(draft.keyword ?? '').trim()
  applied.severity = String(draft.severity ?? '').trim()
  applied.category = String(draft.category ?? '').trim()
  applied.docType = String(draft.docType ?? '').trim()
}

function resetFilters() {
  Object.assign(draft, emptyFilters())
  Object.assign(applied, emptyFilters())
}

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
}

function docTypeTip(docType?: string | null) {
  const key = String(docType ?? '').trim()
  if (!key) return ''
  const i18nKey = `stockOutOpsCheck.docCodeTip.${key}`
  const label = t(i18nKey)
  return label === i18nKey ? '' : label
}

function canJump(name?: string | null) {
  return !!String(name || '').trim()
}

function jump(
  name?: string | null,
  params?: Record<string, string> | null,
  query?: Record<string, string> | null
) {
  const n = String(name || '').trim()
  if (!n) return
  void router.push({
    name: n,
    params: params || undefined,
    query: query || undefined
  })
}

async function runCheck() {
  loading.value = true
  loadError.value = ''
  try {
    result.value = await stockOutApi.runOpsCheck()
  } catch (e: unknown) {
    result.value = null
    loadError.value = getApiErrorMessage(e, t('stockOutOpsCheck.failed'))
  } finally {
    loading.value = false
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stock-out-ops-check-page {
  display: flex;
  flex-direction: column;
  min-height: 100%;
  padding: 24px;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.summary-metrics {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-left: 4px;
}

.summary-metric {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
  white-space: nowrap;
}

.summary-metric__label {
  font-size: 13px;
  color: $text-muted;
}

.summary-metric__value {
  font-size: 16px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  line-height: 1.2;
  color: $text-primary;

  &.is-error {
    color: $danger-color;
  }

  &.is-warning {
    color: $warning-color;
  }

  &.is-info {
    color: $cyan-primary;
  }
}

.summary-metric__divider {
  width: 1px;
  height: 16px;
  background: $border-panel;
  flex-shrink: 0;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  min-width: 88px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  letter-spacing: 0.5px;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s, opacity 0.2s;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &.btn-sm {
    min-width: 0;
    padding: 6px 12px;
    font-size: 12px;
    letter-spacing: 0;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: border-color 0.2s, color 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.hint {
  margin: 0;
  font-size: 13px;
  line-height: 1.4;
  color: $text-secondary;
  white-space: nowrap;
}

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input {
  width: 180px;
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--code {
  width: 180px;
}

.filter-select {
  width: 130px;

  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }

  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }

  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.filter-select--wide {
  width: 140px;
}

.alert {
  margin: 0 0 12px;
  padding: 8px 12px;
  border-radius: $border-radius-md;
  font-size: 13px;
  line-height: 1.5;
}

.alert--err {
  color: $danger-color;
  background: color-mix(in srgb, $danger-color 10%, $layer-2);
  border: 1px solid color-mix(in srgb, $danger-color 28%, $border-panel);
}

.alert--warn {
  color: $warning-color;
  background: color-mix(in srgb, $warning-color 10%, $layer-2);
  border: 1px solid color-mix(in srgb, $warning-color 28%, $border-panel);
}

.result-panel {
  flex: 1;
  min-height: 280px;
  overflow: auto;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-lg;
}

.ops-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  color: $text-primary;
  table-layout: auto;

  th,
  td {
    text-align: left;
    vertical-align: top;
    padding: 10px 14px;
    border-bottom: 1px solid $border-panel;
  }

  th {
    position: sticky;
    top: 0;
    z-index: 1;
    background: $layer-2;
    color: $text-muted;
    font-weight: 600;
  }

  tbody tr:last-child td {
    border-bottom: none;
  }
}

.col-severity {
  width: 72px;
  white-space: nowrap;
}

.col-code {
  width: 1%;
  white-space: nowrap;
  vertical-align: middle;
}

.code-wrap {
  display: inline-block;
  white-space: nowrap;
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 280px;
  margin: 0;
  padding: 24px;
  color: $text-muted;
  font-size: 13px;
}

.link {
  appearance: none;
  border: 0;
  background: none;
  color: $cyan-primary;
  cursor: pointer;
  padding: 0;
  font: inherit;
  text-decoration: none;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
  }
}

.tag {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 12px;
  line-height: 1.4;
  white-space: nowrap;
}

.tag--err {
  color: $danger-color;
  background: color-mix(in srgb, $danger-color 12%, transparent);
  border: 1px solid color-mix(in srgb, $danger-color 28%, transparent);
}

.tag--warn {
  color: $warning-color;
  background: color-mix(in srgb, $warning-color 12%, transparent);
  border: 1px solid color-mix(in srgb, $warning-color 28%, transparent);
}
</style>
