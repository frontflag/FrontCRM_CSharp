<template>
  <div class="finance-page customs-declaration-list-page">
    <div class="page-header-row">
      <h1 class="finance-list-page-title">{{ t('customsPages.declarations.title') }}</h1>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.declarationType"
          clearable
          :placeholder="t('customsPages.declarations.filterDeclarationType')"
          class="filter-select"
          style="width: 120px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.typeImport')" :value="1" />
          <el-option :label="t('customsPages.declarations.typeExport')" :value="2" />
        </el-select>
        <el-select
          v-model="filters.internalStatus"
          clearable
          :placeholder="t('customsPages.declarations.filterInternal')"
          class="filter-select"
          style="width: 130px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.internalPending')" :value="1" />
          <el-option :label="t('customsPages.declarations.internalProcessing')" :value="2" />
          <el-option :label="t('customsPages.declarations.internalDone')" :value="3" />
          <el-option :label="t('customsPages.declarations.internalVoid')" :value="-1" />
        </el-select>
        <el-select
          v-model="filters.customsClearanceStatus"
          clearable
          :placeholder="t('customsPages.declarations.filterClearance')"
          class="filter-select"
          style="width: 120px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.clearanceNone')" :value="0" />
          <el-option :label="t('customsPages.declarations.clearanceReleased')" :value="10" />
          <el-option :label="t('customsPages.declarations.clearanceCleared')" :value="100" />
        </el-select>
        <el-input
          v-model="filters.declarationCode"
          clearable
          :placeholder="t('customsPages.declarations.filterCode')"
          class="search-input"
          style="width: 150px"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-input
          v-model="filters.stockOutRequestId"
          clearable
          :placeholder="t('customsPages.declarations.filterSor')"
          class="search-input"
          style="width: 170px"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-date-picker
          v-model="filters.declareRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          class="date-picker"
          :start-placeholder="t('customsPages.declarations.filterDeclareDate')"
          @change="handleSearch"
        />
        <el-button type="primary" @click="handleSearch">{{ t('customsPages.declarations.search') }}</el-button>
        <el-button @click="resetFilters">{{ t('customsPages.declarations.reset') }}</el-button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="customs-declaration-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedRows"
      v-loading="loading"
      row-class-name="table-row-pointer"
      @row-dblclick="onDblClick"
    >
      <template #col-internalStatus="{ row }">
        <el-tag :type="internalTagType(row.internalStatus)" size="small" effect="plain">
          {{ internalLabel(row.internalStatus) }}
        </el-tag>
      </template>
      <template #col-customsClearanceStatus="{ row }">
        <el-tag :type="clearanceTagType(row.customsClearanceStatus)" size="small" effect="plain">
          {{ clearanceLabel(row.customsClearanceStatus) }}
        </el-tag>
      </template>
      <template #col-declarationCode="{ row }">
        <span class="code-text">{{ row.declarationCode || '—' }}</span>
      </template>
      <template #col-declareDate="{ row }">
        <span class="text-secondary">{{ row.declareDate ? formatDisplayDate(row.declareDate) : '—' }}</span>
      </template>
      <template #col-customsBrokerName="{ row }">
        <span>{{ row.customsBrokerName || '—' }}</span>
      </template>
      <template #col-totalTaxAmount="{ row }">
        <span class="amount-text dock-quote-tier-line">{{ formatTotalAmountNumber(row.totalTaxAmount) }}</span>
      </template>
      <template #col-remark="{ row }">
        <span>{{ row.remark || '—' }}</span>
      </template>
      <template #col-stockOutRequestCode="{ row }">
        <router-link
          v-if="row.stockOutRequestId"
          :to="{ name: 'StockOutNotifyDetail', params: { id: row.stockOutRequestId } }"
          class="cell-link"
          @click.stop
        >
          {{ row.stockOutRequestCode || row.stockOutRequestId }}
        </router-link>
        <span v-else>—</span>
      </template>
      <template #col-createTime="{ row }">
        <span class="text-secondary">{{ row.createTime ? formatDisplayDateTime(row.createTime) : '—' }}</span>
      </template>
      <template #col-createUserDisplay="{ row }">
        <span>{{ row.createUserDisplay || '—' }}</span>
      </template>
      <template #col-actions-header>
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
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button v-if="canWriteLogisticsData" link type="primary" size="small" @click.stop="openClearance(row)">
              {{ t('customsPages.declarations.setClearance') }}
            </el-button>
            <el-button
              v-if="canWriteLogisticsData && row.customsClearanceStatus === 100 && row.internalStatus !== -1"
              link
              type="primary"
              size="small"
              @click.stop="handleCreateArrival(row)"
            >
              {{ t('customsPages.declarations.createArrivalNotifies') }}
            </el-button>
            <el-button v-if="canWriteLogisticsData" link type="danger" size="small" @click.stop="handleDelete(row)">
              {{ t('customsPages.declarations.delete') }}
            </el-button>
            <el-button v-if="isSysAdmin" link type="danger" size="small" @click.stop="handleForceDelete(row)">
              {{ t('customsPages.declarations.forceDelete') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="openClearance(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('customsPages.declarations.setClearance') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canWriteLogisticsData && row.customsClearanceStatus === 100 && row.internalStatus !== -1"
                  @click.stop="handleCreateArrival(row)"
                >
                  <span class="op-more-item op-more-item--primary">{{ t('customsPages.declarations.createArrivalNotifies') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided @click.stop="handleDelete(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('customsPages.declarations.delete') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="handleForceDelete(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('customsPages.declarations.forceDelete') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('customsPages.declarations.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('customsPages.declarations.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onPageSizeChange"
        @current-change="clampPage"
      />
    </div>

    <el-dialog
      v-model="clearanceVisible"
      :title="t('customsPages.declarations.clearanceDialogTitle')"
      width="400px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-select v-model="clearanceForm.status" style="width: 100%">
        <el-option :label="t('customsPages.declarations.clearanceNone')" :value="0" />
        <el-option :label="t('customsPages.declarations.clearanceReleased')" :value="10" />
        <el-option :label="t('customsPages.declarations.clearanceCleared')" :value="100" />
      </el-select>
      <template #footer>
        <el-button @click="clearanceVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="clearanceSaving" @click="saveClearance">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  createCustomsArrivalNotifies,
  deleteCustomsDeclaration,
  fetchCustomsDeclarations,
  forceDeleteCustomsDeclaration,
  patchCustomsClearanceStatus,
  type CustomsDeclarationListItemDto
} from '@/api/customs'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber } from '@/utils/moneyFormat'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const isSysAdmin = authStore.user?.isSysAdmin === true

const loading = ref(false)
const allRows = ref<CustomsDeclarationListItemDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const filters = reactive<{
  declarationType?: number
  internalStatus?: number
  customsClearanceStatus?: number
  declarationCode: string
  stockOutRequestId: string
  declareRange: string[] | null
}>({
  declarationCode: '',
  stockOutRequestId: '',
  declareRange: null
})

const query = reactive({ page: 1, pageSize: 20 })

const clearanceVisible = ref(false)
const clearanceSaving = ref(false)
const clearanceRow = ref<CustomsDeclarationListItemDto | null>(null)
const clearanceForm = reactive({ status: 0 })

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 380
const OP_COL_EXPANDED_MIN_WIDTH = 340
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'internalStatus', label: t('customsPages.declarations.colInternal'), prop: 'internalStatus', width: 120, align: 'center' },
  { key: 'customsClearanceStatus', label: t('customsPages.declarations.colClearance'), prop: 'customsClearanceStatus', width: 110, align: 'center' },
  { key: 'declarationCode', label: t('customsPages.declarations.colDecCode'), prop: 'declarationCode', width: 140, minWidth: 130 },
  { key: 'declareDate', label: t('customsPages.declarations.colDeclareDate'), prop: 'declareDate', width: 120 },
  { key: 'customsBrokerName', label: t('customsPages.declarations.colBroker'), prop: 'customsBrokerName', minWidth: 140, showOverflowTooltip: true },
  { key: 'totalTaxAmount', label: t('customsPages.declarations.colTotal'), prop: 'totalTaxAmount', width: 120, align: 'right' },
  { key: 'remark', label: t('customsPages.declarations.colRemark'), prop: 'remark', minWidth: 120, showOverflowTooltip: true },
  { key: 'stockOutRequestCode', label: t('customsPages.declarations.colSor'), prop: 'stockOutRequestCode', minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('customsPages.declarations.colCreateTime'), prop: 'createTime', width: 170 },
  { key: 'createUserDisplay', label: t('customsPages.declarations.colCreator'), prop: 'createUserDisplay', width: 110, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('customsPages.declarations.colActions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col',
    resizable: false
  }
])

const listTotal = computed(() => allRows.value.length)

const pagedRows = computed(() => {
  const start = (query.page - 1) * query.pageSize
  return allRows.value.slice(start, start + query.pageSize)
})

function internalLabel(v: number) {
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
}

function internalTagType(v: number) {
  if (v === -1) return 'info'
  if (v === 3) return 'success'
  if (v === 2) return 'warning'
  return 'info'
}

function clearanceLabel(v: number) {
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
}

function clearanceTagType(v: number) {
  if (v === 100) return 'success'
  if (v === 10) return 'warning'
  return 'info'
}

function clampPage() {
  const maxPage = Math.max(1, Math.ceil(listTotal.value / query.pageSize) || 1)
  if (query.page > maxPage) query.page = maxPage
}

function onPageSizeChange() {
  query.page = 1
  clampPage()
}

function handleSearch() {
  query.page = 1
  void load()
}

function resetFilters() {
  filters.declarationType = undefined
  filters.internalStatus = undefined
  filters.customsClearanceStatus = undefined
  filters.declarationCode = ''
  filters.stockOutRequestId = ''
  filters.declareRange = null
  handleSearch()
}

async function load() {
  loading.value = true
  try {
    const params: Record<string, unknown> = { take: 500 }
    if (filters.declarationType != null) params.declarationType = filters.declarationType
    if (filters.internalStatus != null) params.internalStatus = filters.internalStatus
    if (filters.customsClearanceStatus != null) params.customsClearanceStatus = filters.customsClearanceStatus
    if (filters.declarationCode.trim()) params.declarationCode = filters.declarationCode.trim()
    if (filters.stockOutRequestId.trim()) params.stockOutRequestId = filters.stockOutRequestId.trim()
    if (filters.declareRange?.length === 2) {
      params.declareDateFrom = filters.declareRange[0]
      params.declareDateTo = filters.declareRange[1]
    }
    allRows.value = await fetchCustomsDeclarations(params)
    clampPage()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
}

function openClearance(row: CustomsDeclarationListItemDto) {
  clearanceRow.value = row
  clearanceForm.status = row.customsClearanceStatus
  clearanceVisible.value = true
}

async function saveClearance() {
  if (!clearanceRow.value) return
  clearanceSaving.value = true
  try {
    await patchCustomsClearanceStatus(clearanceRow.value.id, clearanceForm.status)
    ElMessage.success(t('customsPages.declarations.clearanceSaved'))
    clearanceVisible.value = false
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    clearanceSaving.value = false
  }
}

async function handleDelete(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(
      t('customsPages.declarations.deleteConfirm', { code: row.declarationCode }),
      t('customsPages.declarations.deleteTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await deleteCustomsDeclaration(row.id)
    ElMessage.success(t('customsPages.declarations.deleteSuccess'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function handleForceDelete(row: CustomsDeclarationListItemDto) {
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt(
      t('customsPages.declarations.forceDeletePrompt'),
      t('customsPages.declarations.forceDeleteTitle'),
      { inputPlaceholder: row.declarationCode }
    )
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== String(row.declarationCode || '').trim()) {
    ElMessage.error(t('customsPages.declarations.forceDeleteMismatch'))
    return
  }
  try {
    await forceDeleteCustomsDeclaration(row.id, entered)
    ElMessage.success(t('customsPages.declarations.forceDeleteSuccess'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

function onDblClick(row: CustomsDeclarationListItemDto) {
  router.push({ name: 'CustomsDeclarationDetail', params: { id: row.id } })
}

async function handleCreateArrival(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(
      t('customsPages.declarations.createArrivalConfirm'),
      t('customsPages.declarations.createArrivalNotifies'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    const result = await createCustomsArrivalNotifies(row.id)
    const codes = result.created?.map((c) => c.noticeCode).filter(Boolean).join('、')
    ElMessage.success(
      codes
        ? t('customsPages.declarations.createArrivalOkWithCodes', { codes })
        : t('customsPages.declarations.createArrivalOk', { count: result.createdCount })
    )
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

onMounted(() => {
  void load()
})
</script>

<style lang="scss" scoped>
@import '../Finance/finance-common.scss';

.page-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.pagination-wrap {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
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

.cell-link {
  color: var(--el-color-primary);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
