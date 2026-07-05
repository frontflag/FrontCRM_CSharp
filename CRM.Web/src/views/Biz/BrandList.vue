<template>
  <div class="brand-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z" />
              <line x1="7" y1="7" x2="7.01" y2="7" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('bizBrand.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('bizBrand.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.auditStatus"
          class="search-select search-select--filter"
          clearable
          :placeholder="t('bizBrand.phAuditStatus')"
          :teleported="false"
        >
          <el-option :label="t('bizBrand.auditStatusPending')" :value="1" />
          <el-option :label="t('bizBrand.auditStatusApproved')" :value="2" />
        </el-select>
        <input
          v-model="filters.brandEName"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phBrandEName')"
          @keyup.enter="() => handleSearch()"
        />
        <input
          v-model="filters.brandCName"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phBrandCName')"
          @keyup.enter="() => handleSearch()"
        />
        <input
          v-model="filters.standardBrand"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phStandardBrand')"
          @keyup.enter="() => handleSearch()"
        />
        <input
          v-model="filters.alias"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phAlias')"
          @keyup.enter="() => handleSearch()"
        />
        <input
          v-model="filters.country"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phCountry')"
          @keyup.enter="() => handleSearch()"
        />
        <input
          v-model="filters.remark"
          class="search-input search-input--filter"
          :placeholder="t('bizBrand.phRemark')"
          @keyup.enter="() => handleSearch()"
        />
        <el-date-picker
          v-model="filters.createDateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          class="search-date-range search-date-range--filter"
          :start-placeholder="t('bizBrand.phCreateDateFrom')"
          :end-placeholder="t('bizBrand.phCreateDateTo')"
          :range-separator="t('bizBrand.createDateSep')"
          clearable
          :teleported="false"
        />
        <div class="btn-split-group">
          <button
            type="button"
            class="btn-primary btn-sm"
            :disabled="loading"
            @click="handleSearch(false)"
          >
            {{ t('bizBrand.query') }}
          </button>
          <el-dropdown trigger="click" :disabled="loading" @command="onQueryCommand">
            <button
              type="button"
              class="btn-primary btn-sm btn-primary--caret"
              :disabled="loading"
              :aria-label="t('bizBrand.queryMore')"
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="6 9 12 15 18 9" />
              </svg>
            </button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="exact">{{ t('bizBrand.queryExact') }}</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('bizBrand.reset') }}
        </button>
      </div>
      <div v-if="canWrite" class="search-right">
        <button type="button" class="btn-success btn-sm" @click="openCreate">{{ t('bizBrand.create') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="biz-brand-list-main-v3"
      :columns="brandTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="rows"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
    >
      <template #col-brandEName="{ row }">
        <span>{{ displayCell(row.brandEName) }}</span>
      </template>
      <template #col-brandCName="{ row }">
        <span>{{ displayCell(row.brandCName) }}</span>
      </template>
      <template #col-standardBrand="{ row }">
        <span>{{ displayCell(row.standardBrand) }}</span>
      </template>
      <template #col-alias="{ row }">
        <span>{{ displayCell(row.alias) }}</span>
      </template>
      <template #col-countryCode="{ row }">
        <span>{{ displayCell(row.countryCode) }}</span>
      </template>
      <template #col-country="{ row }">
        <span>{{ displayCell(row.country) }}</span>
      </template>
      <template #col-remark="{ row }">
        <span>{{ displayCell(row.remark) }}</span>
      </template>
      <template #col-createUser="{ row }">
        <span>{{ displayCell(row.createUserName) }}</span>
      </template>
      <template #col-createTime="{ row }">
        <span class="text-secondary">{{ formatDate(row.createTime) }}</span>
      </template>
      <template #col-auditStatus="{ row }">
        <span :class="['audit-status-badge', auditStatusClass(row)]">
          {{ auditStatusLabel(row) }}
        </span>
      </template>
      <template #col-auditUser="{ row }">
        <span>{{ displayCell(row.auditUserName) }}</span>
      </template>
      <template #col-auditTime="{ row }">
        <span class="text-secondary">{{ formatDate(row.auditTime) }}</span>
      </template>
      <template v-if="canWrite" #col-actions-header>
        <div class="list-op-col-header--icon-only">
          <button
            type="button"
            class="op-col-toggle-btn list-op-col-toggle"
            :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
            @click.stop="toggleOpCol"
          >
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template v-if="canWrite" #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <button
              v-if="isPendingAudit(row)"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="openAuditDialog(row)"
            >
              {{ t('bizBrand.audit') }}
            </button>
            <button type="button" class="action-btn action-btn--primary" @click.stop="openEdit(row)">
              {{ t('bizBrand.edit') }}
            </button>
            <button type="button" class="action-btn action-btn--danger" @click.stop="openDeleteDialog(row)">
              {{ t('bizBrand.delete') }}
            </button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="isPendingAudit(row)" @click.stop="openAuditDialog(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('bizBrand.audit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="openEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('bizBrand.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="openDeleteDialog(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('bizBrand.delete') }}</span>
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
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void loadList()"
        @size-change="onPageSizeChange"
      />
    </div>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'add' ? t('bizBrand.dialogAddTitle') : t('bizBrand.dialogEditTitle')"
      width="560px"
      destroy-on-close
      @closed="onDialogClosed"
    >
      <el-form ref="dialogFormRef" :model="dialogForm" :rules="dialogRules" label-width="120px">
        <el-form-item :label="t('bizBrand.colBrandEName')" prop="brandEName">
          <el-input v-model="dialogForm.brandEName" maxlength="200" />
        </el-form-item>
        <el-form-item :label="t('bizBrand.colBrandCName')" prop="brandCName">
          <el-input v-model="dialogForm.brandCName" maxlength="200" />
        </el-form-item>
        <el-form-item :label="t('bizBrand.colStandardBrand')" prop="standardBrand">
          <el-input v-model="dialogForm.standardBrand" maxlength="300" />
        </el-form-item>
        <el-form-item :label="t('bizBrand.colAlias')">
          <div class="alias-field-wrap">
            <el-input
              v-model="dialogForm.alias"
              type="textarea"
              :rows="2"
              maxlength="500"
              :placeholder="t('bizBrand.phAlias')"
            />
            <p class="field-hint">{{ t('bizBrand.aliasHint') }}</p>
          </div>
        </el-form-item>
        <BizBrandCountryFields
          v-model:country="dialogForm.country"
          v-model:country-code="dialogForm.countryCode"
        />
        <el-form-item :label="t('bizBrand.colRemark')">
          <el-input v-model="dialogForm.remark" type="textarea" :rows="3" maxlength="500" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('bizBrand.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="saveDialog">{{ t('bizBrand.save') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="auditDialogVisible"
      :title="t('bizBrand.auditConfirmTitle')"
      width="480px"
      destroy-on-close
      @closed="onAuditDialogClosed"
    >
      <div class="audit-confirm-body">
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colBrandEName') }}</span>
          <span class="audit-confirm-value">{{ displayCell(auditTarget?.brandEName) }}</span>
        </div>
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colBrandCName') }}</span>
          <span class="audit-confirm-value">{{ displayCell(auditTarget?.brandCName) }}</span>
        </div>
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colStandardBrand') }}</span>
          <span class="audit-confirm-value">{{ displayCell(auditTarget?.standardBrand) }}</span>
        </div>
      </div>
      <template #footer>
        <el-button @click="auditDialogVisible = false">{{ t('bizBrand.cancel') }}</el-button>
        <el-button type="primary" :loading="auditing" @click="confirmAudit">{{ t('bizBrand.auditConfirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="deleteDialogVisible"
      :title="t('bizBrand.deleteTitle')"
      width="480px"
      destroy-on-close
      @closed="onDeleteDialogClosed"
    >
      <div class="audit-confirm-body">
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colBrandEName') }}</span>
          <span class="audit-confirm-value">{{ displayCell(deleteTarget?.brandEName) }}</span>
        </div>
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colBrandCName') }}</span>
          <span class="audit-confirm-value">{{ displayCell(deleteTarget?.brandCName) }}</span>
        </div>
        <div class="audit-confirm-row">
          <span class="audit-confirm-label">{{ t('bizBrand.colStandardBrand') }}</span>
          <span class="audit-confirm-value">{{ displayCell(deleteTarget?.standardBrand) }}</span>
        </div>
      </div>
      <template #footer>
        <el-button @click="deleteDialogVisible = false">{{ t('bizBrand.cancel') }}</el-button>
        <el-button type="danger" :loading="deleting" @click="confirmDelete">{{ t('bizBrand.delete') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { bizBrandApi, type BizBrandRow } from '@/api/bizBrand'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import BizBrandCountryFields from '@/components/Biz/BizBrandCountryFields.vue'

const { t } = useI18n()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.isAuthenticated)

const loading = ref(false)
const saving = ref(false)
const rows = ref<BizBrandRow[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const searchExactMatch = ref(false)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const filters = reactive({
  brandCName: '',
  brandEName: '',
  standardBrand: '',
  alias: '',
  country: '',
  remark: '',
  auditStatus: undefined as number | undefined,
  createDateRange: null as [string, string] | null
})

const dialogVisible = ref(false)
const auditDialogVisible = ref(false)
const deleteDialogVisible = ref(false)
const auditing = ref(false)
const deleting = ref(false)
const auditTarget = ref<BizBrandRow | null>(null)
const deleteTarget = ref<BizBrandRow | null>(null)
const dialogMode = ref<'add' | 'edit'>('edit')
const dialogFormRef = ref<FormInstance>()
const editingId = ref<number | null>(null)
const dialogForm = reactive({
  brandEName: '',
  brandCName: '',
  standardBrand: '',
  alias: '',
  countryCode: '',
  country: '',
  remark: ''
})

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 200
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const brandTableColumns = computed<CrmTableColumnDef[]>(() => {
  void opColWidth.value
  void opColMinWidth.value
  const cols: CrmTableColumnDef[] = [
    {
      key: 'auditStatus',
      label: t('bizBrand.colAuditStatus'),
      prop: 'auditStatus',
      width: 120,
      minWidth: 120,
      align: 'center',
      labelClassName: 'brand-col-audit-status-header',
      className: 'brand-col-audit-status',
      resizable: false
    },
    { key: 'brandEName', label: t('bizBrand.colBrandEName'), prop: 'brandEName', minWidth: 140, showOverflowTooltip: true },
    { key: 'brandCName', label: t('bizBrand.colBrandCName'), prop: 'brandCName', minWidth: 140, showOverflowTooltip: true },
    { key: 'standardBrand', label: t('bizBrand.colStandardBrand'), prop: 'standardBrand', minWidth: 160, showOverflowTooltip: true },
    { key: 'alias', label: t('bizBrand.colAlias'), prop: 'alias', minWidth: 140, showOverflowTooltip: true },
    {
      key: 'countryCode',
      label: t('bizBrand.colCountryCode'),
      prop: 'countryCode',
      minWidth: 112,
      showOverflowTooltip: true,
      labelClassName: 'brand-col-country-code-header'
    },
    { key: 'country', label: t('bizBrand.colCountry'), prop: 'country', minWidth: 120, showOverflowTooltip: true },
    {
      key: 'remark',
      label: t('bizBrand.colRemark'),
      prop: 'remark',
      minWidth: 240,
      showOverflowTooltip: true,
      labelClassName: 'brand-col-remark-header'
    },
    { key: 'createUser', label: t('bizBrand.colCreateUser'), prop: 'createUserName', width: 110, showOverflowTooltip: true },
    { key: 'createTime', label: t('bizBrand.colCreateTime'), prop: 'createTime', width: 160 },
    { key: 'auditUser', label: t('bizBrand.colAuditUser'), prop: 'auditUserName', width: 110, showOverflowTooltip: true },
    { key: 'auditTime', label: t('bizBrand.colAuditTime'), prop: 'auditTime', width: 160 }
  ]
  if (canWrite.value) {
    cols.push({
      key: 'actions',
      label: t('bizBrand.colActions'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    })
  }
  return cols
})

const dialogRules = computed<FormRules>(() => {
  if (dialogMode.value !== 'add') return {}
  const req = (message: string) => ({
    required: true,
    validator: (_rule: unknown, value: string, callback: (err?: Error) => void) => {
      if (!String(value ?? '').trim()) callback(new Error(message))
      else callback()
    },
    trigger: 'blur' as const
  })
  return {
    brandEName: [req(t('bizBrand.requiredBrandEName'))],
    brandCName: [req(t('bizBrand.requiredBrandCName'))],
    standardBrand: [req(t('bizBrand.requiredStandardBrand'))]
  }
})

function displayCell(value?: string | number | boolean | null) {
  if (value == null) return '—'
  if (typeof value === 'object') return '—'
  const text = String(value).trim()
  return text || '—'
}

const formatDate = (v?: string | null) => formatDisplayDateTime(v) || '—'

function resolveAuditStatus(row: BizBrandRow): number | null {
  const n = Number(row.auditStatus)
  return Number.isFinite(n) ? n : null
}

function auditStatusLabel(row: BizBrandRow) {
  const status = resolveAuditStatus(row)
  if (status === 1) return t('bizBrand.auditStatusPending')
  if (status === 2) return t('bizBrand.auditStatusApproved')
  return '—'
}

function auditStatusClass(row: BizBrandRow) {
  const status = resolveAuditStatus(row)
  if (status === 1) return 'audit-status-badge--pending'
  if (status === 2) return 'audit-status-badge--approved'
  return 'audit-status-badge--none'
}

function isPendingAudit(row: BizBrandRow) {
  return resolveAuditStatus(row) === 1
}

async function loadList() {
  loading.value = true
  try {
    const data = await bizBrandApi.fetchList({
      brandCName: filters.brandCName,
      brandEName: filters.brandEName,
      standardBrand: filters.standardBrand,
      alias: filters.alias,
      country: filters.country,
      remark: filters.remark,
      auditStatus: filters.auditStatus,
      createTimeFrom: filters.createDateRange?.[0],
      createTimeTo: filters.createDateRange?.[1],
      exactMatch: searchExactMatch.value,
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bizBrand.loadFailed')))
  } finally {
    loading.value = false
  }
}

function handleSearch(exact?: boolean) {
  if (exact !== undefined) searchExactMatch.value = exact
  page.value = 1
  void loadList()
}

function onQueryCommand(command: string) {
  if (command === 'exact') handleSearch(true)
}

function resetFilters() {
  filters.brandCName = ''
  filters.brandEName = ''
  filters.standardBrand = ''
  filters.alias = ''
  filters.country = ''
  filters.remark = ''
  filters.auditStatus = undefined
  filters.createDateRange = null
  searchExactMatch.value = false
  handleSearch(false)
}

function onPageSizeChange() {
  page.value = 1
  void loadList()
}

function onRowDblclick(row: BizBrandRow) {
  if (!canWrite.value) return
  openEdit(row)
}

function resetDialogForm() {
  dialogForm.brandEName = ''
  dialogForm.brandCName = ''
  dialogForm.standardBrand = ''
  dialogForm.alias = ''
  dialogForm.countryCode = ''
  dialogForm.country = ''
  dialogForm.remark = ''
}

function openCreate() {
  dialogMode.value = 'add'
  editingId.value = null
  resetDialogForm()
  dialogVisible.value = true
}

function openEdit(row: BizBrandRow) {
  dialogMode.value = 'edit'
  editingId.value = row.id
  dialogForm.brandEName = row.brandEName ?? ''
  dialogForm.brandCName = row.brandCName ?? ''
  dialogForm.standardBrand = row.standardBrand ?? ''
  dialogForm.alias = row.alias ?? ''
  dialogForm.countryCode = row.countryCode ?? ''
  dialogForm.country = row.country ?? ''
  dialogForm.remark = row.remark ?? ''
  dialogVisible.value = true
}

function openAuditDialog(row: BizBrandRow) {
  if (!isPendingAudit(row)) return
  auditTarget.value = row
  auditDialogVisible.value = true
}

function onAuditDialogClosed() {
  auditTarget.value = null
}

async function confirmAudit() {
  const row = auditTarget.value
  if (!row) return
  auditing.value = true
  try {
    await bizBrandApi.approve(row.id)
    ElMessage.success(t('bizBrand.auditOk'))
    auditDialogVisible.value = false
    await loadList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bizBrand.auditFailed')))
  } finally {
    auditing.value = false
  }
}

function openDeleteDialog(row: BizBrandRow) {
  deleteTarget.value = row
  deleteDialogVisible.value = true
}

function onDeleteDialogClosed() {
  deleteTarget.value = null
}

async function confirmDelete() {
  const row = deleteTarget.value
  if (!row) return
  deleting.value = true
  try {
    await bizBrandApi.remove(row.id)
    ElMessage.success(t('bizBrand.deleteOk'))
    deleteDialogVisible.value = false
    if (rows.value.length <= 1 && page.value > 1) page.value -= 1
    await loadList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bizBrand.deleteFailed')))
  } finally {
    deleting.value = false
  }
}

function onDialogClosed() {
  editingId.value = null
  dialogMode.value = 'edit'
  dialogFormRef.value?.clearValidate()
}

function buildPayload() {
  return {
    brandEName: dialogForm.brandEName.trim() || null,
    brandCName: dialogForm.brandCName.trim() || null,
    standardBrand: dialogForm.standardBrand.trim() || null,
    alias: dialogForm.alias.trim() || null,
    countryCode: dialogForm.countryCode.trim() || null,
    country: dialogForm.country.trim() || null,
    remark: dialogForm.remark.trim() || null
  }
}

async function saveDialog() {
  if (dialogMode.value === 'add') {
    const form = dialogFormRef.value
    if (!form) return
    try {
      await form.validate()
    } catch {
      return
    }
  }

  saving.value = true
  try {
    const payload = buildPayload()
    if (dialogMode.value === 'add') {
      await bizBrandApi.create(payload)
      ElMessage.success(t('bizBrand.createOk'))
    } else {
      const id = editingId.value
      if (!id) return
      await bizBrandApi.update(id, payload)
      ElMessage.success(t('bizBrand.saveOk'))
    }
    dialogVisible.value = false
    await loadList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bizBrand.saveFailed')))
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void loadList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.brand-list-page {
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

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

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
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
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
  gap: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  flex: 1 1 auto;
  min-width: 0;
}

.search-right {
  flex-shrink: 0;
}

.search-input {
  width: 220px;
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--filter {
    width: 160px;
  }
}

.search-select {
  width: 140px;

  &--filter {
    width: 140px;
  }
}

.search-date-range {
  width: 280px;

  &--filter {
    width: 280px;
  }
}

.text-secondary {
  color: $text-muted;
}

:deep(th.brand-col-remark-header .cell),
:deep(th.brand-col-country-code-header .cell),
:deep(th.brand-col-audit-status-header .cell) {
  white-space: nowrap;
}

:deep(td.brand-col-audit-status .cell) {
  padding-left: 6px;
  padding-right: 6px;
}

.audit-status-badge {
  display: inline-block;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
  white-space: nowrap;
  line-height: 1.4;

  &--pending {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }

  &--approved {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }

  &--none {
    color: $text-muted;
  }
}

.audit-confirm-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.audit-confirm-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  font-size: 14px;
  line-height: 1.5;
}

.audit-confirm-label {
  flex: 0 0 108px;
  color: $text-muted;
  text-align: right;
}

.audit-confirm-value {
  flex: 1 1 auto;
  color: $text-primary;
  word-break: break-word;
}

.alias-field-wrap {
  width: 100%;
}

.field-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.btn-split-group {
  display: inline-flex;
  align-items: stretch;
  border-radius: $border-radius-md;
  overflow: hidden;
  vertical-align: middle;

  :deep(.el-dropdown) {
    display: inline-flex;
    align-items: stretch;
  }

  .btn-primary:first-child {
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
    border-right: none;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, #00d4ff 0%, #0099cc 100%);
  border: 1px solid rgba(0, 153, 204, 0.55);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s, box-shadow 0.2s, transform 0.2s;

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

  &.btn-primary--caret {
    border-top-left-radius: 0;
    border-bottom-left-radius: 0;
    border-left: 1px solid rgba(255, 255, 255, 0.28);
    min-width: 34px;
    padding-left: 8px;
    padding-right: 8px;
    justify-content: center;
  }
}

.btn-split-group:hover .btn-primary:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
}

.btn-split-group .btn-primary:hover:not(:disabled) {
  transform: none;
  box-shadow: none;
}

// 新建/新增/创建（UI 规范：success 绿）
.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.85), rgba(70, 191, 145, 0.75));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
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
  font-family: 'Noto Sans SC', sans-serif;
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
</style>
