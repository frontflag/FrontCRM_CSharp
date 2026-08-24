<template>
  <!-- 业务列表页：结构对齐《业务列表规范》《列表搜索栏规范》；表格皮肤见全局 crm-unified-list.scss -->
  <div class="user-feedback-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('userFeedback.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('userFeedback.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.category"
          clearable
          :placeholder="t('userFeedback.filterCategory')"
          class="status-select"
          :teleported="false"
          @change="onFilterChange"
        >
          <el-option :label="t('userFeedback.categoryBug')" value="bug" />
          <el-option :label="t('userFeedback.categorySuggestion')" value="suggestion" />
          <el-option :label="t('userFeedback.categoryOther')" value="other" />
        </el-select>
        <el-select
          v-model="handlingFilter"
          clearable
          :placeholder="t('userFeedback.filterHandling')"
          class="status-select status-select--handling"
          :teleported="false"
          @change="onHandlingFilterChange"
        >
          <el-option :label="t('userFeedback.needHandle')" value="need" />
          <el-option :label="t('userFeedback.handled')" value="handled" />
          <el-option :label="t('userFeedback.noNeed')" value="noneed" />
        </el-select>
        <el-input
          v-model="filters.keyword"
          clearable
          :placeholder="t('userFeedback.keywordPh')"
          class="keyword-input"
          @keyup.enter="onQuery"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">
          {{ t('userFeedback.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">
          {{ t('userFeedback.reset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="ops-user-feedback-list-main"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-class-name="table-row-pointer"
        @row-dblclick="openDetail"
      >
        <template #col-title="{ row }">
          <span class="cell-ellipsis" :title="row.title">{{ row.title }}</span>
        </template>
        <template #col-category="{ row }">
          {{ categoryLabel(row.category) }}
        </template>
        <template #col-summary="{ row }">
          <span class="cell-ellipsis" :title="row.summary">{{ row.summary }}</span>
        </template>
        <template #col-bizRef="{ row }">
          <span class="cell-ellipsis" :title="row.bizRef || undefined">{{ row.bizRef?.trim() || '—' }}</span>
        </template>
        <template #col-submitUserName="{ row }">
          <span class="cell-ellipsis" :title="row.submitUserName || undefined">
            {{ row.submitUserName?.trim() || '—' }}
          </span>
        </template>
        <template #col-createTime="{ row }">
          {{ formatTime(row.createTime) }}
        </template>
        <template #col-needsHandling="{ row }">
          <el-tag :type="row.needsHandling ? 'warning' : 'info'" size="small" effect="plain">
            {{ row.needsHandling ? t('userFeedback.yes') : t('userFeedback.no') }}
          </el-tag>
        </template>
        <template #col-isHandled="{ row }">
          <el-tag :type="row.isHandled ? 'success' : 'info'" size="small" effect="plain">
            {{ row.isHandled ? t('userFeedback.yes') : t('userFeedback.no') }}
          </el-tag>
        </template>
        <template #col-completedDate="{ row }">
          {{ formatDate(row.completedDate) }}
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true">
          <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" />
        </svg>
        <p>{{ t('userFeedback.empty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @size-change="onSizeChange"
        @current-change="load"
      />
    </div>

    <el-drawer v-model="detailOpen" :title="t('userFeedback.detailTitle')" size="520px" append-to-body>
      <div v-if="detail" class="detail-body" v-loading="detailLoading">
        <el-form label-width="100px" size="small">
          <el-form-item :label="t('userFeedback.colTitle')">
            <span>{{ detail.title }}</span>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colCategory')">
            <el-select v-model="editForm.category" style="width: 160px">
              <el-option :label="t('userFeedback.categoryBug')" value="bug" />
              <el-option :label="t('userFeedback.categorySuggestion')" value="suggestion" />
              <el-option :label="t('userFeedback.categoryOther')" value="other" />
            </el-select>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colSummary')">
            <p class="detail-text">{{ detail.summary }}</p>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colBizRef')">
            <span>{{ detail.bizRef || '—' }}</span>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colRepro')">
            <p class="detail-text">{{ detail.reproSteps || '—' }}</p>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colPage')">
            <span class="detail-url" :title="detail.pageUrl || undefined">{{ detail.pageUrl || '—' }}</span>
          </el-form-item>
          <el-form-item :label="t('userFeedback.colNeeds')">
            <el-switch v-model="editForm.needsHandling" />
          </el-form-item>
          <el-form-item :label="t('userFeedback.colHandled')">
            <el-switch v-model="editForm.isHandled" />
          </el-form-item>
          <el-form-item :label="t('userFeedback.colCompleted')">
            <el-date-picker
              v-model="editForm.completedDate"
              type="date"
              value-format="YYYY-MM-DD"
              clearable
            />
          </el-form-item>
          <el-form-item :label="t('userFeedback.colRemark')">
            <el-input v-model="editForm.handleRemark" type="textarea" :rows="3" />
          </el-form-item>
        </el-form>

        <el-collapse>
          <el-collapse-item :title="t('userFeedback.conversation')" name="conv">
            <div v-if="detail.messages?.length" class="conv-list">
              <div v-for="m in detail.messages" :key="m.id" class="conv-item" :class="m.role">
                <div class="conv-role">{{ m.role }}</div>
                <div class="conv-content">{{ m.content }}</div>
              </div>
            </div>
            <div v-else class="detail-empty">{{ t('userFeedback.noMessages') }}</div>
          </el-collapse-item>
        </el-collapse>

        <div class="detail-actions">
          <el-button type="primary" :loading="saving" @click="saveDetail">{{ t('userFeedback.save') }}</el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  feedbackApi,
  type UserFeedbackDetail,
  type UserFeedbackListItem
} from '@/api/feedback'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t, locale } = useI18n()
const route = useRoute()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<UserFeedbackListItem[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const handlingFilter = ref<string | undefined>()
const filters = reactive({
  category: undefined as string | undefined,
  keyword: '',
  needsHandling: undefined as boolean | undefined,
  isHandled: undefined as boolean | undefined
})

const detailOpen = ref(false)
const detailLoading = ref(false)
const saving = ref(false)
const detail = ref<UserFeedbackDetail | null>(null)
const editForm = reactive({
  category: 'other',
  needsHandling: true,
  isHandled: false,
  completedDate: null as string | null,
  handleRemark: ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'title', label: t('userFeedback.colTitle'), prop: 'title', minWidth: 160, showOverflowTooltip: true },
    { key: 'category', label: t('userFeedback.colCategory'), prop: 'category', width: 100 },
    { key: 'summary', label: t('userFeedback.colSummary'), prop: 'summary', minWidth: 220, showOverflowTooltip: true },
    { key: 'bizRef', label: t('userFeedback.colBizRef'), prop: 'bizRef', width: 140, showOverflowTooltip: true },
    { key: 'submitUserName', label: t('userFeedback.colSubmitter'), prop: 'submitUserName', width: 110, showOverflowTooltip: true },
    { key: 'createTime', label: t('userFeedback.colCreateTime'), prop: 'createTime', width: 168 },
    { key: 'needsHandling', label: t('userFeedback.colNeeds'), prop: 'needsHandling', width: 90, align: 'center' },
    { key: 'isHandled', label: t('userFeedback.colHandled'), prop: 'isHandled', width: 90, align: 'center' },
    { key: 'completedDate', label: t('userFeedback.colCompleted'), prop: 'completedDate', width: 120 }
  ]
})

function categoryLabel(c: string) {
  if (c === 'bug') return t('userFeedback.categoryBug')
  if (c === 'suggestion') return t('userFeedback.categorySuggestion')
  return t('userFeedback.categoryOther')
}

function formatTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function onHandlingFilterChange() {
  filters.needsHandling = undefined
  filters.isHandled = undefined
  if (handlingFilter.value === 'need') {
    filters.needsHandling = true
    filters.isHandled = false
  } else if (handlingFilter.value === 'handled') {
    filters.isHandled = true
  } else if (handlingFilter.value === 'noneed') {
    filters.needsHandling = false
  }
  onFilterChange()
}

function onFilterChange() {
  page.value = 1
  void load()
}

function onQuery() {
  page.value = 1
  void load()
}

function onSizeChange() {
  page.value = 1
  void load()
}

function reset() {
  filters.category = undefined
  filters.keyword = ''
  filters.needsHandling = undefined
  filters.isHandled = undefined
  handlingFilter.value = undefined
  page.value = 1
  pageSize.value = 20
  void load()
}

async function load() {
  loading.value = true
  try {
    const res = await feedbackApi.adminList({
      category: filters.category,
      needsHandling: filters.needsHandling,
      isHandled: filters.isHandled,
      keyword: filters.keyword?.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = res.items || []
    total.value = res.total || 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('userFeedback.loadFailed')))
  } finally {
    loading.value = false
  }
}

async function openDetail(row: UserFeedbackListItem) {
  detailOpen.value = true
  detailLoading.value = true
  detail.value = null
  try {
    const d = await feedbackApi.adminDetail(row.id, true)
    detail.value = d
    editForm.category = d.category
    editForm.needsHandling = d.needsHandling
    editForm.isHandled = d.isHandled
    editForm.completedDate = d.completedDate ? String(d.completedDate).slice(0, 10) : null
    editForm.handleRemark = d.handleRemark || ''
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('userFeedback.loadFailed')))
    detailOpen.value = false
  } finally {
    detailLoading.value = false
  }
}

async function saveDetail() {
  if (!detail.value) return
  saving.value = true
  try {
    const d = await feedbackApi.adminPatch(detail.value.id, {
      category: editForm.category,
      needsHandling: editForm.needsHandling,
      isHandled: editForm.isHandled,
      completedDate: editForm.completedDate,
      handleRemark: editForm.handleRemark
    })
    detail.value = d
    ElMessage.success(t('userFeedback.saveOk'))
    void load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('userFeedback.saveFailed')))
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  if (String(route.query.handling || '') === 'need') {
    handlingFilter.value = 'need'
    filters.needsHandling = true
    filters.isHandled = false
  }
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.user-feedback-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left {
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

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.status-select--handling {
  width: 140px;
}

.keyword-input {
  width: 220px;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
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
  transition: all 0.2s;
  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.user-feedback-list-page .table-wrapper {
  position: relative;
  min-height: 200px;
  :deep(.el-table .cell) {
    line-height: 1.35;
  }
  :deep(.table-row-pointer) {
    cursor: pointer;
  }
}

.cell-ellipsis {
  display: block;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: $text-muted;
  p {
    margin: 12px 0 0;
    font-size: 14px;
  }
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
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

.detail-text {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
}

.detail-url {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 12px;
}

.conv-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 280px;
  overflow: auto;
}

.conv-item {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 6px 8px;
  .conv-role {
    font-size: 11px;
    color: var(--el-text-color-secondary);
    margin-bottom: 2px;
  }
  .conv-content {
    font-size: 13px;
    white-space: pre-wrap;
  }
}

.detail-empty {
  padding: 12px 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.detail-actions {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
