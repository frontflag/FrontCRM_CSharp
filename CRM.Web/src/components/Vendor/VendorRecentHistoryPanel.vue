<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { vendorApi } from '@/api/vendor'
import type { Vendor } from '@/types/vendor'
import type { LogRecentItem } from '@/api/logRecent'
import { useAuthStore } from '@/stores/auth'
import { useRecentHistoryList } from '@/composables/useRecentHistoryList'
import { VENDOR_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/vendorRecentHistory'

const BIZ_VENDOR = 'Vendor'

const props = withDefaults(
  defineProps<{
    title?: string
    take?: number
  }>(),
  { title: '', take: 20 }
)

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const canViewVendorInfo = authStore.hasPermission('vendor.info.read')

const { loading, items, reload } = useRecentHistoryList(() => BIZ_VENDOR, () => props.take)

const rowLoading = ref(false)
const rows = ref<Vendor[]>([])

function displayName(row: Vendor) {
  if (!canViewVendorInfo) return '—'
  return row.officialName || row.name || row.nickName || '—'
}

async function resolveVendorsFromLog(logItems: LogRecentItem[]) {
  if (logItems.length === 0) {
    rows.value = []
    return
  }
  rowLoading.value = true
  try {
    const settled = await Promise.allSettled(logItems.map((it) => vendorApi.getVendorById(it.recordId)))
    const list: Vendor[] = []
    for (const r of settled) {
      if (r.status !== 'fulfilled') continue
      list.push(r.value as Vendor)
    }
    rows.value = list
  } catch {
    rows.value = []
  } finally {
    rowLoading.value = false
  }
}

watch(items, (v) => void resolveVendorsFromLog(v), { immediate: true, deep: true })

function goDetail(row: Vendor) {
  router.push(`/vendors/${row.id}`)
}

function onRecentChanged() {
  void reload()
}

onMounted(() => {
  window.addEventListener(VENDOR_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(VENDOR_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})
</script>

<template>
  <div class="vendor-recent-panel" v-loading="loading || rowLoading">
    <div class="vendor-recent-panel__head">{{ title || t('leftPanel.vendorRecentTitle') }}</div>
    <div v-if="!loading && !rowLoading && items.length === 0" class="vendor-recent-panel__empty">
      {{ t('leftPanel.vendorRecentEmpty') }}
    </div>
    <table v-else class="vendor-recent-panel__table">
      <thead>
        <tr>
          <th>{{ t('vendorList.columns.name') }}</th>
          <th>{{ t('vendorList.columns.code') }}</th>
          <th>{{ t('vendorList.columns.industry') }}</th>
          <th>{{ t('leftPanel.credit') }}</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="row.id"
          class="vendor-recent-panel__row"
          @click="goDetail(row)"
        >
          <td class="vendor-recent-panel__name">{{ displayName(row) }}</td>
          <td class="vendor-recent-panel__code">{{ row.code || '—' }}</td>
          <td>{{ row.industry || '—' }}</td>
          <td>{{ row.credit ?? '—' }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.vendor-recent-panel {
  min-height: 80px;
}

.vendor-recent-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.vendor-recent-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.vendor-recent-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);

  th {
    text-align: left;
    font-weight: 500;
    color: rgba(120, 190, 232, 0.65);
    padding: 6px 4px 8px;
    border-bottom: 1px solid rgba(0, 212, 255, 0.12);
    white-space: nowrap;
  }

  td {
    padding: 8px 4px;
    border-bottom: 1px solid rgba(0, 212, 255, 0.06);
    vertical-align: top;
  }
}

.vendor-recent-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.vendor-recent-panel__name {
  word-break: break-all;
  max-width: 96px;
}

.vendor-recent-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}
</style>
