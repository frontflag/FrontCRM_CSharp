<template>
  <div class="draft-list-page">
    <div class="page-header">
      <div>
        <h1 class="title">{{ t('draftList.title') }}</h1>
        <p class="sub-title">{{ t('draftList.subtitle') }}</p>
      </div>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true">
        <el-form-item :label="t('draftList.filters.entityType')">
          <el-select v-model="filters.entityType" clearable :placeholder="t('draftList.filters.allEntityTypes')" style="width: 170px">
            <el-option :label="t('draftList.entityType.CUSTOMER')" value="CUSTOMER" />
            <el-option :label="t('draftList.entityType.VENDOR')" value="VENDOR" />
            <el-option :label="t('draftList.entityType.RFQ')" value="RFQ" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('draftList.filters.status')">
          <el-select v-model="filters.status" clearable :placeholder="t('draftList.filters.allStatus')" style="width: 140px">
            <el-option :label="t('draftList.status.draft')" :value="0" />
            <el-option :label="t('draftList.status.converted')" :value="1" />
            <el-option :label="t('draftList.status.discarded')" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('draftList.filters.keyword')">
          <el-input
            v-model="filters.keyword"
            :placeholder="t('draftList.filters.keywordPlaceholder')"
            clearable
            style="width: 260px"
            @keyup.enter="fetchDrafts"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchDrafts">{{ t('draftList.filters.search') }}</el-button>
          <el-button @click="resetFilters">{{ t('draftList.filters.reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card>
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="draft-list-v2"
        :columns="draftTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="drafts"
        v-loading="loading"
        @row-dblclick="restoreDraft"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="statusTagType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
        <template #col-chineseOfficialName="{ row }">{{ draftChineseDisplay(row as DraftDto) }}</template>
        <template #col-englishOfficialName="{ row }">{{ draftEnglishDisplay(row as DraftDto) }}</template>
        <template #col-entityType="{ row }">{{ entityTypeText(row.entityType) }}</template>
        <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
        <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('draftList.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="restoreDraft(row)">{{ t('draftList.actions.restore') }}</el-button>
              <el-button link type="success" @click.stop="convertDraft(row)" :disabled="row.status !== 0">{{ t('draftList.actions.convert') }}</el-button>
              <el-button link type="danger" @click.stop="deleteDraft(row)">{{ t('draftList.actions.delete') }}</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="restoreDraft(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('draftList.actions.restore') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item :disabled="row.status !== 0" @click.stop="convertDraft(row)">
                    <span class="op-more-item op-more-item--success">{{ t('draftList.actions.convert') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="deleteDraft(row)">
                    <span class="op-more-item op-more-item--danger">{{ t('draftList.actions.delete') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('draftList.columnSettings')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('draftList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { draftApi, type DraftDto } from '@/api/draft'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()

/** 后端历史文案带中文前缀时去掉，避免与前端 i18n 标题重复 */
function trimStr(v: unknown): string {
  if (v == null) return ''
  const s = String(v).trim()
  return s
}

function parseDraftPayload(row: DraftDto): Record<string, unknown> | null {
  try {
    const j = JSON.parse(row.payloadJson || '{}') as unknown
    return j && typeof j === 'object' && !Array.isArray(j) ? (j as Record<string, unknown>) : null
  } catch {
    return null
  }
}

/** 列表「中文全称」：优先表单 payload，其次草稿标题 */
function draftChineseDisplay(row: DraftDto): string {
  const p = parseDraftPayload(row)
  const t = row.entityType?.toUpperCase?.() || ''
  if (p) {
    if (t === 'CUSTOMER') {
      const v = trimStr(p.customerName) || trimStr(p.officialName) || trimStr(row.draftName)
      if (v) return v
    } else if (t === 'VENDOR') {
      const v = trimStr(p.officialName) || trimStr(p.name) || trimStr(row.draftName)
      if (v) return v
    } else if (t === 'RFQ') {
      const v = trimStr(p.product) || trimStr(p.rfqCode) || trimStr(row.draftName)
      if (v) return v
    }
  }
  return trimStr(row.draftName) || '--'
}

function draftEnglishDisplay(row: DraftDto): string {
  const p = parseDraftPayload(row)
  if (!p) return '--'
  const v = trimStr(p.englishOfficialName)
  return v || '--'
}

function stripDraftApiErrorDetail(message: string): string {
  return (message || '')
    .trim()
    .replace(
      /^(获取草稿列表失败|草稿转正式失败|删除草稿失败|保存草稿失败|获取草稿详情失败)[：:]\s*/i,
      ''
    )
    .trim()
}
const loading = ref(false)
const drafts = ref<DraftDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

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

const draftTableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'status', label: t('draftList.columns.status'), width: 110, align: 'center' },
    {
      key: 'chineseOfficialName',
      label: t('draftList.columns.chineseOfficialName'),
      minWidth: 160,
      showOverflowTooltip: true
    },
    {
      key: 'englishOfficialName',
      label: t('draftList.columns.englishOfficialName'),
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'entityType', label: t('draftList.columns.entityType'), width: 120, align: 'center' },
    { key: 'createTime', label: t('draftList.columns.createTime'), width: 180 },
    { key: 'createUser', label: t('draftList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('draftList.columns.actions'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    }
  ]
})

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
    const detail = stripDraftApiErrorDetail(err?.message || '')
    ElMessage.error(
      detail ? `${t('draftList.messages.loadFailed')}: ${detail}` : t('draftList.messages.loadFailed')
    )
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
  if (type === 'CUSTOMER') return t('draftList.entityType.CUSTOMER')
  if (type === 'VENDOR') return t('draftList.entityType.VENDOR')
  if (type === 'RFQ') return t('draftList.entityType.RFQ')
  return type
}

const statusText = (status: number) => {
  if (status === 0) return t('draftList.status.draft')
  if (status === 1) return t('draftList.status.converted')
  if (status === 2) return t('draftList.status.discarded')
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
    ElMessage.error(t('draftList.messages.unsupportedEntity', { type: row.entityType }))
    return
  }
  router.push({ path, query: { draftId: row.draftId } })
}

const convertDraft = async (row: DraftDto) => {
  try {
    const result = await draftApi.convertDraft(row.draftId)
    ElMessage.success(t('draftList.messages.convertSuccess', { id: result.entityId }))
    await fetchDrafts()
  } catch (err: any) {
    const detail = stripDraftApiErrorDetail(err?.message || '')
    ElMessage.error(
      detail ? `${t('draftList.messages.convertFailed')}: ${detail}` : t('draftList.messages.convertFailed')
    )
  }
}

const deleteDraft = async (row: DraftDto) => {
  try {
    await ElMessageBox.confirm(
      t('draftList.messages.deleteConfirmMsg'),
      t('draftList.messages.deleteConfirmTitle'),
      {
        type: 'warning',
        confirmButtonText: t('draftList.messages.deleteConfirmButton'),
        cancelButtonText: t('common.cancel')
      }
    )
    await draftApi.deleteDraft(row.draftId)
    ElMessage.success(t('draftList.messages.deleted'))
    await fetchDrafts()
  } catch (err: any) {
    if (err === 'cancel' || err === 'close') return
    const detail = stripDraftApiErrorDetail(err?.message || '')
    ElMessage.error(
      detail ? `${t('draftList.messages.deleteFailed')}: ${detail}` : t('draftList.messages.deleteFailed')
    )
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
