<template>
  <div class="receipt-write-off-desktop-page">
    <div class="rwo-nav">
      <el-button size="small" :disabled="!canPrev" @click="goPrev">
        {{ t('receiptWriteOffDesktop.nav.prev') }}
      </el-button>
      <Translation
        keypath="receiptWriteOffDesktop.stats.line"
        tag="div"
        class="rwo-nav__stats"
        :title="statsText"
      >
        <template #n>
          <span class="rwo-nav__stats-num">{{ customerBucketCount }}</span>
        </template>
        <template #m>
          <span class="rwo-nav__stats-num">{{ pendingReceiptItemCount }}</span>
        </template>
      </Translation>
      <el-button size="small" :disabled="!canNext" @click="goNext">
        {{ t('receiptWriteOffDesktop.nav.next') }}
      </el-button>
      <el-button size="small" type="primary" plain class="rwo-nav__ledger" @click="goLedger">
        {{ t('financeReceiptWriteOff.openLedger') }}
      </el-button>
    </div>

    <div class="rwo-workspace" v-loading="queueLoading && !selected">
      <FinanceReceiptWriteOffPage
        v-if="selected"
        :key="selectedKey"
        embedded
        :embed-customer-id="selected.customerId"
        :embed-currency="selected.currency ?? null"
        :embed-summary="selected"
        @applied="onApplied"
      />
      <div v-else class="rwo-empty">
        {{ t('receiptWriteOffDesktop.empty.workspace') }}
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
import FinanceReceiptWriteOffPage from '@/views/Finance/FinanceReceiptWriteOffPage.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  receiptWriteOffCustomerKey,
  useReceiptWriteOffDesktopQueueStore
} from '@/stores/receiptWriteOffDesktopQueue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const queueStore = useReceiptWriteOffDesktopQueueStore()

const {
  loading: queueLoading,
  selected,
  selectedKey,
  canPrev,
  canNext,
  customerBucketCount,
  pendingReceiptItemCount
} = storeToRefs(queueStore)

const statsText = computed(() =>
  t('receiptWriteOffDesktop.stats.line', {
    n: customerBucketCount.value,
    m: pendingReceiptItemCount.value
  })
)

function goPrev() {
  queueStore.goPrev()
}

function goNext() {
  queueStore.goNext()
}

function goLedger() {
  router.push({ name: 'FinanceReceiptWriteOffLedger' })
}

async function onApplied() {
  try {
    await queueStore.refreshAfterApply()
  } catch {
    ElMessage.error(t('receiptWriteOffDesktop.messages.refreshFailed'))
  }
}

async function applyRouteFocusOrDefault() {
  const customerId =
    typeof route.query.customerId === 'string' ? route.query.customerId.trim() : ''
  const currencyRaw =
    typeof route.query.currency === 'string' ? route.query.currency.trim() : ''
  const currency = currencyRaw !== '' ? Number(currencyRaw) : NaN

  await queueStore.refreshAll({ keepSelection: false })

  if (customerId && Number.isFinite(currency)) {
    const ok = queueStore.focusItem(customerId, currency)
    if (!ok) {
      ElMessage.warning(t('receiptWriteOffDesktop.messages.focusMissed'))
    }
    if (route.query.customerId != null || route.query.currency != null) {
      await router.replace({ name: 'ReceiptWriteOffDesktop', query: {} })
    }
  }
}

onMounted(async () => {
  workspaceLayout?.toggleLeftPanel(true)
  workspaceLayout?.toggleRightPanel(false)
  try {
    await applyRouteFocusOrDefault()
  } catch {
    ElMessage.error(t('receiptWriteOffDesktop.messages.loadFailed'))
  }
})

onUnmounted(() => {
  queueStore.reset()
})

watch(
  () => (selected.value ? receiptWriteOffCustomerKey(selected.value) : ''),
  () => {
    queueStore.requestScrollToSelected()
  }
)
</script>

<style scoped>
.receipt-write-off-desktop-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  box-sizing: border-box;
}

.rwo-nav {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
}

.rwo-nav__stats {
  flex: 1;
  min-width: 0;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-regular);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rwo-nav__stats-num {
  color: var(--crm-color-amber);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.rwo-nav__ledger {
  flex-shrink: 0;
}

.rwo-workspace {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.rwo-workspace :deep(.write-off-page--embedded) {
  height: 100%;
  min-height: 0;
}

.rwo-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--el-text-color-secondary);
  font-size: 14px;
  padding: 24px;
}
</style>
