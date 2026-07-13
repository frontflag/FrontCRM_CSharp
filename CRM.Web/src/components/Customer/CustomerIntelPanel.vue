<template>
  <div class="customer-intel-side-panel">
    <p v-if="!canInvestigate" class="customer-intel-side-panel__hint">
      {{ t('customerIntel.noPermission') }}
    </p>
    <p v-else-if="!boundContext" class="customer-intel-side-panel__hint">
      {{ t('customerIntel.selectCustomerHint') }}
    </p>
    <template v-else>
      <CustomerIntelCrmContextBar :context="boundContext" />

      <div class="customer-intel-side-panel__toolbar">
        <el-button
          type="primary"
          size="small"
          :loading="investigating"
          :disabled="loadingLatest"
          @click="onInvestigate(false)"
        >
          {{ hasReport ? t('customerIntel.reinvestigate') : t('customerIntel.investigate') }}
        </el-button>
        <el-button
          v-if="hasReport"
          size="small"
          :loading="investigating"
          @click="onInvestigate(true)"
        >
          {{ t('customerIntel.forceRefresh') }}
        </el-button>
      </div>

      <div v-if="historyReports.length > 1" class="customer-intel-side-panel__history">
        <span class="customer-intel-side-panel__history-label">{{ t('customerIntel.history') }}</span>
        <el-select
          :model-value="currentReport?.id ?? ''"
          size="small"
          class="customer-intel-side-panel__history-select"
          @change="onHistoryChange"
        >
          <el-option
            v-for="item in historyReports"
            :key="item.id"
            :label="formatHistoryLabel(item)"
            :value="item.id"
          />
        </el-select>
      </div>

      <div v-if="investigating" class="customer-intel-side-panel__loading">
        <el-icon class="is-loading"><Loading /></el-icon>
        <span>{{ t('customerIntel.loading', { seconds: loadingSeconds }) }}</span>
      </div>

      <div v-else-if="loadError" class="customer-intel-side-panel__error">
        <p>{{ loadError }}</p>
        <el-button size="small" type="primary" @click="onInvestigate(false)">{{ t('customerIntel.retry') }}</el-button>
      </div>

      <div v-else-if="loadingLatest" class="customer-intel-side-panel__loading">
        <el-icon class="is-loading"><Loading /></el-icon>
        <span>{{ t('customerIntel.loadingLatest') }}</span>
      </div>

      <p v-else-if="!hasReport" class="customer-intel-side-panel__hint">
        {{ t('customerIntel.emptyReport') }}
      </p>

      <CustomerIntelResultPanel
        v-else
        :data="reportData"
        :from-cache="!!currentReport?.fromCache"
        layout="embedded"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { AI_PERMISSION_CUSTOMER_INTEL_LOOKUP } from '@/api/ai'
import { useCustomerIntelLookupStore } from '@/stores/customerIntelLookup'
import type { CustomerIntelReportSummary } from '@/api/customerIntel'
import CustomerIntelCrmContextBar from '@/components/Customer/CustomerIntelCrmContextBar.vue'
import CustomerIntelResultPanel from '@/components/Customer/CustomerIntelResultPanel.vue'

const { t } = useI18n()
const authStore = useAuthStore()
const store = useCustomerIntelLookupStore()
const {
  boundContext,
  currentReport,
  historyReports,
  loadingLatest,
  investigating,
  loadError
} = storeToRefs(store)

const canInvestigate = computed(() => authStore.hasPermission(AI_PERMISSION_CUSTOMER_INTEL_LOOKUP))

const reportData = computed(() => {
  const report = currentReport.value?.report
  if (report && typeof report === 'object' && !Array.isArray(report)) {
    return report as Record<string, unknown>
  }
  return null
})

const hasReport = computed(() => !!reportData.value && Object.keys(reportData.value).length > 0)

const loadingSeconds = ref(0)
let loadingTimer: ReturnType<typeof setInterval> | null = null

function stopLoadingTimer() {
  if (loadingTimer != null) {
    clearInterval(loadingTimer)
    loadingTimer = null
  }
}

function syncLoadingSeconds() {
  if (!investigating.value) {
    loadingSeconds.value = 0
    stopLoadingTimer()
    return
  }
  loadingSeconds.value = store.getInvestigateElapsedSeconds()
  if (!loadingTimer) {
    loadingTimer = setInterval(() => {
      if (!investigating.value) {
        stopLoadingTimer()
        return
      }
      loadingSeconds.value = store.getInvestigateElapsedSeconds()
    }, 1000)
  }
}

watch(investigating, syncLoadingSeconds, { immediate: true })
onBeforeUnmount(() => stopLoadingTimer())

watch(
  () => boundContext.value?.customerId,
  (id) => {
    if (!id || !canInvestigate.value) return
    void store.loadLatest()
    void store.loadHistory()
  },
  { immediate: true }
)

function formatHistoryLabel(item: CustomerIntelReportSummary): string {
  const dt = item.createdAt ? new Date(item.createdAt) : null
  const time = dt && !Number.isNaN(dt.getTime()) ? dt.toLocaleString() : item.createdAt
  const who = item.createdByUserName || item.createdBy || ''
  return who ? `${time} · ${who}` : String(time)
}

async function onInvestigate(force: boolean) {
  if (!boundContext.value) return
  if (force && currentReport.value) {
    try {
      await ElMessageBox.confirm(t('customerIntel.forceRefreshConfirm'), t('customerIntel.forceRefresh'), {
        type: 'warning',
        confirmButtonText: t('customerIntel.confirm'),
        cancelButtonText: t('customerIntel.cancel')
      })
    } catch {
      return
    }
  }
  await store.investigate({ force })
}

function onHistoryChange(reportId: string) {
  void store.selectReportById(reportId)
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-intel-side-panel {
  height: 100%;
  min-height: 0;
  overflow: auto;
  padding: 4px 2px 12px;

  &__hint {
    margin: 12px 4px;
    font-size: 13px;
    color: $text-muted;
    line-height: 1.6;
  }

  &__toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 12px;
  }

  &__history {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
    font-size: 12px;
  }

  &__history-label {
    color: $text-muted;
    flex-shrink: 0;
  }

  &__history-select {
    flex: 1;
    min-width: 0;
  }

  &__loading,
  &__error {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 12px 0;
    font-size: 13px;
    color: $text-secondary;
  }

  &__error p {
    margin: 0;
    flex: 1;
  }
}
</style>
