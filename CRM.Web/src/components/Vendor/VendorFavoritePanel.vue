<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { favoriteApi } from '@/api/favorite'
import { vendorApi } from '@/api/vendor'
import type { Vendor } from '@/types/vendor'
import { useAuthStore } from '@/stores/auth'
import { VENDOR_FAVORITES_CHANGED_EVENT } from '@/constants/vendorFavorites'

withDefaults(
  defineProps<{
    title?: string
  }>(),
  { title: '收藏的供应商' }
)

const router = useRouter()
const authStore = useAuthStore()
const canViewVendorInfo = authStore.hasPermission('vendor.info.read')

const loading = ref(false)
const items = ref<Vendor[]>([])

function displayName(row: Vendor) {
  if (!canViewVendorInfo) return '—'
  return row.officialName || row.name || row.nickName || '—'
}

async function loadFavorites() {
  loading.value = true
  try {
    const ids = await favoriteApi.getFavoriteEntityIds('VENDOR')
    if (ids.length === 0) {
      items.value = []
      return
    }
    const settled = await Promise.allSettled(ids.map((id) => vendorApi.getVendorById(id)))
    const list: Vendor[] = []
    for (const r of settled) {
      if (r.status !== 'fulfilled') continue
      list.push(r.value as Vendor)
    }
    items.value = list
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function goDetail(row: Vendor) {
  router.push(`/vendors/${row.id}`)
}

function onFavoritesChanged() {
  void loadFavorites()
}

onMounted(() => {
  void loadFavorites()
  window.addEventListener(VENDOR_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(VENDOR_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})
</script>

<template>
  <div class="vendor-favorite-panel" v-loading="loading">
    <div class="vendor-favorite-panel__head">{{ title }}</div>
    <div v-if="!loading && items.length === 0" class="vendor-favorite-panel__empty">
      暂无收藏；可在供应商列表点击星标进行收藏
    </div>
    <table v-else class="vendor-favorite-panel__table">
      <thead>
        <tr>
          <th>供应商名称</th>
          <th>编号</th>
          <th>行业</th>
          <th>信用</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in items"
          :key="row.id"
          class="vendor-favorite-panel__row"
          @click="goDetail(row)"
        >
          <td class="vendor-favorite-panel__name">{{ displayName(row) }}</td>
          <td class="vendor-favorite-panel__code">{{ row.code || '—' }}</td>
          <td>{{ row.industry || '—' }}</td>
          <td>{{ row.credit ?? '—' }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.vendor-favorite-panel {
  min-height: 80px;
}

.vendor-favorite-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.vendor-favorite-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.vendor-favorite-panel__table {
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

.vendor-favorite-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.vendor-favorite-panel__name {
  word-break: break-all;
  max-width: 96px;
}

.vendor-favorite-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}
</style>
