<template>
  <div class="vendor-intel-side-panel">
    <p v-if="!canInvestigate" class="vendor-intel-side-panel__hint">
      {{ t('vendorIntel.noPermission') }}
    </p>
    <p v-else-if="!boundContext" class="vendor-intel-side-panel__hint">
      {{ t('vendorIntel.selectVendorHint') }}
    </p>
    <template v-else>
      <VendorIntelCrmContextBar :context="boundContext" />

      <div class="vendor-intel-side-panel__toolbar">
        <el-button
          type="primary"
          size="small"
          :loading="boundInvestigating"
          :disabled="boundLoadingLatest"
          @click="onInvestigate(false)"
        >
          {{ hasReport ? t('vendorIntel.reinvestigate') : t('vendorIntel.investigate') }}
        </el-button>
        <el-button
          v-if="hasReport"
          size="small"
          :loading="boundInvestigating"
          @click="onInvestigate(true)"
        >
          {{ t('vendorIntel.forceRefresh') }}
        </el-button>
      </div>

      <div v-if="boundHistoryReports.length > 1" class="vendor-intel-side-panel__history">
        <span class="vendor-intel-side-panel__history-label">{{ t('vendorIntel.history') }}</span>
        <el-select
          :model-value="boundCurrentReport?.id ?? ''"
          size="small"
          class="vendor-intel-side-panel__history-select"
          @change="onHistoryChange"
        >
          <el-option
            v-for="item in boundHistoryReports"
            :key="item.id"
            :label="formatHistoryLabel(item)"
            :value="item.id"
          />
        </el-select>
      </div>

      <div v-if="boundInvestigating" class="vendor-intel-side-panel__loading">
        <el-icon class="is-loading"><Loading /></el-icon>
        <span>{{ t('vendorIntel.loading', { seconds: loadingSeconds }) }}</span>
      </div>

      <div v-else-if="boundLoadError" class="vendor-intel-side-panel__error">
        <p>{{ boundLoadError }}</p>
        <el-button size="small" type="primary" @click="onInvestigate(false)">{{ t('vendorIntel.retry') }}</el-button>
      </div>

      <div v-else-if="boundLoadingLatest" class="vendor-intel-side-panel__loading">
        <el-icon class="is-loading"><Loading /></el-icon>
        <span>{{ t('vendorIntel.loadingLatest') }}</span>
      </div>

      <p v-else-if="!hasReport" class="vendor-intel-side-panel__hint">
        {{ t('vendorIntel.emptyReport') }}
      </p>

      <CustomerIntelResultPanel
        v-else
        :data="reportData"
        :from-cache="!!boundCurrentReport?.fromCache"
        i18n-key-prefix="vendorIntel"
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
import { AI_PERMISSION_VENDOR_INTEL_LOOKUP } from '@/api/ai'
import { useVendorIntelLookupStore } from '@/stores/vendorIntelLookup'
import type { VendorIntelReportSummary } from '@/api/vendorIntel'
import VendorIntelCrmContextBar from '@/components/Vendor/VendorIntelCrmContextBar.vue'
import CustomerIntelResultPanel from '@/components/Customer/CustomerIntelResultPanel.vue'

const { t } = useI18n()
const authStore = useAuthStore()
const store = useVendorIntelLookupStore()
const {
  boundContext,
  boundVendorId,
  boundCurrentReport,
  boundHistoryReports,
  boundLoadError,
  boundInvestigating,
  boundLoadingLatest
} = storeToRefs(store)

const canInvestigate = computed(() => authStore.hasPermission(AI_PERMISSION_VENDOR_INTEL_LOOKUP))

const reportData = computed(() => {
  const report = boundCurrentReport.value?.report
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
  const id = boundVendorId.value
  if (!id || !boundInvestigating.value) {
    loadingSeconds.value = 0
    stopLoadingTimer()
    return
  }
  loadingSeconds.value = store.getInvestigateElapsedSeconds(id)
  if (!loadingTimer) {
    loadingTimer = setInterval(() => {
      const vid = boundVendorId.value
      if (!vid || !store.isVendorInvestigating(vid)) {
        stopLoadingTimer()
        return
      }
      loadingSeconds.value = store.getInvestigateElapsedSeconds(vid)
    }, 1000)
  }
}

watch([boundVendorId, boundInvestigating], syncLoadingSeconds, { immediate: true })
onBeforeUnmount(() => stopLoadingTimer())

watch(
  boundVendorId,
  (id) => {
    if (!id || !canInvestigate.value) return
    void store.loadLatest(id)
    void store.loadHistory()
  },
  { immediate: true }
)

function formatHistoryLabel(item: VendorIntelReportSummary): string {
  const dt = item.createdAt ? new Date(item.createdAt) : null
  const time = dt && !Number.isNaN(dt.getTime()) ? dt.toLocaleString() : item.createdAt
  const who = item.createdByUserName || item.createdBy || ''
  return who ? `${time} · ${who}` : String(time)
}

async function onInvestigate(force: boolean) {
  if (!boundContext.value) return
  if (force && boundCurrentReport.value) {
    try {
      await ElMessageBox.confirm(t('vendorIntel.forceRefreshConfirm'), t('vendorIntel.forceRefresh'), {
        type: 'warning',
        confirmButtonText: t('vendorIntel.confirm'),
        cancelButtonText: t('vendorIntel.cancel')
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

.vendor-intel-side-panel {
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
