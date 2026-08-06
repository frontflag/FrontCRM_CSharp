<template>
  <div class="quote-desktop-page">
    <div class="qd-nav">
      <el-button size="small" :disabled="!canPrev" @click="onPrev">
        {{ t('quoteDesktop.nav.prev') }}
      </el-button>
      <div class="qd-nav__stats">
        {{ t('quoteDesktop.stats.total', { n: total }) }}
      </div>
      <el-button size="small" :disabled="!canNext" @click="onNext">
        {{ t('quoteDesktop.nav.next') }}
      </el-button>
    </div>

    <div class="qd-workspace" v-loading="loading && !selected">
      <QuoteCreate
        v-if="selected"
        :key="selected.id"
        embedded
        :embed-rfq-id="selected.rfqId"
        :embed-rfq-item-id="selected.id"
        :embed-rfq-code="selected.rfqCode"
        @success="onCompleted"
        @mark-no-quote="onCompleted"
      />
      <div v-else class="qd-empty">
        {{ t('quoteDesktop.empty.workspace') }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import QuoteCreate from '@/views/RFQ/QuoteCreate.vue'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useQuoteDesktopQueueStore } from '@/stores/quoteDesktopQueue'
import { useMaterialIntelLookupStore } from '@/stores/materialIntelLookup'
import { useAuthStore } from '@/stores/auth'
import { resolveRfqItemMaterialPn } from '@/utils/materialPn'
import { AI_PERMISSION_MATERIAL_INTEL_LOOKUP } from '@/api/ai'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const queueStore = useQuoteDesktopQueueStore()
const materialIntelLookupStore = useMaterialIntelLookupStore()
const authStore = useAuthStore()
const { loading, selected, total, canPrev, canNext } = storeToRefs(queueStore)

function syncMaterialIntelForSelected() {
  const row = selected.value
  const pn = resolveRfqItemMaterialPn(row)
  materialIntelLookupStore.bindPn(pn)
  if (pn && authStore.hasPermission(AI_PERMISSION_MATERIAL_INTEL_LOOKUP)) {
    void materialIntelLookupStore.ensureLookup(pn, { triggerType: 'auto' })
  }
}

async function applyRouteFocusOrDefault() {
  const focusId =
    typeof route.query.rfqItemId === 'string' ? route.query.rfqItemId.trim() : ''
  if (focusId) {
    const ok = await queueStore.focusItem(focusId)
    if (!ok) {
      ElMessage.warning(t('quoteDesktop.messages.focusMissed'))
    }
    if (route.query.rfqItemId != null) {
      await router.replace({ name: 'QuoteDesktop', query: {} })
    }
    return
  }
  await queueStore.refreshAll()
}

async function onPrev() {
  await queueStore.goPrev()
}

async function onNext() {
  await queueStore.goNext()
}

async function onCompleted() {
  try {
    await queueStore.refreshAfterComplete()
  } catch {
    ElMessage.error(t('quoteDesktop.messages.refreshFailed'))
  }
}

onMounted(async () => {
  workspaceLayout?.toggleLeftPanel(true)
  workspaceLayout?.toggleRightPanel(true)
  try {
    await applyRouteFocusOrDefault()
    syncMaterialIntelForSelected()
  } catch {
    ElMessage.error(t('quoteDesktop.messages.loadFailed'))
  }
})

watch(
  () => selected.value?.id,
  () => {
    syncMaterialIntelForSelected()
  }
)

watch(
  () => String(route.query.rfqItemId ?? ''),
  async (next, prev) => {
    const id = next.trim()
    if (!id || id === String(prev ?? '').trim()) return
    try {
      if (!queueStore.items.length) await queueStore.refreshAll()
      const ok = await queueStore.focusItem(id)
      if (!ok) ElMessage.warning(t('quoteDesktop.messages.focusMissed'))
      await router.replace({ name: 'QuoteDesktop', query: {} })
    } catch {
      /* ignore */
    }
  }
)

onUnmounted(() => {
  materialIntelLookupStore.clearBound()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quote-desktop-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  padding: 0 0 16px;
  box-sizing: border-box;
}

.qd-nav {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: 5px 0;
  padding: 10px 14px;
  border-bottom: 1px solid $border-card;
  background: #f5fdff;

  &__stats {
    flex: 1;
    min-width: 0;
    text-align: center;
    font-size: 13px;
    font-weight: 400;
    color: $text-secondary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.qd-workspace {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 0 16px;
}

.qd-empty {
  padding: 48px 16px;
  text-align: center;
  font-size: 13px;
  opacity: 0.65;
}
</style>
