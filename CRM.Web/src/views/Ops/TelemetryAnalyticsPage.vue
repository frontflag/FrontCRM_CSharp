<template>
  <div class="telemetry-analytics-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M4 19V5" />
              <path d="M4 19h16" />
              <path d="M8 15v-4" />
              <path d="M12 15V8" />
              <path d="M16 15v-6" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('telemetryAnalytics.title') }}</h1>
        </div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('telemetryAnalytics.startDate')"
          :end-placeholder="t('telemetryAnalytics.endDate')"
          :clearable="false"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="load">
          {{ t('telemetryAnalytics.query') }}
        </button>
      </div>
    </div>

    <div v-loading="loading" class="tabs-wrap">
      <el-tabs v-model="activeTab" class="analytics-tabs">
        <el-tab-pane :label="t('telemetryAnalytics.topPages')" name="pages">
          <el-table :data="pages" size="small" stripe empty-text="—" height="100%">
            <el-table-column prop="pageKey" :label="t('telemetryAnalytics.colPage')" min-width="160" show-overflow-tooltip />
            <el-table-column :label="t('telemetryAnalytics.colDescription')" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">{{ displayDesc(row.description) }}</template>
            </el-table-column>
            <el-table-column prop="viewCount" :label="t('telemetryAnalytics.colViews')" width="110" sortable />
            <el-table-column
              prop="visibleMsSum"
              :label="t('telemetryAnalytics.colVisibleMin')"
              width="130"
              sortable
            >
              <template #default="{ row }">{{ formatMin(row.visibleMsSum) }}</template>
            </el-table-column>
            <el-table-column
              prop="activeMsSum"
              :label="t('telemetryAnalytics.colActiveMin')"
              width="130"
              sortable
            >
              <template #default="{ row }">{{ formatMin(row.activeMsSum) }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane :label="t('telemetryAnalytics.topActions')" name="actions">
          <el-table :data="actions" size="small" stripe empty-text="—" height="100%">
            <el-table-column
              prop="actionId"
              :label="t('telemetryAnalytics.colAction')"
              min-width="280"
              class-name="col-action-full"
            />
            <el-table-column :label="t('telemetryAnalytics.colDescription')" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">{{ displayDesc(row.description) }}</template>
            </el-table-column>
            <el-table-column prop="pageKey" :label="t('telemetryAnalytics.colPage')" min-width="120" show-overflow-tooltip />
            <el-table-column prop="clickCount" :label="t('telemetryAnalytics.colClicks')" width="110" sortable />
          </el-table>
        </el-tab-pane>

        <el-tab-pane :label="t('telemetryAnalytics.topApis')" name="apis">
          <el-table :data="apis" size="small" stripe empty-text="—" height="100%">
            <el-table-column prop="method" :label="t('telemetryAnalytics.colMethod')" width="80" />
            <el-table-column prop="pathTemplate" :label="t('telemetryAnalytics.colPath')" min-width="200" show-overflow-tooltip />
            <el-table-column :label="t('telemetryAnalytics.colDescription')" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">{{ displayDesc(row.description) }}</template>
            </el-table-column>
            <el-table-column prop="callCount" :label="t('telemetryAnalytics.colCalls')" width="100" sortable />
            <el-table-column prop="failCount" :label="t('telemetryAnalytics.colFails')" width="110" sortable>
              <template #header>
                <el-tooltip :content="t('telemetryAnalytics.colFailsHint')" placement="top">
                  <span>{{ t('telemetryAnalytics.colFails') }}</span>
                </el-tooltip>
              </template>
            </el-table-column>
            <el-table-column prop="avgDurationMs" :label="t('telemetryAnalytics.colAvgMs')" width="120" sortable />
            <el-table-column prop="maxDurationMs" :label="t('telemetryAnalytics.colMaxMs')" width="120" sortable />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  telemetryApi,
  type TelemetryActionRankRow,
  type TelemetryApiRankRow,
  type TelemetryPageRankRow
} from '@/api/telemetry'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()

function formatLocalDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function defaultRange(): [string, string] {
  const end = new Date()
  const start = new Date()
  start.setDate(end.getDate() - 7)
  return [formatLocalDate(start), formatLocalDate(end)]
}

const dateRange = ref<[string, string]>(defaultRange())
const activeTab = ref('pages')
const loading = ref(false)
const pages = ref<TelemetryPageRankRow[]>([])
const actions = ref<TelemetryActionRankRow[]>([])
const apis = ref<TelemetryApiRankRow[]>([])

function formatMin(ms: number) {
  if (!ms) return '0'
  return (ms / 60000).toFixed(1)
}

function displayDesc(desc?: string | null) {
  const s = (desc || '').trim()
  return s || '—'
}

async function load() {
  loading.value = true
  const range = dateRange.value
  if (!range || range.length < 2 || !range[0] || !range[1]) {
    ElMessage.warning(t('telemetryAnalytics.loadFailed'))
    loading.value = false
    return
  }
  const params = { startDate: range[0], endDate: range[1], take: 50 }
  try {
    const settled = await Promise.allSettled([
      telemetryApi.topPages(params),
      telemetryApi.topActions(params),
      telemetryApi.topApis(params)
    ])
    const errs: string[] = []
    if (settled[0].status === 'fulfilled') pages.value = settled[0].value || []
    else {
      pages.value = []
      errs.push(getApiErrorMessage(settled[0].reason, t('telemetryAnalytics.loadFailed')))
    }
    if (settled[1].status === 'fulfilled') actions.value = settled[1].value || []
    else {
      actions.value = []
      errs.push(getApiErrorMessage(settled[1].reason, t('telemetryAnalytics.loadFailed')))
    }
    if (settled[2].status === 'fulfilled') apis.value = settled[2].value || []
    else {
      apis.value = []
      errs.push(getApiErrorMessage(settled[2].reason, t('telemetryAnalytics.loadFailed')))
    }
    if (errs.length) ElMessage.error(errs[0])
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('telemetryAnalytics.loadFailed')))
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped>
.telemetry-analytics-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;
  min-height: 0;
  padding: 12px 16px 16px;
  box-sizing: border-box;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 8px;
}
.page-icon {
  color: var(--el-color-primary);
}
.page-title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}
.search-bar {
  display: flex;
  align-items: center;
}
.search-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.tabs-wrap {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 0 12px 12px;
}
.analytics-tabs {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}
.analytics-tabs :deep(.el-tabs__content) {
  flex: 1;
  min-height: 0;
  overflow: hidden;
}
.analytics-tabs :deep(.el-tab-pane) {
  height: 100%;
}
.analytics-tabs :deep(.el-table) {
  height: 100%;
}
.btn-primary {
  border: none;
  border-radius: 6px;
  background: var(--el-color-primary);
  color: #fff;
  padding: 6px 12px;
  cursor: pointer;
}
.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.btn-sm {
  font-size: 13px;
}
.analytics-tabs :deep(.col-action-full .cell) {
  white-space: normal;
  word-break: break-all;
  line-height: 1.45;
  overflow: visible;
  text-overflow: clip;
}
</style>
