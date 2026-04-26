<template>
  <div class="customs-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.declarations.title') }}</h1>
    </div>
    <el-form :inline="true" class="filter-form" @submit.prevent>
      <el-form-item :label="t('customsPages.declarations.filterDeclarationType')">
        <el-select v-model="filters.declarationType" clearable :placeholder="t('customsPages.declarations.selectPlaceholder')" style="width: 140px">
          <el-option :label="t('customsPages.declarations.typeImport')" :value="1" />
          <el-option :label="t('customsPages.declarations.typeExport')" :value="2" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('customsPages.declarations.filterInternal')">
        <el-select v-model="filters.internalStatus" clearable style="width: 140px">
          <el-option :label="t('customsPages.declarations.internalPending')" :value="1" />
          <el-option :label="t('customsPages.declarations.internalProcessing')" :value="2" />
          <el-option :label="t('customsPages.declarations.internalDone')" :value="3" />
          <el-option :label="t('customsPages.declarations.internalVoid')" :value="-1" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('customsPages.declarations.filterClearance')">
        <el-select v-model="filters.customsClearanceStatus" clearable style="width: 140px">
          <el-option :label="t('customsPages.declarations.clearanceNone')" :value="0" />
          <el-option :label="t('customsPages.declarations.clearanceReleased')" :value="10" />
          <el-option :label="t('customsPages.declarations.clearanceCleared')" :value="100" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('customsPages.declarations.filterCode')">
        <el-input v-model="filters.declarationCode" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.declarations.filterSor')">
        <el-input v-model="filters.stockOutRequestId" clearable style="width: 180px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.declarations.filterDeclareDate')">
        <el-date-picker
          v-model="filters.declareRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          style="width: 260px"
        />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="load">{{ t('customsPages.declarations.search') }}</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="list" v-loading="loading" stripe border class="data-table" @row-dblclick="onDblClick">
      <el-table-column :label="t('customsPages.declarations.colInternal')" width="120">
        <template #default="{ row }">{{ internalLabel(row.internalStatus) }}</template>
      </el-table-column>
      <el-table-column :label="t('customsPages.declarations.colClearance')" width="120">
        <template #default="{ row }">{{ clearanceLabel(row.customsClearanceStatus) }}</template>
      </el-table-column>
      <el-table-column prop="declareDate" :label="t('customsPages.declarations.colDeclareDate')" width="120">
        <template #default="{ row }">{{ formatDate(row.declareDate) }}</template>
      </el-table-column>
      <el-table-column prop="customsBrokerName" :label="t('customsPages.declarations.colBroker')" min-width="140" />
      <el-table-column prop="totalTaxAmount" :label="t('customsPages.declarations.colTotal')" width="120" align="right" />
      <el-table-column prop="remark" :label="t('customsPages.declarations.colRemark')" min-width="120" show-overflow-tooltip />
      <el-table-column prop="stockOutRequestId" :label="t('customsPages.declarations.colSor')" min-width="200" show-overflow-tooltip />
      <el-table-column prop="declarationCode" :label="t('customsPages.declarations.colDecCode')" width="140" />
      <el-table-column prop="createTime" :label="t('customsPages.declarations.colCreateTime')" width="170">
        <template #default="{ row }">{{ formatDt(row.createTime) }}</template>
      </el-table-column>
      <el-table-column prop="createUserDisplay" :label="t('customsPages.declarations.colCreator')" width="120" />
      <el-table-column :label="t('customsPages.declarations.colActions')" width="320" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click.stop="openClearance(row)">{{ t('customsPages.declarations.setClearance') }}</el-button>
          <el-button
            link
            type="success"
            :disabled="row.customsClearanceStatus !== 100"
            @click.stop="doComplete(row)"
          >
            {{ t('customsPages.declarations.complete') }}
          </el-button>
          <el-button link type="danger" @click.stop="handleDelete(row)">删除</el-button>
          <el-button v-if="isSysAdmin" link type="danger" @click.stop="handleForceDelete(row)">强制删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="clearanceVisible" :title="t('customsPages.declarations.clearanceDialogTitle')" width="400px" destroy-on-close>
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

    <el-dialog v-model="detailVisible" :title="t('customsPages.declarations.detailTitle')" width="900px" destroy-on-close>
      <div v-loading="detailLoading">
        <el-descriptions v-if="detail" :column="2" border>
          <el-descriptions-item :label="t('customsPages.declarations.colDecCode')">{{ detail.declarationCode }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colSor')">{{ detail.stockOutRequestId }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colInternal')">{{ internalLabel(detail.internalStatus) }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colClearance')">{{ clearanceLabel(detail.customsClearanceStatus) }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colDeclareDate')">{{ formatDate(detail.declareDate) }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colTotal')">{{ detail.totalTaxAmount }}</el-descriptions-item>
          <el-descriptions-item :label="t('customsPages.declarations.colRemark')" :span="2">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>
        <h4 class="sub-title">{{ t('customsPages.declarations.itemsTitle') }}</h4>
        <el-table v-if="detail?.items" :data="(detail.items as any[])" size="small" border max-height="360">
          <el-table-column prop="lineNo" label="#" width="56" />
          <el-table-column prop="purchasePn" :label="t('customsPages.items.colPn')" min-width="120" />
          <el-table-column prop="purchaseBrand" :label="t('customsPages.items.colBrand')" width="100" />
          <el-table-column prop="declareQty" :label="t('customsPages.items.colQty')" width="90" />
          <el-table-column prop="totalValueTax" :label="t('customsPages.items.colTotalTax')" width="110" />
        </el-table>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  completeCustomsDeclaration,
  deleteCustomsDeclaration,
  fetchCustomsDeclarationById,
  fetchCustomsDeclarations,
  forceDeleteCustomsDeclaration,
  patchCustomsClearanceStatus,
  type CustomsDeclarationDetailDto,
  type CustomsDeclarationListItemDto
} from '@/api/customs'
import { useAuthStore } from '@/stores/auth'

const { t } = useI18n()
const authStore = useAuthStore()
const isSysAdmin = authStore.user?.isSysAdmin === true
const loading = ref(false)
const list = ref<CustomsDeclarationListItemDto[]>([])
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

const clearanceVisible = ref(false)
const clearanceSaving = ref(false)
const clearanceRow = ref<CustomsDeclarationListItemDto | null>(null)
const clearanceForm = reactive({ status: 0 })

const detailVisible = ref(false)
const detailLoading = ref(false)
const detail = ref<CustomsDeclarationDetailDto | null>(null)

function internalLabel(v: number) {
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
}

function clearanceLabel(v: number) {
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
}

function formatDate(iso: string) {
  if (!iso) return '—'
  return iso.slice(0, 10)
}

function formatDt(iso: string) {
  if (!iso) return '—'
  return iso.replace('T', ' ').slice(0, 19)
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
    list.value = await fetchCustomsDeclarations(params)
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

async function doComplete(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(t('customsPages.declarations.completeConfirm'), t('customsPages.declarations.complete'), {
      type: 'warning'
    })
  } catch {
    return
  }
  try {
    await completeCustomsDeclaration(row.id)
    ElMessage.success(t('customsPages.declarations.completeOk'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function handleDelete(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(`确认删除报关单 ${row.declarationCode} 吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }
  try {
    await deleteCustomsDeclaration(row.id)
    ElMessage.success('删除成功')
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function handleForceDelete(row: CustomsDeclarationListItemDto) {
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt('请输入报关单号以确认强制删除', '强制删除确认', {
      inputPlaceholder: row.declarationCode
    })
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== String(row.declarationCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await forceDeleteCustomsDeclaration(row.id, entered)
    ElMessage.success('强制删除成功')
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function onDblClick(row: CustomsDeclarationListItemDto) {
  detailVisible.value = true
  detail.value = null
  detailLoading.value = true
  try {
    detail.value = await fetchCustomsDeclarationById(row.id)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
    detailVisible.value = false
  } finally {
    detailLoading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped>
.customs-page {
  padding: 20px 24px;
}
.page-header {
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.filter-form {
  margin-bottom: 12px;
}
.data-table {
  width: 100%;
}
.sub-title {
  margin: 16px 0 8px;
  font-size: 14px;
}
</style>
