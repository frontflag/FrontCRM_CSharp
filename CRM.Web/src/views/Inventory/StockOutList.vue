<template>
  <div class="stockout-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockOutList.count', { count: filteredList.length }) }}</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          :placeholder="t('stockOutList.filters.keywordPlaceholder')"
          clearable
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <button type="button" class="btn-secondary" @click="handleSearch">{{ t('stockOutList.filters.search') }}</button>
        <button type="button" class="btn-secondary" @click="fetchList">{{ t('stockOutList.filters.refresh') }}</button>
      </div>
    </div>

    <!-- 结构与 StockOutNotifyList / StockInList 一致：无 row-key、无额外包裹 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-list-main"
      :columns="stockOutTableColumns"
      :show-column-settings="false"
      :data="filteredList"
      v-loading="loading"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-stockOutDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.stockOutDate) }}</span>
      </template>
      <template #col-totalQuantity="{ row }">{{ formatNum(row.totalQuantity) }}</template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('stockOutList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <button
              v-if="row.status !== 4"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="handleMarkFinish(row)"
            >
              {{ t('stockOutList.actions.markFinished') }}
            </button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="row.status !== 4" @click.stop="handleMarkFinish(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('stockOutList.actions.markFinished') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutDto } from '@/api/stockOut'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const list = ref<StockOutDto[]>([])
const keyword = ref('')
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 140
const OP_COL_EXPANDED_MIN_WIDTH = 140
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockOutTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockOutCode', label: t('stockOutList.columns.stockOutCode'), prop: 'stockOutCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'status', label: t('stockOutList.columns.status'), prop: 'status', width: 110, align: 'center' },
  { key: 'sourceCode', label: t('stockOutList.columns.sourceCode'), prop: 'sourceCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'warehouseId', label: t('stockOutList.columns.warehouseId'), prop: 'warehouseId', width: 140, showOverflowTooltip: true },
  { key: 'stockOutDate', label: t('stockOutList.columns.stockOutDate'), prop: 'stockOutDate', width: 170 },
  { key: 'totalQuantity', label: t('stockOutList.columns.totalQuantity'), prop: 'totalQuantity', width: 110, align: 'right' },
  { key: 'remark', label: t('stockOutList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('stockOutList.columns.createTime'), width: 170 },
  { key: 'createUser', label: t('stockOutList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockOutList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

function syncKeywordFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  keyword.value = typeof q.keyword === 'string' ? q.keyword : ''
}

watch(
  () => route.query,
  () => syncKeywordFromRoute(),
  { deep: true, immediate: true }
)

const formatNum = (v: number) => (v == null ? t('quoteList.na') : Number(v).toLocaleString())
const formatDate = (v?: string) => formatDisplayDateTime(v)

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
}

const filteredList = computed(() => {
  if (!keyword.value) return list.value
  const k = keyword.value.toLowerCase()
  return list.value.filter(
    (x) =>
      (x.stockOutCode || '').toLowerCase().includes(k) ||
      (x.sourceCode && x.sourceCode.toLowerCase().includes(k))
  )
})

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockOutApi.getAll()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  const k = keyword.value.trim()
  router.replace({ name: 'StockOutList', query: k ? { keyword: k } : {} })
}

const handleMarkFinish = async (row: StockOutDto) => {
  try {
    await stockOutApi.updateStatus(row.id, 4)
    ElMessage.success(t('stockOutList.messages.markFinishedSuccess'))
    await fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.updateStatusFailed'))
  }
}

onMounted(() => {
  void fetchList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/* 与 StockOutNotifyList.vue 的 .stockout-notify-page 同一套布局 */
.stockout-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}
.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
}
.text-secondary {
  color: $text-muted;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }
  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover {
    text-decoration: underline;
  }
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
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
