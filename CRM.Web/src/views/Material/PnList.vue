<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { inventoryCenterApi, type InventoryOverview } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const allRows = ref<InventoryOverview[]>([])
const keywordInput = ref('')
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

function norm(s: string) {
  return s.trim().toLowerCase()
}

const keywordFromRoute = computed(() => {
  const k = route.query.keyword
  return typeof k === 'string' ? k : Array.isArray(k) ? String(k[0] ?? '') : ''
})

function rowText(r: InventoryOverview) {
  return [r.materialModel, r.materialName, r.materialId]
    .filter((x) => x != null && String(x).trim() !== '')
    .map((x) => String(x))
    .join(' ')
}

const filteredRows = computed(() => {
  const kw = norm(keywordFromRoute.value)
  if (!kw) return allRows.value
  return allRows.value.filter((r) => norm(rowText(r)).includes(kw))
})

function syncInputFromRoute() {
  keywordInput.value = keywordFromRoute.value
}

watch(
  () => route.query.keyword,
  () => syncInputFromRoute(),
  { immediate: true }
)

async function loadOverview() {
  loading.value = true
  try {
    allRows.value = await inventoryCenterApi.getOverview()
  } catch (e) {
    allRows.value = []
    ElMessage.error('加载物料数据失败')
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  const q = norm(keywordInput.value)
  router.replace({ path: '/pn', query: q ? { keyword: keywordInput.value.trim() } : {} })
}

function handleReset() {
  keywordInput.value = ''
  router.replace({ path: '/pn', query: {} })
}

function formatTime(v?: string | null) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

const pnTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'materialModel', label: '物料型号', prop: 'materialModel', minWidth: 200, showOverflowTooltip: true },
  { key: 'materialName', label: '物料名称', prop: 'materialName', minWidth: 160, showOverflowTooltip: true },
  { key: 'warehouse', label: '仓库', width: 160, showOverflowTooltip: true },
  { key: 'onHandQty', label: '在库', prop: 'onHandQty', width: 100, align: 'right' },
  { key: 'availableQty', label: '可用', prop: 'availableQty', width: 100, align: 'right' },
  { key: 'lastMoveTime', label: '最后移动', prop: 'lastMoveTime', width: 170 }
])

onMounted(() => {
  loadOverview()
})
</script>

<template>
  <div class="pn-list-page customer-list-theme">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">PN</div>
          <h1 class="page-title">物料列表</h1>
        </div>
        <div class="count-badge">共 {{ filteredRows.length }} 条（库存视角）</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">关键词</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keywordInput"
            class="search-input"
            placeholder="物料型号、名称或编码…"
            @keyup.enter="handleSearch"
          />
        </div>
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">查询</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">重置</button>
      </div>
    </div>

    <el-card class="table-card" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="pn-list-main"
        :columns="pnTableColumns"
        :show-column-settings="false"
        :data="filteredRows"
        stripe
        class="pn-table"
      >
        <template #col-materialModel="{ row }">{{ row.materialModel || '—' }}</template>
        <template #col-materialName="{ row }">{{ row.materialName || '—' }}</template>
        <template #col-warehouse="{ row }">{{ row.warehouseCode?.trim() || row.warehouseId || '—' }}</template>
        <template #col-lastMoveTime="{ row }">{{ formatTime(row.lastMoveTime) }}</template>
      </CrmDataTable>
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip content="列设置" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.pn-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
  }
  .count-badge {
    padding: 3px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    color: $cyan-primary;
    font-weight: 700;
    font-size: 11px;
  }
}

.search-bar {
  margin-bottom: 16px;
  .search-left {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 12px;
  }
  .filter-field-label {
    font-size: 13px;
    color: $text-muted;
  }
  .search-input-wrap {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 0 12px;
    min-width: 280px;
    height: 36px;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid $border-panel;
    border-radius: 8px;
    .search-icon {
      color: $text-muted;
      flex-shrink: 0;
    }
    .search-input {
      flex: 1;
      min-width: 0;
      border: none;
      background: transparent;
      color: $text-primary;
      font-size: 14px;
      outline: none;
    }
  }
}

.table-card {
  background: $layer-2;
  border: 1px solid $border-panel;
}

.pn-table {
  background: transparent;
}

.pagination-wrapper {
  margin-top: 10px;
  display: flex;
  justify-content: flex-start;
  align-items: flex-start;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
