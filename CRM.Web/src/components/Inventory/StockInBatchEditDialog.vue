<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { stockInBatchApi, type StockInBatchRow, type StockInBatchUpdatePayload } from '@/api/stockInBatch'
import { getApiErrorMessage } from '@/utils/apiError'

const props = defineProps<{
  modelValue: boolean
  batchId: string | null
}>()

const emit = defineEmits<{
  'update:modelValue': [boolean]
  saved: []
}>()

const { t } = useI18n()
const saving = ref(false)
const collapseActive = ref(['1', '2', '3', '4'])
const editForm = ref<{
  id: string
  globalBatchNo: string
  batchDimension: string
  batchUnit: string
  unitNo: string
  batchQty: number
  dc: string
  packageOrigin: string
  waferOrigin: string
  lot: string
  serialNumber: string
  firmwareVersion: string
  partCode: string
  remark: string
} | null>(null)

function str(v: string | null | undefined) {
  return v == null ? '' : String(v)
}

async function loadBatch(id: string) {
  const row = await stockInBatchApi.getById(id)
  if (!row) {
    ElMessage.error(t('stockInDetail.batchPanel.messages.batchNotFound'))
    emit('update:modelValue', false)
    return
  }
  applyRow(row)
}

function applyRow(row: StockInBatchRow) {
  editForm.value = {
    id: row.id,
    globalBatchNo: str(row.globalBatchNo),
    batchDimension: str(row.batchDimension),
    batchUnit: str(row.batchUnit),
    unitNo: str(row.unitNo),
    batchQty: Number(row.batchQty) || 0,
    dc: str(row.dc),
    packageOrigin: str(row.packageOrigin),
    waferOrigin: str(row.waferOrigin),
    lot: str(row.lot),
    serialNumber: str(row.serialNumber),
    firmwareVersion: str(row.firmwareVersion),
    partCode: str(row.partCode),
    remark: str(row.remark)
  }
}

watch(
  () => [props.modelValue, props.batchId] as const,
  ([open, id]) => {
    if (open && id) void loadBatch(id)
    if (!open) editForm.value = null
  }
)

async function saveEdit() {
  if (!editForm.value?.id) return
  saving.value = true
  try {
    const body: StockInBatchUpdatePayload = {
      batchDimension: editForm.value.batchDimension.trim() || null,
      batchUnit: editForm.value.batchUnit.trim() || null,
      unitNo: editForm.value.unitNo.trim() || null,
      batchQty: editForm.value.batchQty,
      dc: editForm.value.dc.trim() || null,
      packageOrigin: editForm.value.packageOrigin.trim() || null,
      waferOrigin: editForm.value.waferOrigin.trim() || null,
      lot: editForm.value.lot.trim() || null,
      serialNumber: editForm.value.serialNumber.trim() || null,
      firmwareVersion: editForm.value.firmwareVersion.trim() || null,
      partCode: editForm.value.partCode.trim() || null,
      remark: editForm.value.remark.trim() || null
    }
    await stockInBatchApi.update(editForm.value.id, body)
    ElMessage.success(t('stockInBatchList.messages.saveSuccess'))
    emit('update:modelValue', false)
    emit('saved')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInBatchList.messages.saveFailed')))
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="t('stockInBatchList.edit.title')"
    width="640px"
    destroy-on-close
    class="stock-in-batch-edit-dialog"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-form v-if="editForm" label-width="120px" label-position="right">
      <el-form-item :label="t('stockInBatchList.edit.globalBatchNo')">
        <el-input :model-value="editForm.globalBatchNo || '—'" disabled />
      </el-form-item>
      <el-collapse v-model="collapseActive">
        <el-collapse-item :title="t('stockInBatchList.edit.panel1')" name="1">
          <el-form-item :label="t('stockInBatchList.columns.batchDimension')">
            <el-input v-model="editForm.batchDimension" maxlength="32" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.batchUnit')">
            <el-input v-model="editForm.batchUnit" maxlength="32" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.unitNo')">
            <el-input v-model="editForm.unitNo" maxlength="128" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.batchQty')">
            <el-input-number v-model="editForm.batchQty" :min="0" :controls="true" class="w-full-num" />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.dc')">
            <el-input v-model="editForm.dc" maxlength="64" show-word-limit />
          </el-form-item>
        </el-collapse-item>
        <el-collapse-item :title="t('stockInBatchList.edit.panel2')" name="2">
          <el-form-item :label="t('stockInBatchList.columns.packageOrigin')">
            <el-input v-model="editForm.packageOrigin" maxlength="200" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.waferOrigin')">
            <el-input v-model="editForm.waferOrigin" maxlength="200" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.lot')">
            <el-input v-model="editForm.lot" maxlength="128" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.serialNumber')">
            <el-input v-model="editForm.serialNumber" maxlength="200" show-word-limit />
          </el-form-item>
        </el-collapse-item>
        <el-collapse-item :title="t('stockInBatchList.edit.panel3')" name="3">
          <el-form-item :label="t('stockInBatchList.columns.firmwareVersion')">
            <el-input v-model="editForm.firmwareVersion" maxlength="128" show-word-limit />
          </el-form-item>
          <el-form-item :label="t('stockInBatchList.columns.partCode')">
            <el-input v-model="editForm.partCode" maxlength="128" show-word-limit />
          </el-form-item>
        </el-collapse-item>
        <el-collapse-item :title="t('stockInBatchList.edit.panel4')" name="4">
          <el-form-item :label="t('stockInBatchList.columns.remark')">
            <el-input v-model="editForm.remark" type="textarea" :rows="4" maxlength="1000" show-word-limit />
          </el-form-item>
        </el-collapse-item>
      </el-collapse>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">{{ t('stockInBatchList.edit.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="saveEdit">{{ t('stockInBatchList.edit.save') }}</el-button>
    </template>
  </el-dialog>
</template>

<style scoped lang="scss">
.w-full-num {
  width: 100%;
}
</style>
