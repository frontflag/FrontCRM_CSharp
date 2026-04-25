<template>
  <div class="packing-list-page">
    <div class="page-header">
      <h1 class="page-title">装箱单</h1>
      <button class="btn-secondary" @click="loadList">刷新</button>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <input
            v-model="keyword"
            class="search-input"
            placeholder="装箱单号 / 销售单号 / 客户"
            @keyup.enter="loadList"
          />
        </div>
        <button class="btn-primary btn-sm" type="button" @click="loadList">搜索</button>
        <button class="btn-ghost btn-sm" type="button" @click="resetFilters">重置</button>
      </div>
    </div>

    <div class="table-card" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="packing-list-main"
        :columns="packingColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="list"
        row-key="id"
      >
        <template #col-customerName="{ row }">
          <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() || '—') }}</span>
        </template>
        <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
        <template #col-actions>
          <div class="action-btns">
            <button class="action-btn action-btn--primary" type="button">详情</button>
            <button class="action-btn action-btn--warning" type="button">编辑</button>
          </div>
        </template>
      </CrmDataTable>
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip content="列设置" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

type PackingRow = {
  id: string
  packingCode: string
  salesOrderCode: string
  customerName: string
  status: string
  createTime?: string
}

const loading = ref(false)
const keyword = ref('')
const list = ref<PackingRow[]>([])
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const packingColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'customerName', label: '客户', prop: 'customerName', minWidth: 180, showOverflowTooltip: true },
  { key: 'status', label: '状态', prop: 'status', width: 120, align: 'center' },
  { key: 'packingCode', label: '装箱单号', prop: 'packingCode', width: 180, showOverflowTooltip: true },
  { key: 'salesOrderCode', label: '销售单号', prop: 'salesOrderCode', width: 180, showOverflowTooltip: true },
  { key: 'createTime', label: '创建时间', prop: 'createTime', width: 180 },
  { key: 'actions', label: '操作', width: 160, fixed: 'right', hideable: false, pinned: 'end', reorderable: false }
])

function formatTime(v?: string) {
  return v ? formatDisplayDateTime(v) : '--'
}

function loadList() {
  loading.value = true
  setTimeout(() => {
    list.value = []
    loading.value = false
    if (keyword.value.trim()) {
      ElMessage.info('装箱单列表接口尚未接入，当前展示为空')
    }
  }, 120)
}

function resetFilters() {
  keyword.value = ''
  loadList()
}

loadList()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
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

.filter-field-label {
  color: $text-muted;
  font-size: 12px;
}

.search-input-wrap {
  display: flex;
  align-items: center;
}

.search-input {
  width: 280px;
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  outline: none;
}

.table-card {
  padding: 12px;
  border-radius: 10px;
  border: 1px solid $border-panel;
  background: $layer-2;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
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

.action-btns {
  display: flex;
  gap: 6px;
}

.action-btn {
  padding: 4px 10px;
  font-size: 12px;
  border-radius: 5px;
  cursor: pointer;
  border: 1px solid transparent;
  &--primary {
    background: rgba(0, 102, 255, 0.12);
    border-color: rgba(0, 212, 255, 0.35);
    color: $cyan-primary;
  }
  &--warning {
    background: rgba(201, 154, 69, 0.12);
    border-color: rgba(201, 154, 69, 0.35);
    color: $color-amber;
  }
}

.pagination-wrapper {
  margin-top: 10px;
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

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
