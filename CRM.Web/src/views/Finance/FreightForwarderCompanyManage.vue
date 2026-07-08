<template>
  <div class="finance-page ff-company-list-page">
    <div class="page-header-row">
      <h1 class="finance-list-page-title">{{ t('freightForwarderCompany.pageTitle') }}</h1>
      <el-button @click="goBack">{{ t('financeFfPayableDetail.back') }}</el-button>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="filters.keyword"
          :placeholder="t('freightForwarderCompany.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-select
          v-model="filters.status"
          :placeholder="t('freightForwarderCompany.filters.status')"
          clearable
          class="filter-select"
          style="width: 120px"
          @change="handleSearch"
        >
          <el-option :label="t('freightForwarderCompany.enabled')" :value="1" />
          <el-option :label="t('freightForwarderCompany.disabled')" :value="0" />
        </el-select>
        <el-button type="primary" @click="handleSearch">{{ t('freightForwarderCompany.filters.search') }}</el-button>
        <el-button @click="resetFilters">{{ t('freightForwarderCompany.filters.reset') }}</el-button>
      </div>
      <div v-if="canWriteFinanceReceipt" class="search-right">
        <el-button type="primary" @click="openCreate">{{ t('freightForwarderCompany.create') }}</el-button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-ff-company-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedRows"
      v-loading="loading"
      row-class-name="table-row-pointer"
      @row-dblclick="onRowDblclick"
    >
      <template #col-companyCode="{ row }">
        <span class="code-text">{{ row.companyCode }}</span>
      </template>
      <template #col-cname="{ row }">
        <span>{{ row.cname }}</span>
      </template>
      <template #col-ename="{ row }">
        <span>{{ row.ename || '—' }}</span>
      </template>
      <template #col-remark="{ row }">
        <span>{{ row.remark || '—' }}</span>
      </template>
      <template #col-status="{ row }">
        <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small" effect="plain">
          {{ row.status === 1 ? t('freightForwarderCompany.enabled') : t('freightForwarderCompany.disabled') }}
        </el-tag>
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
            <el-button link type="primary" size="small" @click.stop="openBanks(row)">
              {{ t('freightForwarderCompany.manageBanks') }}
            </el-button>
            <el-button v-if="canWriteFinanceReceipt" link type="primary" size="small" @click.stop="openEdit(row)">
              {{ t('common.edit') }}
            </el-button>
            <el-button
              v-if="canWriteFinanceReceipt"
              link
              :type="row.status === 1 ? 'danger' : 'success'"
              size="small"
              :loading="statusLoadingId === row.id"
              @click.stop="toggleStatus(row)"
            >
              {{ row.status === 1 ? t('freightForwarderCompany.disable') : t('freightForwarderCompany.enable') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openBanks(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('freightForwarderCompany.manageBanks') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceReceipt" @click.stop="openEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('common.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceReceipt" @click.stop="toggleStatus(row)">
                  <span :class="['op-more-item', row.status === 1 ? 'op-more-item--danger' : 'op-more-item--primary']">
                    {{ row.status === 1 ? t('freightForwarderCompany.disable') : t('freightForwarderCompany.enable') }}
                  </span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('freightForwarderCompany.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('freightForwarderCompany.columnSettings')"
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
        :total="filteredTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onPageSizeChange"
        @current-change="clampPage"
      />
    </div>

    <el-dialog
      v-model="formVisible"
      :title="editingId ? t('freightForwarderCompany.edit') : t('freightForwarderCompany.create')"
      width="520px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-form-item v-if="editingId" :label="t('freightForwarderCompany.colCode')">
          <el-input :model-value="editingCompanyCode" disabled />
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.colCname')" required>
          <el-input v-model="form.cname" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.colEname')">
          <el-input v-model="form.ename" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.colRemark')">
          <el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer-row">
          <el-button
            v-if="editingId && canWriteFinanceReceipt"
            type="danger"
            :loading="deleting"
            @click="deleteCompany"
          >{{ t('freightForwarderCompany.delete') }}</el-button>
          <div class="dialog-footer-actions">
            <el-button @click="formVisible = false">{{ t('common.cancel') }}</el-button>
            <el-button type="primary" :loading="saving" @click="saveCompany">{{ t('common.confirm') }}</el-button>
          </div>
        </div>
      </template>
    </el-dialog>

    <el-drawer v-model="bankDrawerVisible" :title="t('freightForwarderCompany.manageBanks')" size="min(92vw, 720px)" destroy-on-close>
      <div v-if="activeCompany" class="bank-drawer-head">
        <span>{{ activeCompany.cname }}</span>
        <el-button v-if="canWriteFinanceReceipt" type="primary" size="small" @click="openBankForm()">{{ t('freightForwarderCompany.addBank') }}</el-button>
      </div>
      <el-table :data="banks" size="small" border>
        <el-table-column prop="bankName" :label="t('freightForwarderCompany.bankName')" min-width="140" />
        <el-table-column prop="accountName" :label="t('freightForwarderCompany.accountName')" min-width="120" />
        <el-table-column prop="accountNo" :label="t('freightForwarderCompany.accountNo')" min-width="140" />
        <el-table-column :label="t('freightForwarderCompany.default')" width="70">
          <template #default="{ row }">{{ row.isDefault ? '✓' : '' }}</template>
        </el-table-column>
        <el-table-column :label="t('freightForwarderCompany.colActions')" width="120">
          <template #default="{ row }">
            <el-button v-if="canWriteFinanceReceipt" link type="primary" @click="openBankForm(row)">{{ t('common.edit') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-drawer>

    <el-dialog v-model="bankFormVisible" :title="bankEditingId ? t('freightForwarderCompany.editBank') : t('freightForwarderCompany.addBank')" width="520px" destroy-on-close>
      <el-form :model="bankForm" label-width="100px">
        <el-form-item :label="t('freightForwarderCompany.bankName')" required>
          <el-input v-model="bankForm.bankName" />
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.accountName')">
          <el-input v-model="bankForm.accountName" />
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.accountNo')">
          <el-input v-model="bankForm.accountNo" />
        </el-form-item>
        <el-form-item :label="t('financeFfPayableList.colCurrency')">
          <el-select v-model="bankForm.currency" style="width:100%">
            <el-option label="RMB" :value="1" />
            <el-option label="USD" :value="2" />
            <el-option label="EUR" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('freightForwarderCompany.default')">
          <el-checkbox v-model="bankForm.isDefault" />
        </el-form-item>
        <el-form-item v-if="bankEditingId" :label="t('freightForwarderCompany.bankDisabled')">
          <el-checkbox v-model="bankForm.isDisabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="bankFormVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="bankSaving" @click="saveBank">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  createFfCompanyBank,
  createFreightForwarderCompany,
  deleteFreightForwarderCompany,
  fetchFfCompanyBanks,
  fetchFreightForwarderCompaniesAdmin,
  patchFreightForwarderCompanyStatus,
  updateFfCompanyBank,
  updateFreightForwarderCompany,
  type FreightForwarderCompany,
  type FreightForwarderCompanyBank
} from '@/api/freightForwarderCompany'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'

const { t } = useI18n()
const router = useRouter()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const loading = ref(false)
const allRows = ref<FreightForwarderCompany[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const filters = reactive({
  keyword: '',
  status: undefined as number | undefined
})
const query = reactive({ page: 1, pageSize: 20 })

const formVisible = ref(false)
const saving = ref(false)
const deleting = ref(false)
const editingId = ref('')
const editingCompanyCode = ref('')
const form = reactive({ cname: '', ename: '', remark: '' })

const statusLoadingId = ref<string | null>(null)

const bankDrawerVisible = ref(false)
const activeCompany = ref<FreightForwarderCompany | null>(null)
const banks = ref<FreightForwarderCompanyBank[]>([])
const bankFormVisible = ref(false)
const bankSaving = ref(false)
const bankEditingId = ref('')
const bankForm = reactive({
  bankName: '',
  accountName: '',
  accountNo: '',
  currency: 1,
  isDefault: false,
  isDisabled: false
})

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 228
const OP_COL_EXPANDED_MIN_WIDTH = 210
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'companyCode', label: t('freightForwarderCompany.colCode'), prop: 'companyCode', width: 130, minWidth: 120 },
  { key: 'cname', label: t('freightForwarderCompany.colCname'), prop: 'cname', minWidth: 160, showOverflowTooltip: true },
  { key: 'ename', label: t('freightForwarderCompany.colEname'), prop: 'ename', minWidth: 160, showOverflowTooltip: true },
  { key: 'remark', label: t('freightForwarderCompany.colRemark'), prop: 'remark', minWidth: 180, showOverflowTooltip: true },
  { key: 'status', label: t('freightForwarderCompany.colStatus'), prop: 'status', width: 96, align: 'center' },
  {
    key: 'actions',
    label: t('freightForwarderCompany.colActions'),
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

const filteredRows = computed(() => {
  const kw = filters.keyword.trim().toLowerCase()
  return allRows.value.filter((row) => {
    if (filters.status !== undefined && row.status !== filters.status) return false
    if (!kw) return true
    const haystack = [row.companyCode, row.cname, row.ename, row.remark]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    return haystack.includes(kw)
  })
})

const filteredTotal = computed(() => filteredRows.value.length)

const pagedRows = computed(() => {
  const start = (query.page - 1) * query.pageSize
  return filteredRows.value.slice(start, start + query.pageSize)
})

function clampPage() {
  const maxPage = Math.max(1, Math.ceil(filteredTotal.value / query.pageSize) || 1)
  if (query.page > maxPage) query.page = maxPage
}

function onPageSizeChange() {
  query.page = 1
  clampPage()
}

function handleSearch() {
  query.page = 1
  clampPage()
}

function resetFilters() {
  filters.keyword = ''
  filters.status = undefined
  handleSearch()
}

async function loadList() {
  loading.value = true
  try {
    allRows.value = await fetchFreightForwarderCompaniesAdmin()
    clampPage()
  } finally {
    loading.value = false
  }
}

function goBack() {
  router.push({ name: 'FinanceFreightForwarderPayableList' })
}

function onRowDblclick(row: FreightForwarderCompany) {
  if (canWriteFinanceReceipt.value) {
    openEdit(row)
  } else {
    openBanks(row)
  }
}

function openCreate() {
  editingId.value = ''
  editingCompanyCode.value = ''
  form.cname = ''
  form.ename = ''
  form.remark = ''
  formVisible.value = true
}

function openEdit(row: FreightForwarderCompany) {
  editingId.value = row.id
  editingCompanyCode.value = row.companyCode
  form.cname = row.cname
  form.ename = row.ename || ''
  form.remark = row.remark || ''
  formVisible.value = true
}

async function saveCompany() {
  if (!form.cname.trim()) {
    ElMessage.warning(t('freightForwarderCompany.cnameRequired'))
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await updateFreightForwarderCompany(editingId.value, {
        cname: form.cname.trim(),
        ename: form.ename.trim() || undefined,
        remark: form.remark.trim() || undefined
      })
    } else {
      await createFreightForwarderCompany({
        cname: form.cname.trim(),
        ename: form.ename.trim() || undefined,
        remark: form.remark.trim() || undefined
      })
    }
    ElMessage.success(t('common.saveSuccess'))
    formVisible.value = false
    await loadList()
  } catch (err: unknown) {
    const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
    ElMessage.error(msg || t('common.saveFailed'))
  } finally {
    saving.value = false
  }
}

async function deleteCompany() {
  if (!editingId.value) return
  try {
    await ElMessageBox.confirm(
      t('freightForwarderCompany.deleteConfirm'),
      t('freightForwarderCompany.delete'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  deleting.value = true
  try {
    await deleteFreightForwarderCompany(editingId.value)
    ElMessage.success(t('freightForwarderCompany.deleteSuccess'))
    formVisible.value = false
    await loadList()
  } catch (err: unknown) {
    const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
    ElMessage.error(msg || t('freightForwarderCompany.deleteFailed'))
  } finally {
    deleting.value = false
  }
}

async function toggleStatus(row: FreightForwarderCompany) {
  statusLoadingId.value = row.id
  try {
    await patchFreightForwarderCompanyStatus(row.id, row.status === 1 ? 0 : 1)
    await loadList()
  } catch (err: unknown) {
    const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
    ElMessage.error(msg || t('common.saveFailed'))
  } finally {
    statusLoadingId.value = null
  }
}

async function openBanks(row: FreightForwarderCompany) {
  activeCompany.value = row
  banks.value = await fetchFfCompanyBanks(row.id)
  bankDrawerVisible.value = true
}

function openBankForm(row?: FreightForwarderCompanyBank) {
  bankEditingId.value = row?.id || ''
  bankForm.bankName = row?.bankName || ''
  bankForm.accountName = row?.accountName || ''
  bankForm.accountNo = row?.accountNo || ''
  bankForm.currency = row?.currency ?? 1
  bankForm.isDefault = row?.isDefault ?? false
  bankForm.isDisabled = row?.isDisabled ?? false
  bankFormVisible.value = true
}

async function saveBank() {
  if (!activeCompany.value || !bankForm.bankName.trim()) return
  bankSaving.value = true
  try {
    if (bankEditingId.value) {
      await updateFfCompanyBank(bankEditingId.value, {
        bankName: bankForm.bankName.trim(),
        accountName: bankForm.accountName.trim() || undefined,
        accountNo: bankForm.accountNo.trim() || undefined,
        currency: bankForm.currency,
        isDefault: bankForm.isDefault,
        isDisabled: bankForm.isDisabled
      })
    } else {
      await createFfCompanyBank(activeCompany.value.id, {
        bankName: bankForm.bankName.trim(),
        accountName: bankForm.accountName.trim() || undefined,
        accountNo: bankForm.accountNo.trim() || undefined,
        currency: bankForm.currency,
        isDefault: bankForm.isDefault
      })
    }
    bankFormVisible.value = false
    banks.value = await fetchFfCompanyBanks(activeCompany.value.id)
  } finally {
    bankSaving.value = false
  }
}

onMounted(loadList)
</script>

<style lang="scss" scoped>
@import './finance-common.scss';

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

.bank-drawer-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.dialog-footer-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.dialog-footer-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
}
</style>
