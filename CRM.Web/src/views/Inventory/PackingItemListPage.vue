<template>
  <div class="packing-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('packingItemList.title') }}</h1>
        <div class="count-badge">{{ t('packingItemList.count', { count: listTotal }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <input
          v-model="filters.packingCode"
          class="search-input search-input--filter"
          :placeholder="t('packingItemList.filters.packingCode')"
          @keyup.enter="() => void fetchList()"
        />
        <input
          v-model="keyword"
          class="search-input search-input--wide"
          :placeholder="t('packingItemList.filters.keywordPlaceholder')"
          @keyup.enter="() => void fetchList()"
        />
        <button type="button" class="btn-primary btn-sm" @click="() => void fetchList()">{{ t('packingItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('packingItemList.filters.reset') }}</button>
      </div>
    </div>

    <div class="table-card" v-loading="loading">
      <CrmDataTable
        column-layout-key="packing-item-list-main"
        :columns="columns"
        :show-column-settings="false"
        :data="list"
        row-key="id"
        :row-class-name="flowPanelRowClassName"
        @row-click="onRowClick"
      >
        <template #col-customerName="{ row }">
          <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() || '—') }}</span>
        </template>
        <template #col-packingStatus="{ row }">{{ packingStatusLabel(row.packingStatus) }}</template>
        <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
      </CrmDataTable>
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="listPage"
          v-model:page-size="listPageSize"
          :total="listTotal"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="() => void fetchList(false)"
          @size-change="onPageSizeChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, inject, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { packingApi, packingStatusLabel, type PackingItemListRow } from '@/api/packing'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { usePackingDetailFlowPanelStore } from '@/stores/packingDetailFlowPanel'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'

const { t } = useI18n()
const route = useRoute()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const packingFlowStore = usePackingDetailFlowPanelStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const customerWorkspacePanelStore = useCustomerWorkspacePanelStore()
customerWorkspacePanelStore.setSource('packingItem')
const { onOpsPanelRowClick: onCustomerPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'PackingItemList',
  hasSelectedRow: () => !!customerWorkspacePanelStore.boundId,
  setRowOnly: row => customerWorkspacePanelStore.setRowOnly(row),
  selectRow: row => customerWorkspacePanelStore.selectRow(row, t('customerWorkspace.loadFailed')),
  loadSelected: () => {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  dataTabIds: ['r-customer']
})

const loading = ref(false)
const keyword = ref('')
const filters = ref({ packingCode: '' })
const list = ref<PackingItemListRow[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)

const columns = computed<CrmTableColumnDef[]>(() => [
  { key: 'packingCode', label: t('packingItemList.columns.packingCode'), prop: 'packingCode', width: 160, showOverflowTooltip: true },
  { key: 'packingStatus', label: t('packingItemList.columns.status'), width: 110, align: 'center' },
  { key: 'itemCode', label: t('packingItemList.columns.itemCode'), prop: 'itemCode', width: 150, showOverflowTooltip: true },
  { key: 'pn', label: t('packingItemList.columns.pn'), prop: 'pn', width: 160, showOverflowTooltip: true },
  { key: 'brand', label: t('packingItemList.columns.brand'), prop: 'brand', width: 120, showOverflowTooltip: true },
  { key: 'qty', label: t('packingItemList.columns.qty'), prop: 'qty', width: 90, align: 'right' },
  { key: 'sellOrderCode', label: t('packingItemList.columns.sellOrderCode'), prop: 'sellOrderCode', width: 150, showOverflowTooltip: true },
  { key: 'sellOrderItemCode', label: t('packingItemList.columns.sellOrderItemCode'), prop: 'sellOrderItemCode', width: 150, showOverflowTooltip: true },
  { key: 'customerName', label: t('packingItemList.columns.customerName'), minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('packingItemList.columns.createTime'), width: 170 }
])

function formatTime(v?: string) {
  return v ? formatDisplayDateTime(v) : '--'
}

async function fetchList(resetPage = true) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const res = await packingApi.getItemListPaged({
      keyword: keyword.value.trim() || undefined,
      packingCode: filters.value.packingCode.trim() || undefined,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = res.items
    listTotal.value = res.total
  } catch (e) {
    console.error(e)
    ElMessage.error(t('packingItemList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
  if (resetPage) resetListRightPanelOnReload(customerWorkspacePanelStore)
}

function onPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

function resetFilters() {
  keyword.value = ''
  filters.value.packingCode = ''
  void fetchList(true)
}

function onRowClick(row: PackingItemListRow) {
  const packingId = String(row?.packingId || '').trim()
  const itemId = String(row?.id || '').trim()
  if (!packingId || !itemId) return
  void packingFlowStore.bindPackingItemFromList(
    packingId,
    itemId,
    t('packingItemList.flowPanel.loadFailed'),
    t('packingItemList.flowPanel.itemNotFound')
  )
  void onCustomerPanelRowClick({ id: itemId })
}

function flowPanelRowClassName({ row }: { row: PackingItemListRow }) {
  const itemId = String(row?.id || '').trim()
  if (!itemId || !packingFlowStore.selectedPackingItemId) return ''
  return itemId === packingFlowStore.selectedPackingItemId ? 'so-item-row--active' : ''
}

onMounted(() => {
  void fetchList(true)
})

onBeforeUnmount(() => {
  customerWorkspacePanelStore.clear()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-item-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
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
}

.search-bar {
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input {
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  outline: none;
  &--filter {
    width: 160px;
  }
  &--wide {
    width: 280px;
  }
}

.table-card {
  padding: 12px;
  border-radius: 10px;
  border: 1px solid $border-panel;
  background: $layer-2;
}

:deep(.el-table__body tr.el-table__row.so-item-row--active > td.el-table__cell) {
  background: rgba(0, 160, 220, 0.1) !important;
}

.btn-primary {
  padding: 6px 12px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 12px;
  cursor: pointer;
  &.btn-sm {
    padding: 6px 12px;
  }
}

.btn-ghost {
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}
</style>
