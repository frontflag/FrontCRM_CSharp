<template>
  <div class="siwo-write-off-desktop-page">
    <div class="siwo-nav">
      <el-button size="small" :disabled="!canPrev" @click="goPrev">
        {{ t('sellInvoiceWriteOffDesktop.nav.prev') }}
      </el-button>
      <Translation
        keypath="sellInvoiceWriteOffDesktop.stats.line"
        tag="div"
        class="siwo-nav__stats"
        :title="statsText"
      >
        <template #n>
          <span class="siwo-nav__stats-num">{{ customerBucketCount }}</span>
        </template>
        <template #m>
          <span class="siwo-nav__stats-num">{{ pendingInvoiceRecordCount }}</span>
        </template>
      </Translation>
      <el-button size="small" :disabled="!canNext" @click="goNext">
        {{ t('sellInvoiceWriteOffDesktop.nav.next') }}
      </el-button>
    </div>

    <div class="siwo-workspace" v-loading="queueLoading && !selected">
      <SellInvoiceWriteOffWorkspace
        v-if="selected"
        :key="selectedKey"
        embedded
        :embed-customer-id="selected.customerId"
        :embed-currency="selected.currency ?? null"
        :embed-summary="selected"
        @applied="onApplied"
      />
      <div v-else class="siwo-empty">
        {{ t('sellInvoiceWriteOffDesktop.empty.workspace') }}
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
import SellInvoiceWriteOffWorkspace from '@/views/Finance/SellInvoiceWriteOffWorkspace.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  sellInvoiceWriteOffCustomerKey,
  useSellInvoiceWriteOffDesktopQueueStore
} from '@/stores/sellInvoiceWriteOffDesktopQueue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const queueStore = useSellInvoiceWriteOffDesktopQueueStore()

const {
  loading: queueLoading,
  selected,
  selectedKey,
  canPrev,
  canNext,
  customerBucketCount,
  pendingInvoiceRecordCount
} = storeToRefs(queueStore)

const statsText = computed(() =>
  t('sellInvoiceWriteOffDesktop.stats.line', {
    n: customerBucketCount.value,
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
    ElMessage.error(t('sellInvoiceWriteOffDesktop.messages.refreshFailed'))
  }
}

async function applyRouteFocusOrDefault() {
  const customerId = typeof route.query.customerId === 'string' ? route.query.customerId.trim() : ''
  const currencyRaw = typeof route.query.currency === 'string' ? route.query.currency.trim() : ''
  const currency = currencyRaw !== '' ? Number(currencyRaw) : NaN

  await queueStore.refreshAll({ keepSelection: false })

  if (customerId && Number.isFinite(currency)) {
    const ok = queueStore.focusItem(customerId, currency)
    if (!ok) {
      ElMessage.warning(t('sellInvoiceWriteOffDesktop.messages.focusMissed'))
    }
    if (route.query.customerId != null || route.query.currency != null) {
      await router.replace({ name: 'SellInvoiceWriteOffDesktop', query: {} })
    }
  }
}

onMounted(async () => {
  workspaceLayout?.toggleLeftPanel(true)
  workspaceLayout?.toggleRightPanel(false)
  try {
    await applyRouteFocusOrDefault()
  } catch {
    ElMessage.error(t('sellInvoiceWriteOffDesktop.messages.loadFailed'))
  }
})

onUnmounted(() => {
  queueStore.reset()
})

watch(
  () => (selected.value ? sellInvoiceWriteOffCustomerKey(selected.value) : ''),
  () => {
    queueStore.requestScrollToSelected()
  }
)
</script>

<style scoped>
.siwo-write-off-desktop-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  box-sizing: border-box;
}

.siwo-nav {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
}

.siwo-nav__stats {
  flex: 1;
  min-width: 0;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-regular);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.siwo-nav__stats-num {
  color: var(--crm-color-amber);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.siwo-workspace {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.siwo-workspace :deep(.write-off-page--embedded) {
  height: 100%;
  min-height: 0;
}

.siwo-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--el-text-color-secondary);
  font-size: 14px;
  padding: 24px;
}
</style>
