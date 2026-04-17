<template>
  <div class="customs-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.brokers.title') }}</h1>
      <el-button type="primary" @click="openCreate">{{ t('customsPages.brokers.create') }}</el-button>
    </div>
    <el-table
      :data="list"
      v-loading="loading"
      stripe
      border
      class="data-table"
      @row-dblclick="onDblClick"
    >
      <el-table-column prop="brokerCode" :label="t('customsPages.brokers.colCode')" min-width="140" />
      <el-table-column prop="cname" :label="t('customsPages.brokers.colCname')" min-width="160" />
      <el-table-column prop="ename" :label="t('customsPages.brokers.colEname')" min-width="160" show-overflow-tooltip />
      <el-table-column prop="type" :label="t('customsPages.brokers.colRegionType')" width="120">
        <template #default="{ row }">{{ regionLabel(row.type) }}</template>
      </el-table-column>
      <el-table-column prop="remark" :label="t('customsPages.brokers.colRemark')" min-width="200" show-overflow-tooltip />
      <el-table-column prop="status" :label="t('customsPages.brokers.colStatus')" width="100">
        <template #default="{ row }">{{ brokerStatusLabel(row.status) }}</template>
      </el-table-column>
      <el-table-column :label="t('customsPages.brokers.colActions')" width="200" fixed="right">
        <template #default="{ row }">
          <el-button
            v-if="row.status === 1"
            link
            type="danger"
            :loading="statusLoadingId === row.id"
            @click.stop="onToggleStatus(row, 0)"
          >
            {{ t('customsPages.brokers.btnDisable') }}
          </el-button>
          <el-button
            v-else
            link
            type="primary"
            :loading="statusLoadingId === row.id"
            @click.stop="onToggleStatus(row, 1)"
          >
            {{ t('customsPages.brokers.btnEnable') }}
          </el-button>
          <el-button
            link
            type="danger"
            :loading="deleteLoadingId === row.id"
            :disabled="statusLoadingId === row.id"
            @click.stop="doDelete(row)"
          >
            {{ t('customsPages.brokers.btnDelete') }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="createVisible" :title="t('customsPages.brokers.create')" width="520px" destroy-on-close @closed="resetCreate">
      <p class="code-hint">{{ t('customsPages.brokers.codeAutoHint') }}</p>
      <el-form :model="createForm" label-width="120px">
        <el-form-item :label="t('customsPages.brokers.formCname')" required>
          <el-input v-model="createForm.cname" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formEname')">
          <el-input v-model="createForm.ename" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formRegionType')" required>
          <el-select v-model="createForm.type" style="width: 100%">
            <el-option :label="t('customsPages.brokers.regionShenzhen')" :value="CustomsBrokerRegionType.Shenzhen" />
            <el-option :label="t('customsPages.brokers.regionHongKong')" :value="CustomsBrokerRegionType.HongKong" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formRemark')">
          <el-input v-model="createForm.remark" type="textarea" rows="3" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="createSaving" @click="submitCreate">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="editVisible" :title="t('customsPages.brokers.editTitle')" width="520px" destroy-on-close @closed="resetEdit">
      <el-form :model="editForm" label-width="120px">
        <el-form-item :label="t('customsPages.brokers.colCode')">
          <el-input :model-value="editForm.brokerCode" disabled />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formCname')" required>
          <el-input v-model="editForm.cname" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formEname')">
          <el-input v-model="editForm.ename" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formRegionType')" required>
          <el-select v-model="editForm.type" style="width: 100%">
            <el-option :label="t('customsPages.brokers.regionShenzhen')" :value="CustomsBrokerRegionType.Shenzhen" />
            <el-option :label="t('customsPages.brokers.regionHongKong')" :value="CustomsBrokerRegionType.HongKong" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.formRemark')">
          <el-input v-model="editForm.remark" type="textarea" rows="3" maxlength="500" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('customsPages.brokers.colStatus')">
          <span>{{ brokerStatusLabel(editForm.status) }}</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="editSaving" @click="submitEdit">{{ t('common.save') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  createCustomsBroker,
  CustomsBrokerRegionType,
  deleteCustomsBroker,
  fetchCustomsBrokersAdmin,
  patchCustomsBrokerStatus,
  updateCustomsBroker,
  type CustomsBrokerDto
} from '@/api/customs'

type CustomsBrokerRegion = (typeof CustomsBrokerRegionType)[keyof typeof CustomsBrokerRegionType]

const { t } = useI18n()
const loading = ref(false)
const statusLoadingId = ref<string | null>(null)
const deleteLoadingId = ref<string | null>(null)
const list = ref<CustomsBrokerDto[]>([])
const createVisible = ref(false)
const createSaving = ref(false)
const createForm = reactive({
  cname: '',
  ename: '',
  type: CustomsBrokerRegionType.Shenzhen as CustomsBrokerRegion,
  remark: ''
})
const editVisible = ref(false)
const editSaving = ref(false)
const editForm = reactive({
  id: '',
  brokerCode: '',
  cname: '',
  ename: '',
  type: CustomsBrokerRegionType.Shenzhen as CustomsBrokerRegion,
  remark: '',
  status: 1 as number
})

function brokerStatusLabel(s: number) {
  return s === 1 ? t('customsPages.brokers.statusActive') : t('customsPages.brokers.statusInactive')
}

async function onToggleStatus(row: CustomsBrokerDto, status: 0 | 1) {
  statusLoadingId.value = row.id
  try {
    await patchCustomsBrokerStatus(row.id, status)
    ElMessage.success(t('customsPages.brokers.statusUpdated'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    statusLoadingId.value = null
  }
}

async function doDelete(row: CustomsBrokerDto) {
  try {
    await ElMessageBox.confirm(t('customsPages.brokers.deleteConfirm'), t('customsPages.brokers.btnDelete'), {
      type: 'warning'
    })
  } catch {
    return
  }
  deleteLoadingId.value = row.id
  try {
    await deleteCustomsBroker(row.id)
    ElMessage.success(t('customsPages.brokers.deleteOk'))
    if (editVisible.value && editForm.id === row.id) editVisible.value = false
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    deleteLoadingId.value = null
  }
}

function regionLabel(type: number) {
  if (type === CustomsBrokerRegionType.HongKong) return t('customsPages.brokers.regionHongKong')
  return t('customsPages.brokers.regionShenzhen')
}

async function load() {
  loading.value = true
  try {
    list.value = await fetchCustomsBrokersAdmin()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
}

function openCreate() {
  resetCreate()
  createVisible.value = true
}

function resetCreate() {
  createForm.cname = ''
  createForm.ename = ''
  createForm.type = CustomsBrokerRegionType.Shenzhen
  createForm.remark = ''
}

async function submitCreate() {
  if (!createForm.cname.trim()) {
    ElMessage.warning(t('customsPages.brokers.validateCnameRequired'))
    return
  }
  createSaving.value = true
  try {
    await createCustomsBroker({
      cname: createForm.cname.trim(),
      ename: createForm.ename.trim() || undefined,
      type: createForm.type,
      remark: createForm.remark.trim() || undefined
    })
    ElMessage.success(t('common.createSuccess'))
    createVisible.value = false
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    createSaving.value = false
  }
}

function resetEdit() {
  editForm.id = ''
  editForm.brokerCode = ''
  editForm.cname = ''
  editForm.ename = ''
  editForm.type = CustomsBrokerRegionType.Shenzhen
  editForm.remark = ''
  editForm.status = 1
}

function onDblClick(row: CustomsBrokerDto) {
  editForm.id = row.id
  editForm.brokerCode = row.brokerCode
  editForm.cname = row.cname ?? ''
  editForm.ename = row.ename ?? ''
  editForm.type =
    row.type === CustomsBrokerRegionType.HongKong ? CustomsBrokerRegionType.HongKong : CustomsBrokerRegionType.Shenzhen
  editForm.remark = row.remark ?? ''
  editForm.status = row.status
  editVisible.value = true
}

async function submitEdit() {
  if (!editForm.cname.trim()) {
    ElMessage.warning(t('customsPages.brokers.validateCnameRequired'))
    return
  }
  editSaving.value = true
  try {
    await updateCustomsBroker(editForm.id, {
      cname: editForm.cname.trim(),
      ename: editForm.ename.trim() || undefined,
      type: editForm.type,
      remark: editForm.remark.trim() || undefined
    })
    ElMessage.success(t('common.saveSuccess'))
    editVisible.value = false
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    editSaving.value = false
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.data-table {
  width: 100%;
}
.code-hint {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
</style>
