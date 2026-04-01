<template>
  <div class="draft-list-page">
    <div class="page-header">
      <div>
        <h1 class="title">草稿箱</h1>
        <p class="sub-title">Customer / Vendor / RFQ 草稿统一管理</p>
      </div>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true">
        <el-form-item label="业务类型">
          <el-select v-model="filters.entityType" clearable placeholder="全部类型" style="width: 170px">
            <el-option label="客户" value="CUSTOMER" />
            <el-option label="供应商" value="VENDOR" />
            <el-option label="RFQ需求" value="RFQ" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="filters.status" clearable placeholder="全部状态" style="width: 140px">
            <el-option label="草稿" :value="0" />
            <el-option label="已转正式" :value="1" />
            <el-option label="已废弃" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="关键词">
          <el-input
            v-model="filters.keyword"
            placeholder="草稿名/备注"
            clearable
            style="width: 260px"
            @keyup.enter="fetchDrafts"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchDrafts">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card>
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="draft-list-main"
        :columns="draftTableColumns"
        :show-column-settings="false"
        :data="drafts"
        v-loading="loading"
        @row-dblclick="restoreDraft"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="statusTagType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
        <template #col-entityType="{ row }">{{ entityTypeText(row.entityType) }}</template>
        <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
        <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">操作</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="restoreDraft(row)">恢复到编辑页</el-button>
              <el-button link type="success" @click.stop="convertDraft(row)" :disabled="row.status !== 0">转正式</el-button>
              <el-button link type="danger" @click.stop="deleteDraft(row)">删除</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="restoreDraft(row)">
                    <span class="op-more-item op-more-item--primary">恢复到编辑页</span>
                  </el-dropdown-item>
                  <el-dropdown-item :disabled="row.status !== 0" @click.stop="convertDraft(row)">
                    <span class="op-more-item op-more-item--success">转正式</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="deleteDraft(row)">
                    <span class="op-more-item op-more-item--danger">删除</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
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
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { draftApi, type DraftDto } from '@/api/draft'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const loading = ref(false)
const drafts = ref<DraftDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 300
const OP_COL_EXPANDED_MIN_WIDTH = 300
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const draftTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'status', label: '状态', width: 110, align: 'center' },
  { key: 'draftId', label: '草稿ID', prop: 'draftId', minWidth: 260, showOverflowTooltip: true },
  { key: 'draftName', label: '草稿名称', prop: 'draftName', minWidth: 160, showOverflowTooltip: true },
  { key: 'entityType', label: '业务类型', width: 120, align: 'center' },
  { key: 'convertedEntityId', label: '正式ID', prop: 'convertedEntityId', minWidth: 220, showOverflowTooltip: true },
  { key: 'createTime', label: '创建时间', width: 180 },
  { key: 'createUser', label: '创建人', width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: '操作',
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

const filters = reactive<{
  entityType?: string
  status?: number
  keyword?: string
}>({
  entityType: undefined,
  status: undefined,
  keyword: ''
})

const fetchDrafts = async () => {
  loading.value = true
  try {
    drafts.value = await draftApi.getDrafts({
      entityType: filters.entityType,
      status: filters.status,
      keyword: filters.keyword?.trim() || undefined
    })
  } catch (err: any) {
    ElMessage.error(err?.message || '获取草稿列表失败')
  } finally {
    loading.value = false
  }
}

const resetFilters = async () => {
  filters.entityType = undefined
  filters.status = undefined
  filters.keyword = ''
  await fetchDrafts()
}

const entityTypeText = (type: string) => {
  if (type === 'CUSTOMER') return '客户'
  if (type === 'VENDOR') return '供应商'
  if (type === 'RFQ') return 'RFQ需求'
  return type
}

const statusText = (status: number) => {
  if (status === 0) return '草稿'
  if (status === 1) return '已转正式'
  if (status === 2) return '已废弃'
  return String(status)
}

const statusTagType = (status: number) => {
  if (status === 0) return 'info'
  if (status === 1) return 'success'
  if (status === 2) return 'warning'
  return 'info'
}

const formatTime = (value?: string) => (value ? formatDisplayDateTime(value) : '--')

const getCreatePathByEntityType = (entityType: string) => {
  if (entityType === 'CUSTOMER') return '/customers/create'
  if (entityType === 'VENDOR') return '/vendors/create'
  if (entityType === 'RFQ') return '/rfqs/create'
  return ''
}

const restoreDraft = (row: DraftDto) => {
  const path = getCreatePathByEntityType(row.entityType)
  if (!path) {
    ElMessage.error(`不支持的实体类型：${row.entityType}`)
    return
  }
  router.push({ path, query: { draftId: row.draftId } })
}

const convertDraft = async (row: DraftDto) => {
  try {
    const result = await draftApi.convertDraft(row.draftId)
    ElMessage.success(`转正式成功，实体ID：${result.entityId}`)
    await fetchDrafts()
  } catch (err: any) {
    ElMessage.error(err?.message || '草稿转正式失败')
  }
}

const deleteDraft = async (row: DraftDto) => {
  try {
    await ElMessageBox.confirm('确定删除该草稿吗？删除后不可恢复。', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await draftApi.deleteDraft(row.draftId)
    ElMessage.success('草稿已删除')
    await fetchDrafts()
  } catch (err: any) {
    if (err === 'cancel' || err === 'close') return
    ElMessage.error(err?.message || '删除草稿失败')
  }
}

onMounted(fetchDrafts)
</script>

<style scoped>
.draft-list-page {
  padding: 20px;
}
.page-header {
  margin-bottom: 16px;
}
.title {
  margin: 0;
  font-size: 20px;
}
.sub-title {
  margin: 6px 0 0;
  color: #8a8f98;
}
.filter-card {
  margin-bottom: 12px;
}

/* 列表操作列规范（收起/展开） */
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: #00D4FF;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: #00D4FF;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: #00D4FF;
}

.op-more-item--success {
  color: #46BF91;
}

.op-more-item--danger {
  color: #C95745;
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

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
