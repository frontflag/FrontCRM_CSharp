<template>
  <div class="rfq-item-material-panel">
    <p v-if="!canAiLookup" class="rfq-item-material-panel__hint">
      {{ t('rfqHome.aiSearchNoPermission') }}
    </p>
    <p v-else-if="!boundPn" class="rfq-item-material-panel__hint">
      {{ t('rfqItemList.materialPanel.noPn') }}
    </p>
    <template v-else>
      <p class="rfq-item-material-panel__pn">
        <span class="rfq-item-material-panel__pn-lbl">{{ t('rfqItemList.materialPanel.pnLabel') }}</span>
        <span class="rfq-item-material-panel__pn-val">{{ boundPn }}</span>
      </p>

      <div v-if="isLoading" class="rfq-item-material-panel__loading">
        <el-icon class="is-loading rfq-item-material-panel__loading-icon"><Loading /></el-icon>
        <span>{{ t('rfqHome.aiLoading', { seconds: loadingSeconds }) }}</span>
      </div>

      <div v-else-if="errorMessage" class="rfq-item-material-panel__error">
        <p>{{ errorMessage }}</p>
        <el-button size="small" type="primary" @click="retryLookup">{{ t('rfqItemList.materialPanel.retry') }}</el-button>
      </div>

      <p v-else-if="showEmpty" class="rfq-item-material-panel__hint">
        {{ t('rfqHome.aiSearchEmpty') }}
      </p>

      <MaterialIntelResultPanel
        v-else-if="resultData"
        :data="resultData"
        :from-cache="fromCache"
        :layout="panelLayout"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount, inject } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { Loading } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { AI_PERMISSION_MATERIAL_INTEL_LOOKUP } from '@/api/ai'
import { useMaterialIntelLookupStore } from '@/stores/materialIntelLookup'
import MaterialIntelResultPanel from '@/components/RFQ/MaterialIntelResultPanel.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'

const { t } = useI18n()
const authStore = useAuthStore()
const materialIntelStore = useMaterialIntelLookupStore()
const { boundPnNormalized: boundPn, cacheByPn } = storeToRefs(materialIntelStore)
const workspaceLayout = inject(WorkspaceLayoutKey, null)

const canAiLookup = computed(() => authStore.hasPermission(AI_PERMISSION_MATERIAL_INTEL_LOOKUP))

const panelLayout = computed(() =>
  workspaceLayout?.rightFullscreen.value ? 'centered' : 'embedded'
)

const isLoading = computed(() => (boundPn.value ? materialIntelStore.isPnLoading(boundPn.value) : false))

const cacheEntry = computed(() => {
  void cacheByPn.value
  return boundPn.value ? materialIntelStore.getCacheEntry(boundPn.value) : null
})

const resultData = computed(() => (cacheEntry.value?.status === 'done' ? cacheEntry.value.data : null))
const fromCache = computed(() => cacheEntry.value?.fromCache ?? false)
const errorMessage = computed(() =>
  cacheEntry.value?.status === 'error' ? cacheEntry.value.errorMessage || t('rfqHome.aiSearchFailed') : ''
)
const showEmpty = computed(
  () => !!boundPn.value && !isLoading.value && cacheEntry.value?.status === 'done' && !resultData.value
)

const loadingSeconds = ref(0)
let loadingTimer: ReturnType<typeof setInterval> | null = null

function stopLoadingTimer() {
  if (loadingTimer != null) {
    clearInterval(loadingTimer)
    loadingTimer = null
  }
}

function syncLoadingSeconds() {
  if (!boundPn.value || !isLoading.value) {
    loadingSeconds.value = 0
    stopLoadingTimer()
    return
  }
  loadingSeconds.value = materialIntelStore.getLoadingElapsedSeconds(boundPn.value)
  if (!loadingTimer) {
    loadingTimer = setInterval(() => {
      if (!boundPn.value || !materialIntelStore.isPnLoading(boundPn.value)) {
        stopLoadingTimer()
        return
      }
      loadingSeconds.value = materialIntelStore.getLoadingElapsedSeconds(boundPn.value)
    }, 1000)
  }
}

watch([boundPn, isLoading], syncLoadingSeconds, { immediate: true })

onBeforeUnmount(() => stopLoadingTimer())

function retryLookup() {
  if (!boundPn.value) return
  void materialIntelStore.ensureLookup(boundPn.value, { force: true, triggerType: 'manual' })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-item-material-panel {
  height: 100%;
  min-height: 0;
  overflow: auto;
  padding: 4px 2px 12px;
}

.rfq-item-material-panel__hint {
  margin: 12px 4px;
  font-size: 13px;
  line-height: 1.6;
  color: $text-secondary;
}

.rfq-item-material-panel__pn {
  margin: 0 4px 10px;
  font-size: 12px;
  line-height: 1.5;
  color: $text-secondary;
  word-break: break-all;
}

.rfq-item-material-panel__pn-lbl {
  margin-right: 6px;
  color: $text-muted;
}

.rfq-item-material-panel__pn-val {
  font-size: 14px;
  font-weight: 600;
  color: $color-amber;
}

.rfq-item-material-panel__loading {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 16px 4px;
  font-size: 13px;
  color: $text-secondary;
}

.rfq-item-material-panel__loading-icon {
  font-size: 18px;
  color: $cyan-primary;
}

.rfq-item-material-panel__error {
  margin: 12px 4px;
  font-size: 13px;
  line-height: 1.6;
  color: $color-red-brown;

  p {
    margin: 0 0 10px;
  }
}
</style>
