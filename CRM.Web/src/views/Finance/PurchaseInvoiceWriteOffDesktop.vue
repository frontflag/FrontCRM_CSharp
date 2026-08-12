<template>
  <div class="piwo-write-off-desktop-page">
    <div class="piwo-nav">
      <el-button size="small" :disabled="!canPrev" @click="goPrev">
        {{ t('purchaseInvoiceWriteOffDesktop.nav.prev') }}
      </el-button>
      <Translation
        keypath="purchaseInvoiceWriteOffDesktop.stats.line"
        tag="div"
        class="piwo-nav__stats"
        :title="statsText"
      >
        <template #n>
          <span class="piwo-nav__stats-num">{{ vendorBucketCount }}</span>
        </template>
        <template #m>
          <span class="piwo-nav__stats-num">{{ pendingInvoiceRecordCount }}</span>
        </template>
      </Translation>
      <el-button size="small" :disabled="!canNext" @click="goNext">
        {{ t('purchaseInvoiceWriteOffDesktop.nav.next') }}
      </el-button>
    </div>

    <div class="piwo-workspace" v-loading="queueLoading && !selected">
      <PurchaseInvoiceWriteOffWorkspace
        v-if="selected"
        :key="selectedKey"
        embedded
        :embed-vendor-id="selected.vendorId"
        :embed-currency="selected.currency ?? null"
        :embed-summary="selected"
        @applied="onApplied"
      />
      <div v-else class="piwo-empty">
        {{ t('purchaseInvoiceWriteOffDesktop.empty.workspace') }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { Translation, useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import PurchaseInvoiceWriteOffWorkspace from '@/views/Finance/PurchaseInvoiceWriteOffWorkspace.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  purchaseInvoiceWriteOffVendorKey,
  usePurchaseInvoiceWriteOffDesktopQueueStore
} from '@/stores/purchaseInvoiceWriteOffDesktopQueue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const queueStore = usePurchaseInvoiceWriteOffDesktopQueueStore()

const {
  loading: queueLoading,
  selected,
  selectedKey,
  canPrev,
  canNext,
  vendorBucketCount,
  pendingInvoiceRecordCount
} = storeToRefs(queueStore)

const statsText = computed(() =>
  t('purchaseInvoiceWriteOffDesktop.stats.line', {
    n: vendorBucketCount.value,
    m: pendingInvoiceRecordCount.value
  })
)

function goPrev() {
  queueStore.goPrev()
}

function goNext() {
  queueStore.goNext()
}

async function onApplied() {
  try {
    await queueStore.refreshAfterApply()
  } catch {
    ElMessage.error(t('purchaseInvoiceWriteOffDesktop.messages.refreshFailed'))
  }
}

async function applyRouteFocusOrDefault() {
  const vendorId = typeof route.query.vendorId === 'string' ? route.query.vendorId.trim() : ''
  const currencyRaw = typeof route.query.currency === 'string' ? route.query.currency.trim() : ''
  const currency = currencyRaw !== '' ? Number(currencyRaw) : NaN

  await queueStore.refreshAll({ keepSelection: false })

  if (vendorId && Number.isFinite(currency)) {
    const ok = queueStore.focusItem(vendorId, currency)
    if (!ok) {
      ElMessage.warning(t('purchaseInvoiceWriteOffDesktop.messages.focusMissed'))
    }
    if (route.query.vendorId != null || route.query.currency != null) {
      await router.replace({ name: 'PurchaseInvoiceWriteOffDesktop', query: {} })
    }
  }
}

onMounted(async () => {
  workspaceLayout?.toggleLeftPanel(true)
  workspaceLayout?.toggleRightPanel(false)
  try {
    await applyRouteFocusOrDefault()
  } catch {
    ElMessage.error(t('purchaseInvoiceWriteOffDesktop.messages.loadFailed'))
  }
})

onUnmounted(() => {
  queueStore.reset()
})

watch(
  () => (selected.value ? purchaseInvoiceWriteOffVendorKey(selected.value) : ''),
  () => {
    queueStore.requestScrollToSelected()
  }
)
</script>

<style scoped>
.piwo-write-off-desktop-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  box-sizing: border-box;
}

.piwo-nav {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
}

.piwo-nav__stats {
  flex: 1;
  min-width: 0;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-regular);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.piwo-nav__stats-num {
  color: var(--crm-color-amber);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.piwo-workspace {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.piwo-workspace :deep(.write-off-page--embedded) {
  height: 100%;
  min-height: 0;
}

.piwo-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--el-text-color-secondary);
  font-size: 14px;
  padding: 24px;
}
</style>
